# Sicomoro | Sistema de Gestión y Catálogo para Barracas de Madera

<p align="center">
  <strong>🌲 Sicomoro</strong><br>
  Plataforma web/API para inventario, ventas, compras, cobros, documentos y catálogo público de madera.
</p>

<p align="center">
  <img alt="Estado" src="https://img.shields.io/badge/estado-en%20desarrollo%20funcional-2f6f4f">
  <img alt=".NET" src="https://img.shields.io/badge/.NET-8.0-512bd4">
  <img alt="PostgreSQL" src="https://img.shields.io/badge/PostgreSQL-16-336791">
  <img alt="Docker" src="https://img.shields.io/badge/Docker-ready-2496ed">
  <img alt="Arquitectura" src="https://img.shields.io/badge/arquitectura-clean%20architecture-1d2522">
</p>

<p align="center">
  <a href="https://render.com/deploy?repo=https://github.com/Ramiro-sakv/Sicomoro">
    <img src="https://render.com/images/deploy-to-render-button.svg" alt="Deploy to Render">
  </a>
</p>

---

## Tabla de Contenido

- [Descripción](#descripción)
- [Problema y solución](#problema-y-solución)
- [Características principales](#características-principales)
- [Arquitectura del sistema](#arquitectura-del-sistema)
- [Tecnologías utilizadas](#tecnologías-utilizadas)
- [Estructura de carpetas](#estructura-de-carpetas)
- [Instalación local](#instalación-local)
- [Configuración](#configuración)
- [Uso del sistema](#uso-del-sistema)
- [API endpoints](#api-endpoints)
- [Flujos de negocio](#flujos-de-negocio)
- [Buenas prácticas aplicadas](#buenas-prácticas-aplicadas)
- [Despliegue](#despliegue)
- [Roadmap](#roadmap)
- [Autor y créditos](#autor-y-créditos)

---

## Descripción

**Sicomoro** es una plataforma para administrar una barraca de madera de forma ordenada, segura y escalable. El sistema permite controlar productos, inventario, compras, proveedores, ventas, clientes, cobros, documentos, caja, reportes y un catálogo público para mostrar madera disponible a clientes.

Está diseñado como un proyecto real de backend profesional, con una API en **.NET 8**, persistencia en **PostgreSQL**, autenticación por **JWT**, arquitectura por capas y separación clara entre dominio, aplicación, infraestructura y presentación.

La aplicación también incluye un frontend web ligero:

- `/` muestra el catálogo público para clientes.
- `/personal` muestra el acceso interno para trabajadores.
- La sección `Publicidad` permite administrar lo que se publica en el catálogo.
- Los clientes pueden crear una cuenta básica desde el catálogo para futuras cotizaciones.

---

## Problema y Solución

### Problema

Una barraca de madera suele manejar información crítica en cuadernos, hojas de cálculo o mensajes sueltos:

- Stock desactualizado.
- Ventas sin historial claro.
- Deudas difíciles de controlar.
- Compras y transporte sin trazabilidad.
- Clientes sin historial centralizado.
- Documentos internos poco estandarizados.
- Catálogo comercial separado del inventario real.

### Solución

Sicomoro centraliza la operación en un sistema único:

| Área | Cómo ayuda Sicomoro |
|---|---|
| Inventario | Controla entradas, salidas, ajustes, pérdidas y stock mínimo. |
| Ventas | Registra ventas, descuentos, pagos parciales y anulaciones. |
| Clientes | Mantiene historial de compras, deudas y pagos. |
| Compras | Registra proveedores, origen, transporte y recepción de madera. |
| Caja | Ordena ingresos, egresos y movimientos diarios. |
| Catálogo público | Publica productos destacados para clientes sin exponer el sistema interno. |
| Auditoría | Registra acciones importantes para trazabilidad. |

---

## Características Principales

### Para operación interna

- Gestión de usuarios con roles.
- Registro de clientes y proveedores.
- Catálogo interno de productos de madera.
- Control de inventario por producto.
- Borrado protegido por clave para clientes, proveedores, productos, anuncios y usuarios.
- Compras con recepción automática en inventario.
- Ventas con validación de stock.
- Cobros y cuentas por cobrar.
- Caja para ingresos y egresos.
- Transporte y recepción de cargas.
- Reportes operativos.
- Auditoría de operaciones críticas.
- Notificaciones internas.
- Generación de comprobantes PDF internos.
- Instalación como PWA en PC y celular, con acceso directo descargable para Windows.

### Para clientes

- Catálogo público en la página inicial.
- Publicaciones administrables desde el panel interno.
- Anuncios con imagen, descripción, precio visible y botón de contacto.
- Vinculación opcional con productos reales del inventario.

---

## Arquitectura del Sistema

Sicomoro usa una arquitectura por capas inspirada en **Clean Architecture**.

```text
Cliente / Navegador
        |
        v
Sicomoro.Frontend
        |
        v
Sicomoro.Api
        |
        v
Sicomoro.Application
        |
        v
Sicomoro.Domain
        |
        v
Sicomoro.Infrastructure
        |
        v
PostgreSQL
```

### Capas

| Proyecto | Responsabilidad |
|---|---|
| `Sicomoro.Domain` | Entidades, reglas de negocio, enums, eventos y contratos. |
| `Sicomoro.Application` | Casos de uso, commands, queries, DTOs, validaciones y servicios de aplicación. |
| `Sicomoro.Infrastructure` | EF Core, PostgreSQL, repositorios, Unit of Work, PDF, adapters externos. |
| `Sicomoro.Api` | Controllers, JWT, Swagger, middleware de errores y publicación del frontend. |
| `Sicomoro.Frontend` | SPA HTML/CSS/JS para catálogo público y panel interno. |
| `Sicomoro.Tests` | Pruebas unitarias de reglas de negocio. |

---

## Tecnologías Utilizadas

| Tecnología | Uso |
|---|---|
| C# / .NET 8 | Backend principal y API REST. |
| ASP.NET Core Web API | Exposición de endpoints HTTP. |
| Entity Framework Core | ORM para persistencia. |
| PostgreSQL 16 | Base de datos relacional. |
| JWT Bearer | Autenticación y autorización. |
| MediatR | Mediator para desacoplar controllers y casos de uso. |
| Swagger / OpenAPI | Documentación interactiva de la API. |
| Docker / Docker Compose | Ejecución local y despliegue reproducible. |
| xUnit | Pruebas automatizadas. |
| HTML/CSS/JavaScript | Frontend ligero sin framework pesado. |
| Render | Despliegue cloud mediante Blueprint. |

---

## Estructura de Carpetas

```text
Sicomoro/
├── Sicomoro.Api/
│   ├── Controllers/
│   ├── Middlewares/
│   ├── Security/
│   └── Program.cs
├── Sicomoro.Application/
│   ├── Commands/
│   ├── Queries/
│   ├── DTOs/
│   ├── Interfaces/
│   └── Validators/
├── Sicomoro.Domain/
│   ├── Entities/
│   ├── Enums/
│   ├── Events/
│   ├── DomainServices/
│   └── Interfaces/
├── Sicomoro.Infrastructure/
│   ├── Persistence/
│   ├── Repositories/
│   ├── Pdf/
│   ├── Email/
│   ├── FileStorage/
│   └── Facturacion/
├── Sicomoro.Frontend/
│   ├── app.js
│   ├── styles.css
│   ├── index.html
│   ├── manifest.webmanifest
│   └── assets/
├── Sicomoro.Tests/
├── docker-compose.yml
├── render.yaml
└── README.md
```

---

## Instalación Local

### Requisitos

- Docker Desktop.
- Git.
- Navegador moderno.
- Opcional para desarrollo backend: .NET SDK 8.

### Ejecutar con Docker Compose

```powershell
git clone https://github.com/Ramiro-sakv/Sicomoro.git
cd Sicomoro
docker compose up --build
```

Servicios locales:

| Servicio | URL |
|---|---|
| Aplicación principal | `http://localhost:8080` |
| Catálogo público | `http://localhost:8080/` |
| Acceso personal | `http://localhost:8080/personal` |
| Swagger | `http://localhost:8080/swagger` |
| PostgreSQL | `localhost:5432` |

El servicio `frontend` con Nginx queda como perfil opcional. La aplicación principal ya sirve el frontend desde la API, por eso el comando normal no necesita descargar la imagen de Nginx.

```powershell
docker compose --profile separate-frontend up --build
```

### Reiniciar base de datos local

> Este comando borra los datos locales de Docker.

```powershell
docker compose down -v
docker compose up --build
```

---

## Configuración

El sistema usa variables de entorno. En Docker Compose ya vienen valores de desarrollo.

### Variables principales

| Variable | Descripción | Ejemplo |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | Conexión PostgreSQL. | `Host=postgres;Port=5432;Database=sicomoro;Username=sicomoro;Password=sicomoro123` |
| `DATABASE_URL` | Conexión usada por Render. | `postgres://...` |
| `Jwt__Issuer` | Emisor del token JWT. | `Sicomoro` |
| `Jwt__Key` | Clave privada para firmar tokens. | `cambiar-en-produccion` |
| `ApplyMigrationsOnStartup` | Aplica migraciones al iniciar. | `true` |
| `Swagger__Enabled` | Habilita Swagger fuera de desarrollo. | `false` |
| `WhatsApp__Enabled` | Activa integración WhatsApp Cloud API. | `false` |

### Ejemplo `.env`

```env
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=sicomoro;Username=sicomoro;Password=sicomoro123
Jwt__Issuer=Sicomoro
Jwt__Key=Sicomoro-dev-key-change-this-value-32chars
ApplyMigrationsOnStartup=true
Swagger__Enabled=true
WhatsApp__Enabled=false
```

### Usuario inicial

```json
{
  "email": "admin@sicomoro.local",
  "password": "Admin123*"
}
```

---

## Uso del Sistema

### Acceso público

Los clientes ingresan a:

```text
/
```

Ahí ven el catálogo de madera publicado por la barraca.

Desde la misma pantalla pueden crear una cuenta cliente o iniciar sesión como cliente. Estas cuentas no tienen acceso al panel interno.

### Acceso del personal

El equipo interno ingresa desde el botón `Personal` o directamente en:

```text
/personal
```

Roles disponibles:

| Rol | Función |
|---|---|
| Administrador | Control total del sistema. |
| Vendedor | Gestión de ventas y clientes. |
| Inventario | Productos, stock y compras. |
| Cobrador | Cobros, caja y deudas. |
| Gerente | Reportes y administración operativa. |
| Solo lectura | Consulta sin edición. |

### Publicar productos en el catálogo

1. Entrar a `/personal`.
2. Iniciar sesión como administrador o gerente.
3. Ir a `Publicidad`.
4. Crear anuncio.
5. Vincular producto si corresponde.
6. Marcar `Publicado en catálogo`.
7. Ver resultado en `/`.

> Nota de prueba: en la rama `codex/v1.4-catalogo-publico`, el frontend muestra productos de ejemplo cuando no existen anuncios reales publicados. Esto sirve para revisar el diseño del catálogo lleno sin modificar datos de negocio. Antes de lanzar oficialmente, cambiar `CATALOG_DEMO_MODE` a `false` en `Sicomoro.Frontend/app.js` o publicar anuncios reales desde `Publicidad`.

### Instalar en PC o celular

1. Entrar a `/personal`.
2. Iniciar sesión.
3. Abrir `App PC/movil`.
4. En Windows o PC, usar `Instalar como app` desde Chrome, Edge u Opera.
5. Si el navegador no muestra instalación, usar `Descargar acceso PC` para guardar `Sicomoro.url` en el escritorio.

La instalación PWA abre Sicomoro en una ventana propia, con icono de aplicación, pero sigue usando el mismo servidor y la misma base de datos.

---

## API Endpoints

### Autenticación

| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/api/auth/login` | Inicia sesión. |
| `POST` | `/api/auth/register` | Registra usuario con clave autorizada. |

### Portal de clientes

| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/api/clientes-portal/register` | Crea una cuenta pública de cliente. |
| `POST` | `/api/clientes-portal/login` | Inicia sesión como cliente. |

### Clientes

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/clientes` | Lista clientes. |
| `GET` | `/api/clientes/{id}` | Obtiene cliente. |
| `POST` | `/api/clientes` | Crea cliente. |
| `PUT` | `/api/clientes/{id}` | Edita cliente. |
| `DELETE` | `/api/clientes/{id}` | Borra cliente sin historial usando `X-Sicomoro-Operation-Key`. |

### Proveedores

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/proveedores` | Lista proveedores. |
| `POST` | `/api/proveedores` | Crea proveedor. |
| `DELETE` | `/api/proveedores/{id}` | Borra proveedor sin compras registradas usando `X-Sicomoro-Operation-Key`. |

### Productos e inventario

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/api/productos` | Lista productos. |
| `POST` | `/api/productos` | Crea producto. |
| `PUT` | `/api/productos/{id}` | Edita producto. |
| `DELETE` | `/api/productos/{id}` | Borra producto si no tiene historial y se envía `X-Sicomoro-Operation-Key`. |
| `GET` | `/api/inventario` | Consulta stock. |
| `POST` | `/api/inventario/ajuste` | Ajusta inventario. |
| `GET` | `/api/inventario/movimientos` | Lista movimientos. |

### Compras y ventas

| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/api/compras` | Registra compra. |
| `PUT` | `/api/compras/{id}/recibir` | Recibe compra y aumenta inventario. |
| `GET` | `/api/compras` | Lista compras. |
| `POST` | `/api/ventas` | Crea venta pendiente. |
| `PUT` | `/api/ventas/{id}/confirmar` | Confirma venta y descuenta stock. |
| `PUT` | `/api/ventas/{id}/anular` | Anula venta y revierte stock si corresponde. |
| `GET` | `/api/ventas` | Lista ventas. |

### Reportes operativos

El panel web combina reportes de ventas, caja, inventario bajo y clientes deudores en un resumen exportable. Esta vista ayuda al dueño a revisar flujo de caja, deuda pendiente y productos críticos sin entrar módulo por módulo.

### Cobros, caja y documentos

| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/api/cobros/pagos` | Registra pago. |
| `GET` | `/api/cobros/deudas` | Lista deudas. |
| `GET` | `/api/cobros/cliente/{clienteId}` | Deudas de cliente. |
| `GET` | `/api/caja/movimientos` | Lista caja por rango. |
| `POST` | `/api/caja/movimientos` | Registra ingreso o egreso. |
| `POST` | `/api/documentos/venta/{ventaId}/generar` | Genera comprobante PDF. |

### Catálogo público

| Método | Ruta | Acceso | Descripción |
|---|---|---|---|
| `GET` | `/api/catalogo/publico` | Público | Lista anuncios publicados. |
| `GET` | `/api/catalogo/anuncios` | Admin/Gerente | Lista todos los anuncios. |
| `POST` | `/api/catalogo/anuncios` | Admin/Gerente | Crea anuncio. |
| `PUT` | `/api/catalogo/anuncios/{id}` | Admin/Gerente | Actualiza anuncio. |
| `DELETE` | `/api/catalogo/anuncios/{id}` | Admin/Gerente | Elimina anuncio con clave de operación. |

---

## Flujos de Negocio

### Flujo 1: Registrar madera comprada

```text
Proveedor de Beni -> Compra -> Recepción -> Entrada de inventario -> Stock actualizado
```

Ejemplo:

```http
POST /api/compras
Authorization: Bearer TOKEN
Content-Type: application/json
```

```json
{
  "proveedorId": "GUID_PROVEEDOR",
  "origen": "Beni",
  "fechaCompra": "2026-04-27T00:00:00Z",
  "fechaEstimadaLlegada": "2026-05-02T00:00:00Z",
  "costoTransporte": 1200,
  "otrosCostos": 150,
  "observaciones": "Carga de tajibo seco",
  "detalles": [
    {
      "productoId": "GUID_PRODUCTO",
      "cantidad": 100,
      "precioCompra": 35
    }
  ]
}
```

Al recibir:

```http
PUT /api/compras/GUID_COMPRA/recibir
```

Resultado:

- La compra cambia a `Recibida`.
- Se genera movimiento de inventario.
- Aumenta el stock del producto.
- Queda auditoría de la operación.

### Flujo 2: Vender madera a cliente

```text
Cliente -> Venta pendiente -> Confirmación -> Salida de inventario -> Cobro / deuda
```

En el frontend, cada línea de venta incluye una calculadora de **pies tablares** para la forma de venta común en barracas:

```text
piezas x largo en pies x ancho en pulgadas x espesor en pulgadas / 12
```

El resultado se guarda como `cantidad`, por lo que el inventario se descuenta en pies tablares cuando el producto se maneja con esa unidad.

```http
POST /api/ventas
Authorization: Bearer TOKEN
Content-Type: application/json
```

```json
{
  "clienteId": "GUID_CLIENTE",
  "metodoPago": 5,
  "fechaVencimiento": "2026-05-15T00:00:00Z",
  "observaciones": "Venta con pago parcial",
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

Confirmar venta:

```http
PUT /api/ventas/GUID_VENTA/confirmar
Content-Type: application/json
```

```json
{
  "montoPagado": 300
}
```

Resultado:

- Se descuenta inventario.
- Se registra pago inicial.
- Si queda saldo, se genera cuenta por cobrar.
- Puede generarse comprobante PDF.

### Flujo 3: Publicar producto para clientes

```http
POST /api/catalogo/anuncios
Authorization: Bearer TOKEN
Content-Type: application/json
```

```json
{
  "productoId": "GUID_PRODUCTO",
  "titulo": "Tajibo 2x4 seco",
  "subtitulo": "Ideal para estructura y carpintería",
  "descripcion": "Madera resistente, seleccionada y disponible para entrega.",
  "imagenUrl": "https://example.com/tajibo.jpg",
  "precioTexto": "Consultar precio",
  "etiqueta": "Destacado",
  "ctaTexto": "Solicitar cotización",
  "ctaUrl": "#contacto",
  "orden": 1,
  "publicado": true
}
```

Luego el cliente lo ve en:

```text
/
```

---

## Buenas Prácticas Aplicadas

| Práctica | Aplicación en Sicomoro |
|---|---|
| SOLID | Separación de responsabilidades entre capas y contratos. |
| Clean Architecture | El dominio no depende de infraestructura ni API. |
| Repository Pattern | Acceso a datos encapsulado en repositorios. |
| Unit of Work | Transacciones consistentes en ventas, compras y cobros. |
| Mediator | Controllers desacoplados de casos de uso mediante MediatR. |
| Strategy | Cálculo de precios y descuentos extensible. |
| State | Validación de estados de venta, compra y cobro. |
| Factory Method | Creación de documentos internos y futuros documentos fiscales. |
| Observer | Eventos de dominio para notificaciones y auditoría. |
| Chain of Responsibility | Validaciones antes de confirmar venta. |
| Adapter | Integraciones externas como PDF, email, WhatsApp o facturación. |
| Template Method | Estructura común para documentos PDF. |

### Decisiones técnicas

- **.NET 8**: plataforma estable, robusta y mantenible para backend empresarial.
- **PostgreSQL**: base relacional confiable para transacciones de negocio.
- **JWT**: permite separar frontend y backend manteniendo sesiones seguras.
- **Docker**: facilita desarrollo local y despliegue reproducible.
- **Frontend ligero**: reduce complejidad para una primera versión operativa.
- **Catálogo separado del panel interno**: clientes ven productos, trabajadores gestionan operaciones.

---

## Despliegue

El proyecto incluye `render.yaml` para desplegar en Render como Blueprint.

Render crea:

- Web Service Docker `sicomoro`.
- Base PostgreSQL `sicomoro-db`.
- Migraciones automáticas al iniciar.

Pasos:

```text
1. Subir cambios a GitHub.
2. En Render: New > Blueprint.
3. Seleccionar el repositorio.
4. Confirmar render.yaml.
5. Deploy Blueprint.
```

Para producción se recomienda:

- Usar `Swagger__Enabled=false`.
- Usar una clave JWT segura y privada.
- Activar backups de PostgreSQL.
- Configurar dominio propio.
- Revisar roles y permisos antes de dar acceso al personal.

---

## Pruebas

Ejecutar pruebas:

```powershell
dotnet test Sicomoro.sln
```

Con SDK local específico:

```powershell
"C:\Users\ramir\OneDrive\Documentos\New project\.dotnet_home\.dotnet8\dotnet.exe" test Sicomoro.sln
```

Validaciones cubiertas:

- No vender más stock del disponible.
- No registrar pagos mayores al saldo.
- Estados de venta válidos.
- Reglas base de dominio.

---

## Facturación y Documentos

Sicomoro genera comprobantes PDF internos mediante `PdfComprobanteProvider`.

La facturación electrónica oficial queda abstraída detrás de:

```csharp
public interface IFacturacionProvider
{
    Task<DocumentoVenta> GenerarDocumentoVentaAsync(...);
    Task EnviarDocumentoAsync(...);
    Task AnularDocumentoAsync(...);
}
```

`FacturacionElectronicaProvider` está preparado como extensión futura, pero no simula integración fiscal oficial sin normativa, credenciales y configuración real.

---

## Roadmap

| Prioridad | Mejora |
|---|---|
| Alta | Registro de clientes públicos para solicitudes de cotización. |
| Alta | Pedidos desde catálogo hacia el panel interno. |
| Alta | Filtros públicos por tipo de madera, medida y disponibilidad. |
| Media | Panel visual para editar banners y campañas comerciales. |
| Media | Exportación de reportes a Excel/PDF. |
| Media | Recuperación de contraseña. |
| Media | Backups administrados y monitoreo. |
| Media | Mejoras de permisos por acción específica. |
| Baja | Integración WhatsApp Cloud API una vez verificado el negocio en Meta. |
| Baja | Facturación electrónica oficial cuando exista configuración legal completa. |

---

## Autor y Créditos

**Proyecto:** Sicomoro

**Propósito:** Sistema de gestión para barraca de madera y catálogo comercial.

**Autor:** Ramiro Huarachi

**Asistencia técnica:** OpenAI Codex

Este proyecto fue desarrollado como una base profesional y extensible para operar una barraca real, con enfoque en backend, trazabilidad de negocio y crecimiento futuro.
