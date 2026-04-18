import pandas as pd
import time
from sklearn.model_selection import train_test_split
from sklearn.preprocessing import StandardScaler
from sklearn.ensemble import RandomForestClassifier
from sklearn.metrics import classification_report, precision_recall_fscore_support
from imblearn.over_sampling import SMOTE
import joblib

print("Starting Enterprise Fraud Detection Training Pipeline...")
start_time = time.time()

# 1. Load the Dataset
print("Loading dataset (this may take a moment for 284k rows)...")
df = pd.read_csv('creditcard.csv')

# Separate features (X) and labels (y)
X = df.drop('Class', axis=1)
y = df['Class']

# 2. Train/Test Split
# We split BEFORE oversampling. Never oversample your test data, or your evaluation is invalid.
print("Splitting data into train and test sets...")
X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.2, random_state=42, stratify=y)

# 3. Preprocessing: Scaling
# The blueprint specifically requires scaling 'Time' and 'Amount'. 
# The V1-V28 PCA columns are already scaled by definition.
print("Applying StandardScaler to Time and Amount...")
scaler = StandardScaler()

# We only fit the scaler on the training data to prevent data leakage
X_train[['Time', 'Amount']] = scaler.fit_transform(X_train[['Time', 'Amount']])
X_test[['Time', 'Amount']] = scaler.transform(X_test[['Time', 'Amount']])

# 4. Handle Class Imbalance (SMOTE)
# 0.17% fraud means the model will naturally bias toward "Not Fraud". 
# SMOTE synthetically generates new minority class samples to balance the training.
print("Applying SMOTE to balance the training data...")
smote = SMOTE(random_state=42)
X_train_balanced, y_train_balanced = smote.fit_resample(X_train, y_train)

# 5. Model Training
print("Training Random Forest Classifier... (Grab a coffee, this uses all CPU cores and takes a few minutes)")
# n_jobs=-1 tells the model to use all available CPU cores to speed up training
rf_model = RandomForestClassifier(n_estimators=100, random_state=42, n_jobs=-1, class_weight='balanced')
rf_model.fit(X_train_balanced, y_train_balanced)

# 6. Evaluation (Strictly F1, Precision, Recall)
print("Evaluating Model on unseen Test Data...")
y_pred = rf_model.predict(X_test)

# Calculate metrics
precision, recall, f1, _ = precision_recall_fscore_support(y_test, y_pred, average='binary')

print("\n" + "="*50)
print("MODEL EVALUATION METRICS (CLASS 1: FRAUD)")
print("="*50)
print(f"Precision: {precision:.4f} (When it says fraud, how often is it right?)")
print(f"Recall:    {recall:.4f} (Out of all real fraud, how much did it catch?)")
print(f"F1-Score:  {f1:.4f} (The harmonic mean - the true test of a fraud model)")
print("\nDetailed Classification Report:")
print(classification_report(y_test, y_pred))

# 7. Export Model and Scaler
print("Exporting Model and Scaler to disk...")
joblib.dump(rf_model, 'model_v1.pkl')
joblib.dump(scaler, 'scaler_v1.pkl')

execution_time = (time.time() - start_time) / 60
print(f"Pipeline completed successfully in {execution_time:.2f} minutes.")
print("Files generated: model_v1.pkl, scaler_v1.pkl")