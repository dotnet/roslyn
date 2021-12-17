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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.NavigationBar
{
    [ExportLanguageService(typeof(INavigationBarItemServiceRenameOnceTypeScriptMovesToExternalAccess), LanguageNames.CSharp), Shared]
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
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var location =
                symbol.Locations.FirstOrDefault(l => Equals(l.SourceTree, syntaxTree)) ??
                symbol.Locations.FirstOrDefault(l => l.SourceTree != null);

            if (location == null)
                return null;

            var tree = location.SourceTree;
            Contract.ThrowIfNull(tree);

            return new VirtualTreePoint(tree, tree.GetText(cancellationToken), location.SourceSpan.Start);
        }

        protected override async Task<bool> TryNavigateToItemAsync(Document document, WrappedNavigationBarItem item, ITextView textView, CancellationToken cancellationToken)
        {
            await NavigateToSymbolItemAsync(document, (RoslynNavigationBarItem.SymbolItem)item.UnderlyingItem, cancellationToken).ConfigureAwait(false);
            return true;
        }
    }
}
