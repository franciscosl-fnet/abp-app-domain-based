using Microsoft.Extensions.Localization;
using AbpSolution1.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace AbpSolution1;

[Dependency(ReplaceServices = true)]
public class AbpSolution1BrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<AbpSolution1Resource> _localizer;

    public AbpSolution1BrandingProvider(IStringLocalizer<AbpSolution1Resource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
