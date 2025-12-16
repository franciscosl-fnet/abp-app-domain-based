# Guía de Implementación: Resolución de Tenant por Subdominio

Esta guía detalla los pasos necesarios para implementar la resolución de tenant basada en subdominios en una aplicación ABP Framework con Blazor WebAssembly.

## Contexto

La resolución por subdominio permite que cada tenant acceda a la aplicación mediante su propio subdominio (ej: `tenant1.midominio.com`, `tenant2.midominio.com`), proporcionando una experiencia más personalizada y profesional.

## Arquitectura de la Solución

La solución se basa en:
- **Backend (HttpApi.Host)**: Configurar el resolver de tenant por dominio y validación de tokens con wildcards
- **Frontend (Blazor WebAssembly)**: Configurar URLs dinámicas basadas en el subdominio actual
- **Data Seeding**: Actualizar automáticamente las URIs de redirección en OpenIddict cuando se crea un tenant
- **Custom SignIn Manager**: Gestionar el inicio de sesión considerando el contexto del tenant

---

## PASO 1: Configuración del DbMigrator (appsettings.json)

**Archivo**: `src/AbpSolution1.DbMigrator/appsettings.json`

### Modificar las URLs de los clientes OpenIddict

Actualizar las configuraciones de los clientes para incluir el placeholder `{0}` donde irá el subdominio del tenant:

```json
"OpenIddict": {
  "Applications": {
    "AbpSolution1_Blazor": {
      "RootUrl": "https://{0}.midominio.com:44307"
    },
    "AbpSolution1_Swagger": {
      "RootUrl": "https://{0}.api.midominio.com:44399"
    }
  }
}
```

**IMPORTANTE**: Ejecutar el DbMigrator después de estos cambios para actualizar la base de datos.

---

## PASO 2: Configuración del Backend (HttpApi.Host)

### 2.1 Actualizar appsettings.json

**Archivo**: `src/AbpSolution1.HttpApi.Host/appsettings.json`

```json
{
  "App": {
    "SelfUrl": "https://{0}.api.midominio.com:44399",
    "SelfUrlWithSubdomain": "https://{0}.api.midominio.com:44399",
    "FrontEndUrl": "https://{0}.midominio.com:44307",
    "CorsOrigins": "https://*.midominio.com,http://*.midominio.com:44307,https://*.api.midominio.com,http://*.api.midominio.com:44399"
  }
}
```

**Notas**:
- `SelfUrlWithSubdomain`: URL con placeholder para el subdominio
- `FrontEndUrl`: URL del frontend con placeholder
- `CorsOrigins`: Usar wildcards `*` para permitir todos los subdominios

### 2.2 Crear TokenWildcardIssuerValidator

**Archivo**: `src/AbpSolution1.HttpApi.Host/TokenWildcardIssuerValidator.cs` (NUEVO)

```csharp
using Microsoft.IdentityModel.Tokens;

namespace Owl.TokenWildcardIssuerValidator;

public class TokenWildcardIssuerValidator
{
    public static string IssuerValidator(string issuer, SecurityToken token, TokenValidationParameters parameters)
    {
        var validIssuers = parameters.ValidIssuers;
        if (validIssuers != null)
        {
            foreach (var validIssuer in validIssuers)
            {
                if (IsWildcardMatch(issuer, validIssuer))
                {
                    return issuer;
                }
            }
        }
        throw new SecurityTokenInvalidIssuerException($"IDX10205: Issuer validation failed. Issuer: '{issuer}'");
    }

    private static bool IsWildcardMatch(string issuer, string pattern)
    {
        if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(issuer))
            return false;

        if (!pattern.Contains("{0}"))
            return issuer.Equals(pattern, StringComparison.OrdinalIgnoreCase);

        var parts = pattern.Split(new[] { "{0}" }, StringSplitOptions.None);
        if (parts.Length != 2)
            return false;

        return issuer.StartsWith(parts[0], StringComparison.OrdinalIgnoreCase) &&
               issuer.EndsWith(parts[1], StringComparison.OrdinalIgnoreCase);
    }
}
```

### 2.3 Crear BookstoreSigninManager personalizado

**Archivo**: `src/AbpSolution1.HttpApi.Host/AbpSolution1SigninManager.cs` (NUEVO)

