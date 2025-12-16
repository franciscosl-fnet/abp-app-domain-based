# Configuración Local para Resolución de Tenant por Subdominio

## Resumen de Cambios Implementados

Se ha implementado la resolución de tenant por subdominio siguiendo las recomendaciones de ABP Framework para Blazor WebAssembly. Los cambios permiten que cada tenant acceda a la aplicación mediante su propio subdominio (ej: `tenant1.localhost:44366`).

### Archivos Modificados

#### Backend (HttpApi.Host)
1. **AbpSolution1HttpApiHostModule.cs**
   - ✅ Configurado `AbpOpenIddictWildcardDomainOptions` para soportar subdominios wildcard
   - ✅ Agregado `AddDomainTenantResolver` con formato `{0}.localhost:44397`
   - ✅ Creado método `ConfigureTenantResolvers()`

2. **TokenWildcardIssuerValidator.cs** (Nuevo)
   - ✅ Clase para validar tokens con issuers wildcard (preparada para uso futuro si es necesario)

3. **appsettings.json**
   - ✅ Actualizado `CorsOrigins` con wildcards: `https://*.localhost:44366,https://*.localhost:44397`
   - ✅ Agregado `SelfUrlWithSubdomain` y `FrontEndUrl` con placeholders

#### Frontend (Blazor.Client)
1. **AbpSolution1BlazorClientModule.cs**
   - ✅ Modificado `ConfigureAuthentication()` para usar `TenantSubdomainHelper`
   - ✅ Modificado `ConfigureHttpClient()` para resolver URLs dinámicamente

2. **TenantSubdomainHelper.cs** (Nuevo)
   - ✅ Métodos auxiliares para convertir URLs con placeholders `{0}` al subdominio actual

3. **wwwroot/appsettings.json**
   - ✅ URLs actualizadas con placeholders: `https://{0}.localhost:44366` y `https://{0}.localhost:44397`

#### DbMigrator
1. **appsettings.json**
   - ✅ `RootUrl` actualizado con placeholders para los clientes OpenIddict

---

## Pasos para Configuración Local

### 1. Ejecutar DbMigrator
Después de los cambios, es **CRUCIAL** ejecutar el DbMigrator para actualizar las URIs de redirección en OpenIddict:

```powershell
cd src\AbpSolution1.DbMigrator
dotnet run
```

Esto actualizará la base de datos con las nuevas configuraciones de redirect URIs que incluyen wildcards.

### 2. Configurar el archivo hosts de Windows

Para probar subdominios en local, necesitas agregar entradas en el archivo `hosts`:

**Ubicación:** `C:\Windows\System32\drivers\etc\hosts`

**Agregar (como administrador):**
```
127.0.0.1 localhost
127.0.0.1 tenant1.localhost
127.0.0.1 tenant2.localhost
```

**Nota:** Agrega una línea por cada tenant que quieras probar.

### 3. Crear Tenants en la Aplicación

1. Ejecuta la aplicación normalmente en `https://localhost:44366`
2. Inicia sesión como admin
3. Ve a "Administration" > "Tenants"
4. Crea tenants con nombres que coincidan con tus subdominios (ej: `tenant1`, `tenant2`)

### 4. Probar con Subdominios

Una vez creados los tenants, accede a:
- **Sin tenant (host):** `https://localhost:44366`
- **Con tenant1:** `https://tenant1.localhost:44366`
- **Con tenant2:** `https://tenant2.localhost:44366`

**API Backend:**
- **Sin tenant:** `https://localhost:44397`
- **Con tenant1:** `https://tenant1.localhost:44397`

---

## Certificados SSL para Localhost

Si tienes problemas con certificados SSL en subdominios de localhost, puedes:

### Opción 1: Confiar en el certificado de desarrollo
```powershell
dotnet dev-certs https --trust
```

