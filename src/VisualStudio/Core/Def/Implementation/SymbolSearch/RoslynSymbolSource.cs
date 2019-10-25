// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

namespace Microsoft.VisualStudio.LanguageServices.SymbolSearch
{
    internal class RoslynSymbolSource : ISymbolSourceFromLocation
    {
        internal SymbolSourceProvider ServiceProvider { get; private set; }

        public RoslynSymbolSource(SymbolSourceProvider symbolSourceProvider)
        {
            ServiceProvider = symbolSourceProvider;
        }

        public string UniqueId => "Roslyn symbol source";

        public ImageId DisplayIcon => throw new NotImplementedException();

        public string DescriptionText => "Symbols provided by the local language service";

        public string DisplayName => "Local symbol source";

        internal LocalCodeSymbolOrigin LocalOrigin = new LocalCodeSymbolOrigin("Current Solution"); // TODO: Get name of the solution

        public async Task<SymbolSearchStatus> FindSymbolsAsync(string navigationKind, VisualStudio.Language.Intellisense.SymbolSearch.Location sourceLocation, IStreamingSymbolSearchSink sink, CancellationToken token)
        {
            var snapshot = sourceLocation.PersistentSpan.Document.TextBuffer.CurrentSnapshot;
            var roslynDocument = snapshot.GetOpenDocumentInCurrentContextWithChanges();

            var solutionPath = roslynDocument.Project.Solution.FilePath;
            var rootNodeName = !string.IsNullOrWhiteSpace(solutionPath)
                ? $"'{Path.GetFileNameWithoutExtension(solutionPath)}' Solution"
                : $"Current Solution";

            var symbolSearchContext = new SymbolSearchContext(this, sink, rootNodeName);

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
    }
}
