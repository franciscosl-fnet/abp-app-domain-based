using Volo.Abp.Modularity;

namespace AbpSolution1;

public abstract class AbpSolution1ApplicationTestBase<TStartupModule> : AbpSolution1TestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
