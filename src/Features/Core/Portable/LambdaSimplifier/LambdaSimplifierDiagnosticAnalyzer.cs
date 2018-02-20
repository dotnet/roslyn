using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
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
            context.RegisterOperationAction(AnalyzeOperation, OperationKind.AnonymousFunction);
        }

        private void AnalyzeOperation(OperationAnalysisContext context)
        {
            var lambdaExpression = (IAnonymousFunctionOperation)context.Operation;
            var invocationExpression = TryGetInvocationExpression(lambdaExpression);
            if (invocationExpression == null || 
                invocationExpression.TargetMethod == null || 
                invocationExpression.Kind == OperationKind.None)
            {
                return;
            }

            var lambdaParameters = lambdaExpression.Symbol.Parameters;
            var invocationArguments = invocationExpression.Arguments;

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

                var argumentValue = UnwrapImplicitConversion(argument.Value) as IParameterReferenceOperation;
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
            var conversion = value as IConversionOperation;
            return conversion == null
                ? value
                : !conversion.IsImplicit
                    ? null
                    : conversion.Operand;
        }

        public static IInvocationOperation TryGetInvocationExpression(IAnonymousFunctionOperation lambdaExpression)
        {
            var body = lambdaExpression?.Body;
            if (body == null)
            {
                return null;
            }

            var nonImplicitOps = body.Operations.Where(o => !IsImplicitEmptyReturn(o)).ToImmutableArray();
            if (nonImplicitOps.Length != 1)
            {
                return null;
            }

            var firstStatement = nonImplicitOps[0];
            return lambdaExpression.Symbol.ReturnsVoid
                ? TryGetInvocationExpressionForSubLambda(firstStatement)
                : TryGetInvocationExpressionForFuncLambda(firstStatement);
        }

        private static bool IsImplicitEmptyReturn(IOperation operation)
            => operation.IsImplicit && operation is IReturnOperation returnOp && returnOp.ReturnedValue == null;

        private static IInvocationOperation TryGetInvocationExpressionForSubLambda(
            IOperation operation)
        {
            if (operation?.Kind == OperationKind.Invocation)
            {
                return (IInvocationOperation)operation;
            }

            if (operation?.Kind == OperationKind.Conversion)
            {
                return TryGetInvocationExpressionForSubLambda(UnwrapImplicitConversion(operation));
            }

            if (operation?.Kind == OperationKind.ExpressionStatement)
            {
                return TryGetInvocationExpressionForFuncLambda(((IExpressionStatementOperation)operation).Operation);
            }

            return null;

        }

        private static IInvocationOperation TryGetInvocationExpressionForFuncLambda(
            IOperation operation)
        {
            if (operation?.Kind == OperationKind.Invocation)
            {
                return (IInvocationOperation)operation;
            }

            if (operation?.Kind == OperationKind.Conversion)
            {
                return TryGetInvocationExpressionForFuncLambda(UnwrapImplicitConversion(operation));
            }

            if (operation?.Kind == OperationKind.Return)
            {
                return TryGetInvocationExpressionForFuncLambda(((IReturnOperation)operation).ReturnedValue);
            }

            return null;
        }

        public DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public bool OpenFileOnly(Workspace workspace)
            => false;
    }
}
