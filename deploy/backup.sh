#!/bin/sh
set -eu

mkdir -p /backups

while true; do
  stamp="$(date -u +%Y%m%d-%H%M%S)"
  export PGPASSWORD="$POSTGRES_PASSWORD"
  pg_dump -h postgres -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc > "/backups/sicomoro-$stamp.dump"
  find /backups -name "sicomoro-*.dump" -mtime +14 -delete
  sleep 86400
done

