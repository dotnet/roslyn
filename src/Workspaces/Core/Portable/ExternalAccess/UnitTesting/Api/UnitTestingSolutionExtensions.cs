namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingSolutionExtensions
    {
        public static int GetWorkspaceVersion(this Solution solution)
            => solution.WorkspaceVersion;
    }
}
