import os
import logging
import traceback
import asyncio
from fastapi import Request, HTTPException, UploadFile
from fastapi.responses import StreamingResponse, JSONResponse
from typing import Dict, List, Optional, Any, AsyncGenerator, Callable

logging.basicConfig(level=logging.INFO, format='%([ levelname ])s - %(message)s')


class ServiceBase:
    def __init__(self, service_name: str, models_dir: str):
        self.service_name = service_name
        self.models_dir = models_dir
        self.available_models: Dict[str, str] = self._discover_models()

    def _discover_models(self) -> Dict[str, str]:
        models: Dict[str, str] = {}
        try:
            if not os.path.isdir(self.models_dir):
                logging.warning(
                    f"[{self.service_name}] Models directory not found: {self.models_dir}"
                )
                return {}
            for name in os.listdir(self.models_dir):
                path = os.path.join(self.models_dir, name)
                if os.path.isdir(path):
                    models[name] = path
            logging.info(
                f"[{self.service_name}] Discovered models: {list(models.keys())}"
            )
            return models
        except Exception as e:
            logging.error(
                f"[{self.service_name}] Error discovering models: {e}\n{traceback.format_exc()}"
            )
            return {}

    async def get_available_models(self) -> Dict[str, List[str]]:
        return {"Models": list(self.available_models.keys())}

    async def load_model(
        self, request: Request, load_function: Callable[..., Any], *load_args: Any
    ) -> StreamingResponse:
        data: Dict[str, Any] = await request.json()
        model_path: Optional[str] = data.get("path")
        if not model_path:
            raise HTTPException(status_code=400, detail="Model path not provided.")

        async def load_stream() -> AsyncGenerator[str, None]:
            try:
                yield f"[{self.service_name}] Checking for existing model...\n"
                await self._unload_model()
                yield f"[{self.service_name}] Previous model cleared.\n"

                if not os.path.exists(model_path):
                    error_message: str = (
                        f"[{self.service_name}] Model path not found: {model_path}"
                    )
                    logging.error(error_message)
                    yield error_message + "\n"
                    return

                yield f"[{self.service_name}] Loading model from {model_path}...\n"
                await load_function(self, model_path, *load_args)
                yield f"[{self.service_name}] Model loaded successfully.\n"

            except Exception as e:
                error_message: str = (
                    f"[ERROR] Failed to load model: {e}\n{traceback.format_exc()}"
                )
                logging.error(error_message)
                yield error_message + "\n"
                raise HTTPException(status_code=500, detail="Failed to load model.")

        return StreamingResponse(load_stream(), media_type="text/plain")

    async def _unload_model(self) -> None:
        pass


import torch
from transformers import AutoModelForCausalLM, AutoTokenizer, BitsAndBytesConfig


class AIService(ServiceBase):
    def __init__(self):
        self.device: str = "cuda" if torch.cuda.is_available() else "cpu"
        logging.info(f"[AI] Using device: {self.device.upper()}")
        super().__init__("AI", "ai_models")        
        self.model: Optional[AutoModelForCausalLM] = None
        self.tokenizer: Optional[AutoTokenizer] = None
        self.generated: Optional[torch.Tensor] = None

    async def _load_ai_model(self, model_path: str):
        if self.device == "cuda":
            bnb_config: BitsAndBytesConfig = BitsAndBytesConfig(
                load_in_4bit=True,
                bnb_4bit_compute_dtype=torch.float16,
                bnb_4bit_quant_type="nf4",
                bnb_4bit_use_double_quant=False,
            )
            self.model = AutoModelForCausalLM.from_pretrained(
                model_path, device_map="auto", quantization_config=bnb_config
            )
        else:
            self.model = (
                AutoModelForCausalLM.from_pretrained(model_path).to(self.device)
            )
        self.tokenizer = AutoTokenizer.from_pretrained(model_path)
        self.model.eval()

    async def load_model(self, request: Request) -> StreamingResponse:
        return await super().load_model(request, AIService._load_ai_model)

    async def generate_text_stream(self, request: Request) -> StreamingResponse:
        data: Dict[str, Any] = await request.json()
        prompt: Optional[str] = data.get("prompt")
        max_new_tokens: int = data.get("max_new_tokens", 100)
        temperature: float = data.get("temperature", 0.2)
        top_p: float = data.get("top_p", 0.6)

        if self.model is None or self.tokenizer is None:
            logging.error("[AI] Model not loaded.")
            raise HTTPException(status_code=400, detail="Model not loaded.")

        async def token_stream() -> AsyncGenerator[str, None]:
            self.generated = self.tokenizer.encode(prompt, return_tensors="pt").to(
                self.device
            ).clone()

            with torch.no_grad():
                for _ in range(max_new_tokens):
                    outputs = self.model(input_ids=self.generated)
                    next_token_logits: torch.Tensor = outputs.logits[:, -1, :]
                    next_token: torch.Tensor = torch.argmax(
                        next_token_logits, dim=-1
                    ).unsqueeze(-1)
                    self.generated = torch.cat((self.generated, next_token), dim=1)
                    yield self.tokenizer.decode(
                        next_token.squeeze(), skip_special_tokens=True
                    )
                    if next_token.item() == self.tokenizer.eos_token_id:
                        break

        return StreamingResponse(token_stream(), media_type="text/plain")

    async def model_status(self) -> dict:
        if self.model is None or self.tokenizer is None:
            logging.warning("[AI] Model status: No model loaded.")
            return {"loaded": False, "status": "No model loaded."}
        logging.info("Model status: Model is ready for inference.")
        return {"loaded": True, "status": "Model is ready for inference."}

    async def health_check(self) -> dict:
        return {"status": "ok"}


