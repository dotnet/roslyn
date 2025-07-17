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

    private static bool TryGetSingleLambdaParameter(
        LambdaExpressionSyntax lambda,
        [NotNullWhen(true)] out ParameterSyntax? lambdaParameter)
    {
        lambdaParameter = null;
        var whereParameters = lambda switch
        {
            ParenthesizedLambdaExpressionSyntax parenthesizedLambda => parenthesizedLambda.ParameterList.Parameters,
            SimpleLambdaExpressionSyntax simpleLambda => [simpleLambda.Parameter],
            _ => [],
        };

        if (whereParameters is not [var parameter])
            return false;

        lambdaParameter = parameter;
        return true;
    }

    private static bool AnalyzeWhereMethod(
        SemanticModel semanticModel,
        LambdaExpressionSyntax whereLambda,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out ITypeSymbol? whereType)
    {
        whereType = null;

        // has to look like `a => a is ...` or `(T a) => a is ...`
        if (!TryGetSingleLambdaParameter(whereLambda, out var parameter))
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

    private bool AnalyzeInvocationExpression(
        InvocationExpressionSyntax invocationExpression,
        [NotNullWhen(true)] out LambdaExpressionSyntax? whereLambda,
        [NotNullWhen(true)] out InvocationExpressionSyntax? whereInvocation,
        [NotNullWhen(true)] out SimpleNameSyntax? caseOrSelectName,
        [NotNullWhen(true)] out TypeSyntax? caseOrSelectType)
    {
        whereLambda = null;
        whereInvocation = null;
        caseOrSelectName = null;
        caseOrSelectType = null;

        // Both forms need to be accessed off of `.Where(... => ...)`
        // Needs to look like `.Where(...).Cast<...>()`
        if (invocationExpression is not
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Expression: InvocationExpressionSyntax
                    {
                        // Needs to be `.Where(... => ...)`
                        ArgumentList.Arguments: [{ Expression: LambdaExpressionSyntax whereLambda1 }],
                        Expression: MemberAccessExpressionSyntax
                        {
                            Name: IdentifierNameSyntax { Identifier.ValueText: nameof(Enumerable.Where) },
                        },
                    } whereInvocation1,
                },
            })
        {
            return false;
        }

        whereLambda = whereLambda1;
        whereInvocation = whereInvocation1;

        if (invocationExpression is
            {
                // Needs to be `.Cast<T>()`
                ArgumentList.Arguments: [],
                Expression: MemberAccessExpressionSyntax
                {
                    Name: GenericNameSyntax
                    {
                        Identifier.ValueText: nameof(Enumerable.Cast),
                        TypeArgumentList.Arguments: [var castTypeArgument]
                    } castName,
                },
            })
        {
            caseOrSelectName = castName;
            caseOrSelectType = castTypeArgument;
            return true;
        }

        // Needs to be `.Select(a => (T)a)`
        if (invocationExpression is
            {
                ArgumentList.Arguments: [
                    {
                        // a => (T)a
                        Expression: LambdaExpressionSyntax
                        {
                            ExpressionBody: CastExpressionSyntax
                            {
                                Type: var lambdaCastType,
                                Expression: IdentifierNameSyntax castIdentifier,
                            } lambdaCast,
                        } selectLambda
                    }],
                Expression: MemberAccessExpressionSyntax
                {
                    Name: IdentifierNameSyntax
                    {
                        Identifier.ValueText: nameof(Enumerable.Select),
                    } selectName,
                },
            } && TryGetSingleLambdaParameter(selectLambda, out var selectLambdaParameter) &&
            selectLambdaParameter.Identifier.ValueText == castIdentifier.Identifier.ValueText)
        {
            caseOrSelectName = selectName;
            caseOrSelectType = lambdaCastType;
            return true;
        }

        return false;
    }

    private void AnalyzeInvocationExpression(
        SyntaxNodeAnalysisContext context, INamedTypeSymbol enumerableType)
    {
        var cancellationToken = context.CancellationToken;
        var semanticModel = context.SemanticModel;

        if (ShouldSkipAnalysis(context, notification: null))
            return;

        var invocationExpression = (InvocationExpressionSyntax)context.Node;

        if (!AnalyzeInvocationExpression(invocationExpression,
                out var whereLambda,
                out var whereInvocation,
                out var castOrSelectName,
                out var castTypeArgument))
        {
            return;
        }

        if (!AnalyzeWhereMethod(semanticModel, whereLambda, cancellationToken, out var whereType))
            return;

        // Ensure the `is SomeType` and `Cast<SomeType>` are the same type.
        var castType = semanticModel.GetTypeInfo(castTypeArgument, cancellationToken).Type;
        if (castType is null)
            return;

        if (!whereType.Equals(castType))
            return;

        var castOrSelectSymbol = semanticModel.GetSymbolInfo(invocationExpression, cancellationToken).Symbol;
        var whereSymbol = semanticModel.GetSymbolInfo(whereInvocation, cancellationToken).Symbol;

        if (!enumerableType.Equals(castOrSelectSymbol?.OriginalDefinition.ContainingType) ||
            !enumerableType.Equals(whereSymbol?.OriginalDefinition.ContainingType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptor,
            castOrSelectName.Identifier.GetLocation(),
            additionalLocations: [invocationExpression.GetLocation(), castTypeArgument.GetLocation()]));
    }
}