```csharp
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Volo.Abp.DependencyInjection;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace AbpSolution1;

[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(SignInManager<Volo.Abp.Identity.IdentityUser>))]
public class AbpSolution1SigninManager : Microsoft.AspNetCore.Identity.SignInManager<Volo.Abp.Identity.IdentityUser>
{
    public AbpSolution1SigninManager(
        Microsoft.AspNetCore.Identity.UserManager<Volo.Abp.Identity.IdentityUser> userManager,
        Microsoft.AspNetCore.Http.IHttpContextAccessor contextAccessor,
        Microsoft.AspNetCore.Identity.IUserClaimsPrincipalFactory<Volo.Abp.Identity.IdentityUser> claimsFactory,
        Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Identity.IdentityOptions> optionsAccessor,
        Microsoft.Extensions.Logging.ILogger<Microsoft.AspNetCore.Identity.SignInManager<Volo.Abp.Identity.IdentityUser>> logger,
        Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider schemes,
        Microsoft.AspNetCore.Identity.IUserConfirmation<Volo.Abp.Identity.IdentityUser> confirmation)
        : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
    }

    public override async Task<SignInResult> PasswordSignInAsync(string userName, string password, bool isPersistent, bool lockoutOnFailure)
    {
        return await base.PasswordSignInAsync(userName, password, isPersistent, lockoutOnFailure);
    }

    public override async Task<SignInResult> PasswordSignInAsync(IdentityUser user, string password, bool isPersistent, bool lockoutOnFailure)
    {
        return await base.PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure);
    }
}
```

### 2.4 Modificar AbpSolution1HttpApiHostModule

**Archivo**: `src/AbpSolution1.HttpApi.Host/AbpSolution1HttpApiHostModule.cs`

#### Agregar using statements necesarios:
```csharp
using OpenIddict.Server;
using Owl.TokenWildcardIssuerValidator;
using Volo.Abp.OpenIddict.WildcardDomains;
using Microsoft.AspNetCore.Identity;
```

#### En el método PreConfigureServices:
```csharp
public override void PreConfigureServices(ServiceConfigurationContext context)
{
    var configuration = context.Services.GetConfiguration();
    
    // Registrar el SignInManager personalizado
    PreConfigure<IdentityBuilder>(identityBuilder =>
    {
        identityBuilder.AddSignInManager<AbpSolution1SigninManager>();
    });
    
    // Si usas OpenIddict Wildcard Domains (comentado por ahora)
    // var frontEndUrlWithSubdomainPlaceholder = configuration["App:FrontEndUrl"];
    // Configure<AbpOpenIddictWildcardDomainOptions>(options =>
    // {
    //     options.EnableWildcardDomainSupport = true;
    //     options.WildcardDomainsFormat.Add($"{frontEndUrlWithSubdomainPlaceholder}/signin-oidc");
    //     options.WildcardDomainsFormat.Add($"{frontEndUrlWithSubdomainPlaceholder}/signout-callback-oidc");
    // });
    
    // ... resto de PreConfigureServices
}
```

#### En el método ConfigureServices (al inicio):
```csharp
public override void ConfigureServices(ServiceConfigurationContext context)
{
    var configuration = context.Services.GetConfiguration();
    
    // INICIO: Configuración de gestión de subdominios
    var selfDomainWithSubdomainPlaceholder = configuration["App:SelfUrlWithSubdomain"];
    var selfDomain = configuration["App:SelfUrl"];
    
    // Configurar el resolver de tenant por dominio
    Configure<AbpTenantResolveOptions>(options =>
    {
        options.AddDomainTenantResolver(selfDomainWithSubdomainPlaceholder);
    });
    
    var frontEndUrlWithSubdomainPlaceholder = configuration["App:FrontEndUrl"];
    Configure<AbpOpenIddictWildcardDomainOptions>(options =>
    {
        options.EnableWildcardDomainSupport = true;
        options.WildcardDomainsFormat.Add($"{frontEndUrlWithSubdomainPlaceholder}/signin-oidc");
        options.WildcardDomainsFormat.Add($"{frontEndUrlWithSubdomainPlaceholder}/signout-callback-oidc");
    });
    
    // Configurar validación de issuer con wildcards
    Configure<OpenIddictServerOptions>(options =>
    {
        options.TokenValidationParameters.IssuerValidator = TokenWildcardIssuerValidator.IssuerValidator;
        options.TokenValidationParameters.ValidIssuers = new[]
        {
            selfDomain.EnsureEndsWith('/'),
            selfDomainWithSubdomainPlaceholder.EnsureEndsWith('/')
        };
    });
    // FIN: Configuración de gestión de subdominios
    
    // ... resto de ConfigureServices
}
```

