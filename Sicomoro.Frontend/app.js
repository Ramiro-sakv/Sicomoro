const API_DEFAULT = window.SICOMORO_API_BASE
  || (["localhost", "127.0.0.1"].includes(window.location.hostname) ? "http://localhost:8080" : window.location.origin);
const APP_VERSION = "v1.8.0-inventario";
const OPERATION_KEY_HEADER = "X-Sicomoro-Operation-Key";
const MAX_CATALOG_IMAGE_FILE_SIZE = 8 * 1024 * 1024;
let deferredInstallPrompt = null;

function shouldUseMobileLayout() {
  const width = window.visualViewport?.width || window.innerWidth || document.documentElement.clientWidth || screen.width || 9999;
  const phoneOrTablet = /Android|iPhone|iPad|iPod/i.test(navigator.userAgent);
  const coarsePointer = window.matchMedia?.("(pointer: coarse)").matches === true;
  return width <= 820 || ((phoneOrTablet || coarsePointer) && width <= 1100);
}

function syncDeviceLayout() {
  document.documentElement.classList.toggle("mobile-device", shouldUseMobileLayout());
}

syncDeviceLayout();
window.addEventListener("resize", syncDeviceLayout);
window.visualViewport?.addEventListener("resize", syncDeviceLayout);

function normalizeApiBase(value) {
  return String(value || API_DEFAULT).trim().replace(/\/+$/, "");
}

const app = document.getElementById("app");
const state = {
  apiBase: normalizeApiBase(localStorage.getItem("sicomoro_api") || API_DEFAULT),
  token: "",
  user: null,
  clientToken: localStorage.getItem("sicomoro_client_token") || "",
  clientUser: JSON.parse(localStorage.getItem("sicomoro_client_user") || "null"),
  view: localStorage.getItem("sicomoro_view") || "dashboard",
  dashboardRange: localStorage.getItem("sicomoro_dashboard_range") || "mes",
  search: {},
  pages: {},
  selectedClienteId: "",
  selectedProveedorId: "",
  selectedProductoId: "",
  inventoryFilter: "todos",
  cache: {
    clientes: [],
    proveedores: [],
    productos: [],
    inventario: [],
    movimientosInventario: [],
    compras: [],
    ventas: [],
    deudas: [],
    transportes: [],
    notificaciones: [],
    auditoria: [],
    usuarios: [],
    catalogoAnuncios: [],
    perfil: null
  }
};

const views = [
  ["dashboard", "Panel", [1, 2, 3, 4, 5]],
  ["clientes", "Clientes", [1, 2, 4, 5]],
  ["proveedores", "Proveedores", [1, 3, 5]],
  ["productos", "Productos", [1, 3, 5]],
  ["inventario", "Inventario", [1, 3, 5]],
  ["compras", "Compras", [1, 3, 5]],
  ["ventas", "Ventas", [1, 2, 5]],
  ["cobros", "Cobros", [1, 4, 5]],
  ["caja", "Caja", [1, 4, 5]],
  ["transportes", "Transportes", [1, 3, 5]],
  ["documentos", "Documentos", [1, 2, 5]],
  ["reportes", "Reportes", [1, 5]],
  ["publicidad", "Publicidad", [1, 5]],
  ["perfil", "Mi perfil", [1, 2, 3, 4, 5, 6]],
  ["app", "App PC/movil", [1, 2, 3, 4, 5, 6]],
  ["usuarios", "Usuarios", [1]],
  ["notificaciones", "Notificaciones", [1, 2, 3, 4, 5]],
  ["auditoria", "Auditoria", [1, 5]]
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
const tiposMovimientoInventario = {
  1: "Entrada",
  2: "Venta",
  3: "Ajuste",
  4: "Perdida",
  5: "Devolucion",
  6: "Compra",
  7: "Reversion"
};

const ventaEstados = {
  1: "Pendiente",
  2: "Pagada",
  3: "Parcial",
  4: "Anulada"
};

function isAdmin() {
  return currentRole() === 1;
}

function isGestion() {
  return [1, 5].includes(currentRole());
}

function currentRole() {
  if (typeof state.user?.rol === "number") return state.user.rol;
  return (roles.find(([, label]) => label === state.user?.rol) || [6])[0];
}

function visibleViews() {
  const role = currentRole();
  return views.filter(([, , allowed]) => allowed.includes(role));
}

function canAccess(view) {
  return visibleViews().some(([id]) => id === view);
}

function currentPath() {
  return window.location.pathname.replace(/\/+$/, "") || "/";
}

function isPublicCatalogRoute() {
  return ["/", "/catalogo"].includes(currentPath());
}

function isStaffRoute() {
  return currentPath() === "/personal";
}

function navigatePath(path) {
  window.history.pushState(null, "", path);
  render();
}

function navigateStaffLogin() {
  state.token = "";
  state.user = null;
  localStorage.removeItem("sicomoro_token");
  localStorage.removeItem("sicomoro_user");
  navigatePath("/personal");
}

function authRole(auth) {
  if (typeof auth?.rol === "number") return auth.rol;
  return (roles.find(([, label]) => label === auth?.rol) || [6])[0];
}

function rolLabel(value) {
  return (roles.find(([id]) => Number(id) === Number(value)) || [0, value])[1];
}

function money(value) {
  return Number(value || 0).toLocaleString("es-BO", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function numberText(value) {
  if (value == null || value === "") return "";
  const number = Number(value);
  if (!Number.isFinite(number) || number === 0) return "";
  return String(Number(number.toFixed(4)));
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

window.addEventListener("beforeinstallprompt", event => {
  event.preventDefault();
  deferredInstallPrompt = event;
  if (state.view === "app") render();
});

window.addEventListener("appinstalled", () => {
  deferredInstallPrompt = null;
  toast("App instalada");
  if (state.view === "app") render();
});

function isStandaloneApp() {
  return window.matchMedia("(display-mode: standalone)").matches || window.navigator.standalone === true;
}

function canUseInstallPrompt() {
  return Boolean(deferredInstallPrompt) && !isStandaloneApp();
}

function registerServiceWorker() {
  if (!("serviceWorker" in navigator)) return;
  window.addEventListener("load", () => {
    navigator.serviceWorker.register("/service-worker.js").catch(() => {
      console.warn("No se pudo registrar el service worker de Sicomoro.");
    });
  });
}

async function api(path, options = {}) {
  const headers = options.headers || {};
  if (!(options.body instanceof FormData)) headers["Content-Type"] = "application/json";
  if (state.token && !options.skipAuth) headers.Authorization = `Bearer ${state.token}`;

  const { skipAuth, ...fetchOptions } = options;
  const response = await fetch(`${normalizeApiBase(state.apiBase)}${path}`, { ...fetchOptions, headers });
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
    if (message && result !== false) toast(message);
    return result;
  } catch (error) {
    toast(error.message || "Error inesperado");
    throw error;
  }
}

async function safeApi(path, fallback = []) {
  try {
    return await api(path);
  } catch (error) {
    if (["Error HTTP 403", "Error HTTP 404"].includes(error.message)) return fallback;
    throw error;
  }
}

function requestOperationKey(actionName = "borrar") {
  return new Promise(resolve => {
    const backdrop = document.createElement("div");
    backdrop.className = "modal-backdrop";
    backdrop.innerHTML = `
      <div class="modal-card" role="dialog" aria-modal="true">
        <h3>Codigo de seguridad</h3>
        <p>Ingresa la clave para ${esc(actionName)}. El codigo no se mostrara en pantalla.</p>
        <input name="operationKey" type="password" autocomplete="one-time-code" inputmode="numeric" placeholder="********">
        <div class="actions">
          <button type="button" class="primary" data-confirm-key>Confirmar</button>
          <button type="button" data-cancel-key>Cancelar</button>
        </div>
      </div>
    `;
    document.body.appendChild(backdrop);

    const input = backdrop.querySelector("[name='operationKey']");
    const close = value => {
      backdrop.remove();
      if (!value) toast("Operacion cancelada");
      resolve(value ? value.trim() : null);
    };

    backdrop.querySelector("[data-confirm-key]").onclick = () => close(input.value);
    backdrop.querySelector("[data-cancel-key]").onclick = () => close(null);
    backdrop.onclick = event => {
      if (event.target === backdrop) close(null);
    };
    input.onkeydown = event => {
      if (event.key === "Enter") close(input.value);
      if (event.key === "Escape") close(null);
    };
    setTimeout(() => input.focus(), 0);
  });
}

function requestInputModal({ title, message, label, type = "text", value = "", placeholder = "", required = false }) {
  return new Promise(resolve => {
    const backdrop = document.createElement("div");
    backdrop.className = "modal-backdrop";
    backdrop.innerHTML = `
      <div class="modal-card" role="dialog" aria-modal="true">
        <h3>${esc(title)}</h3>
        ${message ? `<p>${esc(message)}</p>` : ""}
        <label>${esc(label)}<input name="modalValue" type="${esc(type)}" value="${esc(value)}" placeholder="${esc(placeholder)}" ${required ? "required" : ""}></label>
        <div class="actions">
          <button type="button" class="primary" data-confirm-modal>Confirmar</button>
          <button type="button" data-cancel-modal>Cancelar</button>
        </div>
      </div>
    `;
    document.body.appendChild(backdrop);

    const input = backdrop.querySelector("[name='modalValue']");
    const close = confirmed => {
      const result = confirmed ? input.value : null;
      backdrop.remove();
      if (!confirmed) toast("Operacion cancelada");
      resolve(result);
    };

    backdrop.querySelector("[data-confirm-modal]").onclick = () => {
      if (required && !input.value.trim()) {
        toast("Completa el campo requerido.");
        return;
      }
      close(true);
    };
    backdrop.querySelector("[data-cancel-modal]").onclick = () => close(false);
    backdrop.onclick = event => {
      if (event.target === backdrop) close(false);
    };
    input.onkeydown = event => {
      if (event.key === "Enter") close(true);
      if (event.key === "Escape") close(false);
    };
    setTimeout(() => input.focus(), 0);
  });
}

async function deleteWithOperationKey(path, actionName = "borrar") {
  const claveOperacion = await requestOperationKey(actionName);
  if (!claveOperacion) return false;
  await api(path, { method: "DELETE", headers: { [OPERATION_KEY_HEADER]: claveOperacion } });
  return true;
}

function setView(view) {
  if (!isStaffRoute()) window.history.pushState(null, "", "/personal");
  if (!canAccess(view)) view = visibleViews()[0]?.[0] || "perfil";
  state.view = view;
  localStorage.setItem("sicomoro_view", view);
  render();
}

function logout() {
  state.token = "";
  state.user = null;
  localStorage.removeItem("sicomoro_token");
  localStorage.removeItem("sicomoro_user");
  if (!isStaffRoute()) window.history.replaceState(null, "", "/personal");
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
    const cells = columns.map(col => `<td data-label="${esc(col.label)}">${col.render ? col.render(row) : esc(row[col.key])}</td>`).join("");
    const actionCell = actions ? `<td data-label="Acciones" class="actions-cell">${actions(row)}</td>` : "";
    return `<tr>${cells}${actionCell}</tr>`;
  }).join("");
  return `<div class="table-wrap"><table><thead><tr>${columns.map(col => `<th>${esc(col.label)}</th>`).join("")}${actionHead}</tr></thead><tbody>${body}</tbody></table></div>`;
}

function buildSalesBuckets(range, ventas) {
  const now = new Date();
  const buckets = [];
  const monthNames = ["Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic"];

  if (range === "semana") {
    for (let i = 6; i >= 0; i--) {
      const d = new Date(now.getFullYear(), now.getMonth(), now.getDate() - i);
      buckets.push({ label: `${String(d.getDate()).padStart(2, "0")}/${String(d.getMonth() + 1).padStart(2, "0")}`, key: dateKey(d), total: 0, count: 0 });
    }
    ventas.forEach(venta => {
      const bucket = buckets.find(x => x.key === dateKey(new Date(venta.fecha)));
      if (bucket) addSaleToBucket(bucket, venta);
    });
  } else if (range === "anio") {
    for (let month = 0; month < 12; month++) {
      buckets.push({ label: monthNames[month], year: now.getFullYear(), month, total: 0, count: 0 });
    }
    ventas.forEach(venta => {
      const d = new Date(venta.fecha);
      const bucket = buckets.find(x => x.year === d.getFullYear() && x.month === d.getMonth());
      if (bucket) addSaleToBucket(bucket, venta);
    });
  } else {
    const daysInMonth = new Date(now.getFullYear(), now.getMonth() + 1, 0).getDate();
    const weeks = Math.ceil(daysInMonth / 7);
    for (let week = 1; week <= weeks; week++) buckets.push({ label: `Sem ${week}`, week, total: 0, count: 0 });
    ventas.forEach(venta => {
      const d = new Date(venta.fecha);
      if (d.getFullYear() !== now.getFullYear() || d.getMonth() !== now.getMonth()) return;
      const week = Math.ceil(d.getDate() / 7);
      const bucket = buckets.find(x => x.week === week);
      if (bucket) addSaleToBucket(bucket, venta);
    });
  }

  return buckets;
}

function addSaleToBucket(bucket, venta) {
  bucket.total += Number(venta.total || 0);
  bucket.count += 1;
}

function dateKey(value) {
  return `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, "0")}-${String(value.getDate()).padStart(2, "0")}`;
}

function renderSalesChart(buckets) {
  const max = Math.max(...buckets.map(x => x.total), 1);
  const total = buckets.reduce((sum, x) => sum + x.total, 0);
  const count = buckets.reduce((sum, x) => sum + x.count, 0);
  const best = buckets.reduce((winner, x) => x.total > winner.total ? x : winner, buckets[0] || { label: "-", total: 0 });
  const activeBuckets = buckets.filter(x => x.total > 0).length || 1;
  const promedio = total / activeBuckets;

  return `
    <div class="chart-summary">
      <div><span>Total del periodo</span><strong>${money(total)}</strong></div>
      <div><span>Ventas</span><strong>${count}</strong></div>
      <div><span>Mejor periodo</span><strong>${esc(best.label)} / ${money(best.total)}</strong></div>
      <div><span>Promedio activo</span><strong>${money(promedio)}</strong></div>
    </div>
    <div class="sales-chart">
      ${buckets.map(bucket => `
        <div class="chart-column" title="${esc(bucket.label)} - ${money(bucket.total)}">
          <div class="chart-track">
            <div class="chart-bar" style="height:${Math.max(4, Math.round(bucket.total / max * 100))}%"></div>
          </div>
          <span>${esc(bucket.label)}</span>
          <strong>${money(bucket.total)}</strong>
        </div>
      `).join("")}
    </div>
  `;
}

function searchBox(key, placeholder = "Buscar") {
  return `<input class="search-input" id="${key}Search" value="${esc(state.search[key] || "")}" placeholder="${esc(placeholder)}">`;
}

function filterRows(key, rows, fields) {
  const term = String(state.search[key] || "").trim().toLowerCase();
  if (!term) return rows;
  return rows.filter(row => fields.some(field => String(typeof field === "function" ? field(row) : row[field] || "").toLowerCase().includes(term)));
}

function paginate(key, rows, pageSize = 20) {
  const pages = Math.max(1, Math.ceil(rows.length / pageSize));
  const page = Math.min(Math.max(Number(state.pages[key] || 1), 1), pages);
  state.pages[key] = page;
  return {
    rows: rows.slice((page - 1) * pageSize, page * pageSize),
    page,
    pages,
    total: rows.length
  };
}

function pager(key, page) {
  if (page.pages <= 1) return `<div class="pager"><span>${page.total} registros</span></div>`;
  return `
    <div class="pager">
      <span>${page.total} registros - pagina ${page.page} de ${page.pages}</span>
      <div>
        <button data-page-key="${key}" data-page-value="${page.page - 1}" ${page.page <= 1 ? "disabled" : ""}>Anterior</button>
        <button data-page-key="${key}" data-page-value="${page.page + 1}" ${page.page >= page.pages ? "disabled" : ""}>Siguiente</button>
      </div>
    </div>
  `;
}

function wireListControls(key, renderer) {
  const search = document.getElementById(`${key}Search`);
  if (search) {
    search.oninput = () => {
      state.search[key] = search.value;
      state.pages[key] = 1;
      renderer();
    };
  }

  document.querySelectorAll(`[data-page-key="${key}"]`).forEach(btn => btn.onclick = () => {
    state.pages[key] = Number(btn.dataset.pageValue);
    renderer();
  });
}

function downloadCsv(filename, rows, columns) {
  const header = columns.map(x => x.label).join(",");
  const body = rows.map(row => columns.map(col => csvCell(col.value ? col.value(row) : row[col.key])).join(",")).join("\n");
  const blob = new Blob([`${header}\n${body}`], { type: "text/csv;charset=utf-8" });
  const link = document.createElement("a");
  link.href = URL.createObjectURL(blob);
  link.download = filename;
  link.click();
  URL.revokeObjectURL(link.href);
}

function downloadTextFile(filename, content, type = "text/plain;charset=utf-8") {
  const blob = new Blob([content], { type });
  const link = document.createElement("a");
  link.href = URL.createObjectURL(blob);
  link.download = filename;
  link.click();
  URL.revokeObjectURL(link.href);
}

function downloadDesktopShortcut() {
  const appUrl = `${window.location.origin}/personal`;
  const iconUrl = `${window.location.origin}/icons/icon-192.png`;
  downloadTextFile("Sicomoro.url", `[InternetShortcut]\r\nURL=${appUrl}\r\nIconFile=${iconUrl}\r\nIconIndex=0\r\n`, "application/octet-stream");
}

async function downloadFile(path, filename = "documento.pdf") {
  const headers = {};
  if (state.token) headers.Authorization = `Bearer ${state.token}`;
  const response = await fetch(`${normalizeApiBase(state.apiBase)}${path}`, { headers });
  if (response.status === 401) {
    logout();
    throw new Error("Sesion expirada");
  }
  if (!response.ok) throw new Error(`No se pudo descargar el archivo (${response.status}).`);
  const disposition = response.headers.get("content-disposition") || "";
  const match = disposition.match(/filename\*?=(?:UTF-8'')?"?([^";]+)"?/i);
  const blob = await response.blob();
  const link = document.createElement("a");
  link.href = URL.createObjectURL(blob);
  link.download = decodeURIComponent(match?.[1] || filename);
  link.click();
  URL.revokeObjectURL(link.href);
}

