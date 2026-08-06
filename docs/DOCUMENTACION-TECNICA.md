# Documentación Técnica — HackerRank1 (LibraryService API)

**Proyecto:** Laboratorio 1 · Paradigmas de Programación
**Objetivo del lab:** Implementar los endpoints faltantes de `LibrariesController` (DELETE) y `BooksController` (POST, GET), junto con sus servicios, y registrarlos en `Startup.cs` para habilitar Inyección de Dependencias (DI).

Esta documentación resume **todo el trabajo realizado hasta llegar al punto actual** (entorno configurado, base de datos conectada a Supabase, API ejecutándose) y lo que sigue (implementación de endpoints pendientes).

---

## 1. Resumen del estado actual

| Aspecto | Estado |
|---|---|
| Compilación (`dotnet build`) | ✅ 0 errores / 23 warnings (cosméticos) |
| Conexión a BD (Supabase/PostgreSQL) | ✅ Verificada en vivo |
| Migración aplicada (`InitialCreate`) | ✅ Aplicada en Supabase |
| Ejecución (`dotnet run --project HackerRank1`) | ✅ Arranca en `https://localhost:7098` |
| Swagger | ✅ Disponible en `/swagger` |
| Autenticación JWT | ✅ Funcional (login + validación de token) |
| Endpoints `Libraries` GET/POST/PUT | ✅ Implementados |
| Endpoints `Libraries` DELETE | ❌ Pendiente |
| Endpoints `Books` GET | ✅ Implementado (sin validación de librería) |
| Endpoints `Books` POST/UPDATE/DELETE | ❌ Pendiente |

---

## 2. Stack tecnológico

| Componente | Versión | Uso |
|---|---|---|
| .NET SDK | 8.0 | Plataforma de la solución |
| ASP.NET Core | 8.0 | Web API |
| EF Core | 8.0.2 | ORM (acceso a datos) |
| Npgsql (EF provider) | 8.0.2 | Driver para PostgreSQL |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.16 | Autenticación JWT |
| Swashbuckle (Swagger) | 6.5.0 | Documentación interactiva |
| Newtonsoft.Json | 13.0.3 | Serialización de DTOs |
| xUnit + FluentAssertions | 2.9.3 / 5.0.0 | Tests de integración |
| Supabase (PostgreSQL 15) | — | Base de datos en la nube |

> **Nota:** el README oficial fue actualizado para reflejar el estado real: el proyecto compila sobre **.NET 8.0** (revisado en `.csproj`) y el proyecto se llama **HackerRank1** (el README original indicaba `.NET 6.0` y `LibraryService.WebAPI`).

---

## 3. Arquitectura

### 3.1 Solución y proyectos

```
Paradigma-lab1 (solución HackerRank1.sln)
├── HackerRank1/                         → Web API (proyecto principal, net8.0)
└── LibraryService.Integration.Test/     → Tests de integración (xUnit, net8.0)
```

### 3.2 Patrón de diseño

Arquitectura **Layered clásica** (en capas), sin capa de repositorios:

```
HTTP Request → Controller → Service → DbContext (EF Core) → PostgreSQL (Supabase)
```

- **Controller:** recibe/valida la petición y devuelve el `StatusCode` correspondiente.
- **Service:** lógica de acceso a datos contra el `LibraryContext`.
- **DbContext:** mapea las entidades `Book` y `Library` a las tablas `Books` y `Libraries`.
- Los DTOs (`BookForm`, `LibraryForm`, `User`) existen y se usan con Newtonsoft (`[JsonProperty]`), aunque algunos controllers aún trabajan con entidades directamente.

### 3.3 Arranque de la aplicación (hosting legacy)

`Program.cs` usa el modelo clásico:

```csharp
Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
```

`Startup.cs` es responsable de:
1. Cargar `JwtSettings` desde configuración (o fallar si no existen).
2. Registrar servicios en DI (secciones 2 y 6):
   - `IAuthenticationService → AuthenticationService` (Scoped)
   - `ILibrariesService → LibrariesService` (Transient)
   - `IBooksService → BooksService` (Transient)
   - `LibraryContext` con `AddDbContextPool` + `UseNpgsql` + retry de conexión (pool = 20).
3. Configurar Autenticación (JWT Bearer) y Autorización.
4. Configurar CORS para el frontend de desarrollo (`http://localhost:5173`, Vite).
5. Ejecutar migraciones al arranque: `db.Database.Migrate()` (inicio de `Configure`).
6. Montar el pipeline: Routing → CORS → Authentication → Authorization → Endpoints.

### 3.4 Modelo de datos

