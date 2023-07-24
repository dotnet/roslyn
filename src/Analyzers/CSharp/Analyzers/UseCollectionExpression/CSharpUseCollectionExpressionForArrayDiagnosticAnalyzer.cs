// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Analyzers.UseCollectionExpression;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed partial class CSharpUseCollectionExpressionForArrayDiagnosticAnalyzer
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    private static readonly DiagnosticDescriptor s_descriptor = CreateDescriptorWithId(
        IDEDiagnosticIds.UseCollectionExpressionForArrayDiagnosticId,
        EnforceOnBuildValues.UseCollectionExpressionForArray,
        new LocalizableResourceString(nameof(AnalyzersResources.Simplify_collection_initialization), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Collection_initialization_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        isUnnecessary: false);

    private static readonly DiagnosticDescriptor s_unnecessaryCodeDescriptor = CreateDescriptorWithId(
        IDEDiagnosticIds.UseCollectionExpressionForArrayDiagnosticId,
        EnforceOnBuildValues.UseCollectionExpressionForArray,
        new LocalizableResourceString(nameof(AnalyzersResources.Simplify_collection_initialization), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Collection_initialization_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        isUnnecessary: true);

    public CSharpUseCollectionExpressionForArrayDiagnosticAnalyzer()
        : base(ImmutableDictionary<DiagnosticDescriptor, IOption2>.Empty
                .Add(s_descriptor, CodeStyleOptions2.PreferCollectionExpression)
                .Add(s_unnecessaryCodeDescriptor, CodeStyleOptions2.PreferCollectionExpression))
    {
    }

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterCompilationStartAction(OnCompilationStart);

    private void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        if (!context.Compilation.LanguageVersion().SupportsCollectionExpressions())
            return;

        // We wrap the SyntaxNodeAction within a CodeBlockStartAction, which allows us to
        // get callbacks for object creation expression nodes, but analyze nodes across the entire code block
        // and eventually report fading diagnostics with location outside this node.
        // Without the containing CodeBlockStartAction, our reported diagnostic would be classified
        // as a non-local diagnostic and would not participate in lightbulb for computing code fixes.
        context.RegisterCodeBlockStartAction<SyntaxKind>(context =>
        {
            context.RegisterSyntaxNodeAction(
                context => AnalyzeArrayInitializer(context),
                SyntaxKind.ArrayInitializerExpression);
        });
    }

    private static void AnalyzeArrayInitializer(SyntaxNodeAnalysisContext context)
    {
        var semanticModel = context.SemanticModel;
        var syntaxTree = semanticModel.SyntaxTree;
        var initializer = (InitializerExpressionSyntax)context.Node;
        var cancellationToken = context.CancellationToken;

        // no point in analyzing if the option is off.
        var option = context.GetAnalyzerOptions().PreferCollectionExpression;
        if (!option.Value)
            return;

        var isConcreteOrImplicitArrayCreation = initializer.Parent is ArrayCreationExpressionSyntax or ImplicitArrayCreationExpressionSyntax;

        // a naked `{ ... }` can only be converted to a collection expression when in the exact form `x = { ... }`
        if (!isConcreteOrImplicitArrayCreation && initializer.Parent is not EqualsValueClauseSyntax)
            return;

        var arrayCreationExpression = isConcreteOrImplicitArrayCreation
            ? (ExpressionSyntax)initializer.GetRequiredParent()
            : initializer;

        if (!UseCollectionExpressionHelpers.CanReplaceWithCollectionExpression(
                semanticModel, arrayCreationExpression, cancellationToken))
        {
            return;
        }

        if (isConcreteOrImplicitArrayCreation)
        {
            var locations = ImmutableArray.Create(initializer.GetLocation());
            context.ReportDiagnostic(DiagnosticHelper.Create(
                s_descriptor,
                arrayCreationExpression.GetFirstToken().GetLocation(),
                option.Notification.Severity,
                additionalLocations: locations,
                properties: null));

            var additionalUnnecessaryLocations = ImmutableArray.Create(
                syntaxTree.GetLocation(TextSpan.FromBounds(
                    arrayCreationExpression.SpanStart,
                    arrayCreationExpression is ArrayCreationExpressionSyntax arrayCreation
                        ? arrayCreation.Type.Span.End
                        : ((ImplicitArrayCreationExpressionSyntax)arrayCreationExpression).CloseBracketToken.Span.End)));

            context.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
                s_unnecessaryCodeDescriptor,
                additionalUnnecessaryLocations[0],
                ReportDiagnostic.Default,
                additionalLocations: locations,
                additionalUnnecessaryLocations: additionalUnnecessaryLocations));
        }
        else
        {
            Debug.Assert(initializer.Parent is EqualsValueClauseSyntax);
            // int[] = { 1, 2, 3 };
            //
            // In this case, we always have a target type, so it should always be valid to convert this to a collection expression.
            context.ReportDiagnostic(DiagnosticHelper.Create(
                s_descriptor,
                initializer.OpenBraceToken.GetLocation(),
                option.Notification.Severity,
                additionalLocations: ImmutableArray.Create(initializer.GetLocation()),
                properties: null));
        }
    }
}