function csvCell(value) {
  return `"${String(value ?? "").replaceAll('"', '""')}"`;
}

function lineInput(row, name) {
  return row.querySelector(`[name="${name}"]`);
}

function measureInput(row, name) {
  return row.querySelector(`[name="${name}"]`);
}

function findInventario(productoId) {
  return state.cache.inventario.find(x => x.productoId === productoId);
}

const MEASURE_BLOCK_TITLE = "Medidas de madera:";

function stripMeasurementBlock(value = "") {
  const text = String(value || "");
  const marker = `\n\n${MEASURE_BLOCK_TITLE}\n`;
  const index = text.indexOf(marker);
  if (index >= 0) return text.slice(0, index).trim();
  const directIndex = text.indexOf(MEASURE_BLOCK_TITLE);
  if (directIndex >= 0) return text.slice(0, directIndex).trim();
  return text.trim();
}

function readVentaDetalles(form) {
  const detalles = Array.from(form.querySelectorAll("[data-venta-line]")).map(row => {
    calculateVentaLine(row);
    return {
      productoId: lineInput(row, "productoId")?.value,
      cantidad: Number(lineInput(row, "cantidad")?.value || 0),
      precioUnitario: Number(lineInput(row, "precioUnitario")?.value || 0),
      descuento: Number(lineInput(row, "descuento")?.value || 0),
      pricingStrategy: lineInput(row, "pricingStrategy")?.value || "normal"
    };
  }).filter(x => x.productoId && x.cantidad > 0);
  if (!detalles.length) throw new Error("Agrega al menos un producto a la venta.");
  return detalles;
}

function readCompraDetalles(form) {
  const detalles = Array.from(form.querySelectorAll("[data-compra-line]")).map(row => ({
    productoId: lineInput(row, "productoId")?.value,
    cantidad: Number(lineInput(row, "cantidad")?.value || 0),
    precioCompra: Number(lineInput(row, "precioCompra")?.value || 0)
  })).filter(x => x.productoId && x.cantidad > 0);
  if (!detalles.length) throw new Error("Agrega al menos un producto a la compra.");
  return detalles;
}

function wireLineItems(containerId, buttonId, rowHtml, priceField) {
  const container = document.getElementById(containerId);
  const button = document.getElementById(buttonId);
  if (!container || !button) return;

  const wire = () => {
    container.querySelectorAll("[data-remove-line]").forEach(btn => btn.onclick = () => {
      if (container.children.length > 1) btn.closest(".line-item").remove();
      updateVentaSummary();
    });
    container.querySelectorAll("[data-product-select]").forEach(select => select.onchange = () => fillLinePrice(select, priceField));
    container.querySelectorAll("[data-venta-line]").forEach(wireVentaLineCalculator);
    updateVentaSummary();
  };

  button.onclick = () => {
    container.insertAdjacentHTML("beforeend", rowHtml());
    wire();
  };
  wire();
}

function fillLinePrice(select, field) {
  const producto = findProducto(select.value);
  const row = select.closest(".line-item");
  const input = row?.querySelector(`[name="${field}"]`);
  if (producto && input && !Number(input.value)) input.value = Number(field === "precioCompra" ? producto.precioCompra : producto.precioVentaSugerido || 0);
  if (row?.matches("[data-venta-line]")) fillVentaLineDimensions(row, producto);
}

function fillVentaLineDimensions(row, producto) {
  if (!producto) return;
  row.querySelectorAll("[data-measure-row]").forEach(measureRow => {
    const largo = measureInput(measureRow, "largoPies");
    const ancho = measureInput(measureRow, "anchoPulgadas");
    const espesor = measureInput(measureRow, "espesorPulgadas");
    if (largo && !Number(largo.value)) largo.value = numberText(producto.largo);
    if (ancho && !Number(ancho.value)) ancho.value = numberText(producto.ancho);
    if (espesor && !Number(espesor.value)) espesor.value = numberText(producto.espesor);
  });
  calculateVentaLine(row);
}

function wireVentaLineCalculator(row) {
  if (row.dataset.calculatorReady !== "1") {
    row.dataset.calculatorReady = "1";
    ["cantidad", "precioUnitario", "descuento", "pricingStrategy"].forEach(name => {
      lineInput(row, name)?.addEventListener("input", () => calculateVentaLine(row));
      lineInput(row, name)?.addEventListener("change", () => calculateVentaLine(row));
    });
    row.querySelector("[data-add-measure]")?.addEventListener("click", () => {
      const list = row.querySelector("[data-measure-list]");
      list?.insertAdjacentHTML("beforeend", measureRowHtml());
      fillVentaLineDimensions(row, findProducto(lineInput(row, "productoId")?.value));
      wireVentaLineCalculator(row);
      calculateVentaLine(row);
    });
  }
  row.querySelectorAll("[data-measure-row]").forEach(measureRow => {
    if (measureRow.dataset.measureReady === "1") return;
    measureRow.dataset.measureReady = "1";
    ["piezas", "largoPies", "anchoPulgadas", "espesorPulgadas"].forEach(name => {
      const input = measureInput(measureRow, name);
      input?.addEventListener("input", () => {
        measureRow.dataset.touched = "1";
        calculateVentaLine(row);
      });
      input?.addEventListener("change", () => {
        measureRow.dataset.touched = "1";
        calculateVentaLine(row);
      });
    });
    measureRow.querySelector("[data-remove-measure]")?.addEventListener("click", () => {
      const rows = row.querySelectorAll("[data-measure-row]");
      if (rows.length > 1) measureRow.remove();
      else measureRow.querySelectorAll("input").forEach(input => input.value = "");
      calculateVentaLine(row);
    });
  });
  fillVentaLineDimensions(row, findProducto(lineInput(row, "productoId")?.value));
  calculateVentaLine(row);
}

function calculateMeasureVolume(measureRow) {
  const piezas = Number(measureInput(measureRow, "piezas")?.value || 0);
  const largoPies = Number(measureInput(measureRow, "largoPies")?.value || 0);
  const anchoPulgadas = Number(measureInput(measureRow, "anchoPulgadas")?.value || 0);
  const espesorPulgadas = Number(measureInput(measureRow, "espesorPulgadas")?.value || 0);
  const completo = piezas > 0 && largoPies > 0 && anchoPulgadas > 0 && espesorPulgadas > 0;
  const volumen = completo ? (piezas * largoPies * anchoPulgadas * espesorPulgadas) / 12 : 0;
  const result = measureRow.querySelector("[data-measure-volume]");
  if (result) result.textContent = completo ? `${money(volumen)} PT` : "-";
  return { piezas, largoPies, anchoPulgadas, espesorPulgadas, volumen, completo };
}

function ventaLineMeasures(row) {
  return Array.from(row.querySelectorAll("[data-measure-row]")).map(calculateMeasureVolume);
}

function ventaLinePieces(row) {
  return ventaLineMeasures(row).reduce((sum, item) => sum + (item.completo ? item.piezas : 0), 0);
}

function calculateVentaLine(row) {
  const cantidadInput = lineInput(row, "cantidad");
  const measures = ventaLineMeasures(row);
  const totalPies = measures.reduce((sum, item) => sum + item.volumen, 0);
  const anyTouched = Array.from(row.querySelectorAll("[data-measure-row]")).some(measureRow => measureRow.dataset.touched === "1");

  if (cantidadInput && totalPies > 0) {
    cantidadInput.value = totalPies.toFixed(4);
  } else if (cantidadInput && anyTouched) {
    cantidadInput.value = "";
  }

  const cantidad = Number(cantidadInput?.value || 0);
  const precio = Number(lineInput(row, "precioUnitario")?.value || 0);
  const descuento = Number(lineInput(row, "descuento")?.value || 0);
  const total = Math.max(0, cantidad * precio - descuento);
  const productoId = lineInput(row, "productoId")?.value;
  const stockActual = Number(findInventario(productoId)?.stockActual ?? 0);
  const stockEl = row.querySelector("[data-line-stock]");
  if (stockEl) {
    const superaStock = productoId && cantidad > stockActual;
    stockEl.innerHTML = productoId
      ? `<span>Stock disponible</span><strong>${money(stockActual)}</strong>${superaStock ? badge("Supera stock", "bad") : badge("OK")}`
      : `<span>Selecciona producto</span><strong>-</strong>`;
    stockEl.classList.toggle("stock-warning", Boolean(superaStock));
  }
  const result = row.querySelector("[data-line-total]");
  if (result) {
    result.textContent = cantidad > 0 || precio > 0
      ? `${money(cantidad)} pies tablares / Bs ${money(total)}`
      : "Complete medidas para calcular";
  }
  updateVentaSummary();
}

function ventaTotalsFromForm(form = document.getElementById("ventaForm")) {
  if (!form) return { lineas: 0, piezas: 0, cantidad: 0, subtotal: 0, descuento: 0, total: 0, stockIssues: 0 };
  return Array.from(form.querySelectorAll("[data-venta-line]")).reduce((acc, row) => {
    const productoId = lineInput(row, "productoId")?.value;
    const piezas = ventaLinePieces(row);
    const cantidad = Number(lineInput(row, "cantidad")?.value || 0);
    const precio = Number(lineInput(row, "precioUnitario")?.value || 0);
    const descuento = Number(lineInput(row, "descuento")?.value || 0);
    const stockActual = Number(findInventario(productoId)?.stockActual ?? 0);
    const bruto = cantidad * precio;
    if (productoId && cantidad > 0) acc.lineas += 1;
    acc.piezas += piezas;
    acc.cantidad += cantidad;
    acc.subtotal += bruto;
    acc.descuento += descuento;
    acc.total += Math.max(0, bruto - descuento);
    if (productoId && cantidad > stockActual) acc.stockIssues += 1;
    return acc;
  }, { lineas: 0, piezas: 0, cantidad: 0, subtotal: 0, descuento: 0, total: 0, stockIssues: 0 });
}

function updateVentaSummary() {
  const form = document.getElementById("ventaForm");
  if (!form) return;
  const totals = ventaTotalsFromForm(form);
  const pagado = Number(document.getElementById("ventaMontoPagado")?.value || 0);
  const saldo = Math.max(0, totals.total - pagado);
  const set = (id, value) => {
    const el = document.getElementById(id);
    if (el) el.textContent = value;
  };
  set("ventaTotalPreview", money(totals.total));
  set("ventaPiesPreview", money(totals.cantidad));
  set("ventaPiezasPreview", money(totals.piezas));
  set("ventaDescuentoPreview", money(totals.descuento));
  set("ventaSaldoPreview", money(saldo));
  set("ventaLineasPreview", String(totals.lineas));
  const stockBox = document.getElementById("ventaStockPreview");
  if (stockBox) {
    stockBox.innerHTML = totals.stockIssues
      ? `${badge(`${totals.stockIssues} linea(s) sin stock`, "bad")}`
      : `${badge("Stock OK")}`;
  }
}

function buildVentaMeasurementBlock(form) {
  const lines = Array.from(form.querySelectorAll("[data-venta-line]")).map((row, index) => {
    const producto = findProducto(lineInput(row, "productoId")?.value);
    const measures = ventaLineMeasures(row).filter(x => x.completo);
    if (!producto || !measures.length) return "";
    const detail = measures
      .map(x => `${x.piezas} pza x ${x.anchoPulgadas}" x ${x.espesorPulgadas}" x ${x.largoPies}' = ${money(x.volumen)} PT`)
      .join("; ");
    const total = Number(lineInput(row, "cantidad")?.value || 0);
    return `${index + 1}. ${producto.nombreComercial}: ${detail}. Total ${money(total)} PT.`;
  }).filter(Boolean);
  return lines.length ? `${MEASURE_BLOCK_TITLE}\n${lines.join("\n")}` : "";
}