**`Library`** → tabla `Libraries`
| Columna | Tipo |
|---|---|
| Id | integer (PK, Identity) |
| Name | text (NOT NULL) |
| Location | text (NOT NULL) |

**`Book`** → tabla `Books`
| Columna | Tipo |
|---|---|
| Id | integer (PK, Identity) |
| Name | text (NOT NULL) |
| Category | text (NOT NULL) |
| LibraryId | integer (FK → Libraries.Id, **ON DELETE CASCADE**) |

La relación se define en la migración `20260528004745_InitialCreate`:
el borrado de una librería **borra en cascada sus libros** en la BD.

---

## 4. Estructura del proyecto

```
HackerRank1/
├── Program.cs                     → Host builder (legacy) + Startup
├── Startup.cs                     → DI, JWT, CORS, Swagger, Migrate()
├── appsettings.json               → Connection string (con placeholder) + JwtSettings
├── Properties/launchSettings.json → Perfiles de arranque, puertos, env Development
├── Controllers/
│   ├── AuthController.cs          → POST /login (emite JWT)
│   ├── LibrariesController.cs     → GET, GET{id}, POST, PUT (+ DELETE pendiente)
│   └── BooksController.cs         → GET (POST/UPDATE/DELETE pendientes)
├── Data/
│   └── LibraryContext.cs          → DbContext + entidades Book y Library
├── Services/
│   ├── AuthenticationService.cs   → valida credenciales hardcodeadas (admin/1234)
│   ├── LibraryService.cs          → CRUD de librerías (Delete pendiente)
│   └── BookService.cs             → CRUD de libros (Add/Update/Delete pendientes)
├── DTO/
│   ├── BookForm.cs                → {id, name, category, libraryId}
│   ├── LibraryForm.cs             → {id, name, location}
│   └── User.cs                    → {id, email, password, role}
├── Entities/
│   └── JwtSettings.cs             → {issuer, audience, secretKey}
├── Helpers/
│   └── TokenGenerator.cs          → firma el JWT (HS256, expira en 1h)
└── Migrations/
    └── 20260528004745_InitialCreate.cs → crea Libraries y Books (+FK cascade)

LibraryService.Integration.Test/
├── IntegrationTest.cs             → 3 tests (add book, get books, delete library)
└── Extensions/HttpResponseExtensions.cs

IntegrationTest/                   → carpeta huérfana (NO está en la solución)
```

---

## 5. Procesos realizados (bitácora)

### Fase 1 — Análisis de arquitectura
- Se inspeccionó la solución completa: se identificaron los 2 proyectos, el patrón en capas, la ausencia de repositorios y el hosting legacy.
- Se detectaron los puntos marcados como "Complete the implementation" (`NotImplementedException`) y el comentario "Implement the DELETE method below" en `LibrariesController`.
- Se confirmó que los servicios **ya están registrados** en `Startup.cs` (líneas 74-75), por lo que el lab no requiere agregar DI.

### Fase 2 — Configuración del entorno (conexión a Supabase)

1. **Instalación de la herramienta `dotnet-ef` (global tool v8.0.2)**, requerida para comandos de migración:
   ```
   dotnet tool install --global dotnet-ef --version 8.0.2
   ```
2. **Creación del proyecto Supabase** (nuevo proyecto vacío) para obtener credenciales de PostgreSQL (host pooler `*.supabase.com`, puerto 5432, usuario `postgres.<ref>`).
3. **Restauración de `appsettings.Development.json`**: el archivo existía con 0 bytes (JSON inválido), lo que impedía cargar configuración. Se restauró su contenido válido.
4. **Decisión de seguridad — User Secrets**: se acordó guardar la contraseña real **solo** en User Secrets y dejar un placeholder en `appsettings.json`, para poder subir el repositorio sin filtrar credenciales.
5. **Bug corregido en la connection string**: originalmente usaba una coma (`,`) en lugar de punto y coma (`;`) antes de `Pooling=false`, lo que rompía el parsing de Npgsql. Se corrigió el separador.
6. **Validación de conectividad** con:
   ```
   dotnet ef migrations list --project HackerRank1
   ```
   La lista se leyó exitosamente contra Supabase, confirmando conexión + autenticación válidas.
7. **Aplicación de la migración `InitialCreate`**: se aplicó automáticamente al arrancar la app vía `db.Database.Migrate()` en `Startup.cs` (no con `dotnet ef database update`). Estado confirmado con `dotnet ef migrations list`: **sin migraciones pendientes**.

### Fase 3 — Verificación en vivo
Con la app corriendo en `https://localhost:7098` (PID 18864), se probaron contra Supabase:

