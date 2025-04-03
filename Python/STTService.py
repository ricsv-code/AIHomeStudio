from vosk import Model, KaldiRecognizer
from fastapi import UploadFile
import os
import wave
import json

class STTService:
    def __init__(self):
        self.available_models = self._discover_models()
        self.active_model = None
        self.active_lang = None

    def _discover_models(self):
        models = {}
        for name in os.listdir("stt_models"):
            path = os.path.join("stt_models", name)
            if os.path.isdir(path):
                models[name] = path
        return models

    def set_model(self, lang_code: str):
        if lang_code not in self.available_models:
            raise ValueError(f"Model '{lang_code}' not found.")
        self.active_model = Model(self.available_models[lang_code])
        self.active_lang = lang_code
        print(f"[STT] Loaded model '{lang_code}'")

    def get_available_models(self):
        return list(self.available_models.keys())

    def transcribe(self, wav_path: str) -> str:
        if self.active_model is None:
            raise RuntimeError("STT model is not loaded.")

        rec = KaldiRecognizer(self.active_model, 16000)
        result_text = ""

        with wave.open(wav_path, "rb") as wf:
            while True:
                data = wf.readframes(4000)
                if len(data) == 0:
                    break
                if rec.AcceptWaveform(data):
                    res = json.loads(rec.Result())
                    result_text += res.get("text", "") + " "
        return result_text.strip()

    def stream_transcription(self, file: UploadFile):
        if self.active_model is None:
            yield "[STTService] No model loaded.\n"
            return

        rec = KaldiRecognizer(self.active_model, 16000)
        while True:
            chunk = file.file.read(4000)
            if not chunk:
                break

            if rec.AcceptWaveform(chunk):
                result = json.loads(rec.Result())
                yield result.get("text", "") + "\n"
            else:
                partial = json.loads(rec.PartialResult())
                if partial.get("partial"):
                    yield "[partial] " + partial["partial"] + "\n"