function ventaObservacionesConMedidas(rawObservaciones, form) {
  const base = stripMeasurementBlock(rawObservaciones);
  const block = buildVentaMeasurementBlock(form);
  return [base, block].filter(Boolean).join("\n\n");
}

function compraLineHtml(detail = {}) {
  return `
    <div class="line-item" data-compra-line>
      <label class="line-product">Producto<select name="productoId" data-product-select required>${entityOptions(productosActivos(), "nombreComercial", detail.productoId)}</select></label>
      <label>Cantidad / pies tablares<input name="cantidad" type="number" step="0.0001" value="${esc(detail.cantidad ?? "")}" required></label>
      <label>Precio compra por PT<input name="precioCompra" type="number" step="0.0001" value="${esc(detail.precioCompra ?? "")}" placeholder="12.5" required></label>
      <button type="button" class="danger" data-remove-line>Quitar</button>
    </div>
  `;
}

function measureRowHtml(detail = {}) {
  return `
    <div class="measure-row" data-measure-row>
      <label>Piezas<input name="piezas" type="number" step="1" min="0" placeholder="4" value="${esc(detail.piezas ?? "")}"></label>
      <label>Ancho pulg.<input name="anchoPulgadas" type="number" step="0.0001" min="0" placeholder="3" value="${esc(detail.anchoPulgadas ?? "")}"></label>
      <label>Espesor pulg.<input name="espesorPulgadas" type="number" step="0.0001" min="0" placeholder="2" value="${esc(detail.espesorPulgadas ?? "")}"></label>
      <label>Largo pies<input name="largoPies" type="number" step="0.0001" min="0" placeholder="10" value="${esc(detail.largoPies ?? "")}"></label>
      <div class="measure-volume" data-measure-volume>-</div>
      <button type="button" class="ghost danger compact" data-remove-measure>Quitar</button>
    </div>
  `;
}

function ventaLineHtml(detail = {}) {
  return `
    <div class="line-item" data-venta-line>
      <div class="venta-line-main">
        <label class="line-product">Producto<select name="productoId" data-product-select required>${entityOptions(productosActivos(), "nombreComercial", detail.productoId)}</select></label>
        <label>Total PT<input name="cantidad" type="number" step="0.0001" value="${esc(detail.cantidad ?? "")}" readonly required></label>
        <label>Precio por PT<input name="precioUnitario" type="number" step="0.0001" value="${esc(detail.precioUnitario ?? "")}" required></label>
        <label>Descuento<input name="descuento" type="number" step="0.0001" value="${esc(detail.descuento ?? 0)}"></label>
        <label>Estrategia<select name="pricingStrategy"><option value="normal">Normal</option><option value="mayorista">Mayorista</option><option value="cliente-frecuente">Cliente frecuente</option><option value="descuento-manual">Descuento manual</option></select></label>
      </div>
      <div class="measure-section">
        <div class="measure-header">
          <div><strong>Planilla de medidas</strong><span>piezas x ancho x espesor x largo / 12</span></div>
          <button type="button" class="secondary compact" data-add-measure>Agregar medida</button>
        </div>
        <div class="measure-list" data-measure-list>${measureRowHtml()}</div>
      </div>
      <div class="line-stock" data-line-stock><span>Selecciona producto</span><strong>-</strong></div>
      <div class="line-math" data-line-total>Complete medidas para calcular</div>
      <button type="button" class="danger" data-remove-line>Quitar</button>
    </div>
  `;
}

async function loadCommon() {
  const [clientes, proveedores, productos, inventario, movimientosInventario, compras, ventas, deudas] = await Promise.all([
    safeApi("/api/clientes", []),
    safeApi("/api/proveedores", []),
    safeApi("/api/productos", []),
    safeApi("/api/inventario", []),
    safeApi("/api/inventario/movimientos", []),
    safeApi("/api/compras", []),
    safeApi("/api/ventas", []),
    safeApi("/api/cobros/deudas", [])
  ]);
  Object.assign(state.cache, { clientes, proveedores, productos, inventario, movimientosInventario, compras, ventas, deudas });
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
        <div class="actions">
          <button class="primary" type="submit">Entrar</button>
          <button class="ghost" type="button" id="publicCatalogBtn">Ver catalogo publico</button>
        </div>
      </form>
    </main>
  `;
  document.getElementById("publicCatalogBtn").onclick = () => navigatePath("/");
  document.getElementById("loginForm").onsubmit = async event => {
    event.preventDefault();
    const data = formData(event.currentTarget);
    state.apiBase = normalizeApiBase(API_DEFAULT);
    localStorage.setItem("sicomoro_api", state.apiBase);
    await safe(async () => {
      const auth = await api("/api/auth/login", {
        method: "POST",
        body: JSON.stringify({ email: data.email, password: data.password })
      });
      if (authRole(auth) === 6) {
        throw new Error("Esta cuenta es de cliente. Usa el acceso de clientes en el catalogo publico.");
      }
      state.token = auth.token;
      state.user = auth;
      if (!isStaffRoute()) window.history.replaceState(null, "", "/personal");
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
            <span class="badge version-badge">${APP_VERSION}</span>
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
  if (["dashboard", "clientes", "proveedores", "productos", "inventario", "compras", "ventas", "cobros", "documentos", "reportes", "publicidad"].includes(state.view)) {
    await loadCommon();
  }
  if (state.view === "transportes") state.cache.transportes = await api("/api/transportes");
  if (state.view === "notificaciones") state.cache.notificaciones = await api("/api/notificaciones?soloNoLeidas=false");
  if (state.view === "auditoria") state.cache.auditoria = await api("/api/auditoria?take=100");
  if (state.view === "perfil") state.cache.perfil = await api("/api/usuarios/me");
  if (state.view === "usuarios" && isAdmin()) state.cache.usuarios = await api("/api/usuarios");
  if (state.view === "publicidad" && isGestion()) state.cache.catalogoAnuncios = await api("/api/catalogo/anuncios");
}

async function render() {
  if (isPublicCatalogRoute()) return renderPublicCatalog();
  if (!state.token) return renderLogin();
  if (!canAccess(state.view)) {
    state.view = visibleViews()[0]?.[0] || "perfil";
    localStorage.setItem("sicomoro_view", state.view);
  }
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
    publicidad: renderPublicidad,
    perfil: renderPerfil,
    app: renderAppInstall,
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
  const buckets = buildSalesBuckets(state.dashboardRange, ventas);
  const ventasPendientes = state.cache.ventas.filter(x => x.estado === 1).length;
  const comprasTransito = state.cache.compras.filter(x => x.estado === 2).length;
  const deudasVencidas = state.cache.deudas.filter(x => x.estado === 4 || (x.fechaVencimiento && date(x.fechaVencimiento) < today())).length;
  const vendido = ventas.reduce((sum, x) => sum + Number(x.total || 0), 0);
  renderShell(`
    <section class="kpi-grid">
      <div class="kpi"><span>Ventas registradas</span><strong>${ventas.length}</strong></div>
      <div class="kpi"><span>Total vendido</span><strong>${money(vendido)}</strong></div>
      <div class="kpi"><span>Deuda pendiente</span><strong>${money(deuda)}</strong></div>
      <div class="kpi"><span>Bajo stock</span><strong>${bajo.length}</strong></div>
      <div class="kpi"><span>Ventas sin confirmar</span><strong>${ventasPendientes}</strong></div>
      <div class="kpi"><span>Compras en transito</span><strong>${comprasTransito}</strong></div>
      <div class="kpi"><span>Deudas vencidas</span><strong>${deudasVencidas}</strong></div>
      <div class="kpi"><span>Productos activos</span><strong>${productosActivos().length}</strong></div>
    </section>
    <section class="panel dashboard-chart-panel">
      <div class="panel-header">
        <h3>Ritmo de ventas</h3>
        <div class="segmented">
          <button class="${state.dashboardRange === "semana" ? "active" : ""}" data-dashboard-range="semana">Semana</button>
          <button class="${state.dashboardRange === "mes" ? "active" : ""}" data-dashboard-range="mes">Mes</button>
          <button class="${state.dashboardRange === "anio" ? "active" : ""}" data-dashboard-range="anio">Ano</button>
        </div>
      </div>
      <div class="panel-body">
        ${renderSalesChart(buckets)}
      </div>
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
  document.querySelectorAll("[data-dashboard-range]").forEach(btn => btn.onclick = () => {
    state.dashboardRange = btn.dataset.dashboardRange;
    localStorage.setItem("sicomoro_dashboard_range", state.dashboardRange);
    renderDashboard();
  });
}

function badge(text, type = "") {
  return `<span class="badge ${type}">${esc(text)}</span>`;
}

function findCliente(id) { return state.cache.clientes.find(x => x.id === id); }
function findProveedor(id) { return state.cache.proveedores.find(x => x.id === id); }
function findProducto(id) { return state.cache.productos.find(x => x.id === id); }
function productosActivos() { return state.cache.productos.filter(x => x.estado === 1); }
function unidadNombre(value) { return (unidades.find(([id]) => Number(id) === Number(value)) || [, "Otra"])[1]; }

function inventarioProducto(productoId) {
  return state.cache.inventario.find(x => x.productoId === productoId);
}

function movimientosProducto(productoId) {
  return state.cache.movimientosInventario
    .filter(x => x.productoId === productoId)
    .sort((a, b) => String(b.fecha).localeCompare(String(a.fecha)));
}

function productoInventario(producto) {
  const inv = inventarioProducto(producto.id);
  const stockActual = Number(inv?.stockActual ?? 0);
  const stockMinimo = Number(producto.stockMinimo ?? inv?.stockMinimo ?? 0);
  const precioCompra = Number(producto.precioCompra ?? 0);
  const precioVenta = Number(producto.precioVentaSugerido ?? 0);
  const margenUnitario = Math.max(0, precioVenta - precioCompra);
  const margenPorcentaje = precioCompra > 0 ? (margenUnitario / precioCompra) * 100 : 0;
  const valorCosto = stockActual * precioCompra;
  const valorVenta = stockActual * precioVenta;
  const cobertura = stockMinimo > 0 ? Math.min(100, (stockActual / stockMinimo) * 100) : 100;
  const bajoStock = producto.estado === 1 && stockMinimo > 0 && stockActual <= stockMinimo;
  return {
    ...producto,
    inventarioId: inv?.id,
    stockActual,
    stockMinimo,
    ubicacionInterna: inv?.ubicacionInterna || "",
    valorCosto,
    valorVenta,
    margenUnitario,
    margenPorcentaje,
    cobertura,
    bajoStock,
    sinStock: stockActual <= 0,
    movimientos: movimientosProducto(producto.id)
  };
}

function productosInventario() {
  return state.cache.productos.map(productoInventario);
}

function estadoInventarioBadge(row) {
  if (row.estado !== 1) return badge("Inactivo", "bad");
  if (row.sinStock) return badge("Sin stock", "bad");
  if (row.bajoStock) return badge("Bajo stock", "warn");
  return badge("Disponible");
}

function inventoryHealthRows() {
  return productosInventario().filter(x => x.estado === 1);
}

function inventorySummary(rows = inventoryHealthRows()) {
  return rows.reduce((acc, row) => {
    acc.productos += 1;
    acc.stock += row.stockActual;
    acc.valorCosto += row.valorCosto;
    acc.valorVenta += row.valorVenta;
    acc.margenPotencial += Math.max(0, row.valorVenta - row.valorCosto);
    if (row.bajoStock) acc.bajoStock += 1;
    if (row.sinStock) acc.sinStock += 1;
    return acc;
  }, { productos: 0, stock: 0, valorCosto: 0, valorVenta: 0, margenPotencial: 0, bajoStock: 0, sinStock: 0 });
}

function stockBar(row) {
  const width = Math.max(0, Math.min(100, row.cobertura || 0));
  return `
    <div class="stock-meter ${row.bajoStock ? "warning" : ""}">
      <div style="width:${width}%"></div>
    </div>
  `;
}

function movimientosMiniTable(movimientos, limit = 6) {
  const rows = movimientos.slice(0, limit);
  if (!rows.length) return `<div class="empty">Sin movimientos registrados para este producto.</div>`;
  return table([
    { label: "Fecha", render: x => date(x.fecha) },
    { label: "Tipo", render: x => esc(tiposMovimientoInventario[x.tipo] || x.tipo) },
    { label: "Cantidad", render: x => money(x.cantidad) },
    { label: "Costo", render: x => money(x.costoUnitario) },
    { label: "Motivo", render: x => esc(x.motivo || "-") }
  ], rows);
}

