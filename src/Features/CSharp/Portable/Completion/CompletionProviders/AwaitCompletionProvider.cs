// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(AwaitCompletionProvider), LanguageNames.CSharp)]
    [Shared]
    internal class AwaitCompletionProvider : LSPCompletionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AwaitCompletionProvider()
        {
        }

        internal override ImmutableHashSet<char> TriggerCharacters => CompletionUtilities.CommonTriggerCharactersWithArgumentList;

        internal override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem completionItem, TextSpan completionListSpan, char? commitKey, bool disallowAddingImports, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var declaration = root?.FindToken(completionListSpan.Start).GetAncestor(node => node.IsAsyncSupportingFunctionSyntax());
            if (root is null || declaration is null)
            {
                return await base.GetChangeAsync(document, completionItem, completionListSpan, commitKey, disallowAddingImports, cancellationToken).ConfigureAwait(false);
            }

            // MethodDeclarationSyntax, LocalFunctionStatementSyntax, AnonymousMethodExpressionSyntax, ParenthesizedLambdaExpressionSyntax, SimpleLambdaExpressionSyntax.

            var newDeclaration = AddAsyncModifier(declaration, SyntaxGenerator.GetGenerator(document));
            var newDocument = document.WithSyntaxRoot(root.ReplaceNode(declaration, newDeclaration));
            var changes = await newDocument.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);
            var newText = await newDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return CompletionChange.Create(CodeAnalysis.Completion.Utilities.Collapse(newText, changes.ToImmutableArray()));
        }

        private static SyntaxNode AddAsyncModifier(SyntaxNode declaration, SyntaxGenerator generator)
        {
            var modifiers = generator.GetModifiers(declaration);
            return generator.WithModifiers(declaration, modifiers.WithAsync(true));
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);

            var workspace = document.Project.Solution.Workspace;
            var syntaxContext = CSharpSyntaxContext.CreateContext(workspace, semanticModel, position, cancellationToken);

            if (!syntaxContext.IsAwaitKeywordContext(position))
            {
                return;
            }

            var method = syntaxContext.TargetToken.GetAncestor(node => node.IsAsyncSupportingFunctionSyntax());
            if (method is not null && !method.GetModifiers().Any(SyntaxKind.AsyncKeyword))
            {
                context.AddItem(CommonCompletionItem.Create("await", string.Empty, CompletionItemRules.Default, inlineDescription: "Make container async"));
            }
        }
    }
}
