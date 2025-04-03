from fastapi import UploadFile, File
from vosk import Model, KaldiRecognizer
import wave

model = Model("models/vosk-model-small-sv-rhasspy-0.15")

@app.post("/stt")
async def stt(file: UploadFile = File(...)):
    with open("temp.wav", "wb") as f:
        f.write(await file.read())

    wf = wave.open("temp.wav", "rb")
    rec = KaldiRecognizer(model, wf.getframerate())

    result_text = ""
    while True:
        data = wf.readframes(4000)
        if len(data) == 0:
            break
        if rec.AcceptWaveform(data):
            res = json.loads(rec.Result())
            result_text += res.get("text", "") + " "

    return {"text": result_text.strip()}
