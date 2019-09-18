using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal interface IUnitTestingRemoteHostClientServiceAccessor : IWorkspaceService
    {
        void Enable();
    }
}
