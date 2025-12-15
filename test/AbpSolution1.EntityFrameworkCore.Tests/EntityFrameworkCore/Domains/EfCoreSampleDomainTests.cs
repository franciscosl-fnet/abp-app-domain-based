using AbpSolution1.Samples;
using Xunit;

namespace AbpSolution1.EntityFrameworkCore.Domains;

[Collection(AbpSolution1TestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<AbpSolution1EntityFrameworkCoreTestModule>
{

}