# STT Service
from vosk import Model, KaldiRecognizer


class STTService(ServiceBase):
    def __init__(self):
        super().__init__("STT", "stt_models")
        self.active_model: Optional[Model] = None
        self.active_lang: Optional[str] = None

    async def _load_stt_model(self, model_path: str):
        self.active_model = Model(model_path)

    async def load_model(self, request: Request) -> StreamingResponse:
        return await super().load_model(request, STTService._load_stt_model)

    async def transcribe(self, wav_path: str) -> str:
        def _transcribe() -> str:
            if self.active_model is None:
                logging.error("STT model is not loaded.")
                raise RuntimeError("STT model is not loaded.")
            rec = KaldiRecognizer(self.active_model, 16000)
            result_text: str = ""
            with wave.open(wav_path, "rb") as wf:
                while True:
                    data: bytes = wf.readframes(4000)
                    if len(data) == 0:
                        break
                    if rec.AcceptWaveform(data):
                        res: Dict[str, Any] = json.loads(rec.Result())
                        result_text += res.get("text", "") + " "
            logging.info(f"[STT] Transcribed text: {result_text.strip()}")
            return result_text.strip()

        return await asyncio.to_thread(_transcribe)

    async def stream_transcription(self, file: UploadFile) -> AsyncGenerator[str, None]:
        if self.active_model is None:
            error_message: str = "[STTService] No model loaded.\n"
            logging.error(error_message)
            yield error_message
            return

        rec = KaldiRecognizer(self.active_model, 16000)
        try:
            while True:
                chunk: bytes = await asyncio.to_thread(file.file.read, 4000)
                if not chunk:
                    break
                if rec.AcceptWaveform(chunk):
                    result: Dict[str, Any] = json.loads(rec.Result())
                    text: str = result.get("text", "")
                    logging.info(f"[STT] Partial transcription: {text}")
                    yield text + "\n"
                else:
                    partial: Dict[str, Any] = json.loads(rec.PartialResult())
                    partial_text: Optional[str] = partial.get("partial", "")
                    if partial_text:
                        logging.info(f"[STT] Partial result: {partial_text}")
                        yield "[partial] " + partial_text + "\n"
        except Exception as e:
            logging.error(f"[STT] Error streaming transcription: {e}")
            raise HTTPException(status_code=500, detail="Error streaming transcription.")
        finally:
            del rec


# TTS Service
from TTS.api import TTS


class TTSService(ServiceBase):
    def __init__(self):
        super().__init__("TTS", "tts_models")
        self.active_model: Optional[TTS] = None
        self.active_name: Optional[str] = None


    async def load_model(self, request: Request): 
        data: Dict[str, str] = await request.json()
        path: str = data.get("path")
        parts: List[str] = path.split("\\")
        model_key: str = parts[-1]
        identifier = model_key.replace("--", "/")
        logging.error(f"TTS MODEL IDENTIFIER: {identifier}")
        self.active_model = TTS(identifier, progress_bar=False, gpu=False)



    async def speak(self, text: str, output_path: str = "output.wav") -> None:
        if self.active_model is None:
            logging.error("No TTS model loaded.")
            raise RuntimeError("No TTS model loaded.")
        await asyncio.to_thread(self.active_model.tts_to_file, text=text, file_path=output_path)
        logging.info(f"[TTS] Generated speech file: {output_path}")