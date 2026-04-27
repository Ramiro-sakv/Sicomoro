# Sicomoro

Backend principal para administrar una barraca de madera. Esta base usa .NET 8 Web API, PostgreSQL, EF Core, JWT, Swagger, MediatR, arquitectura por capas y pruebas con xUnit.

[![Deploy to Render](https://render.com/images/deploy-to-render-button.svg)](https://render.com/deploy?repo=https://github.com/Ramiro-sakv/Sicomoro)

## Arquitectura

- `Sicomoro.Domain`: entidades, enums, eventos, repositorios abstractos, reglas de dominio, Strategy y State.
- `Sicomoro.Application`: commands, queries, DTOs, validaciones Chain of Responsibility, fachadas/casos de uso vía MediatR.
- `Sicomoro.Infrastructure`: EF Core, PostgreSQL, repositorios, Unit of Work, JWT, adapters externos, PDF, facturación.
- `Sicomoro.Api`: controllers, middleware global, Swagger/OpenAPI, JWT, DTOs HTTP.
- `Sicomoro.Frontend`: SPA HTML/CSS/JS servida por Nginx.
- `Sicomoro.Tests`: pruebas unitarias de reglas críticas.

## Patrones incluidos

- Strategy: `IPricingStrategy` para precios normal, mayorista, cliente frecuente y descuento manual.
- State: políticas de estado de venta para impedir acciones inválidas.
- Factory Method: `IDocumentoFactory` crea proveedor PDF o fiscal futuro.
- Repository + Unit of Work: acceso a datos y transacciones.
- Observer: eventos de dominio generan notificaciones.
- Command/Mediator: controllers envían commands/queries a handlers.
- Chain of Responsibility: validaciones antes de confirmar ventas.
- Adapter: email, WhatsApp y facturación electrónica futura.
- Decorator: retry de email y decorador de documentos.
- Template Method: generación PDF con encabezado, cliente, detalle, totales y pie.
- Proxy: reportes sensibles protegidos por rol `Administrador` o `Gerente`.

## Ejecutar

```powershell
cd "C:\Users\ramir\OneDrive\Documentos\New project\Sicomoro"
docker compose up --build
```

API: `http://localhost:8080`  
Swagger: `http://localhost:8080/swagger`
Frontend: `http://localhost:3000`

Produccion:

```text
Ver PRODUCCION.md
```

Render:

```text
El archivo render.yaml esta listo en la raiz del repo.
Render crea un Web Service Docker llamado sicomoro y una base PostgreSQL llamada sicomoro-db.
El frontend queda servido por la misma URL del backend.
```

Flujo esperado:

```text
1. Subir este proyecto a GitHub.
2. En Render: New > Blueprint.
3. Seleccionar el repo.
4. Render detecta render.yaml.
5. Deploy Blueprint.
```

Para reiniciar la base de desarrollo desde cero:

```powershell
docker compose down -v
docker compose up --build
```

Usuario seed:

```json
{
  "email": "admin@sicomoro.local",
  "password": "Admin123*"
}
```

Usuarios:

- Cada usuario puede editar su perfil en `Mi perfil`.
- Los administradores ven la seccion `Usuarios`.
- Para crear usuarios se requiere rol `Administrador` y la clave de creacion `13067264`.
- Los administradores pueden borrar usuarios, excepto su propia cuenta y el ultimo administrador.

## App movil PWA

La rama `codex/v1.2-app-movil` deja preparado Sicomoro como PWA instalable:

- `Sicomoro.Frontend/manifest.webmanifest`: nombre, colores e iconos de instalacion.
- `Sicomoro.Frontend/service-worker.js`: cachea la pantalla inicial y archivos estaticos.
- Pantalla `App movil`: instrucciones para Android, iPhone y boton de instalacion cuando el navegador lo permita.

Para probar instalacion real en celular se necesita HTTPS, por ejemplo Render. En `localhost` sirve para validar el frontend, pero algunos celulares no mostraran el boton de instalacion si se abre por IP local sin HTTPS.

## Comandos locales

```powershell
dotnet test Sicomoro.sln
```

Ruta real del SDK local instalado en esta máquina:

```powershell
C:\Users\ramir\OneDrive\Documentos\New project\.dotnet_home\.dotnet8\dotnet.exe test Sicomoro.sln
```

Migraciones:

```powershell
$env:DOTNET_ROOT="C:\Users\ramir\OneDrive\Documentos\New project\.dotnet_home\.dotnet8"
$env:PATH="$env:DOTNET_ROOT;$env:PATH"
dotnet tool install dotnet-ef --tool-path .tools --version 8.0.11
.\.tools\dotnet-ef.exe database update --project Sicomoro.Infrastructure\Sicomoro.Infrastructure.csproj --startup-project Sicomoro.Api\Sicomoro.Api.csproj
```

## Flujo de prueba

1. Login:

```http
POST /api/auth/login
```

```json
{
  "email": "admin@sicomoro.local",
  "password": "Admin123*"
}
```

Usa el token como `Authorization: Bearer {token}`.

2. Crear proveedor de Beni:

```http
POST /api/proveedores
```

```json
{
  "nombre": "Maderas Beni SRL",
  "lugarOrigen": "Beni",
  "telefono": "70000001",
  "direccion": "Trinidad",
  "tipoMadera": "Tajibo",
  "notas": "Proveedor inicial"
}
```

3. Crear producto Tajibo 2x4:

```http
POST /api/productos
```

```json
{
  "nombreComercial": "Tajibo 2x4",
  "tipoMadera": "Tajibo",
  "unidadMedida": 1,
  "largo": 2,
  "ancho": 4,
  "espesor": 0,
  "calidad": "A",
  "precioCompra": 35,
  "precioVentaSugerido": 55,
  "stockMinimo": 10,
  "observaciones": "Pieza estandar"
}
```

4. Registrar compra:

```http
POST /api/compras
```

```json
{
  "proveedorId": "GUID_PROVEEDOR",
  "origen": "Beni",
  "fechaCompra": "2026-04-25T00:00:00Z",
  "fechaEstimadaLlegada": "2026-04-30T00:00:00Z",
  "costoTransporte": 1200,
  "otrosCostos": 150,
  "observaciones": "Carga inicial",
  "detalles": [
    {
      "productoId": "GUID_PRODUCTO",
      "cantidad": 30,
      "precioCompra": 35
    }
  ]
}
```

5. Recibir compra:

```http
PUT /api/compras/GUID_COMPRA/recibir
```

6. Ver stock:

```http
GET /api/inventario
```

7. Crear cliente:

```http
POST /api/clientes
```

```json
{
  "nombreRazonSocial": "Constructora Norte",
  "ciNit": "1234567",
  "telefono": "70000002",
  "direccion": "Av. Principal",
  "ciudad": "Santa Cruz",
  "notas": "Cliente a credito"
}
```

8. Crear venta:

```http
POST /api/ventas
```

```json
{
  "clienteId": "GUID_CLIENTE",
  "metodoPago": 5,
  "fechaVencimiento": "2026-05-10T00:00:00Z",
  "observaciones": "Venta a credito parcial",
  "detalles": [
    {
      "productoId": "GUID_PRODUCTO",
      "cantidad": 25,
      "precioUnitario": 55,
      "descuento": 0,
      "pricingStrategy": "normal"
    }
  ]
}
```

9. Confirmar venta con pago parcial:

```http
PUT /api/ventas/GUID_VENTA/confirmar
```

```json
{
  "montoPagado": 300
}
```

10. Ver deuda y registrar pago:

```http
GET /api/cobros/deudas
POST /api/cobros/pagos
```

```json
{
  "cobroId": "GUID_COBRO",
  "monto": 200,
  "metodoPago": 1,
  "referencia": "Recibo caja"
}
```

11. Generar comprobante PDF:

```http
POST /api/documentos/venta/GUID_VENTA/generar
```

El archivo se guarda en `storage/documentos` o en el volumen `sicomoro_docs` si se ejecuta con Docker.

## Endpoints adicionales

Productos:

```http
PUT /api/productos/{id}
```

Caja:

```http
GET /api/caja/movimientos?desde=2026-04-01&hasta=2026-04-30
POST /api/caja/movimientos
```

```json
{
  "tipo": 2,
  "monto": 150,
  "concepto": "Gasto menor"
}
```

Transportes:

```http
GET /api/transportes
POST /api/transportes
PUT /api/transportes/{id}/estado
```

```json
{
  "camion": "Volvo FH",
  "chofer": "Juan Perez",
  "placa": "1234ABC",
  "lugarOrigen": "Beni",
  "fechaSalida": "2026-04-25T00:00:00Z",
  "fechaLlegada": null,
  "costoTransporte": 1200,
  "estado": 2,
  "observaciones": "Traslado desde proveedor",
  "compraId": "GUID_COMPRA"
}
```

Auditoria y notificaciones:

```http
GET /api/auditoria?take=100
GET /api/notificaciones?soloNoLeidas=true
```

## Validacion ejecutada

Se valido por Docker este flujo:

- Login con admin seed.
- Crear proveedor de Beni.
- Crear producto Tajibo 2x4.
- Registrar compra y recibirla.
- Ver stock subir a 30.
- Crear cliente y venta.
- Confirmar venta con pago inicial.
- Ver stock bajar a 5 y generar notificacion de bajo stock.
- Registrar pago parcial y ver saldo pendiente.
- Generar PDF interno.
- Consultar auditoria.

## Frontend

El frontend esta hecho sin framework pesado para que sea facil de modificar:

- `Sicomoro.Frontend/index.html`: punto de entrada.
- `Sicomoro.Frontend/styles.css`: colores, grillas, tablas, formularios y responsive.
- `Sicomoro.Frontend/app.js`: estado de sesion, llamadas HTTP y pantallas.
- `Sicomoro.Frontend/Dockerfile`: publica los archivos con Nginx.

Para cambiar una pantalla, busca su funcion en `app.js`:

- `renderClientes`
- `renderProductos`
- `renderInventario`
- `renderCompras`
- `renderVentas`
- `renderCobros`
- `renderCaja`
- `renderTransportes`
- `renderDocumentos`
- `renderReportes`

Cada formulario usa `api("/ruta", { method, body })` contra el backend. El token JWT se guarda en `localStorage` y se envia automaticamente como `Authorization: Bearer`.

Si modificas CSS o JS:

```powershell
docker compose up -d --build frontend
```

Si modificas backend:

```powershell
docker compose up -d --build api
```

Si modificas ambos:

```powershell
docker compose up -d --build
```

## Estado de facturación

`PdfComprobanteProvider` genera un comprobante PDF interno. `FacturacionElectronicaProvider` queda preparado y lanza una excepción explícita hasta contar con normativa, credenciales y configuración oficial.
