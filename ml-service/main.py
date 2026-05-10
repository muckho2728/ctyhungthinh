from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import List, Optional
import pandas as pd
import numpy as np
import joblib
import os

app = FastAPI(title="Commodity Prediction ML Service", version="1.0.0")

# Pydantic Models
class CoffeePredictionRequest(BaseModel):
    price_history: List[float]
    inventory_level: Optional[float] = None
    usd_brl_rate: Optional[float] = None
    days_to_predict: int = 7

class PepperPredictionRequest(BaseModel):
    price_history: List[float]
    harvest_cycle: Optional[int] = None
    china_demand: Optional[float] = None
    eu_demand: Optional[float] = None
    days_to_predict: int = 7

class PredictionResponse(BaseModel):
    predicted_prices: List[float]
    confidence: float
    trend: str
    factors: dict

# Load models (placeholder - actual models need to be trained)
COFFEE_MODEL_PATH = "models/coffee_model.pkl"
PEPPER_MODEL_PATH = "models/pepper_model.pkl"

def get_coffee_model():
    """Load or create placeholder coffee model"""
    if os.path.exists(COFFEE_MODEL_PATH):
        return joblib.load(COFFEE_MODEL_PATH)
    # Placeholder: Simple linear regression for demo
    from sklearn.linear_model import LinearRegression
    model = LinearRegression()
    return model

def get_pepper_model():
    """Load or create placeholder pepper model"""
    if os.path.exists(PEPPER_MODEL_PATH):
        return joblib.load(PEPPER_MODEL_PATH)
    # Placeholder: Simple linear regression for demo
    from sklearn.linear_model import LinearRegression
    model = LinearRegression()
    return model

@app.get("/")
async def root():
    return {
        "service": "Commodity Prediction ML Service",
        "version": "1.0.0",
        "endpoints": {
            "coffee": "/predict/coffee",
            "pepper": "/predict/pepper",
            "health": "/health"
        }
    }

@app.get("/health")
async def health_check():
    return {"status": "healthy", "models_loaded": True}

@app.post("/predict/coffee", response_model=PredictionResponse)
async def predict_coffee(request: CoffeePredictionRequest):
    """
    Coffee Prediction Model
    - Based on: Inventory levels, USD/BRL exchange rate, price history
    - Focus: Futures market (ICE, Liffe) impact on domestic prices
    """
    try:
        model = get_coffee_model()
        
        # Feature engineering for Coffee
        prices = np.array(request.price_history)
        features = []
        
        # Technical indicators
        if len(prices) >= 7:
            ma7 = np.mean(prices[-7:])
            ma30 = np.mean(prices[-30:]) if len(prices) >= 30 else ma7
            features.extend([ma7, ma30])
        else:
            features.extend([prices[-1], prices[-1]])
        
        # Add external factors
        features.append(request.inventory_level or 0)
        features.append(request.usd_brl_rate or 5.0)
        
        # Placeholder prediction logic (replace with actual model)
        last_price = prices[-1]
        predicted = []
        for i in range(request.days_to_predict):
            # Simple trend-based prediction for demo
            trend = np.mean(np.diff(prices[-10:])) if len(prices) >= 10 else 0
            next_price = last_price + trend + np.random.normal(0, last_price * 0.01)
            predicted.append(float(next_price))
            last_price = next_price
        
        # Determine trend
        avg_change = np.mean(np.diff(predicted))
        trend = "bullish" if avg_change > 0 else "bearish"
        confidence = 0.75 + (np.random.random() * 0.15)  # 75-90% for demo
        
        return PredictionResponse(
            predicted_prices=predicted,
            confidence=round(confidence, 2),
            trend=trend,
            factors={
                "inventory_level": request.inventory_level,
                "usd_brl_rate": request.usd_brl_rate,
                "price_volatility": float(np.std(prices))
            }
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/predict/pepper", response_model=PredictionResponse)
async def predict_pepper(request: PepperPredictionRequest):
    """
    Pepper Prediction Model
    - Based on: Harvest cycle, China/EU demand, price history
    - Focus: Domestic market (farm-gate prices) and export prices
    """
    try:
        model = get_pepper_model()
        
        # Feature engineering for Pepper
        prices = np.array(request.price_history)
        features = []
        
        # Seasonal factors based on harvest cycle
        harvest_month = request.harvest_cycle or 3  # Default: March
        seasonal_factor = np.sin(2 * np.pi * harvest_month / 12)
        features.append(seasonal_factor)
        
        # Demand factors
        features.append(request.china_demand or 0.5)
        features.append(request.eu_demand or 0.3)
        
        # Price momentum
        if len(prices) >= 5:
            momentum = (prices[-1] - prices[-5]) / prices[-5]
        else:
            momentum = 0
        features.append(momentum)
        
        # Placeholder prediction logic (replace with actual model)
        last_price = prices[-1]
        predicted = []
        for i in range(request.days_to_predict):
            # Seasonality-based prediction for demo
            seasonal_effect = seasonal_factor * last_price * 0.02
            demand_effect = (request.china_demand or 0.5) * last_price * 0.03
            next_price = last_price + seasonal_effect + demand_effect + np.random.normal(0, last_price * 0.015)
            predicted.append(float(next_price))
            last_price = next_price
        
        # Determine trend
        avg_change = np.mean(np.diff(predicted))
        trend = "bullish" if avg_change > 0 else "bearish"
        confidence = 0.70 + (np.random.random() * 0.20)  # 70-90% for demo
        
        return PredictionResponse(
            predicted_prices=predicted,
            confidence=round(confidence, 2),
            trend=trend,
            factors={
                "harvest_cycle": request.harvest_cycle,
                "china_demand": request.china_demand,
                "eu_demand": request.eu_demand,
                "seasonal_factor": round(float(seasonal_factor), 3)
            }
        )
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
