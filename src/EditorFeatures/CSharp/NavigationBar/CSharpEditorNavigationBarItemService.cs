// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Extensibility.NavigationBar;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NavigationBar;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.CSharp.NavigationBar
{
    [ExportLanguageService(typeof(INavigationBarItemService), LanguageNames.CSharp), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal class CSharpEditorNavigationBarItemService(IThreadingContext threadingContext) : AbstractEditorNavigationBarItemService(threadingContext)
    {
        protected override async Task<bool> TryNavigateToItemAsync(Document document, WrappedNavigationBarItem item, ITextView textView, ITextVersion textVersion, CancellationToken cancellationToken)
        {
            await NavigateToSymbolItemAsync(document, item, (RoslynNavigationBarItem.SymbolItem)item.UnderlyingItem, textVersion, cancellationToken).ConfigureAwait(false);
            return true;
        }
    }
}
