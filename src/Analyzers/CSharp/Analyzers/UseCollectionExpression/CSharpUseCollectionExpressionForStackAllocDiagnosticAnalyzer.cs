// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Analyzers.UseCollectionExpression;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed partial class CSharpUseCollectionExpressionForStackAllocDiagnosticAnalyzer
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    private static readonly DiagnosticDescriptor s_descriptor = CreateDescriptorWithId(
        IDEDiagnosticIds.UseCollectionExpressionForStackAllocDiagnosticId,
        EnforceOnBuildValues.UseCollectionExpressionForStackAlloc,
        new LocalizableResourceString(nameof(AnalyzersResources.Simplify_collection_initialization), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Collection_initialization_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        isUnnecessary: false);

    private static readonly DiagnosticDescriptor s_unnecessaryCodeDescriptor = CreateDescriptorWithId(
        IDEDiagnosticIds.UseCollectionExpressionForStackAllocDiagnosticId,
        EnforceOnBuildValues.UseCollectionExpressionForStackAlloc,
        new LocalizableResourceString(nameof(AnalyzersResources.Simplify_collection_initialization), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Collection_initialization_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        isUnnecessary: true);

    public CSharpUseCollectionExpressionForStackAllocDiagnosticAnalyzer()
        : base(ImmutableDictionary<DiagnosticDescriptor, IOption2>.Empty
                .Add(s_descriptor, CodeStyleOptions2.PreferCollectionExpression)
                .Add(s_unnecessaryCodeDescriptor, CodeStyleOptions2.PreferCollectionExpression))
    {
    }

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterCompilationStartAction(OnCompilationStart);

    private void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var compilation = context.Compilation;
        if (!compilation.LanguageVersion().SupportsCollectionExpressions())
            return;

        //var spanType = compilation.GetBestTypeByMetadataName(typeof(Span<>).FullName);
        //var readOnlySpanType = compilation.GetBestTypeByMetadataName(typeof(ReadOnlySpan<>).FullName);
        //if (spanType is null || readOnlySpanType is null)
        //    return;

        // We wrap the SyntaxNodeAction within a CodeBlockStartAction, which allows us to
        // get callbacks for object creation expression nodes, but analyze nodes across the entire code block
        // and eventually report fading diagnostics with location outside this node.
        // Without the containing CodeBlockStartAction, our reported diagnostic would be classified
        // as a non-local diagnostic and would not participate in lightbulb for computing code fixes.
        context.RegisterCodeBlockStartAction<SyntaxKind>(context =>
        {
            context.RegisterSyntaxNodeAction(
                context => AnalyzeExplicitStackAllocExpression(context),
                SyntaxKind.StackAllocArrayCreationExpression);
            context.RegisterSyntaxNodeAction(
                context => AnalyzeImplicitStackAllocExpression(context),
                SyntaxKind.ImplicitStackAllocArrayCreationExpression);
        });
    }

    private static void AnalyzeImplicitStackAllocExpression(SyntaxNodeAnalysisContext context)
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
                semanticModel, expression, skipVerificationForReplacedNode: false, cancellationToken))
        {
            return;
        }

        var locations = ImmutableArray.Create(expression.GetLocation());
        context.ReportDiagnostic(DiagnosticHelper.Create(
            s_descriptor,
            expression.GetFirstToken().GetLocation(),
            option.Notification.Severity,
            additionalLocations: locations,
            properties: null));

        var additionalUnnecessaryLocations = ImmutableArray.Create(
            syntaxTree.GetLocation(TextSpan.FromBounds(
                expression.SpanStart,
                expression.CloseBracketToken.Span.End)));

        context.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
            s_unnecessaryCodeDescriptor,
            additionalUnnecessaryLocations[0],
            ReportDiagnostic.Default,
            additionalLocations: locations,
            additionalUnnecessaryLocations: additionalUnnecessaryLocations));
    }

    private static void AnalyzeExplicitStackAllocExpression(SyntaxNodeAnalysisContext context)
    {
        var semanticModel = context.SemanticModel;
        var syntaxTree = semanticModel.SyntaxTree;
        var expression = (StackAllocArrayCreationExpressionSyntax)context.Node;
        var cancellationToken = context.CancellationToken;

        // no point in analyzing if the option is off.
        var option = context.GetAnalyzerOptions().PreferCollectionExpression;
        if (!option.Value)
            return;

        // has to either be `stackalloc X[]` or `stackalloc X[const]`.
        if (expression.Type is not ArrayTypeSyntax { RankSpecifiers: [{ Sizes.Count: <= 1 } rankSpecifier] } arrayType)
            return;

        using var _ = ArrayBuilder<Location>.GetInstance(out var locationsBuilder);
        locationsBuilder.Add(expression.GetLocation());

        if (rankSpecifier.Sizes is [var size])
        {
            // if `stackalloc X[const]`, then it has to have a constant value for the size
            if (semanticModel.GetConstantValue(size).Value is not int sizeValue)
                return;

            if (expression.Initializer != null)
            {
                // if there is an initializer, then it has to have the right number of elements.
                if (sizeValue != expression.Initializer.Expressions.Count)
                    return;
            }
            else
            {
                // if there is no initializer, we have to be followed by direct statements that initialize the right
                // number of elements.

                // This needs to be local variable like `ReadOnlySpan<T> x = stackalloc ...
                if (expression.WalkUpParentheses().Parent is not EqualsValueClauseSyntax
                    {
                        Parent: VariableDeclaratorSyntax
                        {
                            Identifier.ValueText: var variableName,
                            Parent: VariableDeclarationSyntax
                            {
                                Parent: LocalDeclarationStatementSyntax localDeclarationStatement
                            }
                        } variableDeclarator,
                    })
                {
                    return;
                }

                //// Has to be either ReadOnlySpan<T> or Span<T>
                //var localVariable = (ILocalSymbol)semanticModel.GetRequiredDeclaredSymbol(variableDeclarator, cancellationToken);
                //if (localVariable.Type?.OriginalDefinition.SpecialType is not SpecialType.read)

                for (var currentIndex = 0; currentIndex < sizeValue; currentIndex++)
                {
                    // Each following statement needs to of the form:
                    //
                    //   x[...] =
                    var currentStatement = localDeclarationStatement.GetNextStatement();
                    if (currentStatement is not ExpressionStatementSyntax
                        {
                            Expression: AssignmentExpressionSyntax
                            {
                                Left: ElementAccessExpressionSyntax
                                {
                                    Expression: IdentifierNameSyntax { Identifier.ValueText: var elementName },
                                    ArgumentList.Arguments: [var elementArgument],
                                } elementAccess,
                                Right: var right,
                            }
                        })
                    {
                        return;
                    }

                    // Ensure we're indexing into the variable created.
                    if (variableName != elementName)
                        return;

                    // The indexing value has to equal the corresponding location in the result.
                    if (semanticModel.GetConstantValue(elementArgument.Expression).Value is not int indexValue ||
                        indexValue != currentIndex)
                    {
                        return;
                    }

                    // this looks like a good statement, add to the right size of the assignment to track as that's what
                    // we'll want to put in the final collection expression.
                    locationsBuilder.Add(right.GetLocation());
                }
            }
        }

        if (!UseCollectionExpressionHelpers.CanReplaceWithCollectionExpression(
                semanticModel, expression, skipVerificationForReplacedNode: false, cancellationToken))
        {
            return;
        }

        var locations = locationsBuilder.ToImmutableAndClear();
        context.ReportDiagnostic(DiagnosticHelper.Create(
            s_descriptor,
            expression.GetFirstToken().GetLocation(),
            option.Notification.Severity,
            additionalLocations: locations,
            properties: null));

        var additionalUnnecessaryLocations = ImmutableArray.Create(
            syntaxTree.GetLocation(TextSpan.FromBounds(
                expression.SpanStart,
                expression.Type.Span.End)));

        context.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
            s_unnecessaryCodeDescriptor,
            additionalUnnecessaryLocations[0],
            ReportDiagnostic.Default,
            additionalLocations: locations,
            additionalUnnecessaryLocations: additionalUnnecessaryLocations));
    }
}
