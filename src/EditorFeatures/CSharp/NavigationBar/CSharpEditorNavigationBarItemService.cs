// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Extensibility.NavigationBar;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NavigationBar;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.NavigationBar
{
    [ExportLanguageService(typeof(INavigationBarItemService), LanguageNames.CSharp), Shared]
    internal class CSharpEditorNavigationBarItemService : AbstractEditorNavigationBarItemService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpEditorNavigationBarItemService()
        {
        }

        public override VirtualTreePoint? GetSymbolItemNavigationPoint(
            Document document, RoslynNavigationBarItem item, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(item.Kind == RoslynNavigationBarItemKind.Symbol);
            var compilation = document.Project.GetRequiredCompilationAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var symbols = item.NavigationSymbolId!.Value.Resolve(compilation, cancellationToken: cancellationToken);

            var symbol = symbols.Symbol;

            if (symbol == null)
            {
                if (item.NavigationSymbolIndex < symbols.CandidateSymbols.Length)
                {
                    symbol = symbols.CandidateSymbols[item.NavigationSymbolIndex.Value];
                }
                else
                {
                    return null;
                }
            }

            var syntaxTree = document.GetSyntaxTreeSynchronously(cancellationToken);
            var location = symbol.Locations.FirstOrDefault(l => l.SourceTree!.Equals(syntaxTree));

            if (location == null)
            {
                location = symbol.Locations.FirstOrDefault();
            }

            if (location == null)
            {
                return null;
            }

            return new VirtualTreePoint(location.SourceTree!, location.SourceTree!.GetText(cancellationToken), location.SourceSpan.Start);
        }

        protected override void NavigateToItem(Document document, WrappedNavigationBarItem item, ITextView textView, CancellationToken cancellationToken)
            => NavigateToSymbolItem(document, item.UnderlyingItem, cancellationToken);
    }
}
