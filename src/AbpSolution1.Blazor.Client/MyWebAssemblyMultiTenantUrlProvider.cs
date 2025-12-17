using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Text.Formatting;
using Volo.Abp.AspNetCore.Components.WebAssembly.MultiTenant;
using Volo.Abp.Http.Client;

namespace AbpSolution1.Blazor.Client;

[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IMultiTenantUrlProvider), typeof(MultiTenantUrlProvider))]
public class MyWebAssemblyMultiTenantUrlProvider : MultiTenantUrlProvider
{
    private static readonly string[] ProtocolPrefixes = ["http://", "https://"];

    protected NavigationManager NavigationManager { get; }
    protected IOptions<WebAssemblyMultiTenantUrlOptions> Options { get; }
    protected IOptions<AbpRemoteServiceOptions> AbpRemoteServiceOptions { get; }

    public MyWebAssemblyMultiTenantUrlProvider(
        ICurrentTenant currentTenant,
        ITenantStore tenantStore,
        NavigationManager navigationManager,
        IOptions<WebAssemblyMultiTenantUrlOptions> options,
        IOptions<AbpRemoteServiceOptions> abpRemoteServiceOptions)
        : base(currentTenant, tenantStore)
    {
        NavigationManager = navigationManager;
        Options = options;
        AbpRemoteServiceOptions = abpRemoteServiceOptions;
    }

    public override async Task<string> GetUrlAsync(string templateUrl)
    {
        var url = NavigationManager.ToAbsoluteUri(NavigationManager.Uri).Authority;
        var currentBlazorUrl = "{0}.localhost:44366";
        var extractResult = FormattedStringValueExtracter.Extract(url, currentBlazorUrl, ignoreCase: true);
        return extractResult.IsMatch ? templateUrl.Replace(TenantPlaceHolder, extractResult.Matches[0].Value) : templateUrl.Replace(TenantPlaceHolder + ".", "");
    }
}
