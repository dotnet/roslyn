// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion
{
    [ExportWorkspaceServiceFactory(typeof(ICompletionHelperService)), Shared]
    internal class CompletionHelperServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CompletionHelperServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new Service(workspaceServices.Workspace);

        private sealed class Service : ICompletionHelperService, IWorkspaceService
        {
            private readonly object _gate = new();

            private CompletionHelper? _lazyCaseSensitiveInstance;
            private CompletionHelper? _lazyCaseInsensitiveInstance;

            public Service(Workspace workspace)
                => workspace.WorkspaceChanged += OnWorkspaceChanged;

            public CompletionHelper GetCompletionHelper(Document document)
            {
                lock (_gate)
                {
                    // Don't bother creating instances unless we actually need them
                    if (_lazyCaseSensitiveInstance == null)
                    {
                        CreateInstances();
                    }

                    Contract.ThrowIfNull(_lazyCaseSensitiveInstance);
                    Contract.ThrowIfNull(_lazyCaseInsensitiveInstance);

                    var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
                    var caseSensitive = syntaxFacts?.IsCaseSensitive ?? true;

                    return caseSensitive
                        ? _lazyCaseSensitiveInstance
                        : _lazyCaseInsensitiveInstance;
                }
            }

            private void CreateInstances()
            {
                _lazyCaseSensitiveInstance = new CompletionHelper(isCaseSensitive: true);
                _lazyCaseInsensitiveInstance = new CompletionHelper(isCaseSensitive: false);
            }

            private void OnWorkspaceChanged(object? sender, WorkspaceChangeEventArgs e)
            {
                if (e.Kind == WorkspaceChangeKind.SolutionRemoved)
                {
                    lock (_gate)
                    {
                        // Solution was unloaded, clear caches if we were caching anything
                        if (_lazyCaseSensitiveInstance != null)
                        {
                            CreateInstances();
                        }
                    }
                }
            }
        }
    }
}
