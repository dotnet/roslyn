// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryLambdaExpression
{
    /// <summary>
    /// DiagnosticAnalyzer that looks code like <c>Goo(() => Bar())</c> and offers to convert it to <c>Goo(Bar)</c>.
    /// This is only offered on C# 11 and above where this delegate can be cached and will not cause allocations each
    /// time.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpRemoveUnnecessaryLambdaExpressionDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public CSharpRemoveUnnecessaryLambdaExpressionDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.RemoveUnnecessaryLambdaExpressionDiagnosticId,
                   EnforceOnBuildValues.RemoveUnnecessaryLambdaExpression,
                   option: null,
                   LanguageNames.CSharp,
                   new LocalizableResourceString(nameof(CSharpFeaturesResources.Remove_unnecessary_lambda_expression), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources)),
                   new LocalizableResourceString(nameof(CSharpFeaturesResources.Lambda_expression_can_be_removed), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources)),
                   isUnnecessary: true)
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(context =>
            {
                //if (((CSharpCompilation)context.Compilation).LanguageVersion >= LanguageVersion.Preview)
                //{
                context.RegisterSyntaxNodeAction(
                    AnalyzeSyntax,
                    SyntaxKind.SimpleLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.AnonymousMethodExpression);
                // }
            });
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;
            var semanticModel = context.SemanticModel;
            var syntaxTree = semanticModel.SyntaxTree;

            var anonymousFunction = (AnonymousFunctionExpressionSyntax)context.Node;

            // Syntax checks first.

            // Don't simplify static lambdas.  The user made them explicitly static to make it clear it doesn't capture
            // anything.  If we get rid of the lambda it won't be clear anymore if capturing/allocs are happening or not.
            if (anonymousFunction.Modifiers.Any(SyntaxKind.StaticKeyword))
                return;

            if (!TryGetInvocation(anonymousFunction, out var invocation, out var wasAwaited))
                return;

            // If we had an async function, but we didn't await the expression inside then we can't convert this. The
            // underlying value was wrapped into a task, and that won't work if directly referencing the function.
            if (wasAwaited != anonymousFunction.Modifiers.Any(SyntaxKind.AsyncKeyword))
                return;

            // We have to have an invocation in the lambda like `() => X()` or `() => expr.X()`.
            var invokedExpression = invocation.Expression;
            if (invokedExpression is not IdentifierNameSyntax and not MemberAccessExpressionSyntax)
                return;

            // lambda and invocation have to agree on number of parameters.
            var parameters = GetParameters(anonymousFunction);
            if (parameters.Count != invocation.ArgumentList.Arguments.Count)
                return;

            // parameters must be passed 1:1 from lambda to invocation.
            for (int i = 0, n = parameters.Count - 1; i < n; i++)
            {
                var parameter = parameters[i];
                var argument = invocation.ArgumentList.Arguments[i];

                if (argument.Expression is not IdentifierNameSyntax argumentIdentifier)
                    return;

                if (parameter.Identifier.ValueText != argumentIdentifier.Identifier.ValueText)
                    return;
            }

            // Looks like a reasonable candidate to simplify.  Now switch to semantics to check for sure.

            // If we have `object obj = x => Goo(x);` we don't want to simplify.  The compiler warns if you write
            // `object obj = Goo;` because of the conversion to a non-delegate type.
            var lambdaTypeInfo = semanticModel.GetTypeInfo(anonymousFunction, cancellationToken);
            if (lambdaTypeInfo.ConvertedType == null || lambdaTypeInfo.ConvertedType.SpecialType is SpecialType.System_Object)
                return;

            var lambdaSymbolInfo = semanticModel.GetSymbolInfo(anonymousFunction, cancellationToken);
            if (lambdaSymbolInfo.Symbol is not IMethodSymbol lambdaMethod)
                return;

            var invokedSymbolInfo = semanticModel.GetSymbolInfo(invokedExpression, cancellationToken);
            if (invokedSymbolInfo.Symbol is not IMethodSymbol invokedMethod)
                return;

            // Methods have to be complimentary.  That means the same number of parameters, with proper
            // co-contravariance for the parameters and return type.
            if (lambdaMethod.Parameters.Length != invokedMethod.Parameters.Length)
                return;

            var compilation = semanticModel.Compilation;

            // Must be able to convert the invoked method return type to the lambda's return type.
            var returnTypeConversion = compilation.ClassifyConversion(invokedMethod.ReturnType, lambdaMethod.ReturnType);
            if (!returnTypeConversion.IsIdentityOrImplicitReference())
                return;

            for (int i = 0, n = lambdaMethod.Parameters.Length; i < n; i++)
            {
                var lambdaParameter = lambdaMethod.Parameters[i];
                var invokedParameter = invokedMethod.Parameters[i];

                // All the lambda parameters must be convertible to the invoked method parameters.
                var parameterConversion = compilation.ClassifyConversion(lambdaParameter.Type, invokedParameter.Type);
                if (!parameterConversion.IsIdentityOrImplicitReference())
                    return;
            }

            // Semantically, this looks good to go.  Now, do an actual speculative replacement to ensure that the
            // non-invoked method reference refers to the same method symbol, and that it converts to the same type that
            // the lambda was.
            var analyzer = new SpeculationAnalyzer(anonymousFunction, invokedExpression, semanticModel, cancellationToken);

            var rewrittenExpression = analyzer.ReplacedExpression;
            var rewrittenSemanticModel = analyzer.SpeculativeSemanticModel;

            var rewrittenSymbolInfo = rewrittenSemanticModel.GetSymbolInfo(rewrittenExpression, cancellationToken);
            if (rewrittenSymbolInfo.Symbol is not IMethodSymbol rewrittenMethod ||
                !invokedMethod.Equals(rewrittenMethod))
            {
                return;
            }

            var rewrittenConvertedType = rewrittenSemanticModel.GetTypeInfo(rewrittenExpression, cancellationToken).ConvertedType;
            if (!lambdaTypeInfo.ConvertedType.Equals(rewrittenConvertedType))
                return;

            var startReportSpan = TextSpan.FromBounds(anonymousFunction.SpanStart, invokedExpression.SpanStart);
            var endReportSpan = TextSpan.FromBounds(invokedExpression.Span.End, anonymousFunction.Span.End);

            context.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
                Descriptor,
                syntaxTree.GetLocation(startReportSpan),
                ReportDiagnostic.Default,
                additionalLocations: ImmutableArray.Create(anonymousFunction.GetLocation()),
                additionalUnnecessaryLocations: ImmutableArray.Create(syntaxTree.GetLocation(endReportSpan))));
        }

        private static SeparatedSyntaxList<ParameterSyntax> GetParameters(AnonymousFunctionExpressionSyntax expression)
            => expression switch
            {
                AnonymousMethodExpressionSyntax anonymousMethod => anonymousMethod.ParameterList?.Parameters ?? default,
                SimpleLambdaExpressionSyntax simpleLambda => SyntaxFactory.SingletonSeparatedList(simpleLambda.Parameter),
                ParenthesizedLambdaExpressionSyntax parenthesizedLambda => parenthesizedLambda.ParameterList.Parameters,
                _ => throw ExceptionUtilities.UnexpectedValue(expression.Kind()),
            };

        private bool TryGetAnonymousFunctionInvocation(
            AnonymousFunctionExpressionSyntax anonymousFunction,
            [NotNullWhen(true)] out InvocationExpressionSyntax? invocation,
            out bool wasAwaited)
        {
            if (anonymousFunction.ExpressionBody != null)
                return TryGetInvocation(anonymousFunction.ExpressionBody, out invocation, out wasAwaited);

            if (anonymousFunction.Block != null && anonymousFunction.Block.Statements.Count == 1)
            {
                var statement = anonymousFunction.Block.Statements[0];
                if (statement is ReturnStatementSyntax { Expression: { } expression })
                    return TryGetInvocation(expression, out invocation, out wasAwaited);

                if (statement is ExpressionStatementSyntax expressionStatement)
                    return TryGetInvocation(expressionStatement.Expression, out invocation, out wasAwaited);
            }

            invocation = null;
            wasAwaited = false;
            return false;
        }

        public static bool TryGetInvocation(
            ExpressionSyntax expression,
            [NotNullWhen(true)] out InvocationExpressionSyntax? invocation,
            out bool wasAwaited)
        {
            wasAwaited = false;

            // if we have either `await Goo()` or `await Goo().ConfigureAwait` then unwrap to get at `Goo()`.
            if (expression is AwaitExpressionSyntax awaitExpression)
            {
                wasAwaited = true;
                expression = awaitExpression.Expression;
                if (expression is InvocationExpressionSyntax
                    {
                        Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: nameof(Task.ConfigureAwait), Expression: var underlying }
                    })
                {
                    expression = underlying;
                }
            }

            invocation = expression as InvocationExpressionSyntax;
            return invocation != null;
        }
    }
}
