from flask import Flask, request, jsonify
import joblib
import pandas as pd

app = Flask(__name__)

# Load the model and scaler into memory ONCE when the server starts
print("Loading model and scaler into memory...")
model = joblib.load('model_v1.pkl')
scaler = joblib.load('scaler_v1.pkl')

@app.route('/predict', methods=['POST'])
def predict():
    try:
        # 1. Get JSON data from the C# application
        data = request.get_json()

        # 2. Convert to DataFrame (model expects a 2D structure with exact column names)
        # We expect 30 features: Time, V1-V28, and Amount
        df = pd.DataFrame([data])

        # 3. Apply the exact same scaling to Time and Amount that we did in training
        df[['Time', 'Amount']] = scaler.transform(df[['Time', 'Amount']])

        # 4. Predict fraud and get the confidence score (probability)
        prediction = model.predict(df)[0]
        probabilities = model.predict_proba(df)[0]
        
        # 5. Return the exact JSON structure demanded by the blueprint
        return jsonify({
            'is_fraud': bool(prediction == 1),
            'confidence': float(probabilities[1]) # Probability of class 1 (Fraud)
        })

    except Exception as e:
        return jsonify({'error': str(e)}), 400

if __name__ == '__main__':
    # Run locally on port 5000
    app.run(port=5000, debug=False)