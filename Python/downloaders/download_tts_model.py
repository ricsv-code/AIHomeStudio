import os
import requests

BASE_URL = "https://coqui.gateway.scarf.sh/models/tts_models--sv--cv--vits/"
FILES = [
    "config.json",
    "model.pth",
    "vocab.json",
    "speakers.json"
]

target_dir = os.path.join("tts_models", "sv-cv-vits")
os.makedirs(target_dir, exist_ok=True)

for file in FILES:
    url = BASE_URL + file
    target_path = os.path.join(target_dir, file)
    
    if os.path.exists(target_path):
        print(f"✔ Already downloaded: {file}")
        continue

    print(f"⬇ Downloading: {file}")
    response = requests.get(url)
    with open(target_path, "wb") as f:
        f.write(response.content)
    print(f"✔ Saved to: {target_path}")
