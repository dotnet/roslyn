// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
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
        if (semanticModel.GetTypeInfo(collectionExpression, cancellationToken).Type is not INamedTypeSymbol collectionExpressionType)
            return null;

        var within = semanticModel.GetEnclosingNamedType(position, cancellationToken);
        if (within == null)
            return null;

        var ilistOfTType = semanticModel.Compilation.IListOfTType();
        var icollectionOfTType = semanticModel.Compilation.ICollectionOfTType();

        var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(withElement.ArgumentList);
        var argumentState = await GetCurrentArgumentStateAsync(
            document, position, textSpan, cancellationToken).ConfigureAwait(false);

        var structuralTypeDisplayService = document.Project.Services.GetRequiredService<IStructuralTypeDisplayService>();
        var documentationCommentFormattingService = document.GetRequiredLanguageService<IDocumentationCommentFormattingService>();

        return TryGetInterfaceItems() ??
            TryGetCollectionBuilderItems() ??
            await TryGetInstanceConstructorItemsAsync().ConfigureAwait(false);

        SignatureHelpItems? TryGetInterfaceItems()
        {
            // When the type is IList<T> or ICollection<T>, we can provide a signature help item for the `(int capacity)`
            // constructor of List<T>, as that's what the compiler will call into.
            if (!Equals(ilistOfTType, collectionExpressionType.OriginalDefinition) &&
                !Equals(icollectionOfTType, collectionExpressionType.OriginalDefinition))
            {
                return null;
            }

            var listOfTType = semanticModel.Compilation.ListOfTType();
            if (listOfTType is null)
                return null;

            var constructedListType = listOfTType.Construct(collectionExpressionType.TypeArguments.Single());
            var constructor = constructedListType.InstanceConstructors.FirstOrDefault(
                m => m.Parameters is [{ Type.SpecialType: SpecialType.System_Int32, Name: "capacity" }]);
            if (constructor is null)
                return null;

            var item = ObjectCreationExpressionSignatureHelpProvider.ConvertNormalTypeConstructor(
                constructor, withElement.SpanStart, semanticModel, structuralTypeDisplayService, documentationCommentFormattingService);
            return CreateSignatureHelpItems(
                [item], textSpan, argumentState, selectedItemIndex: 0, parameterIndexOverride: -1);
        }

        SignatureHelpItems? TryGetCollectionBuilderItems()
        {
            // If the type has a [CollectionBuilder(typeof(...), "...")] attribute on it, find the method it points to, and
            // produce the synthesized signature help items for it (e.g. without the ReadOnlySpan<T> parameter).
            var readonlySpanOfTType = semanticModel.Compilation.ReadOnlySpanOfTType();
            var attribute = collectionExpressionType.GetAttributes().FirstOrDefault(
                a => a.AttributeClass.IsCollectionBuilderAttribute());
            if (attribute is { ConstructorArguments: [{ Value: INamedTypeSymbol builderType }, { Value: string builderMethodName }] })
            {
                var builderMethod = builderType
                    .GetMembers(builderMethodName)
                    .OfType<IMethodSymbol>()
                    .Where(m =>
                        m.IsStatic && m.Parameters.Length >= 1 &&
                        m.Arity == collectionExpressionType.Arity &&
                        (Equals(m.Parameters[0].Type.OriginalDefinition, readonlySpanOfTType) ||
                         Equals(m.Parameters.Last().Type.OriginalDefinition, readonlySpanOfTType)))
                    .FirstOrDefault();

                if (builderMethod != null)
                {
                    var constructedBuilderMethod = builderMethod.Construct([.. collectionExpressionType.TypeArguments]);
                    var slicedParameters = Equals(constructedBuilderMethod.Parameters[0].Type.OriginalDefinition, readonlySpanOfTType)
                        ? builderMethod.Parameters[1..]
                        : builderMethod.Parameters[..^1];

                    var slicedMethod = CodeGenerationSymbolFactory.CreateMethodSymbol(
                        constructedBuilderMethod,
                        parameters: slicedParameters);
                    var item = AbstractOrdinaryMethodSignatureHelpProvider.ConvertMethodGroupMethod(
                        document, slicedMethod, withElement.SpanStart, semanticModel);

                    var (_, parameterIndexOverride) = new LightweightOverloadResolution(semanticModel, position, withElement.ArgumentList.Arguments)
                        .TryFindParameterIndexIfCompatibleMethod(slicedMethod);

                    return CreateSignatureHelpItems(
                        [item], textSpan, argumentState, selectedItemIndex: 0, parameterIndexOverride);
                }
            }

            return null;
        }

        async Task<SignatureHelpItems?> TryGetInstanceConstructorItemsAsync()
        {
            var methods = collectionExpressionType.InstanceConstructors
                .WhereAsArray(c => c.IsAccessibleWithin(within: within, throughType: c.ContainingType))
                .WhereAsArray(s => s.IsEditorBrowsable(options.HideAdvancedMembers, semanticModel.Compilation))
                .Sort(semanticModel, withElement.SpanStart);

            if (methods.IsEmpty)
                return null;

            // guess the best candidate if needed and determine parameter index
            //
            // Can add this back in once the compiler supports getting the SymbolInfo for a WithElement.
            // 
            // var (currentSymbol, parameterIndexOverride) = new LightweightOverloadResolution(semanticModel, position, withElement.ArgumentList.Arguments)
            //    .RefineOverloadAndPickParameter(semanticModel.GetSymbolInfo(objectCreationExpression, cancellationToken), methods);

            var items = methods.SelectAsArray(c =>
                ObjectCreationExpressionSignatureHelpProvider.ConvertNormalTypeConstructor(
                    c, withElement.SpanStart, semanticModel, structuralTypeDisplayService, documentationCommentFormattingService));

            var selectedItem = TryGetSelectedIndex(methods, currentSymbol: null);

            var textSpan = SignatureHelpUtilities.GetSignatureHelpSpan(withElement.ArgumentList);
            var argumentState = await GetCurrentArgumentStateAsync(
                document, position, textSpan, cancellationToken).ConfigureAwait(false);
            return CreateSignatureHelpItems(items, textSpan, argumentState, selectedItem, parameterIndexOverride: -1);
        }
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
