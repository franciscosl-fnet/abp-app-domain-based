using Volo.Abp.Modularity;

namespace AbpSolution1;

[DependsOn(
    typeof(AbpSolution1DomainModule),
    typeof(AbpSolution1TestBaseModule)
)]
public class AbpSolution1DomainTestModule : AbpModule
{

}
