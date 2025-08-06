from fastapi import FastAPI, File, UploadFile
from fastapi.responses import JSONResponse
from PIL import Image
import pytesseract
import io
import re

app = FastAPI()

@app.post("/analyze")
async def analyze_image(file: UploadFile = File(...)):
    contents = await file.read()
    image = Image.open(io.BytesIO(contents))

    # OCR
    raw_text = pytesseract.image_to_string(image)

    # NLP: Extract product names, prices
    product_name = extract_product_name(raw_text)
    price = extract_price(raw_text)

    return JSONResponse({
        "raw_text": raw_text,
        "product_name": product_name,
        "price": price
    })

def extract_price(text):
    match = re.search(r'[$€£]\s?\d+[.,]?\d*', text)
    return match.group(0) if match else None

def extract_product_name(text):
    # Very simple heuristic: first capitalized line with a noun-like pattern
    lines = text.split("\n")
    for line in lines:
        if len(line) > 10 and any(char.isupper() for char in line):
            return line.strip()
    return None
