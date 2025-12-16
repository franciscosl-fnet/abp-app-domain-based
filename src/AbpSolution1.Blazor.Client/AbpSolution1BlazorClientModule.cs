using System;
using System.Net.Http;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AbpSolution1.Blazor.Client.Navigation;
using Localization.Resources.AbpUi;
using Volo.Abp.Localization;
using AbpSolution1.Localization;
using OpenIddict.Abstractions;
using Volo.Abp.AspNetCore.Components.Web.Theming.Routing;
using Volo.Abp.Autofac.WebAssembly;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.UI.Navigation;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Components.WebAssembly.Theming.Bundling;
using Volo.Abp.AspNetCore.Components.WebAssembly.BasicTheme;
using Volo.Abp.SettingManagement.Blazor.WebAssembly;
using Volo.Abp.FeatureManagement.Blazor.WebAssembly;
using Volo.Abp.TenantManagement.Blazor.WebAssembly;
using Volo.Abp.Identity.Blazor.WebAssembly;
using Volo.Abp.Http.Client;

namespace AbpSolution1.Blazor.Client;

[DependsOn(
    typeof(AbpSettingManagementBlazorWebAssemblyModule),
    typeof(AbpFeatureManagementBlazorWebAssemblyModule),
    typeof(AbpAutofacWebAssemblyModule),
    typeof(AbpAspNetCoreComponentsWebAssemblyBasicThemeModule),
    typeof(AbpIdentityBlazorWebAssemblyModule),
    typeof(AbpTenantManagementBlazorWebAssemblyModule),
    typeof(AbpSolution1HttpApiClientModule)
)]
public class AbpSolution1BlazorClientModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var environment = context.Services.GetSingletonInstance<IWebAssemblyHostEnvironment>();
        var builder = context.Services.GetSingletonInstance<WebAssemblyHostBuilder>();

        ConfigureLocalization();
        ConfigureAuthentication(builder);
        ConfigureHttpClient(context, environment);
        ConfigureBlazorise(context);
        ConfigureRouter(context);
        ConfigureMenu(context);
        ConfigureAutoMapper(context);
    }
    
    private void ConfigureLocalization()
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<AbpSolution1Resource>()
                .AddBaseTypes(typeof(AbpUiResource));
        });
    }

    private void ConfigureRouter(ServiceConfigurationContext context)
    {
        Configure<AbpRouterOptions>(options =>
        {
            options.AppAssembly = typeof(AbpSolution1BlazorClientModule).Assembly;
        });
    }

    private void ConfigureMenu(ServiceConfigurationContext context)
    {
        Configure<AbpNavigationOptions>(options =>
        {
            options.MenuContributors.Add(new AbpSolution1MenuContributor(context.Services.GetConfiguration()));
        });
    }

    private void ConfigureBlazorise(ServiceConfigurationContext context)
    {
        context.Services
            .AddBootstrap5Providers()
            .AddFontAwesomeIcons();
    }

    private static void ConfigureAuthentication(WebAssemblyHostBuilder builder)
    {
        var baseUrl = builder.HostEnvironment.BaseAddress;
        var authority = builder.Configuration["AuthServer:Authority"];
        
        // Convertir la Authority con el subdominio del tenant actual
        var tenantAuthority = TenantSubdomainHelper.ConvertToTenantSubDomain(baseUrl, authority);
        
        builder.Services.AddOidcAuthentication(options =>
        {
            builder.Configuration.Bind("AuthServer", options.ProviderOptions);
            options.UserOptions.NameClaim = OpenIddictConstants.Claims.Name;
            options.UserOptions.RoleClaim = OpenIddictConstants.Claims.Role;

            // Usar la Authority con el subdominio del tenant
            options.ProviderOptions.Authority = tenantAuthority;
            
            options.ProviderOptions.DefaultScopes.Add("AbpSolution1");
            options.ProviderOptions.DefaultScopes.Add("roles");
            options.ProviderOptions.DefaultScopes.Add("email");
            options.ProviderOptions.DefaultScopes.Add("phone");
        });
    }
    
    private void ConfigureHttpClient(ServiceConfigurationContext context, IWebAssemblyHostEnvironment environment)
    {
        var builder = context.Services.GetSingletonInstance<WebAssemblyHostBuilder>();
        var baseUrl = environment.BaseAddress;
        var remoteServiceBaseUrl = builder.Configuration["RemoteServices:Default:BaseUrl"];
        
        // Convertir la BaseUrl con el subdominio del tenant actual
        var tenantBaseUrl = TenantSubdomainHelper.ConvertToTenantSubDomain(baseUrl, remoteServiceBaseUrl);
        
        context.Services.AddTransient(sp => new HttpClient
        {
            BaseAddress = new Uri(environment.BaseAddress)
        });
        
        // Configurar RemoteServices con el subdominio del tenant
        Configure<AbpRemoteServiceOptions>(options =>
        {
            options.RemoteServices.Default = new RemoteServiceConfiguration(tenantBaseUrl);
        });
    }

    private void ConfigureAutoMapper(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<AbpSolution1BlazorClientModule>();
        });
    }
}
