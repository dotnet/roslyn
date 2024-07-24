// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.CodeStyle;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed partial class CSharpUseCollectionExpressionForStackAllocDiagnosticAnalyzer
    : AbstractCSharpUseCollectionExpressionDiagnosticAnalyzer
{
    public CSharpUseCollectionExpressionForStackAllocDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.UseCollectionExpressionForStackAllocDiagnosticId,
               EnforceOnBuildValues.UseCollectionExpressionForStackAlloc)
    {
    }

    protected override bool IsSupported(Compilation compilation)
    {
        // Runtime needs to support inline arrays in order for this to be ok.  Otherwise compiler has no good way to
        // emit these collection expressions.
        return compilation.SupportsRuntimeCapability(RuntimeCapability.InlineArrayTypes);
    }

    protected override void InitializeWorker(CodeBlockStartAnalysisContext<SyntaxKind> context, INamedTypeSymbol? expressionType)
    {
        context.RegisterSyntaxNodeAction(context => AnalyzeExplicitStackAllocExpression(context, expressionType), SyntaxKind.StackAllocArrayCreationExpression);
        context.RegisterSyntaxNodeAction(context => AnalyzeImplicitStackAllocExpression(context, expressionType), SyntaxKind.ImplicitStackAllocArrayCreationExpression);
    }

    private void AnalyzeImplicitStackAllocExpression(SyntaxNodeAnalysisContext context, INamedTypeSymbol? expressionType)
    {
        var semanticModel = context.SemanticModel;
        var syntaxTree = semanticModel.SyntaxTree;
        var expression = (ImplicitStackAllocArrayCreationExpressionSyntax)context.Node;
        var cancellationToken = context.CancellationToken;

        // no point in analyzing if the option is off.
        var option = context.GetAnalyzerOptions().PreferCollectionExpression;
        if (option.Value is CollectionExpressionPreference.Never || ShouldSkipAnalysis(context, option.Notification))
            return;

        // Stack alloc can never be wrapped in an interface, so don't even try.
        if (!UseCollectionExpressionHelpers.CanReplaceWithCollectionExpression(
                semanticModel, expression, expressionType, isSingletonInstance: false, allowSemanticsChange: false, skipVerificationForReplacedNode: true, cancellationToken, out _))
        {
            return;
        }

        var locations = ImmutableArray.Create(expression.GetLocation());
        var additionalUnnecessaryLocations = ImmutableArray.Create(
            syntaxTree.GetLocation(TextSpan.FromBounds(
                expression.SpanStart,
                expression.CloseBracketToken.Span.End)));
        context.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
            Descriptor,
            expression.GetFirstToken().GetLocation(),
            option.Notification,
            context.Options,
            additionalLocations: locations,
            additionalUnnecessaryLocations: additionalUnnecessaryLocations,
            properties: null));
    }

    private void AnalyzeExplicitStackAllocExpression(SyntaxNodeAnalysisContext context, INamedTypeSymbol? expressionType)
    {
        var semanticModel = context.SemanticModel;
        var syntaxTree = semanticModel.SyntaxTree;
        var expression = (StackAllocArrayCreationExpressionSyntax)context.Node;
        var cancellationToken = context.CancellationToken;

        // no point in analyzing if the option is off.
        var option = context.GetAnalyzerOptions().PreferCollectionExpression;
        if (option.Value is CollectionExpressionPreference.Never || ShouldSkipAnalysis(context, option.Notification))
            return;

        var allowSemanticsChange = option.Value is CollectionExpressionPreference.WhenTypesLooselyMatch;
        var matches = TryGetMatches(semanticModel, expression, expressionType, allowSemanticsChange, cancellationToken);
        if (matches.IsDefault)
            return;

        var locations = ImmutableArray.Create(expression.GetLocation());
        var additionalUnnecessaryLocations = ImmutableArray.Create(
            syntaxTree.GetLocation(TextSpan.FromBounds(
                expression.SpanStart,
                expression.Type.Span.End)));
        context.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
            Descriptor,
            expression.GetFirstToken().GetLocation(),
            option.Notification,
            context.Options,
            additionalLocations: locations,
            additionalUnnecessaryLocations: additionalUnnecessaryLocations,
            properties: null));
    }

    public static ImmutableArray<CollectionExpressionMatch<StatementSyntax>> TryGetMatches(
        SemanticModel semanticModel,
        StackAllocArrayCreationExpressionSyntax expression,
        INamedTypeSymbol? expressionType,
        bool allowSemanticsChange,
        CancellationToken cancellationToken)
    {
        return UseCollectionExpressionHelpers.TryGetMatches(
            semanticModel,
            expression,
            expressionType,
            isSingletonInstance: false,
            allowSemanticsChange,
            static e => e.Type,
            static e => e.Initializer,
            cancellationToken,
            out _);
    }
}
