#!/bin/sh
set -eu

if [ $# -ne 1 ]; then
  echo "Uso: ./deploy/restore-backup.sh /ruta/backup.dump"
  exit 1
fi

backup_file="$1"
container="$(docker compose --env-file .env.production -f docker-compose.prod.yml ps -q postgres)"

docker cp "$backup_file" "$container:/tmp/restore.dump"
docker compose --env-file .env.production -f docker-compose.prod.yml exec postgres sh -c 'export PGPASSWORD="$POSTGRES_PASSWORD"; pg_restore -h localhost -U "$POSTGRES_USER" -d "$POSTGRES_DB" --clean --if-exists /tmp/restore.dump'

