FROM python:3.10

# ljudst�d
RUN apt-get update && apt-get install -y ffmpeg libsndfile1 git

# arbetsmapp
WORKDIR /app

# kopiering
COPY . .

# dependencies
RUN pip install --upgrade pip
RUN pip install -r requirements.txt

# exponera port f�r FastAPI
EXPOSE 8000

# start
CMD ["uvicorn", "APIServer:app", "--host", "0.0.0.0", "--port", "8000"]