function renderClientes() {
  const clientes = filterRows("clientes", state.cache.clientes, ["nombreRazonSocial", "ciNit", "telefono", "ciudad"]);
  const page = paginate("clientes", clientes);
  const selected = state.selectedClienteId ? findCliente(state.selectedClienteId) : null;
  const historialVentas = selected ? state.cache.ventas.filter(x => x.clienteId === selected.id) : [];
  const historialDeudas = selected ? state.cache.deudas.filter(x => x.clienteId === selected.id) : [];
  const totalCompras = historialVentas.reduce((sum, x) => sum + Number(x.total || 0), 0);
  const saldo = historialDeudas.reduce((sum, x) => sum + Number(x.saldoPendiente || 0), 0);
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
        <div class="panel-header"><h3>Clientes</h3>${searchBox("clientes", "Buscar cliente, CI/NIT, telefono")}</div>
        ${table([
          { label: "Nombre", key: "nombreRazonSocial" },
          { label: "CI/NIT", key: "ciNit" },
          { label: "Telefono", key: "telefono" },
          { label: "Ciudad", key: "ciudad" },
          { label: "Deuda", render: x => money(x.deudaTotal) }
        ], page.rows, row => `
          <div class="split-actions">
            <button data-historial-cliente="${row.id}">Historial</button>
            <button data-delete-cliente="${row.id}" class="danger">Borrar</button>
          </div>
        `)}
        ${pager("clientes", page)}
      </div>
      <div class="panel full-panel">
        <div class="panel-header"><h3>Historial del cliente</h3></div>
        <div class="panel-body">
          ${selected ? `
            <div class="detail-grid">
              <div><span>Cliente</span><strong>${esc(selected.nombreRazonSocial)}</strong></div>
              <div><span>Total comprado</span><strong>${money(totalCompras)}</strong></div>
              <div><span>Saldo pendiente</span><strong>${money(saldo)}</strong></div>
              <div><span>Ventas</span><strong>${historialVentas.length}</strong></div>
            </div>
            ${table([
              { label: "Fecha", render: x => date(x.fecha) },
              { label: "Estado", render: x => badge(ventaEstados[x.estado] || x.estado, x.estado === 4 ? "bad" : x.estado === 3 ? "warn" : "") },
              { label: "Total", render: x => money(x.total) },
              { label: "Pagado", render: x => money(x.montoPagado) },
              { label: "Saldo", render: x => money(x.saldoPendiente) }
            ], historialVentas)}
          ` : `<div class="empty">Selecciona un cliente para ver sus compras, deuda y pagos pendientes.</div>`}
        </div>
      </div>
    </section>
  `, "Clientes");
  wireListControls("clientes", renderClientes);
  document.getElementById("clienteForm").onsubmit = submitJson("/api/clientes", async () => { await loadCommon(); render(); });
  document.querySelectorAll("[data-historial-cliente]").forEach(btn => btn.onclick = () => {
    state.selectedClienteId = btn.dataset.historialCliente;
    renderClientes();
  });
  document.querySelectorAll("[data-delete-cliente]").forEach(btn => btn.onclick = () => safe(async () => {
    const cliente = state.cache.clientes.find(x => x.id === btn.dataset.deleteCliente);
    if (!confirm(`Borrar cliente ${cliente?.nombreRazonSocial || ""}? Si tiene ventas o cobros, el sistema bloqueara el borrado para proteger el historial.`)) return false;
    if (!await deleteWithOperationKey(`/api/clientes/${btn.dataset.deleteCliente}`, "borrar cliente")) return false;
    await loadCommon();
    render();
  }, "Cliente eliminado"));
}

function renderProveedores() {
  const proveedores = filterRows("proveedores", state.cache.proveedores, ["nombre", "lugarOrigen", "telefono", "tipoMadera"]);
  const page = paginate("proveedores", proveedores);
  const selected = state.selectedProveedorId ? findProveedor(state.selectedProveedorId) : null;
  const historialCompras = selected ? state.cache.compras.filter(x => x.proveedorId === selected.id) : [];
  const totalCompras = historialCompras.reduce((sum, x) => sum + Number(x.totalProductos || 0) + Number(x.costoTransporte || 0) + Number(x.otrosCostos || 0), 0);
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
        <div class="panel-header"><h3>Proveedores</h3>${searchBox("proveedores", "Buscar proveedor u origen")}</div>
        ${table([
          { label: "Nombre", key: "nombre" },
          { label: "Origen", key: "lugarOrigen" },
          { label: "Telefono", key: "telefono" },
          { label: "Madera", key: "tipoMadera" }
        ], page.rows, row => `
          <div class="split-actions">
            <button data-historial-proveedor="${row.id}">Historial</button>
            <button data-delete-proveedor="${row.id}" class="danger">Borrar</button>
          </div>
        `)}
        ${pager("proveedores", page)}
      </div>
      <div class="panel full-panel">
        <div class="panel-header"><h3>Historial del proveedor</h3></div>
        <div class="panel-body">
          ${selected ? `
            <div class="detail-grid">
              <div><span>Proveedor</span><strong>${esc(selected.nombre)}</strong></div>
              <div><span>Origen</span><strong>${esc(selected.lugarOrigen)}</strong></div>
              <div><span>Compras</span><strong>${historialCompras.length}</strong></div>
              <div><span>Total comprado</span><strong>${money(totalCompras)}</strong></div>
            </div>
            ${table([
              { label: "Fecha", render: x => date(x.fechaCompra) },
              { label: "Origen", key: "origen" },
              { label: "Estado", render: x => badge(["", "Pendiente", "En transito", "Recibida", "Cancelada"][x.estado] || x.estado, x.estado === 3 ? "" : "warn") },
              { label: "Productos", render: x => money(x.totalProductos) },
              { label: "Costos", render: x => money(Number(x.costoTransporte || 0) + Number(x.otrosCostos || 0)) }
            ], historialCompras)}
          ` : `<div class="empty">Selecciona un proveedor para ver las compras registradas.</div>`}
        </div>
      </div>
    </section>
  `, "Proveedores");
  wireListControls("proveedores", renderProveedores);
  document.getElementById("proveedorForm").onsubmit = submitJson("/api/proveedores", async () => { await loadCommon(); render(); });
  document.querySelectorAll("[data-historial-proveedor]").forEach(btn => btn.onclick = () => {
    state.selectedProveedorId = btn.dataset.historialProveedor;
    renderProveedores();
  });
  document.querySelectorAll("[data-delete-proveedor]").forEach(btn => btn.onclick = () => safe(async () => {
    const proveedor = state.cache.proveedores.find(x => x.id === btn.dataset.deleteProveedor);
    if (!confirm(`Borrar proveedor ${proveedor?.nombre || ""}? Si tiene compras registradas, el sistema bloqueara el borrado para mantener el historial.`)) return false;
    if (!await deleteWithOperationKey(`/api/proveedores/${btn.dataset.deleteProveedor}`, "borrar proveedor")) return false;
    state.selectedProveedorId = state.selectedProveedorId === btn.dataset.deleteProveedor ? "" : state.selectedProveedorId;
    await loadCommon();
    render();
  }, "Proveedor eliminado"));
}