---

## PASO 3: Configuración del Data Seeding

### 3.1 Crear AbpSolution1IdentityDataSeedContributor

**Archivo**: `src/AbpSolution1.Domain/Identity/AbpSolution1IdentityDataSeedContributor.cs` (NUEVO)

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.OpenIddict.Applications;
using Volo.Abp.TenantManagement;

namespace AbpSolution1.Identity;

[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IdentityDataSeedContributor))]
public class AbpSolution1IdentityDataSeedContributor : IdentityDataSeedContributor, ITransientDependency
{
    private readonly ICurrentTenant CurrentTenant;
    private readonly IRepository<OpenIddictApplication> OpenIdDictApplicationRepository;
    private readonly IRepository<Tenant> TenantRepository;

    public AbpSolution1IdentityDataSeedContributor(
        IIdentityDataSeeder identityDataSeeder,
        ICurrentTenant currentTenant,
        IRepository<OpenIddictApplication> openIdDictApplicationRepository,
        IRepository<Tenant> tenantRepository
    )
    : base(identityDataSeeder)
    {
        CurrentTenant = currentTenant;
        OpenIdDictApplicationRepository = openIdDictApplicationRepository;
        TenantRepository = tenantRepository;
    }

    public override async Task SeedAsync(DataSeedContext context)
    {
        await base.SeedAsync(context);

        var tenantId = context?.TenantId;

        var tenantName = await TenantRepository
            .GetQueryableAsync()
            .Result
            .Where(x => x.Id == tenantId)
            .Select(x => x.Name).FirstOrDefaultAsync();

        using (CurrentTenant.Change(tenantId))
        {
            if (!tenantName.IsNullOrWhiteSpace())
            {
                tenantName = tenantName.ToLower();
                
                // Actualizar URIs de redirección en OpenIddict
                var appChanged = false;
                var blazorOpenIdDictApplication = await OpenIdDictApplicationRepository.FirstOrDefaultAsync(x =>
                    x.ClientId == "AbpSolution1_Blazor");

                if (blazorOpenIdDictApplication != null)
                {
                    // Actualizar RedirectUris
                    var currentRedirectUris = blazorOpenIdDictApplication.RedirectUris
                        .Replace("[", string.Empty)
                        .Replace("]", string.Empty)
                        .Split(',')
                        .ToList();

                    var tenantRedirectUri =
                        currentRedirectUris.First().Replace("https://", $"https://{tenantName}.");

                    if (!currentRedirectUris.Contains(tenantRedirectUri))
                    {
                        currentRedirectUris.AddLast(tenantRedirectUri);
                        blazorOpenIdDictApplication.RedirectUris = $"[{currentRedirectUris.JoinAsString(",")}]";
                        Console.WriteLine($"Updated RedirectUris: {blazorOpenIdDictApplication.RedirectUris}");
                        appChanged = true;
                    }

                    // Actualizar PostLogoutRedirectUris
                    var currentPostLogoutRedirectUris = blazorOpenIdDictApplication.PostLogoutRedirectUris
                        .Replace("[", string.Empty)
                        .Replace("]", string.Empty)
                        .Split(',')
                        .ToList();

                    var tenantLogoutUri =
                        currentPostLogoutRedirectUris.First().Replace("https://", $"https://{tenantName}.");

                    if (!currentPostLogoutRedirectUris.Contains(tenantLogoutUri))
                    {
                        currentPostLogoutRedirectUris.AddLast(tenantLogoutUri);
                        blazorOpenIdDictApplication.PostLogoutRedirectUris =
                            $"[{currentPostLogoutRedirectUris.JoinAsString(",")}]";
                        Console.WriteLine($"Updated PostLogoutRedirectUris: {blazorOpenIdDictApplication.PostLogoutRedirectUris}");
                        appChanged = true;
                    }

                    if (appChanged)
                    {
                        await OpenIdDictApplicationRepository.UpdateAsync(blazorOpenIdDictApplication);
                    }
                }
            }
        }
    }
}
```

### 3.2 Registrar el Data Seed Contributor

**Archivo**: `src/AbpSolution1.Domain/AbpSolution1DomainModule.cs`

En el método `ConfigureServices`, agregar:

```csharp
public override void ConfigureServices(ServiceConfigurationContext context)
{
    Configure<AbpMultiTenancyOptions>(options =>
    {
        options.IsEnabled = MultiTenancyConsts.IsEnabled;
    });

    // Registrar el custom identity data seed contributor
    context.Services.Replace(
        ServiceDescriptor.Transient<IdentityDataSeedContributor, AbpSolution1IdentityDataSeedContributor>()
    );
    
    // ... resto de la configuración
}
```

---

## PASO 4: Configuración del Frontend (Blazor WebAssembly)

### 4.1 Actualizar appsettings.json

**Archivo**: `src/AbpSolution1.Blazor.Client/wwwroot/appsettings.json`

```json
{
  "App": {
    "SelfUrl": "https://{0}.midominio.com:44307"
  },
  "AuthServer": {
    "Authority": "https://{0}.api.midominio.com:44399",
    "ClientId": "AbpSolution1_Blazor",
    "ResponseType": "code"
  },
  "RemoteServices": {
    "Default": {
      "BaseUrl": "https://{0}.api.midominio.com:44399"
    }
  }
}
```

### 4.2 Modificar AbpSolution1BlazorClientModule

**Archivo**: `src/AbpSolution1.Blazor.Client/AbpSolution1BlazorClientModule.cs`

#### Agregar campos estáticos:
```csharp
public class AbpSolution1BlazorClientModule : AbpModule
{
    private static readonly string[] ProtocolPrefixes = { "http://", "https://" };
    
