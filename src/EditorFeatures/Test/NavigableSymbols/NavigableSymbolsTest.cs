// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.NavigableSymbols;
using Microsoft.CodeAnalysis.Editor.UnitTests.BraceMatching;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.NavigableSymbols
{
    public class NavigableSymbolsTest
    {
        private static readonly ExportProvider s_exportProvider = MinimalTestExportProvider.CreateExportProvider(
            TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithParts(
                typeof(MockDocumentNavigationServiceProvider),
                typeof(MockSymbolNavigationServiceProvider)));

        [WpfFact]
        public async Task TestCharp()
        {
            using (var workspace = TestWorkspace.CreateCSharp(@"
class C
{
    C$$ c;
}", exportProvider: s_exportProvider))

            {
                await TestNavigated(workspace);
            }
        }

        [WpfFact]
        public async Task TestVB()
        {
            using (var workspace = TestWorkspace.CreateVisualBasic(@"
Class C
    Dim c as C$$;
End Class", exportProvider: s_exportProvider))

            {
                await TestNavigated(workspace);
            }
        }

        private async Task TestNavigated(TestWorkspace workspace)
        {
            var presenter = new[] { new Lazy<IStreamingFindUsagesPresenter>(() => new MockStreamingFindUsagesPresenter(() => { })) };
            var service = new NavigableSymbolService(TestWaitIndicator.Default, presenter);

            var view = workspace.Documents.First().GetTextView();
            var buffer = workspace.Documents.First().GetTextBuffer();
            var caretPosition = view.Caret.Position.BufferPosition.Position;
            var span = new SnapshotSpan(buffer.CurrentSnapshot, new Span(caretPosition, 0));
            var source = service.TryCreateNavigableSymbolSource(view, buffer);
            var symbol = await source.GetNavigableSymbolAsync(span, CancellationToken.None);

            Assert.NotNull(symbol);
            symbol.Navigate(symbol.Relationships.First());

            var navigationService = (MockDocumentNavigationServiceProvider.MockDocumentNavigationService)workspace.Services.GetService<IDocumentNavigationService>();
            Assert.Equal(true, navigationService.TryNavigateToLineAndOffsetReturnValue);
            Assert.Equal(true, navigationService.TryNavigateToPositionReturnValue);
            Assert.Equal(true, navigationService.TryNavigateToSpanReturnValue);
        }
    }
}