| Prueba | Resultado |
|---|---|
| `GET /swagger/v1/swagger.json` | 200 (Swagger OK) |
| `POST /login` con `role` en el body | 200, devuelve JWT (~449 chars) |
| `POST /login` sin `role` | 400 (validación de DTO) |
| `GET /api/libraries` | 200, lista vacía (0 librerías) |
| `GET /api/libraries/{id}` inexistente | 404 |
| `GET /api/libraries/1/books` sin token | 401 (requiere JWT) |
| `GET /api/libraries/1/books` con token | 200 |

### Fase 4 — Control de versiones
- Commit `bb96648` — "chore: configurar conexion a Supabase con user-secrets":
  - `HackerRank1.csproj` (agregado `UserSecretsId`).
  - `appsettings.json` (connection string con placeholder, sin credenciales).
- Push exitoso a `origin/main` (`git push origin HEAD`).
- Working tree limpio.

---

## 6. Configuración actual

### 6.1 Connection string

`appsettings.json` (con **placeholder**, seguro para commitear):

```
Host=aws-1-us-west-2.pooler.supabase.com;Port=5432;Database=postgres;
Username=postgres.gyktxhzyeyisdbafvvpm;Password=[SUPABASE-PASSWORD];
SSL Mode=Require;Trust Server Certificate=true; Pooling=false
```

**Credencial real** (password) → **solo en User Secrets**:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;Password=<real>;..."
```

- `UserSecretsId`: `9cbbfeab-44b3-447c-8aa1-4d5adea68ef6`
- Ubicación local: `%APPDATA%\Microsoft\UserSecrets\9cbbfeab-44b3-447c-8aa1-4d5adea68ef6\secrets.json`
- Las User Secrets **solo se cargan en entorno `Development`** (ya configurado en `launchSettings.json`).

### 6.2 JWT

- `Issuer=MyApp`, `Audience=localhost:80`, `SecretKey` en `appsettings.json`.
- Emisión: `POST /login` con credenciales `admin` / `1234` (hardcodeadas en `AuthenticationService`).
- **Importante:** el body de login debe incluir el campo `role` (ej. `{"email":"admin","password":"1234","role":"admin"}`), si no el endpoint responde 400.
- El token firma con HS256, expira a 1 hora, e incluye claims `NameIdentifier`, `Email`, `Role`.
- Endpoints protegidos con `[Authorize]`: `GET /api/libraries/{libraryId}/books`.

### 6.3 Puertos y entorno

`launchSettings.json`, perfil `HackerRank1`:
- `https://localhost:7098` · `http://localhost:5219`
- `launchUrl: swagger`, `ASPNETCORE_ENVIRONMENT=Development`

### 6.4 ¿Qué implica no tener credenciales reales en el repo?

El repositorio (incluida esta documentación y el `appsettings.json`) **no contiene ninguna credencial real**, solo el placeholder `[SUPABASE-PASSWORD]`. Esto fue una decisión deliberada de seguridad, con beneficios y costos concretos:

**Beneficios:**
- El repositorio es **seguro para compartir**: se puede subir a GitHub, hacer público o entregar al profesor sin filtrar la contraseña de Supabase.
- Sigue el patrón de configuración externa (12-factor): las credenciales viven fuera del código.
- El commit `bb96648` ya está en `origin/main` sin riesgo de exposición.

**Costos / consecuencias (onboarding):**
1. **Quien clone el repo no puede correr la app tal cual.** El `db.Database.Migrate()` al arrancar falla si la conexión no es válida, y el placeholder no lo es.
2. **Configuración manual obligatoria** en cada máquina. Cada desarrollador (o el profesor) necesita registrar la credencial real en su entorno local, de dos formas posibles:
   - Ejecutar `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection-string-con-password-real>"` desde `HackerRank1/`, **o**
   - Tener su propio proyecto Supabase y ajustar host/usuario/contraseña en esa misma variable.
3. **Solo funciona en entorno `Development`**: las User Secrets **no se cargan en `Production`**. Si la API se publicara a producción con esta configuración, la BD no conectaría.
4. **La password vive en un solo lugar físico**: `%APPDATA%\Microsoft\UserSecrets\9cbbfeab-44b3-447c-8aa1-4d5adea68ef6\secrets.json`. Si se pierde o se cambia de equipo, hay que recuperarla desde el panel de Supabase (`Settings → Database → Reset database password`) y re-registrarla.

**Conclusión:** el placeholder garantiza que el repositorio sea seguro, pero traslada la responsabilidad de "poseer la credencial" a fuera del repo. Para el lab, la password se conserva en el entorno local y, si el profesor necesita probar, se le entrega **fuera del repositorio** (chat/correo) o se le documenta el paso 2.

