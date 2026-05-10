-- ============================================================
-- Coffee Commodity Analytics Platform — Subscription Tables
-- Migration: 001_add_subscription_tables
-- ============================================================

-- ─── Subscription Plans ─────────────────────────────────────
CREATE TABLE IF NOT EXISTS subscription_plans (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(50) NOT NULL UNIQUE,
    display_name VARCHAR(100) NOT NULL,
    description TEXT,
    price_monthly DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    price_yearly DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    features JSONB, -- array of feature strings
    api_rate_limit_per_minute INT NOT NULL DEFAULT 30,
    api_rate_limit_per_day INT NOT NULL DEFAULT 1000,
    max_alerts INT NOT NULL DEFAULT 5,
    max_predictions_per_day INT NOT NULL DEFAULT 10,
    historical_data_days INT NOT NULL DEFAULT 30,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    sort_order INT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_subscription_plans_name ON subscription_plans(name);

-- ─── User Subscriptions ─────────────────────────────────────
CREATE TABLE IF NOT EXISTS user_subscriptions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    plan_id UUID NOT NULL REFERENCES subscription_plans(id),
    status VARCHAR(20) NOT NULL DEFAULT 'active', -- active, cancelled, expired, trialing
    trial_ends_at TIMESTAMPTZ,
    current_period_start TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    current_period_end TIMESTAMPTZ NOT NULL DEFAULT NOW() + INTERVAL '1 month',
    cancel_at_period_end BOOLEAN NOT NULL DEFAULT FALSE,
    cancelled_at TIMESTAMPTZ,
    payment_provider VARCHAR(50), -- stripe, paypal, etc.
    provider_subscription_id VARCHAR(255),
    metadata JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(user_id, status) -- Only one active subscription per user
);

CREATE INDEX idx_user_subscriptions_user_id ON user_subscriptions(user_id);
CREATE INDEX idx_user_subscriptions_status ON user_subscriptions(status);
CREATE INDEX idx_user_subscriptions_plan_id ON user_subscriptions(plan_id);

-- ─── Usage Tracking ─────────────────────────────────────────
CREATE TABLE IF NOT EXISTS usage_tracking (
    id BIGSERIAL PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    metric_type VARCHAR(50) NOT NULL, -- api_call, prediction, alert, chart_view
    count INT NOT NULL DEFAULT 1,
    period_start DATE NOT NULL,
    period_end DATE NOT NULL,
    metadata JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(user_id, metric_type, period_start)
);

CREATE INDEX idx_usage_tracking_user_id ON usage_tracking(user_id);
CREATE INDEX idx_usage_tracking_period ON usage_tracking(period_start, period_end);
CREATE INDEX idx_usage_tracking_metric_type ON usage_tracking(metric_type);

-- ─── Feature Flags ───────────────────────────────────────────
CREATE TABLE IF NOT EXISTS feature_flags (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    key VARCHAR(100) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    is_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    allowed_plans TEXT[], -- array of plan names that can access this feature
    metadata JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_feature_flags_key ON feature_flags(key);

-- ─── Triggers ───────────────────────────────────────────────
CREATE TRIGGER update_subscription_plans_updated_at BEFORE UPDATE ON subscription_plans
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_user_subscriptions_updated_at BEFORE UPDATE ON user_subscriptions
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_usage_tracking_updated_at BEFORE UPDATE ON usage_tracking
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_feature_flags_updated_at BEFORE UPDATE ON feature_flags
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ─── Seed Subscription Plans ────────────────────────────────
INSERT INTO subscription_plans (name, display_name, description, price_monthly, price_yearly, features, api_rate_limit_per_minute, api_rate_limit_per_day, max_alerts, max_predictions_per_day, historical_data_days, sort_order) VALUES
(
    'free',
    'Free Plan',
    'Basic access to coffee market data',
    0.00,
    0.00,
    '["Real-time price", "Basic chart", "1 indicator"]'::jsonb,
    10,
    100,
    3,
    5,
    7,
    1
),
(
    'premium',
    'Premium Plan',
    'Full access with advanced analytics',
    19.99,
    199.99,
    '["Real-time price", "Advanced charts", "All indicators", "Predictions", "Unlimited alerts", "Historical data", "API access"]'::jsonb,
    60,
    10000,
    100,
    100,
    365,
    2
)
ON CONFLICT (name) DO NOTHING;

-- ─── Seed Feature Flags ─────────────────────────────────────
INSERT INTO feature_flags (key, name, description, is_enabled, allowed_plans) VALUES
('ml_predictions', 'ML Predictions', 'Access to machine learning price predictions', true, ARRAY['premium']),
('advanced_indicators', 'Advanced Indicators', 'Access to advanced technical indicators', true, ARRAY['premium']),
('api_access', 'API Access', 'REST API access for programmatic usage', false, ARRAY['premium']),
('realtime_websocket', 'Real-time WebSocket', 'Real-time data via WebSocket connection', true, ARRAY['premium'])
ON CONFLICT (key) DO NOTHING;
