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
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

[ExportCompletionProvider(nameof(SpeculativeTCompletionProvider), LanguageNames.CSharp)]
[ExtensionOrder(After = nameof(AwaitCompletionProvider))]
[Shared]
internal sealed class SpeculativeTCompletionProvider : LSPCompletionProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public SpeculativeTCompletionProvider()
    {
    }

    internal override string Language => LanguageNames.CSharp;

    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
        => CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);

    public override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.CommonTriggerCharacters;

    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        try
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;

            var showSpeculativeT = await document.IsValidContextForDocumentOrLinkedDocumentsAsync(
                (doc, ct) => ShouldShowSpeculativeTCompletionItemAsync(doc, context, ct),
                cancellationToken).ConfigureAwait(false);

            if (showSpeculativeT)
            {
                const string T = nameof(T);
                context.AddItem(CommonCompletionItem.Create(
                    T, displayTextSuffix: "", CompletionItemRules.Default, glyph: Glyph.TypeParameter));
            }
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, ErrorSeverity.General))
        {
            // nop
        }
    }

    private static async Task<bool> ShouldShowSpeculativeTCompletionItemAsync(Document document, CompletionContext completionContext, CancellationToken cancellationToken)
    {
        var position = completionContext.Position;
        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        if (syntaxTree.IsInNonUserCode(position, cancellationToken) ||
            syntaxTree.IsPreProcessorDirectiveContext(position, cancellationToken))
        {
            return false;
        }

        var context = await completionContext.GetSyntaxContextWithExistingSpeculativeModelAsync(document, cancellationToken).ConfigureAwait(false);

        if (context.IsTaskLikeTypeContext)
            return false;

        // While it's less likely the user wants to type a (undeclared) type parameter when they are in a statement context, it's probably
        // fine to provide a speculative `T` item here since typing 2 characters would easily filter it out.
        return CompletionUtilities.IsSpeculativeTypeParameterContext(syntaxTree, position, context.SemanticModel, includeStatementContexts: true, cancellationToken);
    }
}
