// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.CodeStyle;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static UseCollectionExpressionHelpers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed partial class CSharpUseCollectionExpressionForArrayDiagnosticAnalyzer()
    : AbstractCSharpUseCollectionExpressionDiagnosticAnalyzer(
        IDEDiagnosticIds.UseCollectionExpressionForArrayDiagnosticId,
        EnforceOnBuildValues.UseCollectionExpressionForArray)
{
    protected override void InitializeWorker(CodeBlockStartAnalysisContext<SyntaxKind> context, INamedTypeSymbol? expressionType)
    {
        context.RegisterSyntaxNodeAction(context => AnalyzeArrayInitializerExpression(context, expressionType), SyntaxKind.ArrayInitializerExpression);
        context.RegisterSyntaxNodeAction(context => AnalyzeArrayCreationExpression(context, expressionType), SyntaxKind.ArrayCreationExpression);
    }

    private void AnalyzeArrayCreationExpression(SyntaxNodeAnalysisContext context, INamedTypeSymbol? expressionType)
    {
        var semanticModel = context.SemanticModel;
        var syntaxTree = semanticModel.SyntaxTree;
        var arrayCreationExpression = (ArrayCreationExpressionSyntax)context.Node;
        var cancellationToken = context.CancellationToken;

        // Don't analyze arrays with initializers here, they're handled in AnalyzeArrayInitializerExpression instead.
        if (arrayCreationExpression.Initializer != null)
            return;

        // no point in analyzing if the option is off.
        var option = context.GetAnalyzerOptions().PreferCollectionExpression;
        if (option.Value is CollectionExpressionPreference.Never || ShouldSkipAnalysis(context, option.Notification))
            return;

        // Analyze the statements that follow to see if they can initialize this array.
        var allowSemanticsChange = option.Value is CollectionExpressionPreference.WhenTypesLooselyMatch;
        var replacementExpression = CreateReplacementCollectionExpressionForAnalysis(arrayCreationExpression.Initializer);
        var matches = TryGetMatches(semanticModel, arrayCreationExpression, replacementExpression, expressionType, allowSemanticsChange, cancellationToken, out var changesSemantics);
        if (matches.IsDefault)
            return;

        ReportArrayCreationDiagnostics(context, syntaxTree, option.Notification, arrayCreationExpression, changesSemantics);
    }

    public static ImmutableArray<CollectionExpressionMatch<StatementSyntax>> TryGetMatches(
        SemanticModel semanticModel,
        ArrayCreationExpressionSyntax expression,
        CollectionExpressionSyntax replacementExpression,
        INamedTypeSymbol? expressionType,
        bool allowSemanticsChange,
        CancellationToken cancellationToken,
        out bool changesSemantics)
    {
        // we have `new T[...] ...;` defer to analyzer to find the items that follow that may need to
        // be added to the collection expression.
        var matches = UseCollectionExpressionHelpers.TryGetMatches(
            semanticModel,
            expression,
            replacementExpression,
            expressionType,
            isSingletonInstance: false,
            allowSemanticsChange,
            static e => e.Type,
            static e => e.Initializer,
            cancellationToken,
            out changesSemantics);
        if (matches.IsDefault)
            return default;

        if (!CanReplaceWithCollectionExpression(
                semanticModel, expression, replacementExpression, expressionType, isSingletonInstance: false, allowSemanticsChange, skipVerificationForReplacedNode: true, cancellationToken, out changesSemantics))
        {
            return default;
        }

        // If we have an initializer that itself is only full of collection expressions (like `{ ["a"], ["b"] }`), then
        // we can only convert if the final type we're converting to has an element type that itself is a collection type.
        if (expression.Initializer is { Expressions.Count: > 0 } &&
            expression.Initializer.Expressions.All(e => e is CollectionExpressionSyntax))
        {
            var convertedType = semanticModel.GetTypeInfo(expression.WalkUpParentheses(), cancellationToken).ConvertedType;
            if (convertedType is null)
                return default;

            var ienumerableType = convertedType.OriginalDefinition.SpecialType is SpecialType.System_Collections_Generic_IEnumerable_T
                ? (INamedTypeSymbol)convertedType
                : convertedType.AllInterfaces.FirstOrDefault(
                    i => i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T);
            if (ienumerableType is null)
                return default;

            if (!IsConstructibleCollectionType(
                    semanticModel.Compilation, ienumerableType.TypeArguments.Single()))
            {
                return default;
            }
        }

        return matches;
    }

    public static ImmutableArray<CollectionExpressionMatch<StatementSyntax>> TryGetMatches(
        SemanticModel semanticModel,
        ImplicitArrayCreationExpressionSyntax expression,
        CollectionExpressionSyntax replacementExpression,
        INamedTypeSymbol? expressionType,
        bool allowSemanticsChange,
        CancellationToken cancellationToken,
        out bool changesSemantics)
    {
        // if we have `new[] { ... }` we have no subsequent matches to add to the collection. All values come
        // from within the initializer.
        if (!CanReplaceWithCollectionExpression(
                semanticModel, expression, replacementExpression, expressionType, isSingletonInstance: false, allowSemanticsChange, skipVerificationForReplacedNode: true, cancellationToken, out changesSemantics))
        {
            return default;
        }

        return [];
    }

    private void AnalyzeArrayInitializerExpression(SyntaxNodeAnalysisContext context, INamedTypeSymbol? expressionType)
    {
        var semanticModel = context.SemanticModel;
        var syntaxTree = semanticModel.SyntaxTree;
        var initializer = (InitializerExpressionSyntax)context.Node;
        var cancellationToken = context.CancellationToken;

        // no point in analyzing if the option is off.
        var option = context.GetAnalyzerOptions().PreferCollectionExpression;
        if (option.Value is CollectionExpressionPreference.Never || ShouldSkipAnalysis(context, option.Notification))
            return;

        var isConcreteOrImplicitArrayCreation = initializer.Parent is ArrayCreationExpressionSyntax or ImplicitArrayCreationExpressionSyntax;

        // a naked `{ ... }` can only be converted to a collection expression when in the exact form `x = { ... }`
        if (!isConcreteOrImplicitArrayCreation && initializer.Parent is not EqualsValueClauseSyntax)
            return;

        var arrayCreationExpression = isConcreteOrImplicitArrayCreation
            ? (ExpressionSyntax)initializer.GetRequiredParent()
            : initializer;

        // Have to actually examine what would happen when we do the replacement, as the replaced value may interact
        // with inference based on the values within.
        var replacementCollectionExpression = CreateReplacementCollectionExpressionForAnalysis(initializer);

        var allowSemanticsChange = option.Value is CollectionExpressionPreference.WhenTypesLooselyMatch;
        if (!CanReplaceWithCollectionExpression(
                semanticModel, arrayCreationExpression, replacementCollectionExpression,
                expressionType, isSingletonInstance: false, allowSemanticsChange, skipVerificationForReplacedNode: true, cancellationToken,
                out var changesSemantics))
        {
            return;
        }

        if (isConcreteOrImplicitArrayCreation)
        {
            var matches = initializer.Parent switch
            {
                ArrayCreationExpressionSyntax arrayCreation => TryGetMatches(semanticModel, arrayCreation, replacementCollectionExpression, expressionType, allowSemanticsChange, cancellationToken, out _),
                ImplicitArrayCreationExpressionSyntax arrayCreation => TryGetMatches(semanticModel, arrayCreation, replacementCollectionExpression, expressionType, allowSemanticsChange, cancellationToken, out _),
                _ => throw ExceptionUtilities.Unreachable(),
            };

            if (matches.IsDefault)
                return;

            ReportArrayCreationDiagnostics(context, syntaxTree, option.Notification, arrayCreationExpression, changesSemantics);
        }
        else
        {
            Debug.Assert(initializer.Parent is EqualsValueClauseSyntax);

            // int[] = { 1, 2, 3 };
            //
            // In this case, we always have a target type, so it should always be valid to convert this to a collection expression.
            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                initializer.OpenBraceToken.GetLocation(),
                option.Notification,
                context.Options,
                additionalLocations: ImmutableArray.Create(initializer.GetLocation()),
                properties: changesSemantics ? ChangesSemantics : null));
        }
    }

    private void ReportArrayCreationDiagnostics(
        SyntaxNodeAnalysisContext context, SyntaxTree syntaxTree, NotificationOption2 notification, ExpressionSyntax expression, bool changesSemantics)
    {
        var properties = changesSemantics ? ChangesSemantics : null;
        var locations = ImmutableArray.Create(expression.GetLocation());
        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            expression.GetFirstToken().GetLocation(),
            notification,
            context.Options,
            additionalLocations: locations,
            properties: properties));

        var additionalUnnecessaryLocations = ImmutableArray.Create(
            syntaxTree.GetLocation(TextSpan.FromBounds(
                expression.SpanStart,
                expression is ArrayCreationExpressionSyntax arrayCreationExpression
                    ? arrayCreationExpression.Type.Span.End
                    : ((ImplicitArrayCreationExpressionSyntax)expression).CloseBracketToken.Span.End)));

        context.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
            UnnecessaryCodeDescriptor,
            additionalUnnecessaryLocations[0],
            NotificationOption2.ForSeverity(UnnecessaryCodeDescriptor.DefaultSeverity),
            context.Options,
            additionalLocations: locations,
            additionalUnnecessaryLocations: additionalUnnecessaryLocations,
            properties: properties));
    }
}