    // ... resto de la clase
}
```

#### Agregar métodos auxiliares:
```csharp
private void ConfigureRemoteServices(WebAssemblyHostBuilder builder)
{
    Configure<AbpRemoteServiceOptions>(options =>
    {
        options.RemoteServices.Default =
            new RemoteServiceConfiguration(GetApiServerAuthorityWithTenantSubDomain(builder));
    });
}

private static string GetAuthServerAuthorityWithTenantSubDomain(WebAssemblyHostBuilder builder)
{
    return ConvertToTenantSubDomain(builder, "AuthServer:Authority");
}

private static string GetApiServerAuthorityWithTenantSubDomain(WebAssemblyHostBuilder builder)
{
    return ConvertToTenantSubDomain(builder, "RemoteServices:Default:BaseUrl");
}

private static string ConvertToTenantSubDomain(WebAssemblyHostBuilder builder, string configPath)
{
    var baseUrl = builder.HostEnvironment.BaseAddress;
    var configUrl = builder.Configuration[configPath];
    return configUrl.Replace("{0}.", GetTenantName(baseUrl));
}

private static string GetTenantName(string baseUrl)
{
    var hostName = baseUrl.RemovePreFix(ProtocolPrefixes);
    var urlSplit = hostName.Split('.');
    // Si la URL tiene 2 partes (o par), asumimos el host
    // Si tiene 3 (o impar), asumimos que hay un subdominio de tenant
    return urlSplit.Length % 2 == 0 ? null : $"{urlSplit.FirstOrDefault()}.";
}
```

#### Modificar ConfigureAuthentication:
```csharp
private static void ConfigureAuthentication(WebAssemblyHostBuilder builder)
{
    builder.Services.AddOidcAuthentication(options =>
    {
        builder.Configuration.Bind("AuthServer", options.ProviderOptions);
        options.UserOptions.NameClaim = OpenIddictConstants.Claims.Name;
        options.UserOptions.RoleClaim = OpenIddictConstants.Claims.Role;
        options.ProviderOptions.DefaultScopes.Add("roles");
        options.ProviderOptions.DefaultScopes.Add("email");
        options.ProviderOptions.DefaultScopes.Add("phone");
        options.ProviderOptions.DefaultScopes.Add("AbpSolution1");
        
        // IMPORTANTE: Sobrescribir la autoridad con el subdominio del tenant
        options.ProviderOptions.Authority = GetAuthServerAuthorityWithTenantSubDomain(builder);
    });
}
```

#### Llamar a ConfigureRemoteServices en ConfigureServices:
```csharp
public override void ConfigureServices(ServiceConfigurationContext context)
{
    var environment = context.Services.GetSingletonInstance<IWebAssemblyHostEnvironment>();
    var builder = context.Services.GetSingletonInstance<WebAssemblyHostBuilder>();

    ConfigureAuthentication(builder);
    ConfigureRemoteServices(builder); // AGREGAR ESTA LÍNEA
    ConfigureHttpClient(context, environment);
    ConfigureBlazorise(context);
    ConfigureRouter(context);
    ConfigureUI(builder);
    ConfigureMenu(context);
    ConfigureAutoMapper(context);
}
```

---

## PASO 5: Configuración Local (Desarrollo)

### 5.1 Modificar el archivo hosts

**Windows**: `C:\Windows\System32\drivers\etc\hosts`
**Linux/Mac**: `/etc/hosts`

Agregar:
```
127.0.0.1 midominio.com
127.0.0.1 tenant1.midominio.com
127.0.0.1 api.midominio.com
127.0.0.1 tenant1.api.midominio.com
```

### 5.2 Configurar applicationhost.config para IIS Express

Si usas IIS Express, modificar los bindings para permitir wildcards.

**Visual Studio**: 
- Archivo ubicado en `.vs/AbpSolution1/config/applicationhost.config`

**JetBrains Rider**: 
- Desmarcar "Generate ApplicationHost.config" en las opciones de configuración de ejecución

Modificar los bindings en el archivo:

```xml
<sites>
    <site name="AbpSolution1.Blazor.Client" id="1">
        <bindings>
            <!-- Cambiar de bindingInformation="*:44307:localhost" a: -->
            <binding protocol="https" bindingInformation="*:44307:" />
        </bindings>
    </site>
    <site name="AbpSolution1.HttpApi.Host" id="2">
        <bindings>
            <!-- Cambiar de bindingInformation="*:44399:localhost" a: -->
            <binding protocol="https" bindingInformation="*:44399:" />
        </bindings>
    </site>
