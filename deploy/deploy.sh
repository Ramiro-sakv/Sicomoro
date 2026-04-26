#!/bin/sh
set -eu

if [ ! -f .env.production ]; then
  echo "Falta .env.production. Copia .env.production.example y cambia las claves."
  exit 1
fi

docker compose --env-file .env.production -f docker-compose.prod.yml pull
docker compose --env-file .env.production -f docker-compose.prod.yml up -d --build
docker compose --env-file .env.production -f docker-compose.prod.yml ps

