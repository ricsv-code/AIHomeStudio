### api_server.py

from fastapi import FastAPI, Request, HTTPException
from fastapi.responses import JSONResponse, StreamingResponse
from services import AIService, STTService, TTSService

import logging, threading, time, os, signal

logging.basicConfig(level=logging.INFO, format='%(levelname)s - %(message)s')

ai_service = AIService()
stt_service = STTService()
tts_service = TTSService()

app = FastAPI()

logging.info("FastAPI server started.");




@app.exception_handler(Exception)
async def global_exception_handler(request: Request, exc: Exception):

    logging.error(f"Unhandled error in api_server: {exc}")
    return JSONResponse(status_code=500, content={"detail": "An unexpected error occurred."})


@app.post("/shutdown")
async def shutdown():
    def shutdown_server():
        time.sleep(1)  
        os.kill(os.getpid(), signal.SIGTERM)    
    threading.Thread(target=shutdown_server).start()
    return {"detail": "FastAPI server is shutting down..."}


### AI-endpoints
@app.post("/ai/generate_stream")
async def generate_text_stream(request: Request):
    try:
        return await ai_service.generate_text_stream(request)
    except Exception as e:

        logging.error(f"Error in /ai/generate_stream: {e}")
        raise HTTPException(status_code=500, detail="Error generating text.")


@app.get("/ai/models")
async def list_ai_models():
    try:
        return await ai_service.get_available_models()
    except Exception as e:
        logging.error(f"Error in /ai/models: {e}")
        raise HTTPException(status_code=500, detail="Error listing AI models.")


@app.post("/ai/load")
async def load_model(request: Request):
    try:
        return await ai_service.load_model(request)
    except Exception as e:
        logging.error(f"Error in /ai/load: {e}")
        raise HTTPException(status_code=500, detail="Failed to load model.")


@app.get("/ai/status")
async def model_status():
    try:
        return await ai_service.model_status()
    except Exception as e:
        logging.error(f"Error in /ai/status: {e}")
        raise HTTPException(status_code=500, detail="Error checking model status.")


### STT-endpoints
@app.get("/stt/models")
async def list_stt_models():
    try:
        return await stt_service.get_available_models()
    except Exception as e:
        logging.error(f"Error in /stt/models: {e}")
        raise HTTPException(status_code=500, detail="Error listing STT models.")


@app.post("/stt/load")
async def load_stt_model(request: Request):
    try:
        return await stt_service.load_model(request)
    except Exception as e:
        logging.error(f"Error in /stt/load: {e}")
        raise HTTPException(status_code=500, detail="Error loading STT model.")


@app.post("/stt/stream")
async def stream_stt(request: Request):
    try:
        return await stt_service.stream_transcription(request)
    except Exception as e:
        logging.error(f"Error in /stt/stream: {e}")
        raise HTTPException(status_code=500, detail="Error streaming STT.")

# Not currently in use

@app.post("/stt/transcribe")
async def transcribe(request: Request):
    try:
        return await stt_service.transcribe(request)
    except Exception as e:
        logging.error(f"Error in /stt/transcribe: {e}")
        raise HTTPException(status_code=500, detail="Error transcribing audio.")



#### TTS-endpoints
@app.get("/tts/models")
async def list_tts_models():
    try:
        return await tts_service.get_available_models()
    except Exception as e:
        logging.error(f"Error in /tts/models: {e}")
        raise HTTPException(status_code=500, detail="Error listing TTS models.")


@app.post("/tts/load")
async def load_tts_model(request: Request):
    try:
        return await tts_service.load_model(request)
    except Exception as e:
        logging.error(f"Error in /tts/load: {e}")
        raise HTTPException(status_code=500, detail="Error loading TTS model.")


@app.post("/tts/speak")
async def speak(request: Request):
    try:
        return await tts_service.speak(request)
    except Exception as e:
        logging.error(f"Error in /tts/speak: {e}")
        raise HTTPException(status_code=500, detail="Error generating speech.")


# API healthcheck
@app.get("/health")
async def health_check():
    return await ai_service.health_check();