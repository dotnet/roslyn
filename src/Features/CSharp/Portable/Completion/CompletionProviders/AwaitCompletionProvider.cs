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
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(AwaitCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(KeywordCompletionProvider))]
    [Shared]
    internal sealed class AwaitCompletionProvider : LSPCompletionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AwaitCompletionProvider()
        {
        }

        public override ImmutableHashSet<char> TriggerCharacters => CompletionUtilities.CommonTriggerCharactersWithArgumentList;

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
                var symbol = semanticModel.GetDeclaredSymbol(method, cancellationToken) as IMethodSymbol;
                if (symbol is not null && IsTask(symbol.ReturnType))
                {
                    context.AddItem(CommonCompletionItem.Create("await", string.Empty, CompletionItemRules.Default, glyph: Glyph.Keyword, inlineDescription: CSharpFeaturesResources.Make_container_async));
                }
            }

            static bool IsTask(ITypeSymbol returnType)
                => returnType.Name is "Task" or "ValueTask";
        }

        internal override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem completionItem, TextSpan completionListSpan, char? commitKey, bool disallowAddingImports, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var declaration = root?.FindToken(completionListSpan.Start).GetAncestor(node => node.IsAsyncSupportingFunctionSyntax());
            if (root is null || declaration is null)
            {
                return await base.GetChangeAsync(document, completionItem, completionListSpan, commitKey, disallowAddingImports, cancellationToken).ConfigureAwait(false);
            }

            // MethodDeclarationSyntax, LocalFunctionStatementSyntax, AnonymousMethodExpressionSyntax, ParenthesizedLambdaExpressionSyntax, SimpleLambdaExpressionSyntax.

            var documentWithAsyncModifier = document.WithSyntaxRoot(root.ReplaceNode(declaration, AddAsyncModifier(document, declaration)));
            var formattedDocument = await Formatter.FormatAsync(documentWithAsyncModifier, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);

            var builder = ArrayBuilder<TextChange>.GetInstance();
            builder.AddRange(await formattedDocument.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false));
            builder.Add(new TextChange(completionListSpan, completionItem.DisplayText));

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var newText = text.WithChanges(builder);
            return CompletionChange.Create(CodeAnalysis.Completion.Utilities.Collapse(newText, builder.ToImmutableArray()));

            static SyntaxNode AddAsyncModifier(Document document, SyntaxNode declaration)
            {
                var generator = SyntaxGenerator.GetGenerator(document);
                var modifiers = generator.GetModifiers(declaration);
                return generator.WithModifiers(declaration, modifiers.WithAsync(true)).WithAdditionalAnnotations(Formatter.Annotation);
            }
        }
    }
}
