// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
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

[ExportSignatureHelpProvider("WithElementSignatureHelpProvider", LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class WithElementSignatureHelpProvider() : AbstractCSharpSignatureHelpProvider
{
    public override bool IsTriggerCharacter(char ch)
        => ch is '(' or ',';

    public override bool IsRetriggerCharacter(char ch)
        => ch == ')';

    private async Task<WithElementSyntax?> TryGetWithElementAsync(
        Document document,
        int position,
        SignatureHelpTriggerReason triggerReason,
        CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        if (!CommonSignatureHelpUtilities.TryGetSyntax(
                root, position, syntaxFacts, triggerReason, IsTriggerToken, IsArgumentListToken, cancellationToken, out WithElementSyntax? expression))
        {
            return null;
        }

        if (expression.ArgumentList is null)
            return null;

        return expression;
    }

    private bool IsTriggerToken(SyntaxToken token)
        => SignatureHelpUtilities.IsTriggerParenOrComma<WithElementSyntax>(token, IsTriggerCharacter);

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
        if (semanticModel.GetTypeInfo(collectionExpression, cancellationToken).Type is not INamedTypeSymbol type)
            return null;

        var within = semanticModel.GetEnclosingNamedType(position, cancellationToken);
        if (within == null)
            return null;

        var ilistOfTType = semanticModel.Compilation.IListOfTType();
        var icollectionOfTType = semanticModel.Compilation.ICollectionOfTType();
        var listOfTType = semanticModel.Compilation.ListOfTType();

        var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(withElement.ArgumentList);
        var argumentState = await GetCurrentArgumentStateAsync(
            document, position, textSpan, cancellationToken).ConfigureAwait(false);

        var structuralTypeDisplayService = document.Project.Services.GetRequiredService<IStructuralTypeDisplayService>();
        var documentationCommentFormattingService = document.GetRequiredLanguageService<IDocumentationCommentFormattingService>();

        // When the type is IList<T> or ICollection<T>, we can provide a signature help item for the `(int capacity)`
        // constructor of List<T>, as that's what the compiler will call into.
        if (Equals(ilistOfTType, type.OriginalDefinition) ||
            Equals(icollectionOfTType, type.OriginalDefinition))
        {
            if (listOfTType is null)
                return null;

            var constructedListType = listOfTType.Construct(type.TypeArguments.Single());
            var constructor = constructedListType.InstanceConstructors.FirstOrDefault(
                m => m.Parameters is [{ Type.SpecialType: SpecialType.System_Int32, Name: "capacity" }]);
            if (constructor is null)
                return null;

            var item = ObjectCreationExpressionSignatureHelpProvider.ConvertNormalTypeConstructor(
                constructor, withElement.SpanStart, semanticModel, structuralTypeDisplayService, documentationCommentFormattingService);
            return CreateSignatureHelpItems(
                [item], textSpan, argumentState, selectedItemIndex: 0, parameterIndexOverride: -1);
        }

        if (type.TypeKind == TypeKind.Delegate)
            return await GetItemsWorkerForDelegateAsync(document, position, objectCreationExpression, type, cancellationToken).ConfigureAwait(false);

        // Get the candidate methods.  Consider the constructor's containing type to be the "through type" instance
        // (which matches the compiler's logic in Binder.IsConstructorAccessible), to ensure that we do not see
        // protected constructors in derived types (but continue to see them in nested types).
        var methods = type.InstanceConstructors
            .WhereAsArray(c => c.IsAccessibleWithin(within: within, throughType: c.ContainingType))
            .WhereAsArray(s => s.IsEditorBrowsable(options.HideAdvancedMembers, semanticModel.Compilation))
            .Sort(semanticModel, objectCreationExpression.SpanStart);

        if (methods.IsEmpty)
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
