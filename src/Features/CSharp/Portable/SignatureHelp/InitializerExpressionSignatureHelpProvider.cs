// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp;

[ExportSignatureHelpProvider(nameof(InitializerExpressionSignatureHelpProvider), LanguageNames.CSharp), Shared]
internal sealed partial class InitializerExpressionSignatureHelpProvider : AbstractOrdinaryMethodSignatureHelpProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public InitializerExpressionSignatureHelpProvider()
    {
    }

    public override ImmutableArray<char> TriggerCharacters => ['{', ','];

    public override ImmutableArray<char> RetriggerCharacters => ['}'];

    private bool TryGetInitializerExpression(
        SyntaxNode root,
        int position,
        ISyntaxFactsService syntaxFacts,
        SignatureHelpTriggerReason triggerReason,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out InitializerExpressionSyntax? expression)
    {
        return CommonSignatureHelpUtilities.TryGetSyntax(root, position, syntaxFacts, triggerReason, IsTriggerToken, IsInitializerExpressionToken, cancellationToken, out expression) &&
           expression != null;
    }

    private bool IsTriggerToken(SyntaxToken token)
        => !token.IsKind(SyntaxKind.None) &&
           token.ValueText.Length == 1 &&
           TriggerCharacters.Contains(token.ValueText[0]) &&
           token.Parent is InitializerExpressionSyntax;

    private static bool IsInitializerExpressionToken(InitializerExpressionSyntax expression, SyntaxToken token)
        => expression.Span.Contains(token.SpanStart) && token != expression.CloseBraceToken;

    protected override async Task<SignatureHelpItems?> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, MemberDisplayOptions options, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (!TryGetInitializerExpression(root, position, document.GetRequiredLanguageService<ISyntaxFactsService>(), triggerInfo.TriggerReason, cancellationToken, out var initializerExpression))
            return null;

        var addMethods = await CommonSignatureHelpUtilities.GetCollectionInitializerAddMethodsAsync(
            document, initializerExpression, options, cancellationToken).ConfigureAwait(false);
        if (addMethods.IsDefaultOrEmpty)
        {
            return null;
        }

        var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(initializerExpression);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        return CreateCollectionInitializerSignatureHelpItems([.. addMethods.Select(s =>
            ConvertMethodGroupMethod(document, s, initializerExpression.OpenBraceToken.SpanStart, semanticModel))],
            textSpan, GetCurrentArgumentState(root, position, syntaxFacts, textSpan, cancellationToken));
    }

    private SignatureHelpState? GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken)
    {
        if (TryGetInitializerExpression(
                root,
                position,
                syntaxFacts,
                SignatureHelpTriggerReason.InvokeSignatureHelpCommand,
                cancellationToken,
                out var expression) &&
            currentSpan.Start == SignatureHelpUtilities.GetSignatureHelpSpan(expression).Start)
        {
            return SignatureHelpUtilities.GetSignatureHelpState(expression, position);
        }

        return null;
    }
}