### Opción 2: Deshabilitar RequireHttpsMetadata en Development
En `appsettings.Development.json` del HttpApi.Host:
```json
{
  "AuthServer": {
    "RequireHttpsMetadata": false
  }
}
```

---

## Configuración para Producción

Cuando despliegues a producción, actualiza estos valores:

### Backend (HttpApi.Host)
**AbpSolution1HttpApiHostModule.cs - PreConfigureServices:**
```csharp
PreConfigure<AbpOpenIddictWildcardDomainOptions>(options =>
{
    options.EnableWildcardDomainSupport = true;
    options.WildcardDomainsFormat.Add("https://{0}.midominio.com");
});
```

**AbpSolution1HttpApiHostModule.cs - ConfigureTenantResolvers:**
```csharp
Configure<AbpTenantResolveOptions>(options =>
{
    options.AddDomainTenantResolver("{0}.midominio.com");
});
```

**appsettings.Production.json:**
```json
{
  "App": {
    "SelfUrl": "https://api.midominio.com",
    "SelfUrlWithSubdomain": "https://{0}.api.midominio.com",
    "FrontEndUrl": "https://{0}.midominio.com",
    "CorsOrigins": "https://*.midominio.com"
  }
}
```

### Frontend (Blazor.Client)
**wwwroot/appsettings.Production.json:**
```json
{
  "App": {
    "SelfUrl": "https://{0}.midominio.com"
  },
  "AuthServer": {
    "Authority": "https://{0}.api.midominio.com",
    "ClientId": "AbpSolution1_Blazor",
    "ResponseType": "code"
  },
  "RemoteServices": {
    "Default": {
      "BaseUrl": "https://{0}.api.midominio.com"
    }
  }
}
```

### DbMigrator
**appsettings.Production.json:**
```json
{
  "OpenIddict": {
    "Applications": {
      "AbpSolution1_Blazor": {
        "ClientId": "AbpSolution1_Blazor",
        "RootUrl": "https://{0}.midominio.com"
      },
      "AbpSolution1_Swagger": {
        "ClientId": "AbpSolution1_Swagger",
        "RootUrl": "https://{0}.api.midominio.com/"
      }
    }
  }
}
```

---

## DNS y Certificados en Producción

### Configuración DNS
Necesitarás un wildcard DNS record:
```
*.midominio.com -> IP_DEL_SERVIDOR
```

### Certificado SSL Wildcard
Obtén un certificado SSL wildcard de Let's Encrypt o tu proveedor:
```
*.midominio.com
```

---

## Troubleshooting

### Problema: "Tenant not found"
- Verifica que el nombre del tenant en la base de datos coincida exactamente con el subdominio
- Revisa que el formato en `AddDomainTenantResolver` sea correcto

### Problema: CORS errors
- Asegúrate de que `CorsOrigins` incluye wildcards: `https://*.localhost:44366`
- Verifica que los puertos coincidan con tu configuración

### Problema: Redirect URI mismatch en OpenIddict
- Ejecuta el DbMigrator nuevamente
- Verifica que las RootUrl en DbMigrator/appsettings.json tengan el placeholder `{0}`

### Problema: Certificado SSL no válido para subdominios
- Confía en el certificado de desarrollo: `dotnet dev-certs https --trust`
- O establece `RequireHttpsMetadata: false` en desarrollo

---

## Verificación Final

1. ✅ DbMigrator ejecutado
2. ✅ Archivo hosts actualizado con subdominios
3. ✅ Tenants creados en la aplicación
4. ✅ Certificados de desarrollo confiables
5. ✅ Acceso exitoso a `https://tenant1.localhost:44366`

---

## Referencias

- [ABP Multi-tenancy Documentation](https://abp.io/docs/latest/framework/architecture/multi-tenancy)
- [ABP Support Thread #10222](https://abp.io/support/questions/10222/DomainSubdomain-Tenant-Resolver-not-working)
- [ABP Wildcard Domains](https://abp.io/docs/latest/framework/infrastructure/openiddict/wildcard-domains)
