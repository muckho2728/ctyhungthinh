-- Migration: Add CommodityType and domestic price fields
-- Date: 2026-05-09

-- Add type column for Coffee/Pepper separation
ALTER TABLE commodity_prices 
ADD COLUMN type VARCHAR(20) DEFAULT 'coffee' NOT NULL;

-- Add region column for domestic prices
ALTER TABLE commodity_prices 
ADD COLUMN region VARCHAR(100);

-- Add grade column for quality specification
ALTER TABLE commodity_prices 
ADD COLUMN grade VARCHAR(100);

-- Add currency column for unit specification
ALTER TABLE commodity_prices 
ADD COLUMN currency VARCHAR(20) DEFAULT 'VND/kg';

-- Create index on type for faster filtering
CREATE INDEX idx_commodity_prices_type ON commodity_prices(type);

-- Update existing records to have type='coffee'
UPDATE commodity_prices SET type = 'coffee' WHERE type IS NULL OR type = '';

-- Add comment
COMMENT ON COLUMN commodity_prices.type IS 'Commodity type: coffee or pepper';
COMMENT ON COLUMN commodity_prices.region IS 'Region for domestic prices (e.g., Đắk Lắk, Chư Sê)';
COMMENT ON COLUMN commodity_prices.grade IS 'Grade/Quality (e.g., Cà phê nhân loại 1, Tiêu đen loại 1)';
COMMENT ON COLUMN commodity_prices.currency IS 'Currency unit (e.g., VND/kg, USD/ton, US cents/lb)';
