// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualBasic;

namespace Microsoft.CodeAnalysis.CSharp.SimplifyLinqExpression;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpSimplifyLinqTypeCheckAndCastDiagnosticAnalyzer()
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer(
        IDEDiagnosticIds.SimplifyLinqTypeCheckAndCastDiagnosticId,
        EnforceOnBuildValues.SimplifyLinqExpression,
        option: null,
        title: new LocalizableResourceString(nameof(AnalyzersResources.Simplify_LINQ_expression), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
{
    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(context =>
        {
            var enumerableType = context.Compilation.GetTypeByMetadataName(typeof(Enumerable).FullName!);
            if (enumerableType is null)
                return;

            context.RegisterSyntaxNodeAction(context => AnalyzeInvocationExpression(context, enumerableType), SyntaxKind.InvocationExpression);
        });
    }

    private static bool AnalyzeWhereMethod(
        SemanticModel semanticModel,
        LambdaExpressionSyntax whereLambda,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out ITypeSymbol? whereType)
    {
        whereType = null;

        // has to look like `a => a is ...` or `(T a) => a is ...`
        var whereParameters = whereLambda switch
        {
            ParenthesizedLambdaExpressionSyntax parenthesizedLambda => parenthesizedLambda.ParameterList.Parameters,
            SimpleLambdaExpressionSyntax simpleLambda => [simpleLambda.Parameter],
            _ => [],
        };

        if (whereParameters is not [var parameter])
            return false;

        // Body needs to be `a is SomeType`
        var parameterName = parameter.Identifier.ValueText;
        if (whereLambda.Body is not BinaryExpressionSyntax(kind: SyntaxKind.IsExpression)
            {
                Left: IdentifierNameSyntax leftIdentifier,
                Right: TypeSyntax whereTypeSyntax
            })
        {
            return false;
        }

        // Value being checked needs to be the parameter passed in.
        if (leftIdentifier.Identifier.ValueText != parameterName)
            return false;

        whereType = semanticModel.GetTypeInfo(whereTypeSyntax, cancellationToken).Type;
        return whereType != null;
    }

    private void AnalyzeInvocationExpression(
        SyntaxNodeAnalysisContext context, INamedTypeSymbol enumerableType)
    {
        var cancellationToken = context.CancellationToken;
        var semanticModel = context.SemanticModel;

        if (ShouldSkipAnalysis(context, notification: null))
            return;

        // Needs to look like `.Where(...).Cast<...>()`
        var invocationExpression = (InvocationExpressionSyntax)context.Node;
        if (invocationExpression is not
            {
                // Needs to be `.Cast<...>()`
                ArgumentList.Arguments: [],
                Expression: MemberAccessExpressionSyntax
                {
                    Name: GenericNameSyntax
                    {
                        Identifier.ValueText: nameof(Enumerable.Cast),
                        TypeArgumentList.Arguments: [var castTypeArgument]
                    } castName,
                    Expression: InvocationExpressionSyntax
                    {
                        // Needs to be `.Where(... => ...)`
                        ArgumentList.Arguments: [{ Expression: LambdaExpressionSyntax whereLambda }],
                        Expression: MemberAccessExpressionSyntax
                        {
                            Name: IdentifierNameSyntax { Identifier.ValueText: nameof(Enumerable.Where) },
                        },
                    } whereInvocation,
                },
            } castInvocation)
        {
            return;
        }

        if (!AnalyzeWhereMethod(semanticModel, whereLambda, cancellationToken, out var whereType))
            return;

        // Ensure the `is SomeType` and `Cast<SomeType>` are the same type.
        var semanticModel = context.SemanticModel;
        var castType = semanticModel.GetTypeInfo(castTypeArgument, cancellationToken).Type;
        if (castType is null)
            return;

        if (!whereType.Equals(castType))
            return;

        var castSymbol = semanticModel.GetSymbolInfo(castInvocation, cancellationToken).Symbol;
        var whereSymbol = semanticModel.GetSymbolInfo(whereInvocation, cancellationToken).Symbol;

        if (!enumerableType.Equals(castSymbol?.OriginalDefinition.ContainingType) ||
            !enumerableType.Equals(whereSymbol?.OriginalDefinition.ContainingType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptor,
            castName.Identifier.GetLocation(),
            additionalLocations: [invocationExpression.GetLocation()]));
    }
}
