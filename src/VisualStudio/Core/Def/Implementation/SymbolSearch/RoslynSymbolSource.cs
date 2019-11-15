// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.GoToBase;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.SymbolSearch;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch
{
    internal class RoslynSymbolSource : ISymbolSourceFromLocation
    {
        internal RoslynSymbolSourceProvider ServiceProvider { get; }

        public RoslynSymbolSource(RoslynSymbolSourceProvider symbolSourceProvider)
        {
            ServiceProvider = symbolSourceProvider;
        }

        ImageId IDecorated.DisplayIcon => throw new NotImplementedException();

        string IDecorated.DescriptionText => ServicesVSResources.Symbol_search_source_description;

        string INamed.DisplayName => ServicesVSResources.Symbol_search_source_name;

        string ISymbolSource.UniqueId => nameof(RoslynSymbolSource);

        public async Task<SymbolSearchStatus> FindSymbolsAsync(string navigationKind, VisualStudio.Language.Intellisense.SymbolSearch.Location sourceLocation, IStreamingSymbolSearchSink sink, CancellationToken token)
        {
            // This method is called off the UI thread
            var snapshot = sourceLocation.PersistentSpan.Document.TextBuffer.CurrentSnapshot;
            var roslynDocument = snapshot.GetOpenDocumentInCurrentContextWithChanges();

            var solutionPath = roslynDocument.Project.Solution.FilePath;
            var rootNodeName = !string.IsNullOrWhiteSpace(solutionPath)
                ? string.Format(ServicesVSResources.Symbol_search_known_solution, Path.GetFileNameWithoutExtension(solutionPath))
                : ServicesVSResources.Symbol_search_current_solution;

            var symbolSearchContext = new SymbolSearchContext(this, sink, rootNodeName);

            try
            {
                switch (navigationKind)
                {
                    case PredefinedSymbolNavigationKinds.Definition:
                        {
                            var goToDefinitionService = roslynDocument.GetLanguageService<IGoToDefinitionService>();
                            goToDefinitionService.TryGoToDefinition(roslynDocument, sourceLocation.PersistentSpan.Span.GetStartPoint(snapshot).Position, token);
                            return SymbolSearchStatus.Completed;
                        }
                    case PredefinedSymbolNavigationKinds.Implementation:
                        {
                            var findUsagesService = roslynDocument.GetLanguageService<IFindUsagesService>();
                            var context = new SimpleFindUsagesContext(token);
                            await findUsagesService.FindImplementationsAsync(roslynDocument, sourceLocation.PersistentSpan.Span.GetStartPoint(snapshot).Position, symbolSearchContext)
                                .ConfigureAwait(false);
                            return SymbolSearchStatus.Completed;
                        }
                    case PredefinedSymbolNavigationKinds.Reference:
                        {
                            var findUsagesService = roslynDocument.GetLanguageService<IFindUsagesService>();
                            var context = new SimpleFindUsagesContext(token);
                            await findUsagesService.FindReferencesAsync(roslynDocument, sourceLocation.PersistentSpan.Span.GetStartPoint(snapshot).Position, symbolSearchContext)
                                .ConfigureAwait(false);
                            return SymbolSearchStatus.Completed;
                        }
                    case PredefinedSymbolNavigationKinds.Base:
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
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return SymbolSearchStatus.Completed; // Update API and use the error status
            }
        }
    }
}
