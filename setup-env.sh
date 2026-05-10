#!/bin/bash
# ============================================================
# Coffee Analytics Platform - Environment Setup Script
# Configures .env file with API keys
# ============================================================

echo ""
echo "========================================"
echo "Environment Configuration Setup"
echo "========================================"
echo ""

# Check if .env exists
if [ ! -f .env ]; then
    echo "Creating .env from .env.example..."
    cp .env.example .env
    echo ".env file created"
    echo ""
else
    echo ".env file already exists"
    echo ""
fi

# Generate random JWT secret
echo "Generating secure JWT secret..."
JWT_SECRET=$(openssl rand -base64 32)

# Update .env file with API key and JWT secret
echo "Updating .env file with configuration..."
echo ""

# Use sed to replace values (works on Mac and Linux)
if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS sed
    sed -i '' "s/TWELVEDATA_API_KEY=your_twelvedata_api_key_here/TWELVEDATA_API_KEY=c5795c825e5447c8a05a7cfe6c5da761/" .env
    sed -i '' "s/JWT_SECRET=your_super_secret_jwt_key_at_least_32_characters_long/JWT_SECRET=$JWT_SECRET/" .env
else
    # Linux sed
    sed -i "s/TWELVEDATA_API_KEY=your_twelvedata_api_key_here/TWELVEDATA_API_KEY=c5795c825e5447c8a05a7cfe6c5da761/" .env
    sed -i "s/JWT_SECRET=your_super_secret_jwt_key_at_least_32_characters_long/JWT_SECRET=$JWT_SECRET/" .env
fi

if [ $? -eq 0 ]; then
    echo "[SUCCESS] .env file updated with:"
    echo "  - TwelveData API Key: c5795c825e5447c8a05a7cfe6c5da761"
    echo "  - JWT Secret: [GENERATED SECURE KEY]"
    echo ""
    echo "IMPORTANT: The .env file contains sensitive data."
    echo "  - Never commit .env to Git"
    echo "  - Keep your API keys secure"
    echo ""
else
    echo "[ERROR] Failed to update .env file"
    echo "Please edit .env manually"
fi

echo "Configuration complete!"
echo "You can now start the system with: docker compose up -d"
echo ""
