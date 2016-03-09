using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Packaging
{
    internal partial class PackageSearchService
    {
        private class SolutionLoadCompleteService : IPackageSearchSolutionLoadCompleteService
        {
            private readonly VisualStudioWorkspaceImpl _workspace;

            public SolutionLoadCompleteService(VisualStudioWorkspaceImpl workspace)
            {
                _workspace = workspace;
            }

            public bool SolutionLoadComplete => _workspace.SolutionLoadComplete;
        }
    }
}