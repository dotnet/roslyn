// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.CompletionProviders.Snippets
{

    internal abstract class ControlStructureCompletionProvider : CommonCompletionProvider
    {
        internal override bool ShouldTriggerCompletion(HostLanguageServices languageServices, SourceText text, int caretPosition, CompletionTrigger trigger, CompletionOptions options)
        {
            return base.ShouldTriggerCompletion(languageServices, text, caretPosition, trigger, options);
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxFacts.IsInNonUserCode(syntaxTree, position, cancellationToken))
            {
                return;
            }

            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            var syntaxContext = (CSharpSyntaxContext)document.GetRequiredLanguageService<ISyntaxContextService>().CreateContext(document, semanticModel, position, cancellationToken);
            var isInsideMethod = syntaxContext.LeftToken.GetAncestors<SyntaxNode>()
                .Any(node => node.IsKind(SyntaxKind.MethodDeclaration) ||
                             node.IsKind(SyntaxKind.LocalFunctionStatement) ||
                             node.IsKind(SyntaxKind.AnonymousMethodExpression) ||
                             node.IsKind(SyntaxKind.ParenthesizedLambdaExpression));

            if (!syntaxContext.IsGlobalStatementContext && !isInsideMethod)
            {
                return;
            }

            var generator = SyntaxGenerator.GetGenerator(document);
            var syntaxKinds = document.GetRequiredLanguageService<ISyntaxKindsService>();
            var completionItem = GetCompletionItems(syntaxContext.TargetToken, generator, syntaxKinds, syntaxFacts);
            context.AddItems(completionItem);
        }

        protected abstract IEnumerable<CompletionItem> GetCompletionItems(SyntaxToken token, SyntaxGenerator generator, ISyntaxKindsService syntaxKinds, ISyntaxFactsService syntaxFacts);
    }
}
