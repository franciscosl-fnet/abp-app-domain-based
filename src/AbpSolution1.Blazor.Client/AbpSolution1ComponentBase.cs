using AbpSolution1.Localization;
using Volo.Abp.AspNetCore.Components;

namespace AbpSolution1.Blazor.Client;

public abstract class AbpSolution1ComponentBase : AbpComponentBase
{
    protected AbpSolution1ComponentBase()
    {
        LocalizationResource = typeof(AbpSolution1Resource);
    }
}