---

## 7. Cómo ejecutar

### 7.1 Primera vez en una máquina nueva (onboarding)

1. Clonar el repositorio.
2. Instalar la herramienta `dotnet-ef` v8.0.2 (si no está): `dotnet tool install --global dotnet-ef --version 8.0.2`.
3. Tener acceso a una BD PostgreSQL válida (el proyecto Supabase existente **o** crear uno propio).
4. Registrar la credencial real en User Secrets (desde `HackerRank1/`):
   ```powershell
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=<host>;Port=5432;Database=postgres;Username=<user>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true;Pooling=false"
   ```
5. Compilar y arrancar (ver sección 7.2).

> ⚠️ Sin credenciales válidas la app compila, pero `Migrate()` falla al arrancar porque la conexión a la BD es inválida (placeholder).

### 7.2 Comandos

```powershell
# Restaurar paquetes y compilar (desde la raíz de la solución)
dotnet clean && dotnet restore && dotnet build

# Arrancar la API (¡siempre con --project, la solución tiene 2 proyectos!)
dotnet run --project HackerRank1
# → Swagger en https://localhost:7098/swagger

# Ver el estado de migraciones
dotnet ef migrations list --project HackerRank1

# Ejecutar tests de integración
dotnet test
```

> ⚠️ `dotnet run` **sin** `--project` falla en la raíz con "No se ha podido encontrar un proyecto para ejecutar", porque la solución contiene dos proyectos.
> ⚠️ Si la app está corriendo, detenerla antes de compilar (el `.exe` queda bloqueado → errores MSB3027/MSB3021).

---

## 8. Lo que sigue — Implementación de endpoints (según el README y los tests)

### 8.1 Requisitos funcionales (del README del lab)

| Endpoint | Éxito | Si no existe la librería |
|---|---|---|
| `POST api/libraries/{libraryId}/books` | **201** | **404** |
| `GET api/libraries/{libraryId}/books` | **200** (lista de libros) | **404** |
| `DELETE api/libraries/{libraryId}` | **204** | **404** |

### 8.2 Tareas concretas pendientes

1. **`LibrariesService.Delete(Library library)`** — `HackerRank1/Services/LibraryService.cs:57`
   - Implementar: quitar de `_libraryContext.Libraries`, guardar cambios, devolver `bool`.
2. **`LibrariesController` — método `DELETE`** — `HackerRank1/Controllers/LibrariesController.cs:56`
   - Obtener la librería; si no existe → `404`; si existe → borrar → `204`.
   - El borrado de libros asociados lo hace la **FK con CASCADE** a nivel BD.
3. **`BooksService.Add / Update / Delete`** — `HackerRank1/Services/BookService.cs:28,34,40`
4. **`BooksController` — método `POST`** (no existe hoy)
   - Recibir `BookForm` (o `Book`), asignar `LibraryId` desde la ruta, insertar → `201`.
   - Si la librería no existe → `404`.
5. **`BooksController.GetAll` — validar existencia de la librería** — `BooksController.cs:25`
   - Hoy devuelve `200` aunque la librería no exista; debe devolver `404` en ese caso (lo exige el test).

### 8.3 Contrato que imponen los tests de integración (`IntegrationTest.cs`)

- `TestAddBook_Ok_GetBook_NotFound`:
  - `POST /api/libraries/1/books` con `{"name": "Test book 1"}` → **201**
  - `POST /api/libraries/100/books` → **404**
- `TestGetBooks_Ok_NotFound`:
  - `GET /api/libraries/2/books` → **200** con 0 libros
  - `GET /api/libraries/1/books` → **200** con 2 libros
  - `GET /api/libraries/31232/books` → **404**
- `TestDeleteLibrary`:
  - `DELETE /api/libraries/1` → **204**
  - `GET /api/libraries/1/books` (tras borrar la librería) → **404**
  - `DELETE /api/libraries/1` de nuevo → **404**

### 8.4 Detalles a cuidar en la implementación

- **`Category` NOT NULL en PostgreSQL**: los tests envían `{"name": ...}` **sin** `category` (con SQLite no importa, pero en Supabase sí). Al mapear el `BookForm` → `Book`, dar default `string.Empty` a `Category` cuando venga `null`.
- Los tests usan **SQLite en memoria** (con `EnsureCreated`) sustituyendo el `LibraryContext` por DI, así que el código no debe depender de detalles de Npgsql.
- Tras eliminar una librería, `GET books` de esa librería debe devolver `404` (librería inexistente) — el cascade borra los libros, pero el `404` sale de la validación de la librería.

