const API_DEFAULT = window.SICOMORO_API_BASE
  || (["localhost", "127.0.0.1"].includes(window.location.hostname) ? "http://localhost:8080" : window.location.origin);

function normalizeApiBase(value) {
  return String(value || API_DEFAULT).trim().replace(/\/+$/, "");
}

const app = document.getElementById("app");
const state = {
  apiBase: normalizeApiBase(localStorage.getItem("sicomoro_api") || API_DEFAULT),
  token: localStorage.getItem("sicomoro_token") || "",
  user: JSON.parse(localStorage.getItem("sicomoro_user") || "null"),
  view: localStorage.getItem("sicomoro_view") || "dashboard",
  cache: {
    clientes: [],
    proveedores: [],
    productos: [],
    inventario: [],
    compras: [],
    ventas: [],
    deudas: [],
    transportes: [],
    notificaciones: [],
    auditoria: [],
    usuarios: [],
    perfil: null
  }
};

const views = [
  ["dashboard", "Panel"],
  ["clientes", "Clientes"],
  ["proveedores", "Proveedores"],
  ["productos", "Productos"],
  ["inventario", "Inventario"],
  ["compras", "Compras"],
  ["ventas", "Ventas"],
  ["cobros", "Cobros"],
  ["caja", "Caja"],
  ["transportes", "Transportes"],
  ["documentos", "Documentos"],
  ["reportes", "Reportes"],
  ["perfil", "Mi perfil"],
  ["usuarios", "Usuarios", "admin"],
  ["notificaciones", "Notificaciones"],
  ["auditoria", "Auditoria"]
];

const roles = [
  [1, "Administrador"], [2, "Vendedor"], [3, "Inventario"], [4, "Cobrador"], [5, "Gerente"], [6, "Solo lectura"]
];

const unidades = [
  [1, "Pieza"], [2, "Tabla"], [3, "Tablon"], [4, "Viga"], [5, "Metro cubico"], [6, "Pie tablar"], [99, "Otra"]
];

const metodosPago = [
  [1, "Efectivo"], [2, "Transferencia"], [3, "QR"], [4, "Tarjeta"], [5, "Credito"], [6, "Mixto"]
];

const estadosTransporte = [
  [1, "Programado"], [2, "En ruta"], [3, "Llegado"], [4, "Cancelado"]
];

const estadosRegistro = [[1, "Activo"], [2, "Inactivo"]];
const tiposCaja = [[1, "Ingreso"], [2, "Egreso"]];

const ventaEstados = {
  1: "Pendiente",
  2: "Pagada",
  3: "Parcial",
  4: "Anulada"
};

function isAdmin() {
  return Number(state.user?.rol) === 1 || state.user?.rol === "Administrador";
}

function visibleViews() {
  return views.filter(view => view[2] !== "admin" || isAdmin());
}

function rolLabel(value) {
  return (roles.find(([id]) => Number(id) === Number(value)) || [0, value])[1];
}

