// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.OrderModifiers;
using Microsoft.CodeAnalysis.OrderModifiers;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

#if CODE_STYLE
using OptionSet = Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions;
#else
using Microsoft.CodeAnalysis.Options;
#endif

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.HideBase
{
    internal partial class HideBaseCodeFixProvider
    {
        private class AddNewKeywordAction : CodeActions.CodeAction
        {
            private readonly Document _document;
            private readonly SyntaxNode _node;

            public override string Title => CSharpCodeFixesResources.Hide_base_member;

            public AddNewKeywordAction(Document document, SyntaxNode node)
            {
                _document = document;
                _node = node;
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var root = await _document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

#if CODE_STYLE
                var options = _document.Project.AnalyzerOptions.GetAnalyzerOptionSet(_node.SyntaxTree, cancellationToken);
#else
                var options = await _document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
#endif

                var newNode = GetNewNode(_node, options);
                var newRoot = root.ReplaceNode(_node, newNode);

                return _document.WithSyntaxRoot(newRoot);
            }

            private static SyntaxNode GetNewNode(SyntaxNode node, OptionSet options)
            {
                var syntaxFacts = CSharpSyntaxFacts.Instance;
                var modifiers = syntaxFacts.GetModifiers(node);
                var newModifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.NewKeyword));

                var option = options.GetOption(CSharpCodeStyleOptions.PreferredModifierOrder);
                if (!CSharpOrderModifiersHelper.Instance.TryGetOrComputePreferredOrder(option.Value, out var preferredOrder) ||
                    !AbstractOrderModifiersHelpers.IsOrdered(preferredOrder, modifiers))
                {
                    return syntaxFacts.WithModifiers(node, newModifiers);
                }

                var orderedModifiers = new SyntaxTokenList(
                    newModifiers.OrderBy(CompareModifiers));

                return syntaxFacts.WithModifiers(node, orderedModifiers);

                int CompareModifiers(SyntaxToken left, SyntaxToken right)
                    => GetOrder(left) - GetOrder(right);

                int GetOrder(SyntaxToken token)
                    => preferredOrder.TryGetValue(token.RawKind, out var value) ? value : int.MaxValue;
            }
        }
    }
}