function renderProductos() {
  const productosBase = productosInventario();
  const productos = filterRows("productos", productosBase, [
    "nombreComercial",
    "tipoMadera",
    "calidad",
    "ubicacionInterna",
    x => unidadNombre(x.unidadMedida)
  ]).sort((a, b) => Number(b.bajoStock) - Number(a.bajoStock) || a.nombreComercial.localeCompare(b.nombreComercial));
  const page = paginate("productos", productos);
  const selected = productosBase.find(x => x.id === state.selectedProductoId) || productosBase[0] || null;
  const resumen = inventorySummary(productosBase.filter(x => x.estado === 1));
  renderShell(`
    <section class="kpi-grid compact">
      <div class="kpi"><span>Productos activos</span><strong>${resumen.productos}</strong></div>
      <div class="kpi"><span>Stock total PT</span><strong>${money(resumen.stock)}</strong></div>
      <div class="kpi"><span>Valor a costo</span><strong>Bs ${money(resumen.valorCosto)}</strong></div>
      <div class="kpi"><span>Bajo stock</span><strong>${resumen.bajoStock}</strong></div>
    </section>
    <section class="layout product-inventory-layout">
      <div class="panel">
        <div class="panel-header"><h3>Producto</h3></div>
        <div class="panel-body">
          <form id="productoForm" class="grid">
            <input type="hidden" name="id">
            <label class="full">Nombre comercial<input name="nombreComercial" required></label>
            <label>Tipo de madera<input name="tipoMadera" value="Tajibo" required></label>
            <label>Unidad<select name="unidadMedida">${options(unidades, 1)}</select></label>
            <label>Largo pies<input name="largo" type="number" step="0.0001" value="10"></label>
            <label>Ancho pulg.<input name="ancho" type="number" step="0.0001" value="4"></label>
            <label>Espesor pulg.<input name="espesor" type="number" step="0.0001" value="2"></label>
            <label>Calidad<input name="calidad" value="A"></label>
            <label>Compra por PT<input name="precioCompra" type="number" step="0.0001" value="12.5"></label>
            <label>Venta sugerida por PT<input name="precioVentaSugerido" type="number" step="0.0001" value="25"></label>
            <label>Stock minimo PT<input name="stockMinimo" type="number" step="0.0001" value="100"></label>
            <label>Estado<select name="estado">${options(estadosRegistro, 1)}</select></label>
            <label class="full">Observaciones<textarea name="observaciones"></textarea></label>
            <div class="actions full">
              <button class="primary">Guardar</button>
              <button type="button" id="limpiarProducto">Nuevo</button>
            </div>
          </form>
          <hr class="form-separator">
          <div class="product-focus">
            ${selected ? `
              <div class="product-focus-head">
                <div>
                  <span>Ficha operativa</span>
                  <strong>${esc(selected.nombreComercial)}</strong>
                  <small>${esc(selected.tipoMadera)} · ${esc(unidadNombre(selected.unidadMedida))}</small>
                </div>
                ${estadoInventarioBadge(selected)}
              </div>
              <div class="product-focus-grid">
                <div><span>Stock actual</span><strong>${money(selected.stockActual)} PT</strong>${stockBar(selected)}</div>
                <div><span>Stock minimo</span><strong>${money(selected.stockMinimo)} PT</strong></div>
                <div><span>Ubicacion</span><strong>${esc(selected.ubicacionInterna || "-")}</strong></div>
                <div><span>Margen por PT</span><strong>Bs ${money(selected.margenUnitario)}</strong><small>${money(selected.margenPorcentaje)}%</small></div>
                <div><span>Valor a costo</span><strong>Bs ${money(selected.valorCosto)}</strong></div>
                <div><span>Valor venta</span><strong>Bs ${money(selected.valorVenta)}</strong></div>
              </div>
            ` : `<div class="empty">Crea o selecciona un producto para ver su ficha operativa.</div>`}
          </div>
          <form id="productoStockForm" class="grid compact-form">
            <label class="full">Ajustar stock de producto<select name="productoId" required>${entityOptions(state.cache.productos, "nombreComercial", selected?.id || "")}</select></label>
            <label>Stock exacto PT<input name="nuevoStock" type="number" step="0.0001" value="${esc(selected?.stockActual ?? 0)}" required></label>
            <label>Ubicacion<input name="ubicacionInterna" value="${esc(selected?.ubicacionInterna || "")}" placeholder="Galpon A / Rack 1"></label>
            <label class="full">Motivo<input name="motivo" value="Ajuste desde ficha de producto"></label>
            <div class="actions full"><button type="submit" class="primary">Actualizar stock</button></div>
          </form>
        </div>
      </div>
      <div class="panel">
        <div class="panel-header"><h3>Productos e inventario</h3>${searchBox("productos", "Buscar producto, madera, ubicacion")}</div>
        ${table([
          { label: "Producto", render: x => `<strong>${esc(x.nombreComercial)}</strong><br><small>${esc(x.tipoMadera)} · ${esc(x.calidad || "-")}</small>` },
          { label: "Medida", render: x => `${money(x.ancho)}" x ${money(x.espesor)}" x ${money(x.largo)}'` },
          { label: "Stock", render: x => `<strong>${money(x.stockActual)} PT</strong><br>${stockBar(x)}<small>Min ${money(x.stockMinimo)} · ${esc(x.ubicacionInterna || "Sin ubicacion")}</small>` },
          { label: "Precios", render: x => `C ${money(x.precioCompra)} / V ${money(x.precioVentaSugerido)}<br><small>Margen Bs ${money(x.margenUnitario)}</small>` },
          { label: "Valor", render: x => `Costo Bs ${money(x.valorCosto)}<br><small>Venta Bs ${money(x.valorVenta)}</small>` },
          { label: "Estado", render: x => estadoInventarioBadge(x) }
        ], page.rows, row => `
          <div class="split-actions">
            <button data-focus-producto="${row.id}">Ver</button>
            <button data-edit-producto="${row.id}">Editar</button>
            <button data-stock-producto="${row.id}">Stock</button>
            <button data-delete-producto="${row.id}" class="danger">Borrar</button>
          </div>
        `)}
        ${pager("productos", page)}
        <div class="panel-body product-movement-panel">
          <h3>Ultimos movimientos${selected ? ` · ${esc(selected.nombreComercial)}` : ""}</h3>
          ${selected ? movimientosMiniTable(selected.movimientos) : `<div class="empty">Selecciona un producto para ver su historial.</div>`}
        </div>
      </div>
    </section>
  `, "Productos");

  wireListControls("productos", renderProductos);
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
  document.getElementById("limpiarProducto").onclick = () => {
    form.reset();
    form.elements.id.value = "";
  };
  document.getElementById("productoStockForm").onsubmit = async event => {
    event.preventDefault();
    await safe(async () => {
      const data = formData(event.currentTarget);
      await api("/api/inventario/ajuste", { method: "POST", body: JSON.stringify(data) });
      state.selectedProductoId = data.productoId;
      await loadCommon();
      render();
    }, "Stock actualizado");
  };
  document.getElementById("productoStockForm").elements.productoId.onchange = event => {
    state.selectedProductoId = event.currentTarget.value;
    renderProductos();
  };
  document.querySelectorAll("[data-focus-producto]").forEach(btn => btn.onclick = () => {
    state.selectedProductoId = btn.dataset.focusProducto;
    renderProductos();
  });
  document.querySelectorAll("[data-stock-producto]").forEach(btn => btn.onclick = () => {
    state.selectedProductoId = btn.dataset.stockProducto;
    renderProductos();
  });
  document.querySelectorAll("[data-edit-producto]").forEach(btn => btn.onclick = () => {
    const producto = findProducto(btn.dataset.editProducto);
    state.selectedProductoId = btn.dataset.editProducto;
    fillForm(form, producto);
    toast("Producto cargado para editar");
  });
  document.querySelectorAll("[data-delete-producto]").forEach(btn => btn.onclick = async () => {
    const producto = findProducto(btn.dataset.deleteProducto);
    if (!confirm(`Borrar definitivamente ${producto?.nombreComercial || "producto"}? Si tiene compras, ventas o movimientos, el sistema no lo borrara para proteger el historial.`)) return;
    await safe(async () => {
      if (!await deleteWithOperationKey(`/api/productos/${btn.dataset.deleteProducto}`, "borrar producto")) return false;
      if (state.selectedProductoId === btn.dataset.deleteProducto) state.selectedProductoId = "";
      await loadCommon();
      render();
    }, "Producto eliminado");
  });
}

function renderInventario() {
  const rowsAll = productosInventario();
  const activeRows = rowsAll.filter(x => x.estado === 1);
  const summary = inventorySummary(activeRows);
  const counts = {
    todos: rowsAll.length,
    bajo: rowsAll.filter(x => x.bajoStock).length,
    sin: rowsAll.filter(x => x.estado === 1 && x.sinStock).length,
    stock: rowsAll.filter(x => x.estado === 1 && x.stockActual > 0).length,
    inactivos: rowsAll.filter(x => x.estado !== 1).length
  };
  const filteredBySearch = filterRows("inventario", rowsAll, [
    "nombreComercial",
    "tipoMadera",
    "calidad",
    "ubicacionInterna",
    x => unidadNombre(x.unidadMedida)
  ]);
  const filteredRows = filteredBySearch.filter(x => {
    if (state.inventoryFilter === "bajo") return x.bajoStock;
    if (state.inventoryFilter === "sin") return x.estado === 1 && x.sinStock;
    if (state.inventoryFilter === "stock") return x.estado === 1 && x.stockActual > 0;
    if (state.inventoryFilter === "inactivos") return x.estado !== 1;
    return true;
  }).sort((a, b) => Number(b.bajoStock) - Number(a.bajoStock) || a.nombreComercial.localeCompare(b.nombreComercial));
  const page = paginate("inventario", filteredRows);
  const selected = rowsAll.find(x => x.id === state.selectedProductoId) || activeRows[0] || rowsAll[0] || null;
  const movimientos = (selected ? selected.movimientos : state.cache.movimientosInventario)
    .slice()
    .sort((a, b) => String(b.fecha).localeCompare(String(a.fecha)))
    .slice(0, 30);
  const filterButton = (id, label) => `<button type="button" data-inventory-filter="${id}" class="${state.inventoryFilter === id ? "active" : ""}">${label} <span>${counts[id]}</span></button>`;
  renderShell(`
    <section class="kpi-grid compact">
      <div class="kpi"><span>Valor inventario a costo</span><strong>Bs ${money(summary.valorCosto)}</strong></div>
      <div class="kpi"><span>Valor proyectado venta</span><strong>Bs ${money(summary.valorVenta)}</strong></div>
      <div class="kpi"><span>Margen potencial</span><strong>Bs ${money(summary.margenPotencial)}</strong></div>
      <div class="kpi"><span>Alertas de stock</span><strong>${summary.bajoStock}</strong></div>
    </section>
    <section class="layout inventory-control-layout">
      <div class="panel">
        <div class="panel-header"><h3>Ajuste inteligente</h3></div>
        <div class="panel-body">
          <form id="inventarioForm" class="grid">
            <label class="full">Producto<select name="productoId" required>${entityOptions(state.cache.productos, "nombreComercial", selected?.id || "")}</select></label>
            <label>Modo<select name="modoAjuste"><option value="exacto">Fijar stock exacto</option><option value="sumar">Sumar entrada</option><option value="restar">Restar salida/perdida</option></select></label>
            <label>Cantidad PT<input name="cantidadMovimiento" type="number" step="0.0001" min="0" value="${esc(selected?.stockActual ?? 0)}" required></label>
            <label>Stock resultante<input name="nuevoStock" type="number" step="0.0001" readonly required></label>
            <label>Ubicacion<input name="ubicacionInterna" value="${esc(selected?.ubicacionInterna || "")}" placeholder="Galpon A / Rack 1"></label>
            <label class="full">Motivo<input name="motivo" value="Ajuste manual de inventario"></label>
            <div class="actions full"><button class="primary">Aplicar</button></div>
          </form>
          <div class="inventory-current-card" id="inventoryCurrentCard">
            ${selected ? `
              <span>Lectura actual</span>
              <strong>${esc(selected.nombreComercial)}</strong>
              <div>${money(selected.stockActual)} PT disponibles · minimo ${money(selected.stockMinimo)}</div>
              ${stockBar(selected)}
              <small>${esc(selected.ubicacionInterna || "Sin ubicacion")}</small>
            ` : `<span>Sin productos registrados</span>`}
          </div>
        </div>
      </div>
      <div class="panel">
        <div class="panel-header"><h3>Inventario operativo</h3>${searchBox("inventario", "Buscar producto, tipo o ubicacion")}</div>
        <div class="inventory-filter segmented">
          ${filterButton("todos", "Todos")}
          ${filterButton("bajo", "Bajo stock")}
          ${filterButton("sin", "Sin stock")}
          ${filterButton("stock", "Con stock")}
          ${filterButton("inactivos", "Inactivos")}
        </div>
        ${table([
          { label: "Producto", render: x => `<strong>${esc(x.nombreComercial)}</strong><br><small>${esc(x.tipoMadera)} · ${esc(unidadNombre(x.unidadMedida))}</small>` },
          { label: "Stock", render: x => `<strong>${money(x.stockActual)} PT</strong>${stockBar(x)}<small>Min ${money(x.stockMinimo)}</small>` },
          { label: "Ubicacion", render: x => esc(x.ubicacionInterna || "-") },
          { label: "Valor costo", render: x => `Bs ${money(x.valorCosto)}` },
          { label: "Valor venta", render: x => `Bs ${money(x.valorVenta)}` },
          { label: "Estado", render: x => estadoInventarioBadge(x) }
        ], page.rows, row => `
          <div class="split-actions">
            <button data-inv-select="${row.id}">Ajustar</button>
            <button data-inv-history="${row.id}">Historial</button>
          </div>
        `)}
        ${pager("inventario", page)}
      </div>
      <div class="panel full-panel">
        <div class="panel-header">
          <h3>Movimientos ${selected ? `· ${esc(selected.nombreComercial)}` : "recientes"}</h3>
        </div>
        ${table([
          { label: "Fecha", render: x => date(x.fecha) },
          { label: "Producto", render: x => esc(findProducto(x.productoId)?.nombreComercial || x.productoId) },
          { label: "Tipo", render: x => esc(tiposMovimientoInventario[x.tipo] || x.tipo) },
          { label: "Cantidad", render: x => money(x.cantidad) },
          { label: "Costo", render: x => money(x.costoUnitario) },
          { label: "Motivo", render: x => esc(x.motivo || "-") }
        ], movimientos)}
      </div>
    </section>
  `, "Inventario");
  wireListControls("inventario", renderInventario);
  document.querySelectorAll("[data-inventory-filter]").forEach(btn => btn.onclick = () => {
    state.inventoryFilter = btn.dataset.inventoryFilter;
    state.pages.inventario = 1;
    renderInventario();
  });
  const form = document.getElementById("inventarioForm");
  const updateAdjustment = () => {
    const product = productoInventario(findProducto(form.elements.productoId.value) || {});
    const amount = Number(form.elements.cantidadMovimiento.value || 0);
    const mode = form.elements.modoAjuste.value;
    let result = amount;
    if (mode === "sumar") result = product.stockActual + amount;
    if (mode === "restar") result = Math.max(0, product.stockActual - amount);
    form.elements.nuevoStock.value = Number(result.toFixed(4));
  };
  ["productoId", "modoAjuste", "cantidadMovimiento"].forEach(name => {
    form.elements[name]?.addEventListener("input", updateAdjustment);
    form.elements[name]?.addEventListener("change", () => {
      if (name === "productoId") {
        const selectedRow = productoInventario(findProducto(form.elements.productoId.value) || {});
        form.elements.ubicacionInterna.value = selectedRow.ubicacionInterna || "";
        state.selectedProductoId = form.elements.productoId.value;
      }
      updateAdjustment();
    });
  });
  updateAdjustment();
  form.onsubmit = async event => {
    event.preventDefault();
    await safe(async () => {
      updateAdjustment();
      const body = {
        productoId: form.elements.productoId.value,
        nuevoStock: Number(form.elements.nuevoStock.value || 0),
        ubicacionInterna: form.elements.ubicacionInterna.value || null,
        motivo: form.elements.motivo.value || "Ajuste manual de inventario"
      };
      await api("/api/inventario/ajuste", { method: "POST", body: JSON.stringify(body) });
      state.selectedProductoId = body.productoId;
      await loadCommon();
      render();
    }, "Inventario actualizado");
  };
  document.querySelectorAll("[data-inv-select], [data-inv-history]").forEach(btn => btn.onclick = () => {
    state.selectedProductoId = btn.dataset.invSelect || btn.dataset.invHistory;
    renderInventario();
  });
}

function renderCompras() {
  const compras = filterRows("compras", state.cache.compras, [
    "origen",
    x => findProveedor(x.proveedorId)?.nombre,
    x => ["", "Pendiente", "En transito", "Recibida", "Cancelada"][x.estado] || x.estado
  ]);
  const page = paginate("compras", compras);
  renderShell(`
    <section class="layout">
      <div class="panel">
        <div class="panel-header"><h3>Compra</h3></div>
        <div class="panel-body">
          <form id="compraForm" class="grid">
            <input type="hidden" name="id">
            <label class="full">Proveedor<select name="proveedorId" required>${entityOptions(state.cache.proveedores, "nombre")}</select></label>
            <label>Origen<input name="origen" value="Beni" required></label>
            <label>Fecha compra<input name="fechaCompra" type="date" value="${today()}"></label>
            <label>Fecha llegada<input name="fechaEstimadaLlegada" type="date"></label>
            <label>Transporte<input name="costoTransporte" type="number" step="0.0001" value="0"></label>
            <label>Otros costos<input name="otrosCostos" type="number" step="0.0001" value="0"></label>
            <div class="full line-section">
              <div class="line-header">
                <strong>Productos comprados</strong>
                <button type="button" id="addCompraLine">Agregar producto</button>
              </div>
              <div id="compraLines">${compraLineHtml()}</div>
            </div>
            <label class="full">Observaciones<input name="observaciones"></label>
            <div class="actions full">
              <button class="primary">Guardar compra</button>
              <button type="button" id="nuevaCompra">Nueva</button>
            </div>
          </form>
        </div>
      </div>
      <div class="panel">
        <div class="panel-header"><h3>Compras</h3>${searchBox("compras", "Buscar compra, proveedor u origen")}</div>
        ${table([
          { label: "Proveedor", render: x => esc(findProveedor(x.proveedorId)?.nombre || x.proveedorId) },
          { label: "Origen", key: "origen" },
          { label: "Estado", render: x => badge(["", "Pendiente", "En transito", "Recibida", "Cancelada"][x.estado] || x.estado, x.estado === 3 ? "" : "warn") },
          { label: "Fecha", render: x => date(x.fechaCompra) },
          { label: "Total", render: x => money(x.totalProductos + x.costoTransporte + x.otrosCostos) }
        ], page.rows, row => `
          <div class="split-actions">
            ${row.estado === 1 ? `<button data-edit-compra="${row.id}">Editar</button>` : ""}
            ${row.estado === 3 ? "" : `<button data-recibir-compra="${row.id}">Recibir</button>`}
          </div>
        `)}
        ${pager("compras", page)}
      </div>
    </section>
  `, "Compras");

  wireListControls("compras", renderCompras);
  wireLineItems("compraLines", "addCompraLine", compraLineHtml, "precioCompra");
  const compraForm = document.getElementById("compraForm");
  document.getElementById("nuevaCompra").onclick = () => {
    compraForm.reset();
    compraForm.elements.id.value = "";
    document.getElementById("compraLines").innerHTML = compraLineHtml();
    wireLineItems("compraLines", "addCompraLine", compraLineHtml, "precioCompra");
  };
  document.getElementById("compraForm").onsubmit = async event => {
    event.preventDefault();
    await safe(async () => {
      const data = formData(event.currentTarget);
      const id = data.id;
      const body = {
        proveedorId: data.proveedorId,
        origen: data.origen,
        fechaCompra: toIsoDate(data.fechaCompra),
        fechaEstimadaLlegada: toIsoDate(data.fechaEstimadaLlegada),
        costoTransporte: data.costoTransporte || 0,
        otrosCostos: data.otrosCostos || 0,
        observaciones: data.observaciones,
        detalles: readCompraDetalles(event.currentTarget)
      };
      if (id) await api(`/api/compras/${id}`, { method: "PUT", body: JSON.stringify(body) });
      else await api("/api/compras", { method: "POST", body: JSON.stringify(body) });
      await loadCommon();
      render();
    }, "Compra guardada");
  };
  document.querySelectorAll("[data-edit-compra]").forEach(btn => btn.onclick = () => {
    const compra = state.cache.compras.find(x => x.id === btn.dataset.editCompra);
    if (!compra) return;
    compraForm.elements.id.value = compra.id;
    compraForm.elements.proveedorId.value = compra.proveedorId;
    compraForm.elements.origen.value = compra.origen || "";
    compraForm.elements.fechaCompra.value = date(compra.fechaCompra);
    compraForm.elements.fechaEstimadaLlegada.value = date(compra.fechaEstimadaLlegada) === "-" ? "" : date(compra.fechaEstimadaLlegada);
    compraForm.elements.costoTransporte.value = compra.costoTransporte || 0;
    compraForm.elements.otrosCostos.value = compra.otrosCostos || 0;
    compraForm.elements.observaciones.value = compra.observaciones || "";
    document.getElementById("compraLines").innerHTML = (compra.detalles?.length ? compra.detalles : [{}]).map(compraLineHtml).join("");
    wireLineItems("compraLines", "addCompraLine", compraLineHtml, "precioCompra");
    toast("Compra cargada para editar");
  });
  document.querySelectorAll("[data-recibir-compra]").forEach(btn => btn.onclick = () => safe(async () => {
    await api(`/api/compras/${btn.dataset.recibirCompra}/recibir`, { method: "PUT" });
    await loadCommon();
    render();
  }, "Compra recibida"));
}

function renderVentas() {
  const ventas = filterRows("ventas", state.cache.ventas, [
    x => findCliente(x.clienteId)?.nombreRazonSocial,
    x => ventaEstados[x.estado] || x.estado,
    x => date(x.fecha)
  ]);
  const page = paginate("ventas", ventas);
  const ventasHoy = state.cache.ventas.filter(x => date(x.fecha) === today());
  const totalHoy = ventasHoy.reduce((sum, x) => sum + Number(x.total || 0), 0);
  const saldoVentas = state.cache.ventas.reduce((sum, x) => sum + Number(x.saldoPendiente || 0), 0);
  renderShell(`
    <section class="ventas-hero">
      <div>
        <span class="catalog-badge">Operacion diaria</span>
        <h3>Ventas de madera</h3>
        <p>Arma la venta por piezas y medidas, revisa stock antes de confirmar y cobra parcial o total en un solo flujo.</p>
      </div>
      <div class="venta-hero-metrics">
        <div><span>Hoy</span><strong>Bs ${money(totalHoy)}</strong></div>
        <div><span>Pendiente</span><strong>Bs ${money(saldoVentas)}</strong></div>
      </div>
    </section>
    <section class="layout sales-layout">
      <div class="panel">
        <div class="panel-header"><h3>Venta</h3></div>
        <div class="panel-body">
          <form id="ventaForm" class="grid">
            <input type="hidden" name="id">
            <label class="full">Cliente<select name="clienteId" required>${entityOptions(state.cache.clientes, "nombreRazonSocial")}</select></label>
            <label>Metodo<select name="metodoPago">${options(metodosPago, 5)}</select></label>
            <label>Vencimiento<input name="fechaVencimiento" type="date"></label>
            <div class="full line-section">
              <div class="line-header">
                <strong>Productos vendidos</strong>
                <button type="button" id="addVentaLine">Agregar producto</button>
              </div>
              <p class="hint">Registra varias medidas del mismo producto como en el cuaderno. El sistema suma pies tablares por fila y descuenta el stock total del producto.</p>
              <div id="ventaLines">${ventaLineHtml()}</div>
            </div>
            <div class="full venta-summary-card">
              <div class="venta-summary-grid">
                <div><span>Total venta</span><strong>Bs <b id="ventaTotalPreview">0,00</b></strong></div>
                <div><span>Pies tablares</span><strong id="ventaPiesPreview">0,00</strong></div>
                <div><span>Piezas</span><strong id="ventaPiezasPreview">0,00</strong></div>
                <div><span>Descuento</span><strong>Bs <b id="ventaDescuentoPreview">0,00</b></strong></div>
                <div><span>Lineas</span><strong id="ventaLineasPreview">0</strong></div>
                <div><span>Revision</span><strong id="ventaStockPreview">${badge("Stock OK")}</strong></div>
              </div>
              <div class="payment-preview">
                <label>Monto pagado al confirmar<input id="ventaMontoPagado" type="number" step="0.0001" min="0" value="0"></label>
                <div><span>Saldo estimado</span><strong>Bs <b id="ventaSaldoPreview">0,00</b></strong></div>
              </div>
            </div>
            <label class="full">Observaciones<textarea name="observaciones" rows="3"></textarea></label>
            <div class="actions full">
              <button class="primary" data-save-venta="draft">Guardar borrador</button>
              <button type="button" class="primary" id="guardarConfirmarVenta">Guardar y confirmar</button>
              <button type="button" id="nuevaVenta">Nueva</button>
            </div>
          </form>
        </div>
      </div>
      <div class="panel">
        <div class="panel-header"><h3>Ventas</h3>${searchBox("ventas", "Buscar venta, cliente o estado")}</div>
        ${table([
          { label: "Cliente", render: x => esc(findCliente(x.clienteId)?.nombreRazonSocial || x.clienteId) },
          { label: "Fecha", render: x => date(x.fecha) },
          { label: "Estado", render: x => badge(ventaEstados[x.estado] || x.estado, x.estado === 4 ? "bad" : x.estado === 3 ? "warn" : "") },
          { label: "Total", render: x => money(x.total) },
          { label: "Saldo", render: x => money(x.saldoPendiente) }
        ], page.rows, row => `
          <div class="split-actions">
            ${row.estado === 1 ? `<button data-edit-venta="${row.id}">Editar</button>` : ""}
            ${row.estado === 1 ? `<button data-confirmar-venta="${row.id}">Confirmar</button>` : ""}
            ${isGestion() && row.estado !== 4 ? `<button data-anular-venta="${row.id}" class="danger">Anular</button>` : ""}
          </div>
        `)}
        ${pager("ventas", page)}
      </div>
    </section>
  `, "Ventas");

  wireListControls("ventas", renderVentas);
  wireLineItems("ventaLines", "addVentaLine", ventaLineHtml, "precioUnitario");
  const ventaForm = document.getElementById("ventaForm");
  const montoPagadoInput = document.getElementById("ventaMontoPagado");
  montoPagadoInput?.addEventListener("input", updateVentaSummary);
  async function guardarVentaActual() {
    const data = formData(ventaForm);
    const id = data.id;
    const body = {
      clienteId: data.clienteId,
      metodoPago: data.metodoPago,
      fechaVencimiento: toIsoDate(data.fechaVencimiento),
      observaciones: ventaObservacionesConMedidas(data.observaciones, ventaForm),
      detalles: readVentaDetalles(ventaForm)
    };
    if (id) {
      const venta = await api(`/api/ventas/${id}`, { method: "PUT", body: JSON.stringify(body) });
      return venta.id || id;
    }
    const creada = await api("/api/ventas", { method: "POST", body: JSON.stringify(body) });
    ventaForm.elements.id.value = creada.id;
    return creada.id;
  }
  document.getElementById("nuevaVenta").onclick = () => {
    ventaForm.reset();
    ventaForm.elements.id.value = "";
    if (montoPagadoInput) montoPagadoInput.value = "0";
    document.getElementById("ventaLines").innerHTML = ventaLineHtml();
    wireLineItems("ventaLines", "addVentaLine", ventaLineHtml, "precioUnitario");
    updateVentaSummary();
  };
  document.getElementById("ventaForm").onsubmit = async event => {
    event.preventDefault();
    await safe(async () => {
      await guardarVentaActual();
      await loadCommon();
      render();
    }, "Venta guardada");
  };
  document.getElementById("guardarConfirmarVenta").onclick = async () => {
    await safe(async () => {
      const ventaId = await guardarVentaActual();
      const montoPagado = Number(montoPagadoInput?.value || 0);
      await api(`/api/ventas/${ventaId}/confirmar`, { method: "PUT", body: JSON.stringify({ montoPagado }) });
      await loadCommon();
      render();
    }, "Venta guardada y confirmada");
  };
  document.querySelectorAll("[data-edit-venta]").forEach(btn => btn.onclick = () => {
    const venta = state.cache.ventas.find(x => x.id === btn.dataset.editVenta);
    if (!venta) return;
    ventaForm.elements.id.value = venta.id;
    ventaForm.elements.clienteId.value = venta.clienteId;
    ventaForm.elements.metodoPago.value = venta.metodoPago || 5;
    ventaForm.elements.fechaVencimiento.value = date(venta.fechaVencimiento) === "-" ? "" : date(venta.fechaVencimiento);
    ventaForm.elements.observaciones.value = stripMeasurementBlock(venta.observaciones || "");
    document.getElementById("ventaLines").innerHTML = (venta.detalles?.length ? venta.detalles : [{}]).map(ventaLineHtml).join("");
    wireLineItems("ventaLines", "addVentaLine", ventaLineHtml, "precioUnitario");
    if (montoPagadoInput) montoPagadoInput.value = String(venta.montoPagado || 0);
    updateVentaSummary();
    toast("Venta cargada para editar");
  });
  document.querySelectorAll("[data-confirmar-venta]").forEach(btn => btn.onclick = async () => {
    const montoValue = await requestInputModal({
      title: "Confirmar venta",
      message: "Registra cuanto pago el cliente ahora. Si es credito, deja 0.",
      label: "Monto pagado",
      type: "number",
      value: "0"
    });
    if (montoValue == null) return;
    const monto = Number(montoValue || 0);
    await safe(async () => {
      await api(`/api/ventas/${btn.dataset.confirmarVenta}/confirmar`, { method: "PUT", body: JSON.stringify({ montoPagado: monto }) });
      await loadCommon();
      render();
    }, "Venta confirmada");
  });
  document.querySelectorAll("[data-anular-venta]").forEach(btn => btn.onclick = async () => {
    const motivo = await requestInputModal({
      title: "Anular venta",
      message: "Indica el motivo para auditoria.",
      label: "Motivo",
      value: "Anulada desde frontend",
      required: true
    });
    if (motivo == null) return;
    await safe(async () => {
      await api(`/api/ventas/${btn.dataset.anularVenta}/anular`, { method: "PUT", body: JSON.stringify({ motivo }) });
      await loadCommon();
      render();
    }, "Venta anulada");
  });
  updateVentaSummary();
}

function renderCobros() {
  const deudas = filterRows("cobros", state.cache.deudas, [
    x => findCliente(x.clienteId)?.nombreRazonSocial,
    x => x.ventaId,
    x => date(x.fechaVencimiento)
  ]);
  const page = paginate("cobros", deudas);
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
        <div class="panel-header"><h3>Deudas</h3>${searchBox("cobros", "Buscar cliente o venta")}</div>
        ${table([
          { label: "Cliente", render: x => esc(findCliente(x.clienteId)?.nombreRazonSocial || x.clienteId) },
          { label: "Venta", key: "ventaId" },
          { label: "Total", render: x => money(x.montoTotal) },
          { label: "Saldo", render: x => money(x.saldoPendiente) },
          { label: "Vence", render: x => date(x.fechaVencimiento) }
        ], page.rows)}
        ${pager("cobros", page)}
      </div>
    </section>
  `, "Cobros");
  wireListControls("cobros", renderCobros);
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
  const ingresos = (rows || []).filter(x => x.tipo === 1).reduce((sum, x) => sum + Number(x.monto || 0), 0);
  const egresos = (rows || []).filter(x => x.tipo === 2).reduce((sum, x) => sum + Number(x.monto || 0), 0);
  document.getElementById("cajaResult").innerHTML = `
    <div class="kpi-grid compact">
      <div class="kpi"><span>Ingresos automaticos/manuales</span><strong>${money(ingresos)}</strong></div>
      <div class="kpi"><span>Egresos</span><strong>${money(egresos)}</strong></div>
      <div class="kpi"><span>Saldo del periodo</span><strong>${money(ingresos - egresos)}</strong></div>
      <div class="kpi"><span>Movimientos</span><strong>${(rows || []).length}</strong></div>
    </div>
    <p class="hint">Los cobros iniciales de ventas y pagos de cuentas por cobrar entran a caja automaticamente. Aqui registra solo ingresos o egresos manuales.</p>
    ${table([
    { label: "Fecha", render: x => date(x.fecha) },
    { label: "Tipo", render: x => x.tipo === 1 ? badge("Ingreso") : badge("Egreso", "warn") },
    { label: "Monto", render: x => money(x.monto) },
    { label: "Concepto", key: "concepto" }
  ], rows || [])}
  `;
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
        ], state.cache.ventas, row => `<button data-descargar-pdf="${row.id}">Descargar PDF</button>`)}
      </div>
    </section>
  `, "Documentos");
  document.getElementById("documentoForm").onsubmit = async event => {
    event.preventDefault();
    const data = formData(event.currentTarget);
    const documento = await safe(() => api(`/api/documentos/venta/${data.ventaId}/generar`, { method: "POST" }), "Documento generado");
    document.getElementById("documentoResult").innerHTML = `
      <strong>${esc(documento.numero)}</strong><br>
      <span>${esc(documento.rutaArchivo)}</span>
      <div class="actions"><button type="button" data-descargar-pdf="${data.ventaId}">Descargar PDF</button></div>
    `;
    wirePdfDownloads();
  };
  wirePdfDownloads();
}

function wirePdfDownloads() {
  document.querySelectorAll("[data-descargar-pdf]").forEach(btn => btn.onclick = () => safe(
    () => downloadFile(`/api/documentos/venta/${btn.dataset.descargarPdf}/descargar`, "comprobante-sicomoro.pdf"),
    "PDF descargado"
  ));
}

function renderReportes() {
  renderShell(`
    <section class="layout">
      <div class="panel">
        <div class="panel-header"><h3>Control de reportes</h3></div>
        <div class="panel-body">
          <div class="report-quick">
            ${reportMetric("Ventas", state.cache.ventas.filter(x => x.estado !== 4).length, "Operaciones no anuladas")}
            ${reportMetric("Vendido", money(state.cache.ventas.filter(x => x.estado !== 4).reduce((sum, x) => sum + Number(x.total || 0), 0)), "Total historico cargado")}
            ${reportMetric("Deuda", money(state.cache.deudas.reduce((sum, x) => sum + Number(x.saldoPendiente || 0), 0)), "Cuentas por cobrar")}
            ${reportMetric("Bajo stock", state.cache.inventario.filter(x => Number(x.stockActual) <= Number(x.stockMinimo)).length, "Productos a revisar")}
          </div>
          <form id="reportForm" class="grid">
            <label>Desde<input name="desde" type="date" value="${monthStart()}"></label>
            <label>Hasta<input name="hasta" type="date" value="${today()}"></label>
            <div class="actions full">
              <button class="primary" type="button" id="reporteCompleto">Resumen completo</button>
              <button>Ventas</button>
              <button type="button" id="reporteCaja">Caja</button>
              <button type="button" id="reporteBajo">Inventario bajo</button>
              <button type="button" id="reporteDeudores">Deudores</button>
              <button type="button" id="exportarReporte">Exportar ultimo CSV</button>
            </div>
          </form>
        </div>
      </div>
      <div class="panel">
        <div class="panel-header"><h3>Resultado</h3></div>
        <div class="panel-body" id="reportResult">${renderReportHome()}</div>
      </div>
    </section>
  `, "Reportes");
  const form = document.getElementById("reportForm");
  let ultimoReporte = null;
  document.getElementById("reporteCompleto").onclick = async () => {
    const data = formData(form);
    await safe(async () => {
      const [ventas, caja, bajo, deudores] = await Promise.all([
        api(`/api/reportes/ventas?desde=${data.desde}&hasta=${data.hasta}`),
        api(`/api/reportes/caja?desde=${data.desde}&hasta=${data.hasta}`),
        api("/api/reportes/inventario-bajo"),
        api("/api/reportes/clientes-deudores")
      ]);
      const resumenRows = [
        { seccion: "Ventas", indicador: "Cantidad", valor: ventas.cantidadVentas },
        { seccion: "Ventas", indicador: "Total vendido", valor: ventas.totalVentas },
        { seccion: "Ventas", indicador: "Total pagado", valor: ventas.totalPagado },
        { seccion: "Ventas", indicador: "Saldo pendiente", valor: ventas.saldoPendiente },
        { seccion: "Caja", indicador: "Ingresos", valor: caja.ingresos },
        { seccion: "Caja", indicador: "Egresos", valor: caja.egresos },
        { seccion: "Caja", indicador: "Saldo", valor: caja.saldo },
        { seccion: "Inventario", indicador: "Productos bajo stock", valor: bajo.length },
        { seccion: "Cobranza", indicador: "Clientes con deuda", valor: deudores.length }
      ];
      ultimoReporte = {
        filename: `resumen-negocio-${data.desde}-${data.hasta}.csv`,
        rows: resumenRows,
        columns: [
          { label: "Seccion", key: "seccion" },
          { label: "Indicador", key: "indicador" },
          { label: "Valor", key: "valor" }
        ]
      };
      reportHtml(`
        <div class="report-block">
          <h3>Resumen del negocio</h3>
          <p class="hint">Periodo ${esc(data.desde)} a ${esc(data.hasta)}. Este resumen cruza ventas, caja, cobranza e inventario critico.</p>
          <div class="kpi-grid">
            <div class="kpi"><span>Ventas</span><strong>${ventas.cantidadVentas}</strong></div>
            <div class="kpi"><span>Total vendido</span><strong>${money(ventas.totalVentas)}</strong></div>
            <div class="kpi"><span>Pagado</span><strong>${money(ventas.totalPagado)}</strong></div>
            <div class="kpi"><span>Saldo ventas</span><strong>${money(ventas.saldoPendiente)}</strong></div>
            <div class="kpi"><span>Ingresos caja</span><strong>${money(caja.ingresos)}</strong></div>
            <div class="kpi"><span>Egresos caja</span><strong>${money(caja.egresos)}</strong></div>
            <div class="kpi"><span>Saldo caja</span><strong>${money(caja.saldo)}</strong></div>
            <div class="kpi"><span>Bajo stock</span><strong>${bajo.length}</strong></div>
          </div>
          <div class="layout report-detail-layout">
            <div class="panel"><div class="panel-header"><h3>Clientes deudores</h3></div>${table([{ label: "Cliente", key: "nombreRazonSocial" }, { label: "Deuda", render: x => money(x.deudaTotal) }, { label: "Telefono", key: "telefono" }], deudores.slice(0, 8))}</div>
            <div class="panel"><div class="panel-header"><h3>Inventario bajo</h3></div>${table([{ label: "Producto", key: "producto" }, { label: "Stock", render: x => money(x.stockActual) }, { label: "Minimo", render: x => money(x.stockMinimo) }], bajo.slice(0, 8))}</div>
          </div>
        </div>
      `);
    }, "Reporte completo generado");
  };
  form.onsubmit = async event => {
    event.preventDefault();
    const data = formData(form);
    const r = await safe(() => api(`/api/reportes/ventas?desde=${data.desde}&hasta=${data.hasta}`), "");
    ultimoReporte = {
      filename: `ventas-${data.desde}-${data.hasta}.csv`,
      rows: [r],
      columns: [
        { label: "Desde", value: x => date(x.desde) },
        { label: "Hasta", value: x => date(x.hasta) },
        { label: "Cantidad", key: "cantidadVentas" },
        { label: "Total", key: "totalVentas" },
        { label: "Pagado", key: "totalPagado" },
        { label: "Saldo", key: "saldoPendiente" }
      ]
    };
    reportHtml(`<div class="kpi-grid"><div class="kpi"><span>Cantidad</span><strong>${r.cantidadVentas}</strong></div><div class="kpi"><span>Total</span><strong>${money(r.totalVentas)}</strong></div><div class="kpi"><span>Pagado</span><strong>${money(r.totalPagado)}</strong></div><div class="kpi"><span>Saldo</span><strong>${money(r.saldoPendiente)}</strong></div></div>`);
  };
  document.getElementById("reporteCaja").onclick = async () => {
    const data = formData(form);
    const r = await safe(() => api(`/api/reportes/caja?desde=${data.desde}&hasta=${data.hasta}`), "");
    ultimoReporte = {
      filename: `caja-${data.desde}-${data.hasta}.csv`,
      rows: [r],
      columns: [
        { label: "Desde", value: x => date(x.desde) },
        { label: "Hasta", value: x => date(x.hasta) },
        { label: "Ingresos", key: "ingresos" },
        { label: "Egresos", key: "egresos" },
        { label: "Saldo", key: "saldo" }
      ]
    };
    reportHtml(`<div class="kpi-grid"><div class="kpi"><span>Ingresos</span><strong>${money(r.ingresos)}</strong></div><div class="kpi"><span>Egresos</span><strong>${money(r.egresos)}</strong></div><div class="kpi"><span>Saldo</span><strong>${money(r.saldo)}</strong></div></div>`);
  };
  document.getElementById("reporteBajo").onclick = async () => {
    const rows = await safe(() => api("/api/reportes/inventario-bajo"), "");
    ultimoReporte = {
      filename: `inventario-bajo-${today()}.csv`,
      rows,
      columns: [
        { label: "Producto", key: "producto" },
        { label: "Stock", key: "stockActual" },
        { label: "Minimo", key: "stockMinimo" },
        { label: "Ubicacion", key: "ubicacionInterna" }
      ]
    };
    reportHtml(table([{ label: "Producto", key: "producto" }, { label: "Stock", render: x => money(x.stockActual) }, { label: "Minimo", render: x => money(x.stockMinimo) }], rows));
  };
  document.getElementById("reporteDeudores").onclick = async () => {
    const rows = await safe(() => api("/api/reportes/clientes-deudores"), "");
    ultimoReporte = {
      filename: `clientes-deudores-${today()}.csv`,
      rows,
      columns: [
        { label: "Cliente", key: "nombreRazonSocial" },
        { label: "Deuda", key: "deudaTotal" },
        { label: "Telefono", key: "telefono" },
        { label: "Ciudad", key: "ciudad" }
      ]
    };
    reportHtml(table([{ label: "Cliente", key: "nombreRazonSocial" }, { label: "Deuda", render: x => money(x.deudaTotal) }, { label: "Telefono", key: "telefono" }], rows));
  };
  document.getElementById("exportarReporte").onclick = () => {
    if (!ultimoReporte) {
      toast("Primero genera un reporte.");
      return;
    }
    downloadCsv(ultimoReporte.filename, ultimoReporte.rows, ultimoReporte.columns);
  };
}

function reportHtml(html) {
  document.getElementById("reportResult").innerHTML = html;
}

function reportMetric(label, value, hint) {
  return `<div class="report-card"><span>${esc(label)}</span><strong>${esc(value)}</strong><small>${esc(hint)}</small></div>`;
}

function renderReportHome() {
  const bajo = state.cache.inventario
    .filter(x => Number(x.stockActual) <= Number(x.stockMinimo))
    .slice(0, 5);
  const deudores = [...state.cache.deudas]
    .sort((a, b) => Number(b.saldoPendiente || 0) - Number(a.saldoPendiente || 0))
    .slice(0, 5);
  return `
    <div class="report-block">
      <h3>Vista rapida</h3>
      <p class="hint">Usa el resumen completo para revisar caja, ventas, deuda e inventario en un solo corte.</p>
      <div class="layout report-detail-layout">
        <div class="panel">
          <div class="panel-header"><h3>Deudas mas importantes</h3></div>
          ${table([
            { label: "Cliente", render: x => esc(findCliente(x.clienteId)?.nombreRazonSocial || x.clienteId) },
            { label: "Saldo", render: x => money(x.saldoPendiente) },
            { label: "Vence", render: x => date(x.fechaVencimiento) }
          ], deudores)}
        </div>
        <div class="panel">
          <div class="panel-header"><h3>Stock para revisar</h3></div>
          ${table([
            { label: "Producto", key: "producto" },
            { label: "Stock", render: x => money(x.stockActual) },
            { label: "Minimo", render: x => money(x.stockMinimo) }
          ], bajo)}
        </div>
      </div>
    </div>
  `;
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
            <label>Rol / cargo<input value="${esc(rolLabel(perfil.rol))}" disabled></label>
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

function renderAppInstall() {
  const standalone = isStandaloneApp();
  const secure = window.isSecureContext || ["localhost", "127.0.0.1"].includes(window.location.hostname);
  const swSupport = "serviceWorker" in navigator;
  const promptReady = canUseInstallPrompt();
  const status = standalone
    ? ["Instalada", "Sicomoro ya esta abierto como app en este dispositivo.", "good"]
    : promptReady
      ? ["Lista para instalar", "Tu navegador ya permite crear el acceso directo de app.", "good"]
      : secure && swSupport
        ? ["Preparada", "Si no aparece el boton de instalacion, usa el menu del navegador.", "warn"]
        : ["Requiere HTTPS", "La instalacion real funciona en Render u otro dominio con HTTPS.", "bad"];

  renderShell(`
    <section class="install-grid">
      <div class="panel install-hero">
        <div class="panel-body">
          <div class="app-icon-preview">S</div>
          <div>
            <span class="badge ${status[2]}">${status[0]}</span>
            <h3>Instalar Sicomoro en PC y celular</h3>
            <p>La app se abre en una ventana propia, usa la misma cuenta y se conecta al mismo servidor de Sicomoro.</p>
            <div class="actions">
              <button id="installAppBtn" class="primary" ${promptReady ? "" : "disabled"}>Instalar como app</button>
              <button id="downloadDesktopShortcutBtn">Descargar acceso PC</button>
              <button id="copyAppUrlBtn">Copiar link</button>
            </div>
            <p class="hint">${status[1]}</p>
          </div>
        </div>
      </div>

      <div class="panel">
        <div class="panel-header"><h3>Windows / PC</h3></div>
        <div class="panel-body install-steps">
          <div><strong>1</strong><span>Abre Sicomoro en Chrome, Edge u Opera desde la PC.</span></div>
          <div><strong>2</strong><span>Presiona Instalar como app. Si no aparece, usa el icono de instalacion o el menu del navegador.</span></div>
          <div><strong>3</strong><span>Confirma Instalar. Sicomoro quedara como app con ventana propia y acceso en el escritorio o menu inicio.</span></div>
        </div>
      </div>

      <div class="panel">
        <div class="panel-header"><h3>Acceso directo</h3></div>
        <div class="panel-body install-steps">
          <div><strong>1</strong><span>Usa Descargar acceso PC si solo quieres un archivo para abrir Sicomoro rapido.</span></div>
          <div><strong>2</strong><span>Guarda el archivo Sicomoro.url en el escritorio.</span></div>
          <div><strong>3</strong><span>Ese acceso abre el panel personal en el navegador disponible.</span></div>
        </div>
      </div>

      <div class="panel">
        <div class="panel-header"><h3>Android</h3></div>
        <div class="panel-body install-steps">
          <div><strong>1</strong><span>Abre Sicomoro desde Chrome en el celular.</span></div>
          <div><strong>2</strong><span>Toca Instalar app. Si no aparece, abre el menu del navegador.</span></div>
          <div><strong>3</strong><span>Elige Agregar a pantalla principal y confirma.</span></div>
        </div>
      </div>

      <div class="panel">
        <div class="panel-header"><h3>iPhone / iPad</h3></div>
        <div class="panel-body install-steps">
          <div><strong>1</strong><span>Abre Sicomoro desde Safari.</span></div>
          <div><strong>2</strong><span>Toca Compartir.</span></div>
          <div><strong>3</strong><span>Elige Agregar a pantalla de inicio.</span></div>
        </div>
      </div>

      <div class="panel full-panel">
        <div class="panel-header"><h3>Estado tecnico</h3></div>
        <div class="panel-body">
          <div class="detail-grid">
            <div><span>Modo app</span><strong>${standalone ? "Si" : "No"}</strong></div>
            <div><span>Conexion segura</span><strong>${secure ? "Si" : "No"}</strong></div>
            <div><span>Service worker</span><strong>${swSupport ? "Compatible" : "No compatible"}</strong></div>
            <div><span>Servidor</span><strong>${esc(state.apiBase)}</strong></div>
          </div>
          <p class="hint">La app instalada necesita internet para guardar ventas, compras, cobros e inventario. Lo que queda preparado sin internet es la carga inicial de la pantalla.</p>
        </div>
      </div>
    </section>
  `, "App PC/movil");

  document.getElementById("installAppBtn").onclick = async () => {
    if (!deferredInstallPrompt) {
      toast("Usa el menu del navegador para agregar Sicomoro a la pantalla principal.");
      return;
    }
    deferredInstallPrompt.prompt();
    await deferredInstallPrompt.userChoice;
    deferredInstallPrompt = null;
    render();
  };

  document.getElementById("downloadDesktopShortcutBtn").onclick = () => {
    downloadDesktopShortcut();
    toast("Acceso para PC descargado");
  };

  document.getElementById("copyAppUrlBtn").onclick = async () => {
    try {
      if (!navigator.clipboard) throw new Error("Clipboard no disponible");
      await navigator.clipboard.writeText(window.location.origin);
      toast("Link copiado");
    } catch {
      window.prompt("Copia este link para abrir Sicomoro:", window.location.origin);
    }
  };
}

async function renderPublicCatalog() {
  syncDeviceLayout();
  let anuncios = [];
  let error = "";
  try {
    anuncios = await api("/api/catalogo/publico", { skipAuth: true });
  } catch (ex) {
    error = ex.message || "No se pudo cargar el catalogo.";
  }

  const catalogItems = anuncios;
  const destacados = catalogItems.slice(0, 3);
  const heroBackground = destacados[0]?.imagenUrl || "/assets/catalogo-hero.png";
  app.innerHTML = `
    <main class="public-site">
      <header class="public-hero" style="background-image: linear-gradient(90deg, rgba(15,31,23,.88), rgba(15,31,23,.46)), url('${esc(heroBackground)}')">
        <nav class="public-nav">
          <strong>Sicomoro</strong>
          <div>
            <a href="#catalogo">Catalogo</a>
            <a href="#contacto">Contacto</a>
            ${state.clientUser ? `<button class="ghost" id="clientLogoutBtn">Salir cliente</button>` : `<a href="#clientes">Clientes</a>`}
            <button class="ghost" id="goAdminBtn">Personal</button>
          </div>
        </nav>
        <section class="public-hero-content public-hero-grid">
          <div class="public-hero-copy">
            <span class="catalog-badge">Barraca de madera</span>
            <h1>Madera seleccionada para obra, carpinteria y proyectos especiales</h1>
            <p>Consulta disponibilidad, medidas y precios actualizados desde el inventario interno de Sicomoro.</p>
            <div class="public-actions">
              <a class="primary public-button" href="#catalogo">Ver maderas</a>
              <a class="public-button" href="#contacto">Solicitar cotizacion</a>
            </div>
          </div>
          ${renderClientAccessCard()}
        </section>
      </header>

      <section class="public-section public-intro">
        <div><span>Compra segura</span><strong>Productos publicados por el equipo de Sicomoro</strong></div>
        <div><span>Stock visible</span><strong>Disponibilidad conectada al inventario</strong></div>
        <div><span>Atencion directa</span><strong>Ventas y entregas coordinadas con la barraca</strong></div>
      </section>

      <section class="public-section" id="catalogo">
        <div class="public-section-head">
          <div>
            <span class="catalog-badge">Catalogo</span>
            <h2>Maderas destacadas</h2>
          </div>
          <p>${catalogItems.length ? `${catalogItems.length} publicaciones disponibles` : "Todavia no hay publicaciones activas."}</p>
        </div>
        ${error ? `<div class="public-empty">${esc(error)}</div>` : ""}
        ${catalogItems.length ? `<div class="catalog-grid">${catalogItems.map(item => renderCatalogCard(item)).join("")}</div>` : `<div class="public-empty">El catalogo publico esta listo. Publica anuncios desde el panel interno para que aparezcan aqui.</div>`}
      </section>

      <section class="public-section public-contact" id="contacto">
        <div>
          <span class="catalog-badge">Cotizaciones</span>
          <h2>Pregunta por medidas, volumen y entrega</h2>
          <p>Esta pagina muestra lo publicado por la barraca. Para cerrar una venta, coordina con el equipo y registra la operacion en el panel interno.</p>
        </div>
        <button class="primary" id="publicAdminBtn">Acceso personal</button>
      </section>
      <footer class="public-footer">Barraca Sicomoro - Documento comercial informativo, sin validez fiscal oficial.</footer>
    </main>
  `;
  document.getElementById("goAdminBtn").onclick = navigateStaffLogin;
  document.getElementById("publicAdminBtn").onclick = navigateStaffLogin;
  document.getElementById("clientLogoutBtn")?.addEventListener("click", logoutClient);
  bindClientAccessForms();
}

function renderClientAccessCard() {
  if (state.clientUser) {
    return `
      <aside class="client-access-card signed" id="clientes">
        <span class="catalog-badge">Cuenta cliente</span>
        <h2>Hola, ${esc(state.clientUser.nombre || "cliente")}</h2>
        <p>Tu cuenta esta lista para futuras cotizaciones, pedidos y seguimiento de compras.</p>
        <div class="client-session-box">
          <span>Email</span>
          <strong>${esc(state.clientUser.email)}</strong>
        </div>
        <button class="ghost" id="clientLogoutCardBtn">Cerrar sesion de cliente</button>
      </aside>
    `;
  }

  return `
    <aside class="client-access-card" id="clientes">
      <span class="catalog-badge">Clientes</span>
      <h2>Accede o crea tu cuenta</h2>
      <p>Guarda tus datos para futuras cotizaciones y pedidos de madera.</p>
      <div class="client-forms">
        <form id="clientLoginForm" autocomplete="off">
          <strong>Ingresar</strong>
          <label>Email<input name="email" type="email" autocomplete="off" required></label>
          <label>Contrasena<input name="password" type="password" autocomplete="off" required></label>
          <button class="primary">Entrar como cliente</button>
        </form>
        <form id="clientRegisterForm" autocomplete="off">
          <strong>Crear cuenta</strong>
          <label>Nombre o razon social<input name="nombre" required></label>
          <label>Email<input name="email" type="email" autocomplete="off" required></label>
          <label>Contrasena<input name="password" type="password" minlength="8" autocomplete="new-password" required></label>
          <label>Telefono<input name="telefono"></label>
          <label>Ciudad<input name="ciudad"></label>
          <button>Crear cuenta cliente</button>
        </form>
      </div>
    </aside>
  `;
}

function bindClientAccessForms() {
  document.getElementById("clientLogoutCardBtn")?.addEventListener("click", logoutClient);
  const loginForm = document.getElementById("clientLoginForm");
  if (loginForm) {
    loginForm.onsubmit = async event => {
      event.preventDefault();
      const data = formData(event.currentTarget);
      await safe(async () => {
        const auth = await api("/api/clientes-portal/login", {
          method: "POST",
          skipAuth: true,
          body: JSON.stringify({ email: data.email, password: data.password })
        });
        setClientSession(auth);
        render();
      }, "Cliente conectado");
    };
  }

  const registerForm = document.getElementById("clientRegisterForm");
  if (registerForm) {
    registerForm.onsubmit = async event => {
      event.preventDefault();
      const data = formData(event.currentTarget);
      await safe(async () => {
        const auth = await api("/api/clientes-portal/register", {
          method: "POST",
          skipAuth: true,
          body: JSON.stringify(data)
        });
        setClientSession(auth);
        render();
      }, "Cuenta cliente creada");
    };
  }
}

function setClientSession(auth) {
  state.clientToken = auth.token;
  state.clientUser = auth;
  localStorage.setItem("sicomoro_client_token", auth.token);
  localStorage.setItem("sicomoro_client_user", JSON.stringify(auth));
}

function logoutClient() {
  state.clientToken = "";
  state.clientUser = null;
  localStorage.removeItem("sicomoro_client_token");
  localStorage.removeItem("sicomoro_client_user");
  render();
}

function renderCatalogCard(item) {
  const image = item.imagenUrl || "/assets/catalogo-hero.png";
  const product = item.producto || "Madera disponible";
  const stock = item.stockActual == null ? "Consultar stock" : `Stock: ${money(item.stockActual)}`;
  const detail = [item.tipoMadera, item.unidadMedida].filter(Boolean).join(" / ");
  const ctaText = item.ctaTexto || "Solicitar cotizacion";
  const ctaHref = item.ctaUrl || "#contacto";
  return `
    <article class="catalog-card">
      <div class="catalog-card-media" style="background-image: url('${esc(image)}')">
        ${item.etiqueta ? `<span>${esc(item.etiqueta)}</span>` : ""}
      </div>
      <div class="catalog-card-body">
        <div>
          <small>${esc(product)}</small>
          <h3>${esc(item.titulo)}</h3>
          ${item.subtitulo ? `<strong>${esc(item.subtitulo)}</strong>` : ""}
        </div>
        <p>${esc(item.descripcion)}</p>
        <div class="catalog-meta">
          <span>${esc(detail || "Medidas a consultar")}</span>
          <span>${esc(stock)}</span>
        </div>
        <div class="catalog-card-foot">
          <b>${esc(item.precioTexto || "Precio a consultar")}</b>
          <a href="${esc(ctaHref)}">${esc(ctaText)}</a>
        </div>
      </div>
    </article>
  `;
}

function readFileAsDataUrl(file) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(reader.result);
    reader.onerror = () => reject(new Error("No se pudo leer la imagen."));
    reader.readAsDataURL(file);
  });
}

async function catalogImageFileToDataUrl(file) {
  if (!file) return "";
  if (!file.type.startsWith("image/")) throw new Error("Selecciona una imagen valida.");
  if (file.size > MAX_CATALOG_IMAGE_FILE_SIZE) throw new Error("La imagen es muy pesada. Usa una imagen menor a 8 MB.");

  const source = await readFileAsDataUrl(file);
  return new Promise((resolve, reject) => {
    const img = new Image();
    img.onload = () => {
      const maxSide = 1280;
      const scale = Math.min(1, maxSide / Math.max(img.width, img.height));
      const width = Math.max(1, Math.round(img.width * scale));
      const height = Math.max(1, Math.round(img.height * scale));
      const canvas = document.createElement("canvas");
      canvas.width = width;
      canvas.height = height;
      const ctx = canvas.getContext("2d");
      ctx.fillStyle = "#fffaf0";
      ctx.fillRect(0, 0, width, height);
      ctx.drawImage(img, 0, 0, width, height);
      let output = canvas.toDataURL("image/jpeg", .82);
      if (output.length > 1_400_000) output = canvas.toDataURL("image/jpeg", .68);
      resolve(output);
    };
    img.onerror = () => reject(new Error("No se pudo procesar la imagen. Usa JPG, PNG o WEBP."));
    img.src = source;
  });
}

function updateCatalogImagePreview(value) {
  const preview = document.getElementById("catalogImagePreview");
  if (!preview) return;
  if (!value) {
    preview.innerHTML = "<span>Vista previa de imagen</span>";
    preview.style.backgroundImage = "";
    preview.classList.remove("has-image");
    return;
  }
  preview.innerHTML = "";
  preview.style.backgroundImage = `url("${String(value).replace(/"/g, "%22")}")`;
  preview.classList.add("has-image");
}

