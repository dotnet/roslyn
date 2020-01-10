// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.GoToBase;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense.SymbolSearch;
using Microsoft.VisualStudio.Utilities;
using VsLocation = Microsoft.VisualStudio.Language.Intellisense.SymbolSearch.Location;

namespace Microsoft.CodeAnalysis.Editor.Implementation.SymbolSearch
{
    internal class SymbolSearchSource : ISymbolSourceFromLocation
    {
        /// <summary>
        /// Name used in the user interface
        /// </summary>
        string INamed.DisplayName => EditorFeaturesResources.Symbol_search_source_name;

        /// <summary>
        /// Used for diagnostics
        /// </summary>
        string ISymbolSource.Id => "Roslyn Symbol Source";

        internal SymbolSearchSourceProvider SymbolSourceProvider { get; }

        public SymbolSearchSource(SymbolSearchSourceProvider symbolSourceProvider)
        {
            SymbolSourceProvider = symbolSourceProvider;
        }

        public async Task<SymbolSearchStatus> FindSymbolsAsync(
            string navigationKind, VsLocation sourceLocation, 
            ISymbolSearchCallback callback, CancellationToken token)
        {
            // This method is called off the UI thread
            var snapshot = sourceLocation.PersistentSpan.Document.TextBuffer.CurrentSnapshot;
            var roslynDocument = snapshot.GetOpenDocumentInCurrentContextWithChanges();

            var solutionPath = roslynDocument.Project.Solution.FilePath;
            var rootNodeName = !string.IsNullOrWhiteSpace(solutionPath)
                ? string.Format(EditorFeaturesResources.Symbol_search_known_solution, Path.GetFileNameWithoutExtension(solutionPath))
                : EditorFeaturesResources.Symbol_search_current_solution;

            var symbolSearchContext = new SymbolSearchContext(this, callback, rootNodeName, token);

            switch (navigationKind)
            {
                case PredefinedNavigationKinds.Definition:
                    {
                        // This is not yet supported,
                        // because Roslyn implementation depends on UI thread.
                        return SymbolSearchStatus.Failed;
                        // Existing Roslyn implementation, disabled to not interfere with demos
                        /*
                        var goToDefinitionService = roslynDocument.GetLanguageService<IGoToDefinitionService>();
                        goToDefinitionService.TryGoToDefinition(roslynDocument, sourceLocation.PersistentSpan.Span.GetStartPoint(snapshot).Position, token);
                        return SymbolSearchStatus.Completed;
                        */
                    }
                case PredefinedNavigationKinds.Implementation:
                    {
                        var findUsagesService = roslynDocument.GetLanguageService<IFindUsagesService>();
                        var context = new SimpleFindUsagesContext(token);
                        await findUsagesService.FindImplementationsAsync(roslynDocument, sourceLocation.PersistentSpan.Span.GetStartPoint(snapshot).Position, symbolSearchContext)
                            .ConfigureAwait(false);
                        return SymbolSearchStatus.Completed;
                    }
                case PredefinedNavigationKinds.Reference:
                    {
                        var findUsagesService = roslynDocument.GetLanguageService<IFindUsagesService>();
                        var context = new SimpleFindUsagesContext(token);
                        await findUsagesService.FindReferencesAsync(roslynDocument, sourceLocation.PersistentSpan.Span.GetStartPoint(snapshot).Position, symbolSearchContext)
                            .ConfigureAwait(false);
                        return SymbolSearchStatus.Completed;
                    }
                case PredefinedNavigationKinds.Base:
                    {
                        var goToBaseService = roslynDocument.GetLanguageService<IGoToBaseService>();
                        var context = new SimpleFindUsagesContext(token);
                        await goToBaseService.FindBasesAsync(roslynDocument, sourceLocation.PersistentSpan.Span.GetStartPoint(snapshot).Position, symbolSearchContext)
                            .ConfigureAwait(false);
                        return SymbolSearchStatus.Completed;
                    }
                default:
                    return SymbolSearchStatus.Withheld;
            }
        }
    }
}
