import openai

openai.api_key = "YOUR_API_KEY"

def extract_product_info(text):
    prompt = f"""
    Extract product information from the following text. Please ignore the link and other infos as this is a OCR from a web screenshot. There may be multiple products. Return JSON only.
    Text:
    {text}
    """

    response = openai.ChatCompletion.create(
        model="gpt-4",
        messages=[{"role": "user", "content": prompt}],
        temperature=0.2,
    )

    return response['choices'][0]['message']['content']