---

## 9. Observaciones y riesgos (opcionales, no bloquean)

| Tema | Detalle | Impacto |
|---|---|---|
| 23 warnings de build | `CS8618` (propiedades no-nullable), `CS8603/8604/8625` (posibles nulos), `xUnit1031` (`.Result` bloqueante en tests) | Ninguno; cosmético |
| Versiones de EF en tests | `LibraryService.Integration.Test.csproj` referencia EF Core/Mvc.Testing **6.0.0**, la API usa EF **8.0.2** | Riesgo bajo; los tests compilan hoy |
| README oficial | Actualizado al estado real (`.NET 8.0`, proyecto `HackerRank1`, credenciales vía User Secrets, sección de onboarding) | Resuelto en esta sesión |
| Carpeta `IntegrationTest/` huérfana | No está incluida en la solución | Ignorable |
| Login con credenciales hardcodeadas | `admin`/`1234` y `SecretKey` en `appsettings.json` | Solo ambiente de lab; no usar en producción |
| `.github/workflows/` vacío | No hay CI configurado | — |

---

## 10. Plan de implementación de endpoints

> Estado: **aprobado** — este plan se documenta ANTES de tocar código y su ejecución se registra en la sección 11.

### 10.1 Alcance

| Archivo | Cambio |
|---|---|
| `Services/LibraryService.cs` | Implementar `Delete(Library)` (era `NotImplementedException`) |
| `Services/BookService.cs` | Implementar `Add`, `Update`, `Delete` (eran `NotImplementedException`) |
| `Controllers/LibrariesController.cs` | Agregar `DELETE api/libraries/{libraryId}` |
| `Controllers/BooksController.cs` | Agregar `POST api/libraries/{libraryId}/books` + validar librería en `GET` |

Sin cambios en `Startup.cs` (DI ya registrada) ni en la BD (no requiere nueva migración).

### 10.2 Orden de trabajo (por dependencias)

**Fase 1 — Servicios** (los controllers dependen de ellos):
1. `LibrariesService.Delete` → `Remove` + `SaveChangesAsync` + `return true`. Los libros asociados los borra la FK con CASCADE a nivel BD.
2. `BooksService.Add` → `AddAsync` + `SaveChangesAsync` + `return book`.
3. `BooksService.Update` → buscar por `Id`, sobreescribir campos, `Update` + guardar.
4. `BooksService.Delete` → `Remove` + guardar + `return true`.

**Fase 2 — Controllers**:
5. `LibrariesController.Delete` → buscar librería; `null` ⇒ 404; existe ⇒ borrar ⇒ 204.
6. `BooksController.GetAll` → validar existencia de la librería; `null` ⇒ 404; si no ⇒ lista (200).
7. `BooksController.Add` (POST) → recibir `BookForm`, validar librería ⇒ 404; mapear a `Book` (`LibraryId` de la ruta, `Category ?? string.Empty`); `Add` ⇒ **201**.

**Fase 3 — Verificación**:
8. `dotnet build` → 0 errores.
9. `dotnet test --project LibraryService.Integration.Test` → 3 tests en verde.
10. Smoke test manual contra Supabase (POST/GET books, DELETE library).

### 10.3 Decisiones de diseño

| Decisión | Resolución | Motivo |
|---|---|---|
| Body del POST books | Usar **`BookForm`** (DTO) y mapear a entidad | Contrato camelCase del README + permite default de `Category` |
| `Category` ausente en el body | Default a `string.Empty` | NOT NULL en Postgres; los tests no la envían |
| Borrado de libros al eliminar librería | Vía FK **CASCADE** de la BD | Ya definido en la migración `InitialCreate` |
| Endpoints books `PUT`/`DELETE` | Se implementan los servicios pero **no** se exponen endpoints | El lab solo exige POST y GET para books |
| Respuesta del POST | **201 Created** | Lo exige README y tests |

### 10.4 Contrato de comportamiento (verificado contra `IntegrationTest.cs`)

| Caso | Request | Respuesta esperada |
|---|---|---|
| POST book a librería existente | `POST /api/libraries/1/books` `{"name": "..."}` | **201** |
| POST book a librería inexistente | `POST /api/libraries/100/books` | **404** |
| GET books de librería existente | `GET /api/libraries/1/books` | **200** con lista |
| GET books de librería inexistente | `GET /api/libraries/31232/books` | **404** |
| DELETE librería existente | `DELETE /api/libraries/1` | **204** |
| GET books tras borrar librería | `GET /api/libraries/1/books` | **404** |
| DELETE librería inexistente | `DELETE /api/libraries/1` | **404** |
