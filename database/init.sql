-- ============================================================
-- Coffee Commodity Analytics Platform — Database Init Script
-- PostgreSQL 16
-- ============================================================

-- ─── Extensions ──────────────────────────────────────────
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm"; -- for text search

-- ─── Enum Types ──────────────────────────────────────────
CREATE TYPE user_role AS ENUM ('free', 'premium', 'admin');
CREATE TYPE alert_condition AS ENUM ('above', 'below');
CREATE TYPE alert_status AS ENUM ('active', 'triggered', 'disabled');
CREATE TYPE prediction_method AS ENUM ('linear_regression', 'prophet', 'lstm');
CREATE TYPE trend_direction AS ENUM ('bullish', 'bearish', 'neutral');

-- ─── Users ───────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(512) NOT NULL,
    full_name VARCHAR(255),
    role user_role NOT NULL DEFAULT 'free',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    email_verified BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ -- soft delete
);

CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_role ON users(role);

-- ─── Refresh Tokens ──────────────────────────────────────
CREATE TABLE IF NOT EXISTS refresh_tokens (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token VARCHAR(512) UNIQUE NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    is_revoked BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    revoked_at TIMESTAMPTZ,
    replaced_by_token VARCHAR(512) -- token rotation
);

CREATE INDEX idx_refresh_tokens_user_id ON refresh_tokens(user_id);
CREATE INDEX idx_refresh_tokens_token ON refresh_tokens(token);

-- ─── Commodity Prices (realtime cache snapshots) ─────────
CREATE TABLE IF NOT EXISTS commodity_prices (
    id BIGSERIAL PRIMARY KEY,
    symbol VARCHAR(20) NOT NULL,
    open DECIMAL(12,4),
    high DECIMAL(12,4),
    low DECIMAL(12,4),
    close DECIMAL(12,4) NOT NULL,
    volume BIGINT,
    percent_change DECIMAL(8,4),
    timestamp TIMESTAMPTZ NOT NULL,
    interval VARCHAR(10) NOT NULL DEFAULT '1day',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_commodity_prices_symbol ON commodity_prices(symbol);
CREATE INDEX idx_commodity_prices_timestamp ON commodity_prices(timestamp DESC);
CREATE INDEX idx_commodity_prices_symbol_interval ON commodity_prices(symbol, interval, timestamp DESC);

-- ─── Historical Prices (for ML training & analytics) ─────
CREATE TABLE IF NOT EXISTS historical_prices (
    id BIGSERIAL PRIMARY KEY,
    symbol VARCHAR(20) NOT NULL,
    date DATE NOT NULL,
    open DECIMAL(12,4) NOT NULL,
    high DECIMAL(12,4) NOT NULL,
    low DECIMAL(12,4) NOT NULL,
    close DECIMAL(12,4) NOT NULL,
    volume BIGINT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(symbol, date)
);

CREATE INDEX idx_historical_prices_symbol_date ON historical_prices(symbol, date DESC);

-- ─── Predictions ─────────────────────────────────────────
CREATE TABLE IF NOT EXISTS predictions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    symbol VARCHAR(20) NOT NULL,
    method prediction_method NOT NULL,
    predicted_price DECIMAL(12,4) NOT NULL,
    confidence DECIMAL(5,4) NOT NULL CHECK (confidence >= 0 AND confidence <= 1),
    trend trend_direction NOT NULL,
    forecast_data JSONB, -- array of { date, price } for forecast line
    horizon_days INT NOT NULL DEFAULT 7,
    target_date TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_predictions_symbol ON predictions(symbol);
CREATE INDEX idx_predictions_created_at ON predictions(created_at DESC);

-- ─── Alerts ──────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS alerts (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    symbol VARCHAR(20) NOT NULL,
    condition alert_condition NOT NULL,
    threshold DECIMAL(12,4) NOT NULL,
    status alert_status NOT NULL DEFAULT 'active',
    note TEXT,
    triggered_at TIMESTAMPTZ,
    triggered_price DECIMAL(12,4),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_alerts_user_id ON alerts(user_id);
CREATE INDEX idx_alerts_symbol_status ON alerts(symbol, status);

-- ─── Market News ─────────────────────────────────────────
CREATE TABLE IF NOT EXISTS market_news (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    title VARCHAR(500) NOT NULL,
    summary TEXT,
    source VARCHAR(255),
    source_url TEXT,
    sentiment DECIMAL(4,2), -- -1.0 to 1.0
    published_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_market_news_published_at ON market_news(published_at DESC);

-- ─── Technical Indicators Cache ──────────────────────────
CREATE TABLE IF NOT EXISTS technical_indicators (
    id BIGSERIAL PRIMARY KEY,
    symbol VARCHAR(20) NOT NULL,
    interval VARCHAR(10) NOT NULL,
    indicator_type VARCHAR(50) NOT NULL, -- RSI, MACD, SMA, EMA, BBANDS
    indicator_data JSONB NOT NULL,
    calculated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_technical_indicators_symbol_interval ON technical_indicators(symbol, interval, indicator_type, calculated_at DESC);

-- ─── Trigger: auto-update updated_at ─────────────────────
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_alerts_updated_at BEFORE UPDATE ON alerts
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ─── Seed: Admin User (password: Admin@123) ──────────────
-- BCrypt hash of 'Admin@123' (cost=11)
INSERT INTO users (email, password_hash, full_name, role, email_verified) VALUES
('admin@coffeeanalytics.com',
 '$2a$11$placeholder_hash_change_in_production',
 'System Admin',
 'admin',
 TRUE)
ON CONFLICT (email) DO NOTHING;
