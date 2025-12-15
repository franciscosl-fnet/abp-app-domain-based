using AbpSolution1.Samples;
using Xunit;

namespace AbpSolution1.EntityFrameworkCore.Applications;

[Collection(AbpSolution1TestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<AbpSolution1EntityFrameworkCoreTestModule>
{

}
