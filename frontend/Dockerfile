# ─── Build Stage ──────────────────────────────────────────
FROM node:20-alpine AS build
WORKDIR /app

# Copy package files for layer caching
COPY package*.json ./
RUN npm ci --only=production

# Copy source and build
COPY . .
ARG VITE_API_BASE_URL=/api
ARG VITE_SIGNALR_URL=/hub
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}
ENV VITE_SIGNALR_URL=${VITE_SIGNALR_URL}

RUN npm run build

# ─── Runtime Stage (nginx) ────────────────────────────────
FROM nginx:alpine AS runtime
WORKDIR /usr/share/nginx/html

# Copy built assets from build stage
COPY --from=build /app/dist .

# Copy nginx config for SPA
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Security: run as non-root
RUN addgroup -g 1001 -S nginx || true && \
    adduser -S -D -H -u 1001 -h /usr/share/nginx/html -s /sbin/nologin -G nginx -g nginx nginx || true && \
    chown -R nginx:nginx /usr/share/nginx/html && \
    chown -R nginx:nginx /var/cache/nginx && \
    chown -R nginx:nginx /var/log/nginx && \
    chown -R nginx:nginx /etc/nginx/conf.d && \
    touch /var/run/nginx.pid && \
    chown -R nginx:nginx /var/run/nginx.pid

USER nginx

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
