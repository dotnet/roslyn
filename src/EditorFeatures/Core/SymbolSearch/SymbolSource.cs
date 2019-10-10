using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.GoToBase;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.SymbolSearch;

namespace Microsoft.CodeAnalysis.Editor.SymbolSearch
{
    class SymbolSource : ISymbolSourceFromLocation
    {
        private SymbolSourceProvider _symbolSourceProvider;

        public SymbolSource(SymbolSourceProvider symbolSourceProvider)
        {
            _symbolSourceProvider = symbolSourceProvider;
        }

        public string UniqueId => "Roslyn symbol source";

        public ImageId DisplayIcon => throw new NotImplementedException();

        public string DescriptionText => "Symbols provided by the local language service";

        public string DisplayName => "Local symbol source";

        public async Task<SymbolSearchStatus> FindSymbolsAsync(string navigationKind, VisualStudio.Language.Intellisense.SymbolSearch.Location sourceLocation, IStreamingSymbolSearchSink sink, CancellationToken token)
        {
            var snapshot = sourceLocation.PersistentSpan.Document.TextBuffer.CurrentSnapshot;
            var roslynDocument = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            switch (navigationKind)
            {
                case PredefinedNavigationKinds.Definition:
                    {
                        var goToDefinitionService = roslynDocument.GetLanguageService<IGoToDefinitionService>();
                        goToDefinitionService.TryGoToDefinition(roslynDocument, sourceLocation.PersistentSpan.Span.GetStartPoint(snapshot).Position, token);
                        return SymbolSearchStatus.Completed;
                    }
                case PredefinedNavigationKinds.Implementation:
                    {
                        var findUsagesService = roslynDocument.GetLanguageService<IFindUsagesService>();
                        var context = new SimpleFindUsagesContext(token);
                        findUsagesService.FindImplementationsAsync(roslynDocument, sourceLocation.PersistentSpan.Span.GetStartPoint(snapshot).Position, context);
                        return SymbolSearchStatus.Completed;
                    }
                case PredefinedNavigationKinds.Reference:
                    {
                        var findUsagesService = roslynDocument.GetLanguageService<IFindUsagesService>();
                        var context = new SimpleFindUsagesContext(token);
                        findUsagesService.FindReferencesAsync(roslynDocument, sourceLocation.PersistentSpan.Span.GetStartPoint(snapshot).Position, context);
                        return SymbolSearchStatus.Completed;
                    }
                case PredefinedNavigationKinds.Base:
                    {
                        var goToBaseService = roslynDocument.GetLanguageService<IGoToBaseService>();
                        var context = new SimpleFindUsagesContext(token);
                        await goToBaseService.FindBasesAsync(roslynDocument, sourceLocation.PersistentSpan.Span.GetStartPoint(snapshot).Position, context);
                        return SymbolSearchStatus.Completed;
                    }
                default:
                    return SymbolSearchStatus.Withheld;
            }
        }
    }
}
