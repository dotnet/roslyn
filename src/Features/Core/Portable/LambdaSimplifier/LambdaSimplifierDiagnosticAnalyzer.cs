using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.LambdaSimplifier
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal class LambdaSimplifierDiagnosticAnalyzer : DiagnosticAnalyzer, IBuiltInAnalyzer
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(FeaturesResources.Simplify_lambda_expression), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(FeaturesResources.Simplify_lambda_expression), WorkspacesResources.ResourceManager, typeof(FeaturesResources));

        private static readonly DiagnosticDescriptor s_descriptor = new DiagnosticDescriptor(
            IDEDiagnosticIds.LambdaSimplifierDiagnosticId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.Style,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(s_descriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterOperationAction(AnalyzeOperation, OperationKind.LambdaExpression);
        }

        private void AnalyzeOperation(OperationAnalysisContext context)
        {
            var lambdaExpression = (ILambdaExpression)context.Operation;
            var invocationExpression = TryGetInvocationExpression(lambdaExpression);
            if (invocationExpression == null || 
                invocationExpression.TargetMethod == null || 
                invocationExpression.IsInvalid)
            {
                return;
            }

            var lambdaParameters = lambdaExpression.Signature.Parameters;
            var invocationArguments = invocationExpression.ArgumentsInParameterOrder;

            if (lambdaParameters.Length != invocationArguments.Length)
            {
                return;
            }

            for (int i = 0, n = lambdaParameters.Length; i < n; i++)
            {
                var lambdaParameter = lambdaParameters[i];
                var argument = invocationArguments[i];

                if (lambdaParameter == null || argument == null)
                {
                    return;
                }

                var argumentValue = UnwrapImplicitConversion(argument.Value) as IParameterReferenceExpression;
                if (!lambdaParameter.Equals(argumentValue?.Parameter))
                {
                    return;
                }

                if (argument.Parameter.RefKind != RefKind.None)
                {
                    // Can't simplify it if one of the parameters is passed to a ref/out param.
                    return;
                }
            }

            var diagnostic = Diagnostic.Create(s_descriptor, lambdaExpression.Syntax.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

        private static IOperation UnwrapImplicitConversion(IOperation value)
        {
            var conversion = value as IConversionExpression;
            return conversion == null
                ? value
                : conversion.IsExplicit
                    ? null
                    : conversion.Operand;
        }

        public static IInvocationExpression TryGetInvocationExpression(ILambdaExpression lambdaExpression)
        {
            var body = lambdaExpression?.Body;
            if (body?.Statements.Length != 1)
            {
                return null;
            }

            var firstStatement = body.Statements[0];
            return lambdaExpression.Signature.ReturnsVoid
                ? TryGetInvocationExpressionForSubLambda(firstStatement)
                : TryGetInvocationExpressionForFuncLambda(firstStatement);
        }

        private static IInvocationExpression TryGetInvocationExpressionForSubLambda(
            IOperation operation)
        {
            if (operation?.Kind == OperationKind.InvocationExpression)
            {
                return (IInvocationExpression)operation;
            }

            if (operation?.Kind == OperationKind.ConversionExpression)
            {
                return TryGetInvocationExpressionForSubLambda(UnwrapImplicitConversion(operation));
            }

            if (operation?.Kind == OperationKind.ExpressionStatement)
            {
                return TryGetInvocationExpressionForFuncLambda(((IExpressionStatement)operation).Expression);
            }

            return null;

        }

        private static IInvocationExpression TryGetInvocationExpressionForFuncLambda(
            IOperation operation)
        {
            if (operation?.Kind == OperationKind.InvocationExpression)
            {
                return (IInvocationExpression)operation;
            }

            if (operation?.Kind == OperationKind.ConversionExpression)
            {
                return TryGetInvocationExpressionForFuncLambda(UnwrapImplicitConversion(operation));
            }

            if (operation?.Kind == OperationKind.ReturnStatement)
            {
                return TryGetInvocationExpressionForFuncLambda(((IReturnStatement)operation).ReturnedValue);
            }

            return null;
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
    }
}
