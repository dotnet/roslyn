// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static UseCollectionExpressionHelpers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed partial class CSharpUseCollectionExpressionForCreateDiagnosticAnalyzer
    : AbstractCSharpUseCollectionExpressionDiagnosticAnalyzer
{
    public const string UnwrapArgument = nameof(UnwrapArgument);

    private static readonly ImmutableDictionary<string, string?> s_unwrapArgumentProperties =
        ImmutableDictionary<string, string?>.Empty.Add(UnwrapArgument, UnwrapArgument);

    public CSharpUseCollectionExpressionForCreateDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.UseCollectionExpressionForCreateDiagnosticId,
               EnforceOnBuildValues.UseCollectionExpressionForCreate)
    {
    }

    protected override bool IsSupported(Compilation compilation)
        => compilation.CollectionBuilderAttribute() is not null;

    protected override void InitializeWorker(CodeBlockStartAnalysisContext<SyntaxKind> context)
        => context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);

    private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
    {
        var semanticModel = context.SemanticModel;
        var syntaxTree = semanticModel.SyntaxTree;
        var cancellationToken = context.CancellationToken;

        // no point in analyzing if the option is off.
        var option = context.GetAnalyzerOptions().PreferCollectionExpression;
        if (!option.Value)
            return;

        var invocationExpression = (InvocationExpressionSyntax)context.Node;
        if (!IsCollectionFactoryCreate(semanticModel, invocationExpression, out var memberAccess, out var unwrapArgument, cancellationToken))
            return;

        // Make sure we can actually use a collection expression in place of the full invocation.
        if (!CanReplaceWithCollectionExpression(semanticModel, invocationExpression, skipVerificationForReplacedNode: true, cancellationToken))
            return;

        var locations = ImmutableArray.Create(invocationExpression.GetLocation());
        var properties = unwrapArgument ? s_unwrapArgumentProperties : null;

        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            memberAccess.Name.Identifier.GetLocation(),
            option.Notification.Severity,
            additionalLocations: locations,
            properties));

        var additionalUnnecessaryLocations = ImmutableArray.Create(
            syntaxTree.GetLocation(TextSpan.FromBounds(
                invocationExpression.SpanStart,
                invocationExpression.ArgumentList.OpenParenToken.Span.End)),
            invocationExpression.ArgumentList.CloseParenToken.GetLocation());

        context.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
            UnnecessaryCodeDescriptor,
            additionalUnnecessaryLocations[0],
            ReportDiagnostic.Default,
            additionalLocations: locations,
            additionalUnnecessaryLocations: additionalUnnecessaryLocations,
            properties));
    }
}