</sites>
```

---

## PASO 6: Pruebas

### 6.1 Ejecutar la aplicación

1. Ejecutar el HttpApi.Host
2. Ejecutar el Blazor.Client

### 6.2 Probar el flujo completo

1. Acceder como administrador a `https://midominio.com:44307`
2. Crear un nuevo tenant (ej: "tenant1")
3. Cerrar sesión
4. Navegar a `https://tenant1.midominio.com:44307`
5. Deberías ser redirigido a `https://tenant1.api.midominio.com:44399` para autenticación
6. Iniciar sesión con el usuario administrador del tenant
7. Deberías ser redirigido de vuelta a `https://tenant1.midominio.com:44307` autenticado

---

## Notas Adicionales

### Seguridad
- En producción, usar certificados SSL válidos
- Configurar correctamente los CORS para el dominio específico
- Validar que los subdominios correspondan a tenants válidos

### Performance
- Considerar caché para la resolución de tenants
- Implementar rate limiting por tenant

### Mantenimiento
- Documentar los subdominios asignados a cada tenant
- Implementar un proceso para agregar/remover subdominios dinámicamente

### Despliegue en Producción
- Configurar DNS wildcard (*.midominio.com)
- Usar un reverse proxy (nginx, IIS) para gestionar los subdominios
- Configurar certificados SSL wildcard

---

## Troubleshooting

### Error: "Tenant not found"
- Verificar que el nombre del tenant coincida con el subdominio
- Revisar la configuración del DomainTenantResolver

### Error: "Invalid redirect_uri"
- Verificar que las URIs estén correctamente actualizadas en OpenIddict
- Ejecutar el DbMigrator después de crear el tenant

### CORS errors
- Verificar que los wildcards estén correctamente configurados en CorsOrigins
- Asegurarse de que el dominio esté en minúsculas

---

## Referencias

- Repositorio de ejemplo: https://github.com/gdunit/AbpBlazorMultiTenantSubdomain
- Documentación ABP Multi-Tenancy: https://docs.abp.io/en/abp/latest/Multi-Tenancy
- Documentación OpenIddict: https://documentation.openiddict.com/

---

## Checklist de Implementación

- [ ] Paso 1: Configurar appsettings.json del DbMigrator
- [ ] Paso 1: Ejecutar DbMigrator
- [ ] Paso 2.1: Actualizar appsettings.json del HttpApi.Host
- [ ] Paso 2.2: Crear TokenWildcardIssuerValidator
- [ ] Paso 2.3: Crear AbpSolution1SigninManager
- [ ] Paso 2.4: Modificar AbpSolution1HttpApiHostModule
- [ ] Paso 3.1: Crear AbpSolution1IdentityDataSeedContributor
- [ ] Paso 3.2: Registrar el data seed contributor en el DomainModule
- [ ] Paso 4.1: Actualizar appsettings.json del Blazor.Client
- [ ] Paso 4.2: Modificar AbpSolution1BlazorClientModule
- [ ] Paso 5.1: Modificar archivo hosts
- [ ] Paso 5.2: Configurar applicationhost.config (si aplica)
- [ ] Paso 6: Pruebas completas
