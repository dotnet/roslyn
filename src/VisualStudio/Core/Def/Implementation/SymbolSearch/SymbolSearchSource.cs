// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.GoToBase;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.SymbolSearch;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SymbolSearch
{
    internal class SymbolSearchSource : ISymbolSourceFromLocation
    {
        internal SymbolSearchSourceProvider ServiceProvider { get; }

        public SymbolSearchSource(SymbolSearchSourceProvider symbolSourceProvider)
        {
            ServiceProvider = symbolSourceProvider;
        }

        public ImageId Icon => default;

        public string DescriptionText => string.Empty;

        string INamed.DisplayName => ServicesVSResources.Symbol_search_source_name;

        string ISymbolSource.Id => nameof(SymbolSearchSource);

        public async Task<SymbolSearchStatus> FindSymbolsAsync(string navigationKind, VisualStudio.Language.Intellisense.SymbolSearch.Location sourceLocation, ISymbolSearchCallback callback, CancellationToken token)
        {
            // This method is called off the UI thread
            var snapshot = sourceLocation.PersistentSpan.Document.TextBuffer.CurrentSnapshot;
            var roslynDocument = snapshot.GetOpenDocumentInCurrentContextWithChanges();

            var solutionPath = roslynDocument.Project.Solution.FilePath;
            var rootNodeName = !string.IsNullOrWhiteSpace(solutionPath)
                ? string.Format(ServicesVSResources.Symbol_search_known_solution, Path.GetFileNameWithoutExtension(solutionPath))
                : ServicesVSResources.Symbol_search_current_solution;

            var symbolSearchContext = new SymbolSearchContext(this, callback, rootNodeName, token);

            try
            {
                switch (navigationKind)
                {
                    case PredefinedNavigationKinds.Definition:
                        {
                            // This is not yet supported,
                            // because Roslyn implementation depends on UI thread.
                            return SymbolSearchStatus.Withheld;
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
            catch
            {
                // Allow platform to log and handle this.
                throw;
            }
        }
    }
}