function money(value) {
  return Number(value || 0).toLocaleString("es-BO", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function date(value) {
  if (!value) return "-";
  return String(value).slice(0, 10);
}

function esc(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;");
}

function toast(message) {
  const el = document.createElement("div");
  el.className = "toast";
  el.textContent = message;
  document.body.appendChild(el);
  setTimeout(() => el.remove(), 3600);
}

async function api(path, options = {}) {
  const headers = options.headers || {};
  if (!(options.body instanceof FormData)) headers["Content-Type"] = "application/json";
  if (state.token) headers.Authorization = `Bearer ${state.token}`;

  const response = await fetch(`${normalizeApiBase(state.apiBase)}${path}`, { ...options, headers });
  const text = await response.text();
  let payload = null;

  try {
    payload = text ? JSON.parse(text) : null;
  } catch {
    payload = null;
  }

  if (response.status === 401) {
    logout();
    throw new Error("Sesion expirada");
  }

  if (!response.ok || payload?.success === false) {
    throw new Error(payload?.error || text || `Error HTTP ${response.status}`);
  }

  return payload?.data ?? payload;
}

async function safe(action, message = "Operacion completada") {
  try {
    const result = await action();
    if (message) toast(message);
    return result;
  } catch (error) {
    toast(error.message || "Error inesperado");
    throw error;
  }
}

function setView(view) {
  state.view = view;
  localStorage.setItem("sicomoro_view", view);
  render();
}

function logout() {
  state.token = "";
  state.user = null;
  localStorage.removeItem("sicomoro_token");
  localStorage.removeItem("sicomoro_user");
  render();
}

function options(items, selected) {
  return items.map(([value, label]) => `<option value="${value}" ${String(value) === String(selected ?? "") ? "selected" : ""}>${esc(label)}</option>`).join("");
}

function entityOptions(items, labelField = "nombre", selected = "") {
  return `<option value="">Seleccione</option>` + items.map(item => {
    const label = item[labelField] || item.nombreComercial || item.nombreRazonSocial || item.id;
    return `<option value="${item.id}" ${item.id === selected ? "selected" : ""}>${esc(label)}</option>`;
  }).join("");
}

function formData(form) {
  const data = {};
  new FormData(form).forEach((value, key) => {
    if (value === "") data[key] = null;
    else if (key.endsWith("Id") || key === "id") data[key] = value;
    else if (["cantidad", "precioCompra", "precioVentaSugerido", "stockMinimo", "largo", "ancho", "espesor", "costoTransporte", "otrosCostos", "precioUnitario", "descuento", "monto", "nuevoStock"].includes(key)) data[key] = Number(value);
    else if (["unidadMedida", "estado", "metodoPago", "tipo", "rol"].includes(key)) data[key] = Number(value);
    else data[key] = value;
  });
  return data;
}

function table(columns, rows, actions) {
  if (!rows.length) return `<div class="empty">Sin registros</div>`;
  const actionHead = actions ? "<th>Acciones</th>" : "";
  const body = rows.map(row => {
    const cells = columns.map(col => `<td>${col.render ? col.render(row) : esc(row[col.key])}</td>`).join("");
    const actionCell = actions ? `<td>${actions(row)}</td>` : "";
    return `<tr>${cells}${actionCell}</tr>`;
  }).join("");
  return `<div class="table-wrap"><table><thead><tr>${columns.map(col => `<th>${esc(col.label)}</th>`).join("")}${actionHead}</tr></thead><tbody>${body}</tbody></table></div>`;
}

async function loadCommon() {
  const [clientes, proveedores, productos, inventario, compras, ventas, deudas] = await Promise.all([
    api("/api/clientes"),
    api("/api/proveedores"),
    api("/api/productos"),
    api("/api/inventario"),
    api("/api/compras"),
    api("/api/ventas"),
    api("/api/cobros/deudas")
  ]);
  Object.assign(state.cache, { clientes, proveedores, productos, inventario, compras, ventas, deudas });
}

function renderLogin() {
  app.innerHTML = `
    <main class="login-page">
      <form class="login-card" id="loginForm" autocomplete="off">
        <h1>Sicomoro</h1>
        <p>Barraca de madera</p>
        <label>Email
          <input name="email" type="email" autocomplete="off" required>
        </label>
        <label>Password
          <input name="password" type="password" autocomplete="off" required>
        </label>
        <label>API
          <input name="apiBase" value="${esc(state.apiBase)}">
        </label>
        <div class="actions">
          <button class="primary" type="submit">Entrar</button>
        </div>
      </form>
    </main>
  `;
  document.getElementById("loginForm").onsubmit = async event => {
    event.preventDefault();
    const data = formData(event.currentTarget);
    state.apiBase = normalizeApiBase(data.apiBase || API_DEFAULT);
    localStorage.setItem("sicomoro_api", state.apiBase);
    await safe(async () => {
      const auth = await api("/api/auth/login", {
        method: "POST",
        body: JSON.stringify({ email: data.email, password: data.password })
      });
      state.token = auth.token;
      state.user = auth;
      localStorage.setItem("sicomoro_token", auth.token);
      localStorage.setItem("sicomoro_user", JSON.stringify(auth));
      await loadCommon();
      render();
    }, "Sesion iniciada");
  };
}

function renderShell(content, title) {
  app.innerHTML = `
    <div class="app-shell">
      <aside class="sidebar">
        <div class="brand">
          <h1>Sicomoro</h1>
          <span>${esc(state.user?.nombre || "Sistema")}</span>
        </div>
        <nav class="nav">
          ${visibleViews().map(([id, label]) => `<button class="${state.view === id ? "active" : ""}" data-view="${id}">${label}</button>`).join("")}
        </nav>
        <div class="session">
          <span>${esc(state.user?.email || "")}</span>
          <button class="ghost" id="logoutBtn">Salir</button>
        </div>
      </aside>
      <main class="main">
        <div class="topbar">
          <h2>${esc(title)}</h2>
          <div class="topbar-actions">
            <span class="badge">${esc(state.apiBase)}</span>
            <button id="refreshBtn">Actualizar</button>
          </div>
        </div>
        ${content}
      </main>
    </div>
  `;

  document.querySelectorAll("[data-view]").forEach(btn => btn.onclick = () => setView(btn.dataset.view));
  document.getElementById("logoutBtn").onclick = logout;
  document.getElementById("refreshBtn").onclick = async () => safe(async () => {
    await refreshView();
    render();
  }, "Datos actualizados");
}

async function refreshView() {
  if (["dashboard", "clientes", "proveedores", "productos", "inventario", "compras", "ventas", "cobros", "documentos", "reportes"].includes(state.view)) {
    await loadCommon();
  }
  if (state.view === "transportes") state.cache.transportes = await api("/api/transportes");
  if (state.view === "notificaciones") state.cache.notificaciones = await api("/api/notificaciones?soloNoLeidas=false");
  if (state.view === "auditoria") state.cache.auditoria = await api("/api/auditoria?take=100");
  if (state.view === "perfil") state.cache.perfil = await api("/api/usuarios/me");
  if (state.view === "usuarios" && isAdmin()) state.cache.usuarios = await api("/api/usuarios");
}

async function render() {
  if (!state.token) return renderLogin();
  if (state.view === "usuarios" && !isAdmin()) state.view = "perfil";
  await safe(refreshView, "");
  const renderer = {
    dashboard: renderDashboard,
    clientes: renderClientes,
    proveedores: renderProveedores,
    productos: renderProductos,
    inventario: renderInventario,
    compras: renderCompras,
    ventas: renderVentas,
    cobros: renderCobros,
    caja: renderCaja,
    transportes: renderTransportes,
    documentos: renderDocumentos,
    reportes: renderReportes,
    perfil: renderPerfil,
    usuarios: renderUsuarios,
    notificaciones: renderNotificaciones,
    auditoria: renderAuditoria
  }[state.view] || renderDashboard;
  renderer();
}

function renderDashboard() {
  const deuda = state.cache.deudas.reduce((sum, x) => sum + Number(x.saldoPendiente || 0), 0);
  const bajo = state.cache.inventario.filter(x => Number(x.stockActual) <= Number(x.stockMinimo));
  const ventas = state.cache.ventas.filter(x => x.estado !== 4);
  const vendido = ventas.reduce((sum, x) => sum + Number(x.total || 0), 0);
  renderShell(`
    <section class="kpi-grid">
      <div class="kpi"><span>Ventas registradas</span><strong>${ventas.length}</strong></div>
      <div class="kpi"><span>Total vendido</span><strong>${money(vendido)}</strong></div>
      <div class="kpi"><span>Deuda pendiente</span><strong>${money(deuda)}</strong></div>
      <div class="kpi"><span>Bajo stock</span><strong>${bajo.length}</strong></div>
    </section>
    <section class="layout">
      <div class="panel">
        <div class="panel-header"><h3>Inventario bajo</h3></div>
        ${table([
          { label: "Producto", render: x => esc(x.producto) },
          { label: "Stock", render: x => money(x.stockActual) },
          { label: "Minimo", render: x => money(x.stockMinimo) }
        ], bajo)}
      </div>
      <div class="panel">
        <div class="panel-header"><h3>Deudas</h3></div>
        ${table([
          { label: "Cliente", render: x => esc(findCliente(x.clienteId)?.nombreRazonSocial || x.clienteId) },
          { label: "Venta", key: "ventaId" },
          { label: "Saldo", render: x => money(x.saldoPendiente) },
          { label: "Estado", render: x => badge(x.estado === 4 ? "Vencido" : "Pendiente", x.estado === 4 ? "bad" : "warn") }
        ], state.cache.deudas)}
      </div>
    </section>
  `, "Panel");
}

function badge(text, type = "") {
  return `<span class="badge ${type}">${esc(text)}</span>`;
}

function findCliente(id) { return state.cache.clientes.find(x => x.id === id); }
function findProveedor(id) { return state.cache.proveedores.find(x => x.id === id); }
function findProducto(id) { return state.cache.productos.find(x => x.id === id); }
function productosActivos() { return state.cache.productos.filter(x => x.estado === 1); }

function renderClientes() {
  renderShell(`
    <section class="layout">
      <div class="panel">
        <div class="panel-header"><h3>Cliente</h3></div>
        <div class="panel-body">
          <form id="clienteForm" class="grid">
            <label class="full">Nombre o razon social<input name="nombreRazonSocial" required></label>
            <label>CI/NIT<input name="ciNit"></label>
            <label>Telefono<input name="telefono"></label>
            <label>Ciudad<input name="ciudad"></label>
            <label>Direccion<input name="direccion"></label>
            <label class="full">Notas<textarea name="notas"></textarea></label>
            <div class="actions full"><button class="primary">Guardar</button></div>
          </form>
        </div>
      </div>
      <div class="panel">
        <div class="panel-header"><h3>Clientes</h3></div>
        ${table([
          { label: "Nombre", key: "nombreRazonSocial" },
          { label: "CI/NIT", key: "ciNit" },
          { label: "Telefono", key: "telefono" },
          { label: "Ciudad", key: "ciudad" },
          { label: "Deuda", render: x => money(x.deudaTotal) }
        ], state.cache.clientes, row => `<button data-delete-cliente="${row.id}" class="danger">Inactivar</button>`)}
      </div>
    </section>
  `, "Clientes");
  document.getElementById("clienteForm").onsubmit = submitJson("/api/clientes", async () => { await loadCommon(); render(); });
  document.querySelectorAll("[data-delete-cliente]").forEach(btn => btn.onclick = () => safe(async () => {
    await api(`/api/clientes/${btn.dataset.deleteCliente}`, { method: "DELETE" });
    await loadCommon();
    render();
  }, "Cliente inactivado"));
}

function renderProveedores() {
  renderShell(`
    <section class="layout">
      <div class="panel">
        <div class="panel-header"><h3>Proveedor</h3></div>
        <div class="panel-body">
          <form id="proveedorForm" class="grid">
            <label class="full">Nombre<input name="nombre" required></label>
            <label>Lugar de origen<input name="lugarOrigen" value="Beni" required></label>
            <label>Telefono<input name="telefono"></label>
            <label class="full">Direccion<input name="direccion"></label>
            <label class="full">Tipo de madera<input name="tipoMadera"></label>
            <label class="full">Notas<textarea name="notas"></textarea></label>
            <div class="actions full"><button class="primary">Guardar</button></div>
          </form>
        </div>
      </div>
      <div class="panel">
        <div class="panel-header"><h3>Proveedores</h3></div>
        ${table([
          { label: "Nombre", key: "nombre" },
          { label: "Origen", key: "lugarOrigen" },
          { label: "Telefono", key: "telefono" },
          { label: "Madera", key: "tipoMadera" }
        ], state.cache.proveedores)}
      </div>
    </section>
  `, "Proveedores");
  document.getElementById("proveedorForm").onsubmit = submitJson("/api/proveedores", async () => { await loadCommon(); render(); });
}

function renderProductos() {
  renderShell(`
    <section class="layout">
      <div class="panel">
        <div class="panel-header"><h3>Producto</h3></div>
        <div class="panel-body">
          <form id="productoForm" class="grid">
            <input type="hidden" name="id">
            <label class="full">Nombre comercial<input name="nombreComercial" required></label>
            <label>Tipo de madera<input name="tipoMadera" value="Tajibo" required></label>
            <label>Unidad<select name="unidadMedida">${options(unidades, 1)}</select></label>
            <label>Largo<input name="largo" type="number" step="0.0001" value="2"></label>
            <label>Ancho<input name="ancho" type="number" step="0.0001" value="4"></label>
            <label>Espesor<input name="espesor" type="number" step="0.0001" value="0"></label>
            <label>Calidad<input name="calidad" value="A"></label>
            <label>Compra<input name="precioCompra" type="number" step="0.0001" value="35"></label>
            <label>Venta sugerida<input name="precioVentaSugerido" type="number" step="0.0001" value="55"></label>
            <label>Stock minimo<input name="stockMinimo" type="number" step="0.0001" value="10"></label>
            <label>Estado<select name="estado">${options(estadosRegistro, 1)}</select></label>
            <label class="full">Observaciones<textarea name="observaciones"></textarea></label>
            <div class="actions full">
              <button class="primary">Guardar</button>
              <button type="button" id="limpiarProducto">Nuevo</button>
            </div>
          </form>
        </div>
      </div>
      <div class="panel">
        <div class="panel-header"><h3>Productos</h3></div>
        ${table([
          { label: "Nombre", key: "nombreComercial" },
          { label: "Tipo", key: "tipoMadera" },
          { label: "Compra", render: x => money(x.precioCompra) },
          { label: "Venta", render: x => money(x.precioVentaSugerido) },
          { label: "Minimo", render: x => money(x.stockMinimo) },
          { label: "Estado", render: x => x.estado === 1 ? badge("Activo") : badge("Inactivo", "bad") }
        ], state.cache.productos, row => `
          <div class="split-actions">
            <button data-edit-producto="${row.id}">Editar</button>
            <button data-delete-producto="${row.id}" class="danger">Borrar</button>
          </div>
        `)}
      </div>
    </section>
  `, "Productos");

  const form = document.getElementById("productoForm");
  form.onsubmit = async event => {
    event.preventDefault();
    await safe(async () => {
      const data = formData(form);
      const id = data.id;
      delete data.id;
      if (id) await api(`/api/productos/${id}`, { method: "PUT", body: JSON.stringify(data) });
      else await api("/api/productos", { method: "POST", body: JSON.stringify(data) });
      await loadCommon();
      render();
    }, "Producto guardado");
  };
  document.getElementById("limpiarProducto").onclick = () => form.reset();
  document.querySelectorAll("[data-edit-producto]").forEach(btn => btn.onclick = () => fillForm(form, findProducto(btn.dataset.editProducto)));
  document.querySelectorAll("[data-delete-producto]").forEach(btn => btn.onclick = async () => {
    const producto = findProducto(btn.dataset.deleteProducto);
    if (!confirm(`Borrar definitivamente ${producto?.nombreComercial || "producto"}? Si tiene compras, ventas o movimientos, el sistema no lo borrara para proteger el historial.`)) return;
    await safe(async () => {
      await api(`/api/productos/${btn.dataset.deleteProducto}`, { method: "DELETE" });
      await loadCommon();
      render();
    }, "Producto eliminado");
  });
}

function renderInventario() {
  renderShell(`
    <section class="layout">
      <div class="panel">
        <div class="panel-header"><h3>Ajuste</h3></div>
        <div class="panel-body">
          <form id="inventarioForm" class="grid">
            <label class="full">Producto<select name="productoId" required>${entityOptions(productosActivos(), "nombreComercial")}</select></label>
            <label>Nuevo stock<input name="nuevoStock" type="number" step="0.0001" required></label>
            <label>Ubicacion<input name="ubicacionInterna" placeholder="A1"></label>
            <label class="full">Motivo<input name="motivo" value="Ajuste manual"></label>
            <div class="actions full"><button class="primary">Aplicar</button></div>
          </form>
        </div>
      </div>
      <div class="panel">
        <div class="panel-header"><h3>Stock</h3></div>
        ${table([
          { label: "Producto", key: "producto" },
          { label: "Stock", render: x => money(x.stockActual) },
          { label: "Minimo", render: x => money(x.stockMinimo) },
          { label: "Ubicacion", key: "ubicacionInterna" },
          { label: "Estado", render: x => Number(x.stockActual) <= Number(x.stockMinimo) ? badge("Bajo", "bad") : badge("OK") }
        ], state.cache.inventario)}
      </div>
    </section>
  `, "Inventario");
  document.getElementById("inventarioForm").onsubmit = submitJson("/api/inventario/ajuste", async () => { await loadCommon(); render(); });
}

function renderCompras() {
  renderShell(`
    <section class="layout">
      <div class="panel">
        <div class="panel-header"><h3>Compra</h3></div>
        <div class="panel-body">
          <form id="compraForm" class="grid">
            <label class="full">Proveedor<select name="proveedorId" required>${entityOptions(state.cache.proveedores, "nombre")}</select></label>
            <label>Origen<input name="origen" value="Beni" required></label>
            <label>Fecha compra<input name="fechaCompra" type="date" value="${today()}"></label>
            <label>Fecha llegada<input name="fechaEstimadaLlegada" type="date"></label>
            <label>Transporte<input name="costoTransporte" type="number" step="0.0001" value="0"></label>
            <label>Otros costos<input name="otrosCostos" type="number" step="0.0001" value="0"></label>
            <label class="full">Producto<select name="productoId" required>${entityOptions(productosActivos(), "nombreComercial")}</select></label>
            <label>Cantidad<input name="cantidad" type="number" step="0.0001" required></label>
            <label>Precio compra<input name="precioCompra" type="number" step="0.0001" required></label>
            <label class="full">Observaciones<input name="observaciones"></label>
            <div class="actions full"><button class="primary">Registrar</button></div>
          </form>
        </div>
      </div>
      <div class="panel">
        <div class="panel-header"><h3>Compras</h3></div>
        ${table([
          { label: "Proveedor", render: x => esc(findProveedor(x.proveedorId)?.nombre || x.proveedorId) },
          { label: "Origen", key: "origen" },
          { label: "Estado", render: x => badge(["", "Pendiente", "En transito", "Recibida", "Cancelada"][x.estado] || x.estado, x.estado === 3 ? "" : "warn") },
          { label: "Fecha", render: x => date(x.fechaCompra) },
          { label: "Total", render: x => money(x.totalProductos + x.costoTransporte + x.otrosCostos) }
        ], state.cache.compras, row => row.estado === 3 ? "" : `<button data-recibir-compra="${row.id}">Recibir</button>`)}
      </div>
    </section>
  `, "Compras");

  document.getElementById("compraForm").onsubmit = async event => {
    event.preventDefault();
    await safe(async () => {
      const data = formData(event.currentTarget);
      const body = {
        proveedorId: data.proveedorId,
        origen: data.origen,
        fechaCompra: toIsoDate(data.fechaCompra),
        fechaEstimadaLlegada: toIsoDate(data.fechaEstimadaLlegada),
        costoTransporte: data.costoTransporte || 0,
        otrosCostos: data.otrosCostos || 0,
        observaciones: data.observaciones,
        detalles: [{ productoId: data.productoId, cantidad: data.cantidad, precioCompra: data.precioCompra }]
      };
      await api("/api/compras", { method: "POST", body: JSON.stringify(body) });
      await loadCommon();
      render();
    }, "Compra registrada");
  };
  document.querySelectorAll("[data-recibir-compra]").forEach(btn => btn.onclick = () => safe(async () => {
    await api(`/api/compras/${btn.dataset.recibirCompra}/recibir`, { method: "PUT" });
    await loadCommon();
    render();
  }, "Compra recibida"));
}

function renderVentas() {
  renderShell(`
    <section class="layout">
      <div class="panel">
        <div class="panel-header"><h3>Venta</h3></div>
        <div class="panel-body">
          <form id="ventaForm" class="grid">
            <label class="full">Cliente<select name="clienteId" required>${entityOptions(state.cache.clientes, "nombreRazonSocial")}</select></label>
            <label>Metodo<select name="metodoPago">${options(metodosPago, 5)}</select></label>
            <label>Vencimiento<input name="fechaVencimiento" type="date"></label>
            <label class="full">Producto<select name="productoId" required>${entityOptions(productosActivos(), "nombreComercial")}</select></label>
            <label>Cantidad<input name="cantidad" type="number" step="0.0001" required></label>
            <label>Precio unitario<input name="precioUnitario" type="number" step="0.0001" required></label>
            <label>Descuento<input name="descuento" type="number" step="0.0001" value="0"></label>
            <label>Estrategia<select name="pricingStrategy"><option value="normal">Normal</option><option value="mayorista">Mayorista</option><option value="cliente-frecuente">Cliente frecuente</option><option value="descuento-manual">Descuento manual</option></select></label>
            <label class="full">Observaciones<input name="observaciones"></label>
            <div class="actions full"><button class="primary">Crear venta</button></div>
          </form>
        </div>
      </div>
      <div class="panel">
        <div class="panel-header"><h3>Ventas</h3></div>
        ${table([
          { label: "Cliente", render: x => esc(findCliente(x.clienteId)?.nombreRazonSocial || x.clienteId) },
          { label: "Fecha", render: x => date(x.fecha) },
          { label: "Estado", render: x => badge(ventaEstados[x.estado] || x.estado, x.estado === 4 ? "bad" : x.estado === 3 ? "warn" : "") },
          { label: "Total", render: x => money(x.total) },
          { label: "Saldo", render: x => money(x.saldoPendiente) }
        ], state.cache.ventas, row => `
          <div class="split-actions">
            ${row.estado === 1 ? `<button data-confirmar-venta="${row.id}">Confirmar</button>` : ""}
            ${row.estado !== 4 ? `<button data-anular-venta="${row.id}" class="danger">Anular</button>` : ""}
          </div>
        `)}
      </div>
    </section>
  `, "Ventas");

  document.getElementById("ventaForm").onsubmit = async event => {
    event.preventDefault();
    await safe(async () => {
      const data = formData(event.currentTarget);
      const body = {
        clienteId: data.clienteId,
        metodoPago: data.metodoPago,
        fechaVencimiento: toIsoDate(data.fechaVencimiento),
        observaciones: data.observaciones,
        detalles: [{
          productoId: data.productoId,
          cantidad: data.cantidad,
          precioUnitario: data.precioUnitario,
          descuento: data.descuento || 0,
          pricingStrategy: data.pricingStrategy || "normal"
        }]
      };
      await api("/api/ventas", { method: "POST", body: JSON.stringify(body) });
      await loadCommon();
      render();
    }, "Venta creada");
  };
  document.querySelectorAll("[data-confirmar-venta]").forEach(btn => btn.onclick = async () => {
    const monto = Number(prompt("Monto pagado", "0") || 0);
    await safe(async () => {
      await api(`/api/ventas/${btn.dataset.confirmarVenta}/confirmar`, { method: "PUT", body: JSON.stringify({ montoPagado: monto }) });
      await loadCommon();
      render();
    }, "Venta confirmada");
  });
  document.querySelectorAll("[data-anular-venta]").forEach(btn => btn.onclick = async () => {
    const motivo = prompt("Motivo", "Anulada desde frontend") || "Anulada";
    await safe(async () => {
      await api(`/api/ventas/${btn.dataset.anularVenta}/anular`, { method: "PUT", body: JSON.stringify({ motivo }) });
      await loadCommon();
      render();
    }, "Venta anulada");
  });
}

function renderCobros() {
  renderShell(`
    <section class="layout">
      <div class="panel">
        <div class="panel-header"><h3>Pago</h3></div>
        <div class="panel-body">
          <form id="pagoForm" class="grid">
            <label class="full">Cobro<select name="cobroId" required>${entityOptions(state.cache.deudas.map(d => ({ ...d, nombre: `${findCliente(d.clienteId)?.nombreRazonSocial || d.clienteId} - saldo ${money(d.saldoPendiente)}` })), "nombre")}</select></label>
            <label>Monto<input name="monto" type="number" step="0.0001" required></label>
            <label>Metodo<select name="metodoPago">${options(metodosPago, 1)}</select></label>
            <label class="full">Referencia<input name="referencia"></label>
            <div class="actions full"><button class="primary">Registrar pago</button></div>
          </form>
        </div>
      </div>
      <div class="panel">
        <div class="panel-header"><h3>Deudas</h3></div>
        ${table([
          { label: "Cliente", render: x => esc(findCliente(x.clienteId)?.nombreRazonSocial || x.clienteId) },
          { label: "Venta", key: "ventaId" },
          { label: "Total", render: x => money(x.montoTotal) },
          { label: "Saldo", render: x => money(x.saldoPendiente) },
          { label: "Vence", render: x => date(x.fechaVencimiento) }
        ], state.cache.deudas)}
      </div>
    </section>
  `, "Cobros");
  document.getElementById("pagoForm").onsubmit = submitJson("/api/cobros/pagos", async () => { await loadCommon(); render(); });
}

function renderCaja() {
  const desde = monthStart();
  const hasta = today();
  renderShell(`
    <section class="layout">
      <div class="panel">
        <div class="panel-header"><h3>Movimiento</h3></div>
        <div class="panel-body">
          <form id="cajaForm" class="grid">
            <label>Tipo<select name="tipo">${options(tiposCaja, 1)}</select></label>
            <label>Monto<input name="monto" type="number" step="0.0001" required></label>
            <label class="full">Concepto<input name="concepto" required></label>
            <div class="actions full"><button class="primary">Registrar</button></div>
          </form>
        </div>
      </div>
      <div class="panel">
        <div class="panel-header"><h3>Caja</h3><button id="loadCaja">Cargar mes</button></div>
        <div id="cajaResult" class="panel-body"></div>
      </div>
    </section>
  `, "Caja");
  document.getElementById("cajaForm").onsubmit = submitJson("/api/caja/movimientos", () => loadCaja(desde, hasta));
  document.getElementById("loadCaja").onclick = () => loadCaja(desde, hasta);
  loadCaja(desde, hasta);
}

async function loadCaja(desde, hasta) {
  const rows = await safe(() => api(`/api/caja/movimientos?desde=${desde}&hasta=${hasta}`), "");
  document.getElementById("cajaResult").innerHTML = table([
    { label: "Fecha", render: x => date(x.fecha) },
    { label: "Tipo", render: x => x.tipo === 1 ? badge("Ingreso") : badge("Egreso", "warn") },
    { label: "Monto", render: x => money(x.monto) },
    { label: "Concepto", key: "concepto" }
  ], rows || []);
}

function renderTransportes() {
  renderShell(`
    <section class="layout">
      <div class="panel">
        <div class="panel-header"><h3>Transporte</h3></div>
        <div class="panel-body">
          <form id="transporteForm" class="grid">
            <label>Camion<input name="camion"></label>
            <label>Chofer<input name="chofer"></label>
            <label>Placa<input name="placa"></label>
            <label>Origen<input name="lugarOrigen" value="Beni" required></label>
            <label>Salida<input name="fechaSalida" type="date"></label>
            <label>Llegada<input name="fechaLlegada" type="date"></label>
            <label>Costo<input name="costoTransporte" type="number" step="0.0001" value="0"></label>
            <label>Estado<select name="estado">${options(estadosTransporte, 1)}</select></label>
            <label class="full">Compra<select name="compraId">${entityOptions(state.cache.compras.map(c => ({ ...c, nombre: `${findProveedor(c.proveedorId)?.nombre || c.proveedorId} - ${date(c.fechaCompra)}` })), "nombre")}</select></label>
            <label class="full">Observaciones<input name="observaciones"></label>
            <div class="actions full"><button class="primary">Guardar</button></div>
          </form>
        </div>
      </div>
      <div class="panel">
        <div class="panel-header"><h3>Transportes</h3></div>
        ${table([
          { label: "Chofer", key: "chofer" },
          { label: "Placa", key: "placa" },
          { label: "Origen", key: "lugarOrigen" },
          { label: "Costo", render: x => money(x.costoTransporte) },
          { label: "Estado", render: x => badge((estadosTransporte.find(e => e[0] === x.estado) || [0, x.estado])[1], x.estado === 3 ? "" : "warn") }
        ], state.cache.transportes, row => `<button data-llegar-transporte="${row.id}">Llegado</button>`)}
      </div>
    </section>
  `, "Transportes");

  document.getElementById("transporteForm").onsubmit = async event => {
    event.preventDefault();
    await safe(async () => {
      const data = formData(event.currentTarget);
      data.fechaSalida = toIsoDate(data.fechaSalida);
      data.fechaLlegada = toIsoDate(data.fechaLlegada);
      await api("/api/transportes", { method: "POST", body: JSON.stringify(data) });
      state.cache.transportes = await api("/api/transportes");
      render();
    }, "Transporte guardado");
  };
  document.querySelectorAll("[data-llegar-transporte]").forEach(btn => btn.onclick = () => safe(async () => {
    await api(`/api/transportes/${btn.dataset.llegarTransporte}/estado`, { method: "PUT", body: JSON.stringify({ estado: 3, fechaLlegada: new Date().toISOString() }) });
    state.cache.transportes = await api("/api/transportes");
    render();
  }, "Transporte actualizado"));
}

function renderDocumentos() {
  renderShell(`
    <section class="layout">
      <div class="panel">
        <div class="panel-header"><h3>Documento de venta</h3></div>
        <div class="panel-body">
          <form id="documentoForm" class="grid">
            <label class="full">Venta<select name="ventaId" required>${entityOptions(state.cache.ventas.map(v => ({ ...v, nombre: `${findCliente(v.clienteId)?.nombreRazonSocial || v.clienteId} - ${money(v.total)}` })), "nombre")}</select></label>
            <div class="actions full"><button class="primary">Generar PDF</button></div>
          </form>
          <div id="documentoResult" class="empty"></div>
        </div>
      </div>
      <div class="panel">
        <div class="panel-header"><h3>Ventas</h3></div>
        ${table([
          { label: "Cliente", render: x => esc(findCliente(x.clienteId)?.nombreRazonSocial || x.clienteId) },
          { label: "Estado", render: x => ventaEstados[x.estado] || x.estado },
          { label: "Total", render: x => money(x.total) }
        ], state.cache.ventas)}
      </div>
    </section>
  `, "Documentos");
  document.getElementById("documentoForm").onsubmit = async event => {
    event.preventDefault();
    const data = formData(event.currentTarget);
    const documento = await safe(() => api(`/api/documentos/venta/${data.ventaId}/generar`, { method: "POST" }), "Documento generado");
    document.getElementById("documentoResult").innerHTML = `<strong>${esc(documento.numero)}</strong><br>${esc(documento.rutaArchivo)}`;
  };
}

function renderReportes() {
  renderShell(`
    <section class="layout">
      <div class="panel">
        <div class="panel-header"><h3>Rango</h3></div>
        <div class="panel-body">
          <form id="reportForm" class="grid">
            <label>Desde<input name="desde" type="date" value="${monthStart()}"></label>
            <label>Hasta<input name="hasta" type="date" value="${today()}"></label>
            <div class="actions full">
              <button class="primary">Ventas</button>
              <button type="button" id="reporteCaja">Caja</button>
              <button type="button" id="reporteBajo">Inventario bajo</button>
              <button type="button" id="reporteDeudores">Deudores</button>
            </div>
          </form>
        </div>
      </div>
      <div class="panel">
        <div class="panel-header"><h3>Resultado</h3></div>
        <div class="panel-body" id="reportResult"></div>
      </div>
    </section>
  `, "Reportes");
  const form = document.getElementById("reportForm");
  form.onsubmit = async event => {
    event.preventDefault();
    const data = formData(form);
    const r = await safe(() => api(`/api/reportes/ventas?desde=${data.desde}&hasta=${data.hasta}`), "");
    reportHtml(`<div class="kpi-grid"><div class="kpi"><span>Cantidad</span><strong>${r.cantidadVentas}</strong></div><div class="kpi"><span>Total</span><strong>${money(r.totalVentas)}</strong></div><div class="kpi"><span>Pagado</span><strong>${money(r.totalPagado)}</strong></div><div class="kpi"><span>Saldo</span><strong>${money(r.saldoPendiente)}</strong></div></div>`);
  };
  document.getElementById("reporteCaja").onclick = async () => {
    const data = formData(form);
    const r = await safe(() => api(`/api/reportes/caja?desde=${data.desde}&hasta=${data.hasta}`), "");
    reportHtml(`<div class="kpi-grid"><div class="kpi"><span>Ingresos</span><strong>${money(r.ingresos)}</strong></div><div class="kpi"><span>Egresos</span><strong>${money(r.egresos)}</strong></div><div class="kpi"><span>Saldo</span><strong>${money(r.saldo)}</strong></div></div>`);
  };
  document.getElementById("reporteBajo").onclick = async () => {
    const rows = await safe(() => api("/api/reportes/inventario-bajo"), "");
    reportHtml(table([{ label: "Producto", key: "producto" }, { label: "Stock", render: x => money(x.stockActual) }, { label: "Minimo", render: x => money(x.stockMinimo) }], rows));
  };
  document.getElementById("reporteDeudores").onclick = async () => {
    const rows = await safe(() => api("/api/reportes/clientes-deudores"), "");
    reportHtml(table([{ label: "Cliente", key: "nombreRazonSocial" }, { label: "Deuda", render: x => money(x.deudaTotal) }, { label: "Telefono", key: "telefono" }], rows));
  };
}

function reportHtml(html) {
  document.getElementById("reportResult").innerHTML = html;
}

function renderPerfil() {
  const perfil = state.cache.perfil || state.user || {};
  renderShell(`
    <section class="layout">
      <div class="panel">
        <div class="panel-header"><h3>Mi perfil</h3></div>
        <div class="panel-body">
          <form id="perfilForm" class="grid" autocomplete="off">
            <label class="full">Nombre completo<input name="nombre" value="${esc(perfil.nombre || "")}" required></label>
            <label>Email<input name="email" type="email" value="${esc(perfil.email || "")}" required></label>
            <label>CI/NIT<input name="ciNit" value="${esc(perfil.ciNit || "")}"></label>
            <label>Telefono<input name="telefono" value="${esc(perfil.telefono || "")}"></label>
            <label>Cargo<input name="cargo" value="${esc(perfil.cargo || rolLabel(perfil.rol))}"></label>
            <label class="full">Direccion<input name="direccion" value="${esc(perfil.direccion || "")}"></label>
            <label class="full">Notas<textarea name="notas">${esc(perfil.notas || "")}</textarea></label>
            <div class="actions full"><button class="primary">Guardar perfil</button></div>
          </form>
        </div>
      </div>
      <div class="panel">
        <div class="panel-header"><h3>Seguridad</h3></div>
        <div class="panel-body">
          <form id="passwordForm" class="grid" autocomplete="off">
            <label class="full">Contrasena actual<input name="passwordActual" type="password" required></label>
            <label>Nueva contrasena<input name="nuevaPassword" type="password" minlength="8" required></label>
            <label>Repetir contrasena<input name="confirmarPassword" type="password" minlength="8" required></label>
            <div class="actions full"><button class="primary">Cambiar contrasena</button></div>
          </form>
        </div>
      </div>
    </section>
  `, "Mi perfil");

  document.getElementById("perfilForm").onsubmit = async event => {
    event.preventDefault();
    await safe(async () => {
      const updated = await api("/api/usuarios/me", { method: "PUT", body: JSON.stringify(formData(event.currentTarget)) });
      state.cache.perfil = updated;
      state.user = { ...state.user, nombre: updated.nombre, email: updated.email, rol: updated.rol };
      localStorage.setItem("sicomoro_user", JSON.stringify(state.user));
      render();
    }, "Perfil actualizado");
  };

  document.getElementById("passwordForm").onsubmit = async event => {
    event.preventDefault();
    const data = formData(event.currentTarget);
    if (data.nuevaPassword !== data.confirmarPassword) {
      toast("Las contrasenas no coinciden");
      return;
    }

    await safe(async () => {
      await api("/api/usuarios/me/password", {
        method: "PUT",
        body: JSON.stringify({ passwordActual: data.passwordActual, nuevaPassword: data.nuevaPassword })
      });
      event.currentTarget.reset();
    }, "Contrasena actualizada");
  };
}

function renderUsuarios() {
  renderShell(`
    <section class="layout">
      <div class="panel">
        <div class="panel-header"><h3>Crear usuario</h3></div>
        <div class="panel-body">
          <form id="usuarioForm" class="grid" autocomplete="off">
            <label class="full">Clave de creacion<input name="claveCreacion" type="password" required></label>
            <label class="full">Nombre completo<input name="nombre" required></label>
            <label>Email<input name="email" type="email" required></label>
            <label>Contrasena<input name="password" type="password" minlength="8" required></label>
            <label>Rol<select name="rol">${options(roles, 6)}</select></label>
            <label>CI/NIT<input name="ciNit"></label>
            <label>Telefono<input name="telefono"></label>
            <label>Cargo<input name="cargo"></label>
            <label class="full">Direccion<input name="direccion"></label>
            <label class="full">Notas<textarea name="notas"></textarea></label>
            <div class="actions full"><button class="primary">Crear usuario</button></div>
          </form>
        </div>
      </div>
      <div class="panel">
        <div class="panel-header"><h3>Usuarios</h3></div>
        ${table([
          { label: "Nombre", key: "nombre" },
          { label: "Email", key: "email" },
          { label: "Rol", render: x => badge(rolLabel(x.rol), Number(x.rol) === 1 ? "warn" : "") },
          { label: "Telefono", key: "telefono" },
          { label: "Cargo", key: "cargo" }
        ], state.cache.usuarios, row => `
          <div class="split-actions">
            ${row.id === state.user?.usuarioId ? `<span class="badge">Tu cuenta</span>` : `<button data-delete-usuario="${row.id}" class="danger">Borrar</button>`}
          </div>
        `)}
      </div>
    </section>
  `, "Usuarios");

  document.getElementById("usuarioForm").onsubmit = async event => {
    event.preventDefault();
    await safe(async () => {
      await api("/api/usuarios", { method: "POST", body: JSON.stringify(formData(event.currentTarget)) });
      event.currentTarget.reset();
      state.cache.usuarios = await api("/api/usuarios");
      render();
    }, "Usuario creado");
  };

  document.querySelectorAll("[data-delete-usuario]").forEach(btn => btn.onclick = async () => {
    const usuario = state.cache.usuarios.find(x => x.id === btn.dataset.deleteUsuario);
    if (!confirm(`Borrar usuario ${usuario?.email || ""}? Esta accion no borra auditorias ni ventas ya registradas.`)) return;
    await safe(async () => {
      await api(`/api/usuarios/${btn.dataset.deleteUsuario}`, { method: "DELETE" });
      state.cache.usuarios = await api("/api/usuarios");
      render();
    }, "Usuario eliminado");
  });
}

function renderNotificaciones() {
  renderShell(`
    <section class="panel">
      <div class="panel-header"><h3>Notificaciones</h3></div>
      ${table([
        { label: "Fecha", render: x => date(x.creadoEn) },
        { label: "Tipo", key: "tipo" },
        { label: "Titulo", key: "titulo" },
        { label: "Mensaje", key: "mensaje" },
        { label: "Estado", render: x => x.leida ? badge("Leida") : badge("Nueva", "warn") }
      ], state.cache.notificaciones)}
    </section>
  `, "Notificaciones");
}

function renderAuditoria() {
  renderShell(`
    <section class="panel">
      <div class="panel-header"><h3>Auditoria</h3></div>
      ${table([
        { label: "Fecha", render: x => String(x.fechaHora).replace("T", " ").slice(0, 19) },
        { label: "Accion", key: "accion" },
        { label: "Entidad", key: "entidad" },
        { label: "EntidadId", key: "entidadId" },
        { label: "Usuario", key: "usuarioId" }
      ], state.cache.auditoria)}
    </section>
  `, "Auditoria");
}

function submitJson(path, after) {
  return async event => {
    event.preventDefault();
    await safe(async () => {
      await api(path, { method: "POST", body: JSON.stringify(formData(event.currentTarget)) });
      if (after) await after();
    });
  };
}

function fillForm(form, data) {
  Object.entries(data || {}).forEach(([key, value]) => {
    const input = form.elements[key];
    if (!input) return;
    input.value = value ?? "";
  });
}

function today() {
  return new Date().toISOString().slice(0, 10);
}

function monthStart() {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-01`;
}

function toIsoDate(value) {
  return value ? `${value}T00:00:00Z` : null;
}

render();
