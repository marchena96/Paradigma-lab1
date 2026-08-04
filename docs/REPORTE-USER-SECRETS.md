# Reporte — User Secrets en HackerRank1

**Proyecto:** Laboratorio 1 · Paradigmas de Programación
**Tema:** Cómo y dónde se configura User Secrets en el proyecto local, y por qué las credenciales reales nunca suben a git.
**Lección 2:** los secretos son una decisión de arquitectura inicial, no un parche al final. Es mucho más difícil sanear un secreto ya expuesto en git.

---

## 1. Problema

La contraseña de Supabase no podía subirse al repositorio. Se necesita una conexión real a la base de datos en desarrollo, pero el repo es público y ningún secreto debe quedar commiteado.

## 2. Solución aplicada

- `appsettings.json` quedó con el placeholder `[SUPABASE-PASSWORD]` (estructura pública, sin secreto).
- La password real vive en **User Secrets** (archivo local fuera del repo), referenciado por el `UserSecretsId` del `.csproj`.

---

## 3. La cadena completa (5 piezas conectadas)

### 3.1 La "etiqueta" que identifica el cajón — `HackerRank1/HackerRank1.csproj:7`

```xml
<UserSecretsId>9cbbfeab-44b3-447c-8aa1-4d5adea68ef6</UserSecretsId>
```

Este GUID es el **nombre del cajón** de tu máquina. Es lo único que se sube a git: es público y sin secreto.

### 3.2 El cajón físico — fuera del repo

```powershell
C:\Users\Estudiantes UNA\AppData\Roaming\Microsoft\UserSecrets\9cbbfeab-44b3-447c-8aa1-4d5adea68ef6\secrets.json
```

Ojo la coincidencia: **el nombre de la carpeta = el `UserSecretsId` del csproj**. Ahí vive la password real. Está en `%APPDATA%`, no dentro de `Paradigma-lab1`, por eso git no la ve.

### 3.3 La llave que activa la carga — `HackerRank1/Properties/launchSettings.json:19`

```json
"ASPNETCORE_ENVIRONMENT": "Development"
```

User secrets **solo se cargan cuando el entorno es Development**.

### 3.4 El que hace el trabajo automático — `Program.cs:14`

```csharp
Host.CreateDefaultBuilder(args)
```

`CreateDefaultBuilder` hace esto por ti, sin que escribas nada: si el entorno es Development, agrega el proveedor **User Secrets** a la cadena de configuración, y sabe dónde buscar porque lee el `UserSecretsId` del ensamblado (generado desde el csproj en compilación).

### 3.5 El que lo consume — `Startup.cs:88`

```csharp
Configuration.GetConnectionString("DefaultConnection")
```

En runtime la cadena de configuración se fusiona en orden **(el último gana)**:

| Origen | Valor |
|---|---|
| `appsettings.json` | `"Password=[SUPABASE-PASSWORD]"` (placeholder) |
| **User Secrets (local)** | `"Password=SupabaseDb1!"` (real — pisa el placeholder) |
| Variables de entorno | (no definidas) |

---

## 4. Así se configuró originalmente (los 2 comandos)

Desde la carpeta `HackerRank1/`:

```powershell
dotnet user-secrets init        # creó el GUID en el csproj y habilitó la herramienta
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;Password=...;"
```

> El nombre de la clave usa `:` como separador de ruta (`ConnectionStrings:DefaultConnection`), que es exactamente lo que `GetConnectionString("DefaultConnection")` busca. Por eso en tu `secrets.json` la clave está escrita así, con dos puntos.

---

## 5. Verificación (dónde comprobarlo)

| Afirmación | Cómo comprobarla | Resultado |
|---|---|---|
| La contraseña no sube a git | `git grep -in "Password=" -- HackerRank1` | Solo placeholders, sin secretos |
| `appsettings.json` tiene el placeholder | `git show HEAD:HackerRank1/appsettings.json` | `[SUPABASE-PASSWORD]` |
| El secreto vive fuera del repo | `git ls-files \| Select-String secrets` | No existe `secrets.json` en git |
| El `UserSecretsId` lo conecta | `HackerRank1/HackerRank1.csproj:7` | GUID `9cbbfeab-…-a68ef6` |

---

## 6. Resumen

El `.csproj` guarda la **dirección del cajón** (`UserSecretsId`), el cajón vive en `%APPDATA%` **fuera del repo**, `CreateDefaultBuilder` lo abre automáticamente en Development, y su contenido **pisa** el placeholder de `appsettings.json` sin exponerse jamás a git.
