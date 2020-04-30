using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingSolutionServiceUtilities
    {
        public static Workspace PrimaryWorkspace => SolutionService.PrimaryWorkspace;
    }
}
