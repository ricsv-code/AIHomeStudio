### TTSService

from TTS.api import TTS
import os

class TTSService:
    def __init__(self):
        self.models = self._discover_models()
        self.active_model = None
        self.active_name = None

    def _discover_models(self):
        models = {}
        for name in os.listdir("tts_models"):
            path = os.path.join("tts_models", name)
            if os.path.isdir(path):
                models[name] = path
        return models

    def load_model(self, model_key: str):
        if model_key not in self.models:
            raise ValueError(f"TTS model '{model_key}' not found.")
        self.active_model = TTS(model_path=self.models[model_key], progress_bar=False)
        self.active_name = model_key
        print(f"[TTS] Loaded model: {model_key}")

    def get_available_models(self):
        return list(self.models.keys())

    def speak(self, text: str, output_path: str = "output.wav"):
        if self.active_model is None:
            raise RuntimeError("No TTS model loaded.")
        self.active_model.tts_to_file(text=text, file_path=output_path)
