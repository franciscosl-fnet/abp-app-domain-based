using Volo.Abp.Modularity;

namespace AbpSolution1;

[DependsOn(
    typeof(AbpSolution1ApplicationModule),
    typeof(AbpSolution1DomainTestModule)
)]
public class AbpSolution1ApplicationTestModule : AbpModule
{

}
