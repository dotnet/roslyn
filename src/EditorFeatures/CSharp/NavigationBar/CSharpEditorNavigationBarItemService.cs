// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Extensibility.NavigationBar;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NavigationBar;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.CSharp.NavigationBar
{
    [ExportLanguageService(typeof(INavigationBarItemService), LanguageNames.CSharp), Shared]
    internal class CSharpEditorNavigationBarItemService : AbstractEditorNavigationBarItemService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpEditorNavigationBarItemService(IThreadingContext threadingContext)
            : base(threadingContext)
        {
        }

        protected override async Task<VirtualTreePoint?> GetSymbolNavigationPointAsync(
            Document document, ISymbol symbol, CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var location = symbol.Locations.FirstOrDefault(l => l.SourceTree!.Equals(syntaxTree));

            if (location == null)
                location = symbol.Locations.FirstOrDefault();

            if (location == null)
                return null;

            return new VirtualTreePoint(location.SourceTree!, location.SourceTree!.GetText(cancellationToken), location.SourceSpan.Start);
        }

        protected override Task NavigateToItemAsync(Document document, WrappedNavigationBarItem item, ITextView textView, CancellationToken cancellationToken)
            => NavigateToSymbolItemAsync(document, (RoslynNavigationBarItem.SymbolItem)item.UnderlyingItem, cancellationToken);
    }
}
