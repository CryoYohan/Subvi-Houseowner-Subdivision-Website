import spacy
from flask import Flask, request, jsonify
from spellchecker import SpellChecker
import random
from datetime import datetime

# Load spaCy model
nlp = spacy.load('en_core_web_sm')

# Initialize Flask app
app = Flask(__name__)

# Initialize spell checker
spell = SpellChecker()

# Define intent mapping with multiple response templates
intent_responses = {
    "Greetings": [
        "Hi, how may I assist you today?",
        "Hello! How can I help you?",
        "Greetings! What do you need assistance with?",
        "Good day! How can I assist you?",
        "{} How may I help you today?",
        "{} What do you need assistance with?",
        "{} How can I be of service?"
    ],
    "Goodbye": [
        "You're welcome! Have a great day!",
        "Thank you for reaching out. Have a wonderful day!",
        "Goodbye! Feel free to ask if you need anything else.",
        "Take care! Let me know if you need more assistance.",
        "Thanks for chatting! If you have any concern you can chat with me anytime. I am always here to assist you."
    ],
    "Price": [
        "The price of our houses starts from {}.",
        "Our houses are available starting at {}.",
        "The cost of homes in our subdivision begins at {}."
    ],
    "Location": [
        "Our subdivision is located in {}.",
        "You'll find our community in {}.",
        "We are situated in {}."
    ],
    "Availability": [
        "Yes, we have available houses in {}.",
        "There are homes currently available in {}.",
        "You can find available units in {}."
    ],
    "Payment": [
        "We offer flexible payment plans including {}.",
        "Our financing options include {}.",
        "You can choose from different payment methods such as {}."
    ],
    "Contract": [
        "Please contact our admin to get more details about the contract terms.",
        "For contract information, reach out to our admin team.",
        "You may discuss contract details with our office representatives."
    ],
    "Amenities": [
        "Our subdivision includes amenities such as {}.",
        "You can enjoy features like {} in our community.",
        "We offer various amenities, including {}."
    ],
    "Maintenance": [
        "For maintenance requests, please contact our support team or visit the admin office.",
        "Need maintenance? Reach out to our office for assistance.",
        "Maintenance concerns? Our support staff is here to help."
    ],
    "Security": [
        "We have 24/7 security with {}.",
        "Your safety is our priority, with {} in place.",
        "Our community features {} for enhanced security."
    ],
    "HOA": [
        "The Homeowners Association (HOA) manages community rules, events, and maintenance.",
        "Our HOA takes care of community affairs, maintenance, and resident concerns.",
        "If you have HOA-related inquiries, feel free to contact them."
    ],
    "TitleProcessing": [
        "Title processing typically takes {}. Please visit our office for document submission.",
        "The process of securing a title takes about {}. Reach out for details.",
        "Title applications usually take {}. Let us guide you through the steps."
    ],
    "General": [
        "I am sorry, I can't help you with that. For more information, you can contact our admin.",
        "I'm not sure about that. Please contact our admin for assistance.",
        "I currently do not have information on that. You may refer to our office for further help."
    ]
}

# Define simple intent rules
def get_intent(message):
    message = message.lower()
    
    if any(greet in message for greet in ["hi", "hello", "help", "good morning", "good afternoon", "good evening", "good day"]):
        return "Greetings"
    elif any(farewell in message for farewell in ["thank you", "thanks", "bye", "goodbye", "see you", "take care"]):
        return "Goodbye"
    elif "price" in message or "cost" in message or "how much" in message:
        return "Price"
    elif "location" in message or "where" in message:
        return "Location"
    elif "available" in message or "stock" in message or "houses" in message:
        return "Availability"
    elif "payment" in message or "installment" in message:
        return "Payment"
    elif "contract" in message or "agreement" in message:
        return "Contract"
    elif "amenities" in message or "facilities" in message:
        return "Amenities"
    elif "maintenance" in message or "repair" in message:
        return "Maintenance"
    elif "security" in message or "safety" in message:
        return "Security"
    elif "hoa" in message or "homeowners association" in message:
        return "HOA"
    elif "title" in message or "ownership" in message or "documents" in message:
        return "TitleProcessing"
    else:
        return "General"

# Function to check for spelling mistakes
def check_spelling(message):
    words = message.split()
    misspelled = spell.unknown(words)
    return bool(misspelled)

def get_time_based_greeting():
    current_hour = datetime.now().hour
    
    if 5 <= current_hour < 12:
        return "Good morning!"
    elif 12 <= current_hour < 17:
        return "Good afternoon!"
    elif 17 <= current_hour < 21:
        return "Good evening!"
    else:
        return "Good night."

@app.route("/predict", methods=["POST"])
def predict():
    data = request.get_json()
    user_message = data.get("message", "")
    
    if check_spelling(user_message):
        return jsonify({"response": "I'm not sure about that. Could you clarify?"})
    
    doc = nlp(user_message)
    intent = get_intent(user_message)
    entities = {ent.text: ent.label_ for ent in doc.ents}

    # If the intent is "Greetings", use time-based greeting
    if intent == "Greetings":
        greeting = get_time_based_greeting()
        response = intent_responses["Greetings"][0].format(greeting)
    else:
    
        # Get a random response template
        response_template = random.choice(intent_responses.get(intent, intent_responses["General"]))
    
        # Format response with extracted entities if applicable
        # Need modification for the actual data coming from database
        if entities:
            response = response_template.format(", ".join(entities.keys()))
        else:
            response = response_template.format("our standard options")
    
    return jsonify({"response": response, "intent": intent, "entities": entities})

if __name__ == "__main__":
    app.run(debug=True)
