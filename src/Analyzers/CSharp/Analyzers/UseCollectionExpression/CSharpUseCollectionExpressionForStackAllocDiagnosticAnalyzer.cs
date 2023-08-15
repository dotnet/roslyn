// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
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

    protected override void InitializeWorker(CodeBlockStartAnalysisContext<SyntaxKind> context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeExplicitStackAllocExpression, SyntaxKind.StackAllocArrayCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeImplicitStackAllocExpression, SyntaxKind.ImplicitStackAllocArrayCreationExpression);
    }

    private void AnalyzeImplicitStackAllocExpression(SyntaxNodeAnalysisContext context)
    {
        var semanticModel = context.SemanticModel;
        var syntaxTree = semanticModel.SyntaxTree;
        var expression = (ImplicitStackAllocArrayCreationExpressionSyntax)context.Node;
        var cancellationToken = context.CancellationToken;

        // no point in analyzing if the option is off.
        var option = context.GetAnalyzerOptions().PreferCollectionExpression;
        if (!option.Value)
            return;

        if (!UseCollectionExpressionHelpers.CanReplaceWithCollectionExpression(
                semanticModel, expression, skipVerificationForReplacedNode: true, cancellationToken))
        {
            return;
        }

        var locations = ImmutableArray.Create(expression.GetLocation());
        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            expression.GetFirstToken().GetLocation(),
            option.Notification.Severity,
            additionalLocations: locations,
            properties: null));

        var additionalUnnecessaryLocations = ImmutableArray.Create(
            syntaxTree.GetLocation(TextSpan.FromBounds(
                expression.SpanStart,
                expression.CloseBracketToken.Span.End)));

        context.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
            UnnecessaryCodeDescriptor,
            additionalUnnecessaryLocations[0],
            ReportDiagnostic.Default,
            additionalLocations: locations,
            additionalUnnecessaryLocations: additionalUnnecessaryLocations));
    }

    private void AnalyzeExplicitStackAllocExpression(SyntaxNodeAnalysisContext context)
    {
        var semanticModel = context.SemanticModel;
        var syntaxTree = semanticModel.SyntaxTree;
        var expression = (StackAllocArrayCreationExpressionSyntax)context.Node;
        var cancellationToken = context.CancellationToken;

        // no point in analyzing if the option is off.
        var option = context.GetAnalyzerOptions().PreferCollectionExpression;
        if (!option.Value)
            return;

        var matches = TryGetMatches(semanticModel, expression, cancellationToken);
        if (matches.IsDefault)
            return;

        var locations = ImmutableArray.Create(expression.GetLocation());
        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            expression.GetFirstToken().GetLocation(),
            option.Notification.Severity,
            additionalLocations: locations,
            properties: null));

        var additionalUnnecessaryLocations = ImmutableArray.Create(
            syntaxTree.GetLocation(TextSpan.FromBounds(
                expression.SpanStart,
                expression.Type.Span.End)));

        context.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
            UnnecessaryCodeDescriptor,
            additionalUnnecessaryLocations[0],
            ReportDiagnostic.Default,
            additionalLocations: locations,
            additionalUnnecessaryLocations: additionalUnnecessaryLocations));
    }

    public static ImmutableArray<CollectionExpressionMatch<StatementSyntax>> TryGetMatches(
        SemanticModel semanticModel,
        StackAllocArrayCreationExpressionSyntax expression,
        CancellationToken cancellationToken)
    {
        return UseCollectionExpressionHelpers.TryGetMatches(
            semanticModel,
            expression,
            static e => e.Type,
            static e => e.Initializer,
            cancellationToken);
    }
}
