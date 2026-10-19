# Resumen de Ventas

Aplicación Android para llevar el control de ventas de líneas móviles desde el teléfono: registrar ventas, revisar pendientes, ver resúmenes por mes y respaldar los datos en tu propio Google Drive.

> **Aplicación independiente.** No es un producto oficial ni está afiliada, patrocinada ni respaldada por Claro ni por ninguna otra empresa. Las marcas mencionadas pertenecen a sus respectivos dueños.

**Última versión: 1.3**

---

## Descarga

1. Entra a la sección **[Releases](../../releases)** de este repositorio.
2. Descarga el archivo `.apk` de la última versión.
3. Ábrelo en tu teléfono. Si Android lo pide, permite **instalar apps de orígenes desconocidos** para el navegador o gestor de archivos que estés usando.

**Requisitos:** teléfono Android con procesador de 64 bits (prácticamente todos los modelos de los últimos años).

La guía de uso en PDF está en el mismo apartado de Releases.

---

## Qué puedes hacer

- **Registrar ventas** con el botón flotante **Nueva Venta**, disponible en las pantallas principales: RUT, nombre, apellido, teléfono, plan, estado y fechas. Si el RUT ya existe, la app autocompleta los datos del cliente.
- **Seguir las ventas pendientes** desde **Inicio**: marcarlas como activas, editarlas o eliminarlas. Arriba ves un contador con las **ventas activadas del mes en curso**.
- **Revisar si una venta se activó** con el botón **Revisar activación**, que abre la consulta de portabilidad dentro de la app e intenta escribir el número de la venta en el campo. El número también queda copiado en el portapapeles.
- **Revisar si un cliente pagó** con el botón **Revisar pago** de cada venta en **Resumen**, que copia el RUT del cliente y abre el Portal Pagos, e intenta escribirlo en el campo correspondiente.
- **Revisar el resumen mensual** con navegación por mes o por rango de fechas, ordenamiento (por fecha o nombre) y filtros por plan y por estado.
- **Copiar con un toque** el RUT (sin puntos ni guión) y el teléfono (sin espacios).
- **Editar ventas ya guardadas**, incluida la fecha de activación.
- **Registrar kits prepago** en la pestaña **Kits**: escanea con la cámara el código de barras de la SIM (o escribe el número a mano) y la app guarda el número con la fecha y hora del registro. Avisa si la SIM ya estaba registrada y muestra el total del mes.
- **Administrar planes** desde **Más**: crear, renombrar, cambiar su valor o eliminarlos.
- **Abrir páginas web de consulta** dentro de la app (pago y consulta de números portados).
- **Respaldar y restaurar** tus datos en Google Drive.

---

## Tecnologías

- **C# y .NET MAUI** (.NET 11 en versión preliminar), solo para Android.
- **SQLite** (`sqlite-net-pcl`) para la base de datos local.
- **Google Drive API v3** con OAuth 2.0 (PKCE) y la carpeta privada de la app (`appDataFolder`) para los respaldos.
- **BarcodeScanning.Native.Maui** para leer códigos de barras con la cámara (basado en ML Kit de Google en Android).
- **WebView** con JavaScript inyectado para rellenar campos de páginas externas.
- Interfaz construida en código C#, con navegación de `Shell`.

### Estructura del código

| Archivo | Contenido |
|---|---|
| `Data.cs` | Modelos, base de datos SQLite y consultas |
| `Pages.cs` | Pantallas de la app, incluido el escáner de SIM |
| `Ui.cs` | Componentes visuales compartidos (tarjetas, botón flotante, encabezados) |
| `DriveSync.cs` | Inicio de sesión con Google y respaldo/restauración en Drive |
| `AppShell.xaml` | Navegación con barra inferior: Inicio, Resumen, Kits y Más |

---

## Primer uso

La app parte vacía, con tres planes de ejemplo que puedes editar o eliminar desde **Más > Planes**.

Para guardar un respaldo, ve a **Más > Respaldo** y pulsa **Conectar con Google**:

1. Elige tu cuenta.
2. Es posible que Google muestre el aviso *"Google no ha verificado esta app"*. Pulsa **Avanzado** y luego **Ir a Resumen Ventas**.
3. Acepta el permiso. Solo da acceso a una carpeta oculta de la app, no a tus otros archivos de Drive.

