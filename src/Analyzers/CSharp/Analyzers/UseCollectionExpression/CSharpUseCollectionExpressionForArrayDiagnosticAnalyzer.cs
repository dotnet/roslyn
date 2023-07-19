// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UseCollectionExpression;

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
        IDEDiagnosticIds.UseCollectionInitializerDiagnosticId,
        EnforceOnBuildValues.UseCollectionExpressionForArray,
        new LocalizableResourceString(nameof(AnalyzersResources.Simplify_collection_initialization), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Collection_initialization_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        isUnnecessary: true);

    public CSharpUseCollectionExpressionForArrayDiagnosticAnalyzer()
        : base(ImmutableDictionary<DiagnosticDescriptor, IOption2>.Empty
                .Add(s_descriptor, CodeStyleOptions2.PreferCollectionExpressionForArray)
                .Add(s_unnecessaryCodeDescriptor, CodeStyleOptions2.PreferCollectionExpressionForArray))
    {
    }

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterCompilationStartAction(OnCompilationStart);

    private void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        if (!context.Compilation.LanguageVersion().IsCSharp12OrAbove())
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

    private void AnalyzeArrayInitializer(SyntaxNodeAnalysisContext context)
    {
        var semanticModel = context.SemanticModel;
        var objectCreationExpression = (InitializerExpressionSyntax)context.Node;
        var language = objectCreationExpression.Language;
        var cancellationToken = context.CancellationToken;

        // no point in analyzing if the option is off.
        var option = context.GetAnalyzerOptions().PreferCollectionExpressionForArray;
        if (!option.Value)
            return;


    }

    //private void AnalyzeNode(SyntaxNodeAnalysisContext context, INamedTypeSymbol ienumerableType)
    //{
    //    var semanticModel = context.SemanticModel;
    //    var objectCreationExpression = (TObjectCreationExpressionSyntax)context.Node;
    //    var language = objectCreationExpression.Language;
    //    var cancellationToken = context.CancellationToken;

    //    var option = context.GetAnalyzerOptions().PreferCollectionInitializer;
    //    if (!option.Value)
    //    {
    //        // not point in analyzing if the option is off.
    //        return;
    //    }

    //    // Object creation can only be converted to collection initializer if it
    //    // implements the IEnumerable type.
    //    var objectType = context.SemanticModel.GetTypeInfo(objectCreationExpression, cancellationToken);
    //    if (objectType.Type == null || !objectType.Type.AllInterfaces.Contains(ienumerableType))
    //        return;

    //    var matches = UseCollectionInitializerAnalyzer<TExpressionSyntax, TStatementSyntax, TObjectCreationExpressionSyntax, TMemberAccessExpressionSyntax, TInvocationExpressionSyntax, TExpressionStatementSyntax, TVariableDeclaratorSyntax>.Analyze(
    //        semanticModel, GetSyntaxFacts(), objectCreationExpression, cancellationToken);

    //    if (matches == null || matches.Value.Length == 0)
    //        return;

    //    var containingStatement = objectCreationExpression.FirstAncestorOrSelf<TStatementSyntax>();
    //    if (containingStatement == null)
    //        return;

    //    var nodes = ImmutableArray.Create<SyntaxNode>(containingStatement).AddRange(matches.Value);
    //    var syntaxFacts = GetSyntaxFacts();
    //    if (syntaxFacts.ContainsInterleavedDirective(nodes, cancellationToken))
    //        return;

    //    var locations = ImmutableArray.Create(objectCreationExpression.GetLocation());

    //    context.ReportDiagnostic(DiagnosticHelper.Create(
    //        s_descriptor,
    //        objectCreationExpression.GetFirstToken().GetLocation(),
    //        option.Notification.Severity,
    //        additionalLocations: locations,
    //        properties: null));

    //    FadeOutCode(context, matches.Value, locations);
    //}

    //private void FadeOutCode(
    //    SyntaxNodeAnalysisContext context,
    //    ImmutableArray<TExpressionStatementSyntax> matches,
    //    ImmutableArray<Location> locations)
    //{
    //    var syntaxTree = context.Node.SyntaxTree;
    //    var syntaxFacts = GetSyntaxFacts();

    //    foreach (var match in matches)
    //    {
    //        var expression = syntaxFacts.GetExpressionOfExpressionStatement(match);

    //        if (syntaxFacts.IsInvocationExpression(expression))
    //        {
    //            var arguments = syntaxFacts.GetArgumentsOfInvocationExpression(expression);
    //            var additionalUnnecessaryLocations = ImmutableArray.Create(
    //                syntaxTree.GetLocation(TextSpan.FromBounds(match.SpanStart, arguments[0].SpanStart)),
    //                syntaxTree.GetLocation(TextSpan.FromBounds(arguments.Last().FullSpan.End, match.Span.End)));

    //            // Report the diagnostic at the first unnecessary location. This is the location where the code fix
    //            // will be offered.
    //            var location1 = additionalUnnecessaryLocations[0];

    //            context.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
    //                s_unnecessaryCodeDescriptor,
    //                location1,
    //                ReportDiagnostic.Default,
    //                additionalLocations: locations,
    //                additionalUnnecessaryLocations: additionalUnnecessaryLocations));
    //        }
    //    }
    //}
}
