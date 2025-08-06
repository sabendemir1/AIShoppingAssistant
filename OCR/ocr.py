from fastapi import FastAPI, File, UploadFile
from fastapi.responses import JSONResponse
from PIL import Image
import pytesseract
import io
import re
import openai
from openai import OpenAI

app = FastAPI()

@app.post("/analyze")
async def analyze_image(file: UploadFile = File(...)):
    contents = await file.read()
    image = Image.open(io.BytesIO(contents))

    raw_text = pytesseract.image_to_string(image)

    # Call GPT to extract structured product info
    try:
        product_info_json = extract_product_info_with_gpt(raw_text)
    except Exception as e:
        product_info_json = {"error": str(e)}

    return JSONResponse({
        "extracted_product_info": product_info_json
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

openai.api_key = "YOUR_OPENAI_API_KEY"  # Replace with your real key

def extract_product_info_with_gpt(text):
    prompt = f"""
    You are an AI that extracts product listings from unstructured OCR text scraped from online shops, marketplaces, or classified ad sites. 
    
    Some of this text comes from menus, search bars, filters, or other UI elements. **Ignore all of that**. You should only extract real product listings.
    
    For each product found in the text, return a compact JSON object with the following fields **if available**:
    
    - brand
    - model
    - category (e.g. car, phone, TV)
    - year
    - mileage
    - engine
    - fuel_type
    - transmission
    - color
    - features
    - price
    - currency
    
    You may return multiple products in a list. If no valid products are found, return an empty list.
    
    📌 Only return the JSON. No explanation. No markdown. No extra text.
    
    Here is the raw OCR text:
    {text}
    """

    response = client.chat.completions.create(
        model="gpt-4o",
        messages=[{"role": "user", "content": prompt}],
        temperature=0.2,
    )

    return response.choices[0].message.content.strip()


