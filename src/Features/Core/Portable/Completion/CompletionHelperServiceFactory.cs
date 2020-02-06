using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Completion
{
    [ExportWorkspaceServiceFactory(typeof(ICompletionHelperService)), Shared]
    internal class CompletionHelperServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        public CompletionHelperServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new Service(workspaceServices.Workspace);
        }

        private class Service : ICompletionHelperService, IWorkspaceService
        {
            private readonly object _gate = new object();

            private CompletionHelper _caseSensitiveInstance;
            private CompletionHelper _caseInsensitiveInstance;

            public Service(Workspace workspace)
            {
                workspace.WorkspaceChanged += OnWorkspaceChanged;
            }

            public CompletionHelper GetCompletionHelper(Document document)
            {
                lock (_gate)
                {
                    // Don't bother creating instances unless we actually need them
                    if (_caseSensitiveInstance == null)
                    {
                        CreateInstances();
                    }

                    var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
                    var caseSensitive = syntaxFacts?.IsCaseSensitive ?? true;

                    return caseSensitive
                        ? _caseSensitiveInstance
                        : _caseInsensitiveInstance;
                }
            }

            private void CreateInstances()
            {
                _caseSensitiveInstance = new CompletionHelper(isCaseSensitive: true);
                _caseInsensitiveInstance = new CompletionHelper(isCaseSensitive: false);
            }

            private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
            {
                if (e.Kind == WorkspaceChangeKind.SolutionRemoved)
                {
                    lock (_gate)
                    {
                        // Solution was unloaded, clear caches if we were caching anything
                        if (_caseSensitiveInstance != null)
                        {
                            CreateInstances();
                        }
                    }
                }
            }
        }
    }
}
