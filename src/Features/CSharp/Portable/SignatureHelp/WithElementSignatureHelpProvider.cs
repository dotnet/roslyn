// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp;

[ExportSignatureHelpProvider("WithElementSignatureHelpProvider", LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class WithElementSignatureHelpProvider() : AbstractCSharpSignatureHelpProvider
{
    public override ImmutableArray<char> TriggerCharacters { get; } = ['(', ','];
    public override ImmutableArray<char> RetriggerCharacters { get; } = [')'];

    private async Task<WithElementSyntax?> TryGetWithElementAsync(
        Document document,
        int position,
        SignatureHelpTriggerReason triggerReason,
        CancellationToken cancellationToken)
    {
        var withElement = await CommonSignatureHelpUtilities.TryGetSyntaxAsync<WithElementSyntax>(
            document, position, triggerReason, IsTriggerToken, IsArgumentListToken, cancellationToken).ConfigureAwait(false);

        return withElement?.ArgumentList is null ? null : withElement;
    }

    private bool IsTriggerToken(SyntaxToken token)
        => SignatureHelpUtilities.IsTriggerParenOrComma<WithElementSyntax>(token, TriggerCharacters);

    private static bool IsArgumentListToken(WithElementSyntax expression, SyntaxToken token)
    {
        return expression.ArgumentList != null &&
            expression.ArgumentList.Span.Contains(token.SpanStart) &&
            token != expression.ArgumentList.CloseParenToken;
    }

    protected override async Task<SignatureHelpItems?> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, MemberDisplayOptions options, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var withElement = await TryGetWithElementAsync(
            document, position, triggerInfo.TriggerReason, cancellationToken).ConfigureAwait(false);
        if (withElement?.Parent is not CollectionExpressionSyntax collectionExpression)
            return null;

        var semanticModel = await document.ReuseExistingSpeculativeModelAsync(withElement, cancellationToken).ConfigureAwait(false);
        if (semanticModel.GetTypeInfo(collectionExpression, cancellationToken).ConvertedType is not INamedTypeSymbol collectionExpressionType)
            return null;

        var within = semanticModel.GetEnclosingNamedType(position, cancellationToken);
        if (within == null)
            return null;

        var creationMethods = withElement
            .GetCreationMethods(semanticModel, cancellationToken)
            .WhereAsArray(s => s.IsEditorBrowsable(options.HideAdvancedMembers, semanticModel.Compilation))
            .Sort(semanticModel, withElement.SpanStart);

        if (creationMethods.IsEmpty)
            return null;

        // guess the best candidate if needed and determine parameter index
        //
        // Can add this back in once the compiler supports getting the SymbolInfo for a WithElement.
        // 
        // var (currentSymbol, parameterIndexOverride) = new LightweightOverloadResolution(semanticModel, position, withElement.ArgumentList.Arguments)
        //    .RefineOverloadAndPickParameter(semanticModel.GetSymbolInfo(withElement, cancellationToken), methods);
        ISymbol? currentSymbol = null;
        var parameterIndexOverride = -1;

        var structuralTypeDisplayService = document.Project.Services.GetRequiredService<IStructuralTypeDisplayService>();
        var documentationCommentFormattingService = document.GetRequiredLanguageService<IDocumentationCommentFormattingService>();

        var items = creationMethods.SelectAsArray(c => c.MethodKind == MethodKind.Constructor
            ? ObjectCreationExpressionSignatureHelpProvider.ConvertNormalTypeConstructor(c, withElement.SpanStart, semanticModel, structuralTypeDisplayService, documentationCommentFormattingService)
            : AbstractOrdinaryMethodSignatureHelpProvider.ConvertMethodGroupMethod(document, c, withElement.SpanStart, semanticModel));

        var selectedItem = TryGetSelectedIndex(creationMethods, currentSymbol);

        var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(withElement.ArgumentList);
        var argumentState = await GetCurrentArgumentStateAsync(
            document, position, textSpan, cancellationToken).ConfigureAwait(false);
        return CreateSignatureHelpItems(items, textSpan, argumentState, selectedItem, parameterIndexOverride);
    }

    private async Task<SignatureHelpState?> GetCurrentArgumentStateAsync(
        Document document, int position, TextSpan currentSpan, CancellationToken cancellationToken)
    {
        var expression = await TryGetWithElementAsync(
            document, position, SignatureHelpTriggerReason.InvokeSignatureHelpCommand, cancellationToken).ConfigureAwait(false);
        if (expression != null &&
            currentSpan.Start == SignatureHelpUtilities.GetSignatureHelpSpan(expression.ArgumentList).Start)
        {
            return SignatureHelpUtilities.GetSignatureHelpState(expression.ArgumentList, position);
        }

        return null;
    }
}
