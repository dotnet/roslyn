using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal interface IUnitTestingExperimentationServiceAccessor : IWorkspaceService
    {
        bool IsExperimentEnabled(string experimentName);
    }
}
