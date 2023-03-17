// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(TypeImportCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(PropertySubpatternCompletionProvider))]
    [Shared]
    internal sealed class TypeImportCompletionProvider : AbstractTypeImportCompletionProvider<UsingDirectiveSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TypeImportCompletionProvider()
        {
        }

        internal override string Language => LanguageNames.CSharp;

        public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
            => CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);

        public override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.CommonTriggerCharacters;

        protected override bool IsFinalSemicolonOfUsingOrExtern(SyntaxNode directive, SyntaxToken token)
        {
            if (token.IsKind(SyntaxKind.None) || token.IsMissing)
                return false;

            return directive switch
            {
                UsingDirectiveSyntax usingDirective => usingDirective.SemicolonToken == token,
                ExternAliasDirectiveSyntax externAliasDirective => externAliasDirective.SemicolonToken == token,
                _ => false,
            };
        }

        protected override async Task<bool> ShouldProvideParenthesisCompletionAsync(
            Document document,
            CompletionItem item,
            char? commitKey,
            CancellationToken cancellationToken)
        {
            if (commitKey is ';' or '.')
            {
                // Only consider add '()' if the type is used under object creation context
                var position = item.Span.Start;
                var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var leftToken = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
                return syntaxTree.IsObjectCreationTypeContext(position, leftToken, cancellationToken);
            }

            return false;
        }

        protected override ImmutableArray<UsingDirectiveSyntax> GetAliasDeclarationNodes(SyntaxNode node)
            => node.GetEnclosingUsingDirectives()
                .Where(n => n.Alias != null)
                .ToImmutableArray();
    }
}
