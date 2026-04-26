# Sicomoro en Produccion

## Lo que necesitas

- Un VPS Ubuntu con Docker y Docker Compose.
- Un dominio apuntando a la IP publica del VPS.
- Puertos abiertos: `80` y `443`.
- No abrir PostgreSQL a internet.

## Preparar servidor

En Ubuntu:

```bash
sudo apt update
sudo apt install -y ca-certificates curl git
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER
```

Cierra sesion y vuelve a entrar.

## Subir proyecto

```bash
git clone TU_REPO Sicomoro
cd Sicomoro
cp .env.production.example .env.production
nano .env.production
```

Cambia:

```text
DOMAIN=app.tudominio.com
ACME_EMAIL=tu-correo
POSTGRES_PASSWORD=clave-larga
JWT_KEY=clave-muy-larga
```

## Levantar

```bash
chmod +x deploy/*.sh
./deploy/deploy.sh
```

El sitio quedara en:

```text
https://app.tudominio.com
```

La API queda detras del mismo dominio:

```text
https://app.tudominio.com/api
```

## Backups

El servicio `backup` genera un dump diario y conserva 14 dias en el volumen `sicomoro_backups`.

Para ver los backups:

```bash
docker volume inspect sicomoro_sicomoro_backups
```

## Seguridad aplicada

- PostgreSQL no expone puerto publico.
- HTTPS automatico con Caddy y Let's Encrypt.
- Frontend y API salen por el mismo dominio.
- `POST /api/auth/register` requiere rol `Administrador`.
- Swagger queda apagado por defecto en produccion.

## Primer acceso

Usuario seed:

```text
admin@sicomoro.local
Admin123*
```

Cambia esa clave creando otro administrador y eliminando/inactivando el usuario de prueba en una siguiente iteracion.

## Deploy en Render

Tambien esta preparado para Render con `render.yaml`.

Render no levanta el `docker-compose.yml` local. En su lugar usa el Blueprint:

- `sicomoro`: Web Service Docker con API + frontend en la misma URL.
- `sicomoro-db`: PostgreSQL administrado por Render.
- `DATABASE_URL`: conectado automaticamente desde la base de Render.
- `ApplyMigrationsOnStartup=true`: aplica migraciones al arrancar.
- `Swagger__Enabled=false`: Swagger apagado en produccion.

Pasos:

```text
1. Subir el repo a GitHub.
2. Abrir Render Dashboard.
3. New > Blueprint.
4. Elegir el repo.
5. Confirmar Deploy Blueprint.
```

Despues del deploy, entra a la URL `.onrender.com` que muestre Render.