Usa **Subir respaldo a Drive** al terminar tu jornada. **Restaurar desde Drive** reemplaza todos los datos del teléfono por los del respaldo, y sirve para cambiar de equipo.

> Usa la app desde **un solo teléfono a la vez**: el respaldo es una copia completa y no mezcla datos de distintos equipos.

---

## Privacidad

- Los datos de ventas y clientes se guardan **solo en tu teléfono**.
- La app **no tiene servidores propios** y no envía tus datos a ningún tercero.
- Si conectas Google Drive, el respaldo se guarda en una **carpeta oculta de la app dentro de tu propio Drive** (permiso `drive.appdata`). Nadie más puede verlo, y la app no puede leer tus demás archivos.
- Puedes **desconectar tu cuenta** desde **Más > Respaldo**, o revocar el acceso en tu cuenta de Google en *Seguridad > Conexiones con terceros*.
- La app pide permiso de **cámara** solo para escanear el código de barras de la SIM. No toma ni guarda fotos.
- Las páginas web integradas son sitios externos con sus propias políticas de privacidad.

---

## Compilar desde el código

Requisitos: SDK de .NET con la carga de trabajo de MAUI para Android, JDK 17 y el SDK de Android. Las dependencias (SQLite y el lector de códigos de barras) se restauran solas con NuGet.

```
dotnet build -t:Run -f net11.0-android
```

El respaldo en Drive necesita **tu propio ID de cliente de OAuth** (tipo Android) en Google Cloud, creado con el nombre de paquete de la app y el SHA-1 de tu clave de firma. Reemplázalo en `DriveSync.cs` (constante `ClientId`) y en `Platforms/Android/WebAuthCallbackActivity.cs` (esquema de URI). La clave de firma no se incluye en este repositorio.

---

## Historial de versiones

### v1.3
- **Nuevo:** pestaña **Kits** en la barra inferior, entre **Resumen** y **Más**, para registrar kits prepago con SIM. Se escanea el código de barras con la cámara o se escribe el número, y se guarda con la fecha y hora. Avisa si la SIM ya estaba registrada y muestra el total del mes.
- **Nuevo:** la app solicita permiso de cámara, usado solo para el escáner.

### v1.2
- **Nuevo:** botón **Revisar activación** en cada venta pendiente del Inicio. Abre la consulta de números portados e intenta rellenar el número de la venta, que además se copia al portapapeles.
- **Nuevo:** botón **Revisar pago** en cada venta del **Resumen**. Copia el RUT del cliente (sin puntos ni guión) y abre el Portal Pagos, intentando escribirlo en el campo.
- **Mejora:** los botones de cada venta pendiente se reorganizan: **Editar**, **Marcar activa** y **Eliminar** en una fila, y **Revisar activación** debajo, a todo lo ancho.
- **Mejora:** la pantalla **Más** muestra solo el número de versión.

### v1.1
- **Nuevo:** navegación con barra inferior (**Inicio**, **Resumen** y **Más**).
- **Nuevo:** botón flotante **Nueva Venta**, visible en las pantallas principales. Se abre sobre la pantalla actual, sin dejar pestañas "pegadas" en el formulario.
- **Nuevo:** pantalla **Más** con **Planes**, **Portal Pagos**, **Números portados** y **Respaldo**.
- **Nuevo:** páginas web de pago y de consulta de números portados dentro de la app, con ajuste al teclado para que no tape el campo que estás escribiendo.
- **Corrección:** el RUT y el teléfono ahora se pueden copiar desde que se abre la app, sin cambiar de pestaña.
- **Nuevo:** el número de versión se muestra en **Más**.

### v1.0
- Versión inicial: registro de ventas, pendientes, resumen mensual con orden y filtros (plan, estado y rango de fechas), edición de ventas, copia de RUT y teléfono, administración de planes y respaldo en Google Drive.

---

## Licencia

Copyright (c) 2026 BlackbirdSCV. Todos los derechos reservados.

El código se publica como **muestra de trabajo** y puede consultarse para conocer cómo está hecho. No se permite copiarlo, modificarlo, redistribuirlo ni publicar versiones derivadas sin autorización escrita. La aplicación se distribuye de forma gratuita para uso personal.

## Contacto

<!-- Agrega aquí cómo contactarte: correo o enlace -->
