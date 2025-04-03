from fastapi import FastAPI, Request, HTTPException, UploadFile, File, Form
from fastapi.responses import JSONResponse, StreamingResponse, PlainTextResponse, FileResponse
from pydantic import BaseModel
from typing import Optional
from STTService import STTService
from TTSService import TTSService
import torch, os, sys, json, time
from transformers import AutoTokenizer, AutoModelForCausalLM, BitsAndBytesConfig
from contextlib import asynccontextmanager

model = None
tokenizer = None
device = "cuda" if torch.cuda.is_available() else "cpu"
print(f"Using device: {device.upper()}")

app = FastAPI()

@app.exception_handler(Exception)
async def global_exception_handler(request: Request, exc: Exception):
    print(f"Unhandled error: {exc}")
    return JSONResponse(status_code=500, content={"detail": str(exc)})



### load ai model

class LoadModelRequest(BaseModel):
    path: str  

@app.post("/load_model")
def load_model(request: LoadModelRequest):
    def load_stream():
        global model, tokenizer

        yield "[MODEL] Checking for existing model...\n"
        if model is not None:
            yield "[MODEL] Unloading previous model...\n"
            del model
            del tokenizer
            if torch.cuda.is_available():
                torch.cuda.empty_cache()
            yield "[MODEL] Previous model cleared.\n"

        if not os.path.exists(request.path):
            yield "[ERROR] Model path not found.\n"
            return

        yield f"[MODEL] Loading tokenizer from {request.path}...\n"
        tokenizer = AutoTokenizer.from_pretrained(request.path)

        yield "[MODEL] Loading model...\n"
        if device == "cuda":
            bnb_config = BitsAndBytesConfig(
                load_in_4bit=True,
                bnb_4bit_compute_dtype=torch.float16,
                bnb_4bit_quant_type="nf4",
                bnb_4bit_use_double_quant=False
            )
            model = AutoModelForCausalLM.from_pretrained(
                request.path,
                device_map="auto",
                quantization_config=bnb_config
            )
        else:
            model = AutoModelForCausalLM.from_pretrained(request.path).to(device)

        model.eval()
        yield "[MODEL] Model loaded and ready.\n"

    return StreamingResponse(load_stream(), media_type="text/plain")




### generate stream

class GenerateRequest(BaseModel):
    prompt: str
    system_prompt: Optional[str] = None
    max_new_tokens: int = 100
    temperature: float = 0.2
    top_p: float = 0.6

@app.post("/generate_stream")
async def generate_text_stream(request: GenerateRequest):
    global model, tokenizer
    if model is None or tokenizer is None:
        raise HTTPException(status_code=400, detail="Model not loaded")

    system_prompt = request.system_prompt or "Du är en hjälpsam AI."
    prompt = f"GPT4 Correct System:{system_prompt}<|end_of_turn|>\n{request.prompt}"
    if not prompt.endswith("GPT4 Correct Assistant:"):
        prompt += "GPT4 Correct Assistant:"

    input_ids = tokenizer.encode(prompt, return_tensors="pt").to(device)
    generated = input_ids.clone()

    def token_stream():
        nonlocal generated
        with torch.no_grad():
            for _ in range(request.max_new_tokens):
                outputs = model(input_ids=generated)
                next_token_logits = outputs.logits[:, -1, :]
                next_token = torch.argmax(next_token_logits, dim=-1).unsqueeze(-1)
                generated = torch.cat((generated, next_token), dim=1)
                yield tokenizer.decode(next_token.squeeze(), skip_special_tokens=True)
                if next_token.item() == tokenizer.eos_token_id:
                    break

    return StreamingResponse(token_stream(), media_type="text/plain")


### STT Service API's

stt_service = STTService()

### get available models

@app.get("/stt/models")
def list_stt_models():
    return {"models": stt_service.get_available_models()}

### STT load chosen model
@app.post("/stt/load")
def load_stt_model(lang: str = Form(...)):
    try:
        if stt_service.active_model is not None:
            print("[STT] Unloading previous model...")
            stt_service.active_model = None
        stt_service.set_model(lang)
        return {"status": "ok", "active": lang}
    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e))


### use loaded model (streaming recording)
@app.post("/stt/stream")
async def stream_stt(file: UploadFile = File(...)):
    return StreamingResponse(
        stt_service.stream_transcription(file),
        media_type="text/plain"
    )


### use loaded model (from full file)

@app.post("/stt")
async def transcribe(file: UploadFile = File(...)):
    try:
        if stt_service.active_model is None:
            raise HTTPException(status_code=400, detail="No STT model loaded")

        wav_path = "temp.wav"
        with open(wav_path, "wb") as f:
            f.write(await file.read())

        text = stt_service.transcribe(wav_path)
        return {"text": text}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))




### TTS Service

tts_service = TTSService()

### get tts models

@app.get("/tts/models")
def list_tts_models():
    return {"models": tts_service.get_available_models()}

### TTS load chosen model
@app.post("/tts/load")
def load_tts_model(lang: str = Form(...)):
    try:

        if tts_service.active_model is not None:
            print("[TTS] Unloading previous model...")
            tts_service.active_model = None
        tts_service.load_model(lang)
        return {"status": "ok", "active": lang}
    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e))


### use loaded model

class TTSRequest(BaseModel):
    text: str

@app.post("/tts/speak")
async def speak(req: TTSRequest):
    try:
        output_file = "output.wav"
        tts_service.speak(req.text, output_path=output_file)
        return FileResponse(output_file, media_type="audio/wav", filename=output_file)
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))





### training

### start training endpoint

class TrainRequest(BaseModel):
    dataset_path: str
    model_path: str
    output_path: Optional[str] = "model_finetuned"
    epochs: int = 3

@app.post("/train")
def train(request: TrainRequest):
    try:
        import subprocess

        args = [
            "python",
            "train_model.py",
            request.model_path,
            request.dataset_path,
            request.output_path,
            str(request.epochs)
        ]

        subprocess.Popen(args)
        return {"status": "started", "output_dir": request.output_path}

    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

### training progress endpoint

@app.get("/train/progress")
def training_progress(output_path: str = "model_finetuned"):
    log_path = os.path.join(output_path, "progress.log")
    if not os.path.exists(log_path):
        return PlainTextResponse("No progress yet.", status_code=202)

    with open(log_path, "r", encoding="utf-8") as f:
        content = f.read()
    return PlainTextResponse(content)


### AI-model check

@app.get("/model/status")
def model_status():
    if model is None or tokenizer is None:
        return {"loaded": False, "status": "No model loaded."}
    return {"loaded": True, "status": "Model is ready for inference."}



### API healthcheck

@app.get("/health")
def health_check():
    return {"status": "ok"}
