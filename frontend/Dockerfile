# Imagem do frontend. O contexto de build é a raiz do repositório (veja docker-compose.yml).

# ---- build ----
FROM node:20-alpine AS build
WORKDIR /app

COPY frontend/package*.json ./
# Usa o package-lock.json quando existir (instalação reproduzível); senão, instala normalmente.
RUN if [ -f package-lock.json ]; then npm ci; else npm install; fi

COPY frontend/ .
RUN npm run build

# ---- runtime ----
FROM nginx:1.27-alpine

COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
