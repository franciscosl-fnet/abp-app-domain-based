using Volo.Abp.Modularity;

namespace AbpSolution1;

/* Inherit from this class for your domain layer tests. */
public abstract class AbpSolution1DomainTestBase<TStartupModule> : AbpSolution1TestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
