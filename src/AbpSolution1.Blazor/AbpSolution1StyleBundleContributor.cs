using Volo.Abp.AspNetCore.Mvc.UI.Bundling;

namespace AbpSolution1.Blazor;

public class AbpSolution1StyleBundleContributor : BundleContributor
{
    public override void ConfigureBundle(BundleConfigurationContext context)
    {
        context.Files.Add(new BundleFile("main.css", true));
    }
}