function readAnuncioForm(form) {
  const data = formData(form);
  return {
    productoId: data.productoId || null,
    titulo: data.titulo,
    subtitulo: data.subtitulo,
    descripcion: data.descripcion,
    imagenUrl: data.imagenUrl,
    precioTexto: data.precioTexto,
    etiqueta: data.etiqueta,
    ctaTexto: data.ctaTexto,
    ctaUrl: data.ctaUrl,
    orden: Number(data.orden || 0),
    publicado: form.elements.publicado.checked
  };
}

function renderPublicidad() {
  const anuncios = state.cache.catalogoAnuncios;
  renderShell(`
    <section class="layout">
      <div class="panel">
        <div class="panel-header"><h3>Publicar anuncio</h3></div>
        <div class="panel-body">
          <form id="anuncioForm" class="grid" autocomplete="off">
            <input name="id" type="hidden">
            <label class="full">Producto vinculado<select name="productoId">${entityOptions(productosActivos(), "nombreComercial")}</select></label>
            <label class="full">Titulo<input name="titulo" placeholder="Tajibo seco 2x4 para estructura" required></label>
            <label class="full">Subtitulo<input name="subtitulo" placeholder="Ideal para obra fina y carpinteria"></label>
            <label class="full">Descripcion<textarea name="descripcion" placeholder="Describe calidad, medidas, disponibilidad y uso recomendado." required></textarea></label>
            <div class="full catalog-image-box">
              <label>Imagen por link<input name="imagenUrl" placeholder="https://..."></label>
              <div class="catalog-image-actions">
                <label class="file-picker">Cargar imagen desde dispositivo<input id="catalogImageFile" type="file" accept="image/png,image/jpeg,image/webp"></label>
                <button type="button" id="clearCatalogImageBtn">Quitar imagen</button>
              </div>
              <div id="catalogImagePreview" class="image-preview"><span>Vista previa de imagen</span></div>
            </div>
            <label>Precio visible<input name="precioTexto" placeholder="Consultar precio"></label>
            <label>Etiqueta<input name="etiqueta" placeholder="Nuevo / Oferta / Seco"></label>
            <label>Texto del boton<input name="ctaTexto" placeholder="Solicitar cotizacion"></label>
            <label>Link del boton<input name="ctaUrl" placeholder="#contacto o https://..."></label>
            <label>Orden<input name="orden" type="number" value="0"></label>
            <label class="checkbox-line"><input name="publicado" type="checkbox"> Publicado en catalogo</label>
            <div class="actions full">
              <button class="primary">Guardar anuncio</button>
              <button type="button" id="clearAnuncioBtn">Limpiar</button>
            </div>
          </form>
        </div>
      </div>
      <div class="panel">
        <div class="panel-header">
          <h3>Anuncios del catalogo</h3>
          <button id="openCatalogBtn">Ver pagina publica</button>
        </div>
        ${table([
          { label: "Titulo", key: "titulo" },
          { label: "Producto", render: x => x.producto || "-" },
          { label: "Precio", render: x => x.precioTexto || "Consultar" },
          { label: "Orden", key: "orden" },
          { label: "Estado", render: x => x.publicado ? badge("Publicado") : badge("Oculto", "warn") }
        ], anuncios, row => `
          <div class="split-actions">
            <button data-edit-anuncio="${row.id}">Editar</button>
            <button data-toggle-anuncio="${row.id}">${row.publicado ? "Ocultar" : "Publicar"}</button>
            <button class="danger" data-delete-anuncio="${row.id}">Borrar</button>
          </div>
        `)}
      </div>
    </section>
  `, "Publicidad");

  const form = document.getElementById("anuncioForm");
  const imageFile = document.getElementById("catalogImageFile");
  document.getElementById("openCatalogBtn").onclick = () => navigatePath("/");
  document.getElementById("clearAnuncioBtn").onclick = () => {
    form.reset();
    form.elements.id.value = "";
    form.elements.orden.value = "0";
    updateCatalogImagePreview("");
  };
  document.getElementById("clearCatalogImageBtn").onclick = () => {
    form.elements.imagenUrl.value = "";
    imageFile.value = "";
    updateCatalogImagePreview("");
  };
  form.elements.imagenUrl.oninput = event => updateCatalogImagePreview(event.currentTarget.value.trim());
  imageFile.onchange = async event => {
    const file = event.currentTarget.files?.[0];
    if (!file) return;
    await safe(async () => {
      const dataUrl = await catalogImageFileToDataUrl(file);
      form.elements.imagenUrl.value = dataUrl;
      updateCatalogImagePreview(dataUrl);
    }, "Imagen cargada");
  };
  form.onsubmit = async event => {
    event.preventDefault();
    const data = readAnuncioForm(form);
    const id = form.elements.id.value;
    await safe(async () => {
      await api(id ? `/api/catalogo/anuncios/${id}` : "/api/catalogo/anuncios", {
        method: id ? "PUT" : "POST",
        body: JSON.stringify(data)
      });
      state.cache.catalogoAnuncios = await api("/api/catalogo/anuncios");
      render();
    }, id ? "Anuncio actualizado" : "Anuncio creado");
  };

  document.querySelectorAll("[data-edit-anuncio]").forEach(btn => btn.onclick = () => {
    const item = anuncios.find(x => x.id === btn.dataset.editAnuncio);
    if (!item) return;
    fillForm(form, {
      id: item.id,
      productoId: item.productoId,
      titulo: item.titulo,
      subtitulo: item.subtitulo,
      descripcion: item.descripcion,
      imagenUrl: item.imagenUrl,
      precioTexto: item.precioTexto,
      etiqueta: item.etiqueta,
      ctaTexto: item.ctaTexto,
      ctaUrl: item.ctaUrl,
      orden: item.orden
    });
    imageFile.value = "";
    updateCatalogImagePreview(item.imagenUrl || "");
    form.elements.publicado.checked = Boolean(item.publicado);
    form.scrollIntoView({ behavior: "smooth", block: "start" });
  });

  document.querySelectorAll("[data-toggle-anuncio]").forEach(btn => btn.onclick = async () => {
    const item = anuncios.find(x => x.id === btn.dataset.toggleAnuncio);
    if (!item) return;
    await safe(async () => {
      await api(`/api/catalogo/anuncios/${item.id}`, {
        method: "PUT",
        body: JSON.stringify({ ...item, publicado: !item.publicado })
      });
      state.cache.catalogoAnuncios = await api("/api/catalogo/anuncios");
      render();
    }, item.publicado ? "Anuncio ocultado" : "Anuncio publicado");
  });

  document.querySelectorAll("[data-delete-anuncio]").forEach(btn => btn.onclick = async () => {
    const item = anuncios.find(x => x.id === btn.dataset.deleteAnuncio);
    if (!confirm(`Borrar anuncio ${item?.titulo || ""}?`)) return;
    await safe(async () => {
      if (!await deleteWithOperationKey(`/api/catalogo/anuncios/${btn.dataset.deleteAnuncio}`, "borrar anuncio")) return false;
      state.cache.catalogoAnuncios = await api("/api/catalogo/anuncios");
      render();
    }, "Anuncio eliminado");
  });
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
            <label>Rol / cargo<select name="rol">${options(roles, 6)}</select></label>
            <label>CI/NIT<input name="ciNit"></label>
            <label>Telefono<input name="telefono"></label>
            <label class="full">Direccion<input name="direccion"></label>
            <label class="full">Notas<textarea name="notas"></textarea></label>
            <div class="actions full"><button class="primary">Crear usuario</button></div>
          </form>
          <hr class="form-separator">
          <form id="resetPasswordForm" class="grid" autocomplete="off">
            <label class="full">Usuario<select name="usuarioId" required>${entityOptions(state.cache.usuarios.map(u => ({ ...u, nombre: `${u.nombre} - ${u.email}` })), "nombre")}</select></label>
            <label class="full">Nueva contrasena temporal<input name="nuevaPassword" type="password" minlength="8" required></label>
            <div class="actions full"><button>Resetear contrasena</button></div>
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
          { label: "Cargo", render: x => rolLabel(x.rol) }
        ], state.cache.usuarios, row => `
          <div class="split-actions">
            ${row.id === state.user?.usuarioId ? `<span class="badge">Tu cuenta</span>` : `<button data-delete-usuario="${row.id}" class="danger">Borrar</button>`}
          </div>
        `)}
      </div>
      <div class="panel full-panel danger-zone">
        <div class="panel-header"><h3>Reiniciar datos del negocio</h3></div>
        <div class="panel-body">
          <p class="hint">Borra clientes, proveedores, productos, inventario, compras, ventas, cobros, caja, documentos, anuncios, notificaciones, auditoria y usuarios que no sean administradores. Se conserva tu cuenta administradora para volver a cargar todo desde cero.</p>
          <button id="resetBusinessDataBtn" type="button" class="danger">Borrar datos y empezar de 0</button>
        </div>
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
      if (!await deleteWithOperationKey(`/api/usuarios/${btn.dataset.deleteUsuario}`, "borrar usuario")) return false;
      state.cache.usuarios = await api("/api/usuarios");
      render();
    }, "Usuario eliminado");
  });

  document.getElementById("resetPasswordForm").onsubmit = async event => {
    event.preventDefault();
    const data = formData(event.currentTarget);
    await safe(async () => {
      await api(`/api/usuarios/${data.usuarioId}/password`, {
        method: "PUT",
        body: JSON.stringify({ nuevaPassword: data.nuevaPassword })
      });
      event.currentTarget.reset();
    }, "Contrasena reseteada");
  };

  document.getElementById("resetBusinessDataBtn").onclick = async () => {
    if (!confirm("Esto borrara definitivamente casi todos los datos del negocio y solo conservara administradores. Deseas continuar?")) return;
    if (!confirm("Ultima confirmacion: esta accion no se puede deshacer desde la pagina.")) return;
    const claveOperacion = await requestOperationKey("reiniciar datos del negocio");
    if (!claveOperacion) return;
    await safe(async () => {
      const resultado = await api("/api/sistema/reiniciar-datos", {
        method: "POST",
        headers: { [OPERATION_KEY_HEADER]: claveOperacion },
        body: "{}"
      });
      Object.keys(state.cache).forEach(key => {
        if (Array.isArray(state.cache[key])) state.cache[key] = [];
      });
      state.cache.perfil = null;
      state.cache.usuarios = await api("/api/usuarios");
      toast(`Datos reiniciados. Admins conservados: ${resultado.administradoresConservados ?? "-"}`);
      setView("dashboard");
    }, "");
  };
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

window.addEventListener("popstate", () => render());
registerServiceWorker();
render();
