// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp;

[ExportSignatureHelpProvider("ObjectCreationExpressionSignatureHelpProvider", LanguageNames.CSharp), Shared]
internal partial class ObjectCreationExpressionSignatureHelpProvider : AbstractCSharpSignatureHelpProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ObjectCreationExpressionSignatureHelpProvider()
    {
    }

    public override bool IsTriggerCharacter(char ch)
        => ch is '(' or ',';

    public override bool IsRetriggerCharacter(char ch)
        => ch == ')';

    private async Task<BaseObjectCreationExpressionSyntax?> TryGetObjectCreationExpressionAsync(
        Document document,
        int position,
        SignatureHelpTriggerReason triggerReason,
        CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        if (!CommonSignatureHelpUtilities.TryGetSyntax(
                root, position, syntaxFacts, triggerReason, IsTriggerToken, IsArgumentListToken, cancellationToken, out BaseObjectCreationExpressionSyntax? expression))
        {
            return null;
        }

        if (expression.ArgumentList is null)
            return null;

        return expression;
    }

    private bool IsTriggerToken(SyntaxToken token)
        => SignatureHelpUtilities.IsTriggerParenOrComma<BaseObjectCreationExpressionSyntax>(token, IsTriggerCharacter);

    private static bool IsArgumentListToken(BaseObjectCreationExpressionSyntax expression, SyntaxToken token)
    {
        return expression.ArgumentList != null &&
            expression.ArgumentList.Span.Contains(token.SpanStart) &&
            token != expression.ArgumentList.CloseParenToken;
    }

    protected override async Task<SignatureHelpItems?> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, SignatureHelpOptions options, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var objectCreationExpression = await TryGetObjectCreationExpressionAsync(
            document, position, triggerInfo.TriggerReason, cancellationToken).ConfigureAwait(false);
        if (objectCreationExpression is not { ArgumentList: not null })
            return null;

        var semanticModel = await document.ReuseExistingSpeculativeModelAsync(objectCreationExpression, cancellationToken).ConfigureAwait(false);
        if (semanticModel.GetTypeInfo(objectCreationExpression, cancellationToken).Type is not INamedTypeSymbol type)
            return null;

        var within = semanticModel.GetEnclosingNamedType(position, cancellationToken);
        if (within == null)
            return null;

        var symbolDisplayService = document.GetLanguageService<ISymbolDisplayService>();
        if (type.TypeKind == TypeKind.Delegate)
            return await GetItemsWorkerForDelegateAsync(document, position, objectCreationExpression, type, cancellationToken).ConfigureAwait(false);

        // get the candidate methods
        var methods = type.InstanceConstructors
            .WhereAsArray(c => c.IsAccessibleWithin(within))
            .WhereAsArray(s => s.IsEditorBrowsable(options.HideAdvancedMembers, semanticModel.Compilation))
            .Sort(semanticModel, objectCreationExpression.SpanStart);

        if (!methods.Any())
            return null;

        // guess the best candidate if needed and determine parameter index
        var (currentSymbol, parameterIndexOverride) = new LightweightOverloadResolution(semanticModel, position, objectCreationExpression.ArgumentList.Arguments)
            .RefineOverloadAndPickParameter(semanticModel.GetSymbolInfo(objectCreationExpression, cancellationToken), methods);

        // present items and select
        var structuralTypeDisplayService = document.Project.Services.GetRequiredService<IStructuralTypeDisplayService>();
        var documentationCommentFormattingService = document.GetRequiredLanguageService<IDocumentationCommentFormattingService>();

        var items = methods.SelectAsArray(c =>
            ConvertNormalTypeConstructor(c, objectCreationExpression, semanticModel, structuralTypeDisplayService, documentationCommentFormattingService));

        var selectedItem = TryGetSelectedIndex(methods, currentSymbol);

        var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(objectCreationExpression.ArgumentList);
        var argumentState = await GetCurrentArgumentStateAsync(
            document, position, textSpan, cancellationToken).ConfigureAwait(false);
        return CreateSignatureHelpItems(items, textSpan, argumentState, selectedItem, parameterIndexOverride);
    }

    private async Task<SignatureHelpItems?> GetItemsWorkerForDelegateAsync(Document document, int position, BaseObjectCreationExpressionSyntax objectCreationExpression,
        INamedTypeSymbol type, CancellationToken cancellationToken)
    {
        var semanticModel = await document.ReuseExistingSpeculativeModelAsync(objectCreationExpression, cancellationToken).ConfigureAwait(false);
        Debug.Assert(type.TypeKind == TypeKind.Delegate);
        Debug.Assert(objectCreationExpression.ArgumentList is not null);

        var invokeMethod = type.DelegateInvokeMethod;
        if (invokeMethod is null)
            return null;

        // determine parameter index
        var parameterIndexOverride = new LightweightOverloadResolution(semanticModel, position, objectCreationExpression.ArgumentList.Arguments)
            .FindParameterIndexIfCompatibleMethod(invokeMethod);

        // present item and select
        var structuralTypeDisplayService = document.Project.Services.GetRequiredService<IStructuralTypeDisplayService>();
        var items = ConvertDelegateTypeConstructor(objectCreationExpression, invokeMethod, semanticModel, structuralTypeDisplayService, position);
        var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(objectCreationExpression.ArgumentList);
        var argumentState = await GetCurrentArgumentStateAsync(
            document, position, textSpan, cancellationToken).ConfigureAwait(false);
        return CreateSignatureHelpItems(items, textSpan, argumentState, selectedItemIndex: 0, parameterIndexOverride);
    }

    private async Task<SignatureHelpState?> GetCurrentArgumentStateAsync(
        Document document, int position, TextSpan currentSpan, CancellationToken cancellationToken)
    {
        var expression = await TryGetObjectCreationExpressionAsync(
            document, position, SignatureHelpTriggerReason.InvokeSignatureHelpCommand, cancellationToken).ConfigureAwait(false);
        if (expression is { ArgumentList: not null } &&
            currentSpan.Start == SignatureHelpUtilities.GetSignatureHelpSpan(expression.ArgumentList).Start)
        {
            return SignatureHelpUtilities.GetSignatureHelpState(expression.ArgumentList, position);
        }

        return null;
    }
}
