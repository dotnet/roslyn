// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
    internal sealed class CSharpRemoveUnnecessaryLambdaExpressionDiagnosticAnalyzer : AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer
    {
        public CSharpRemoveUnnecessaryLambdaExpressionDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.RemoveUnnecessaryLambdaExpressionDiagnosticId,
                   EnforceOnBuildValues.RemoveUnnecessaryLambdaExpression,
                   CSharpCodeStyleOptions.PreferMethodGroupConversion,
                   fadingOption: null,
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.Remove_unnecessary_lambda_expression), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.Lambda_expression_can_be_removed), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(context =>
            {
                if (context.Compilation.LanguageVersion().IsCSharp11OrAbove())
                {
                    var expressionType = context.Compilation.ExpressionOfTType();
                    var conditionalAttributeType = context.Compilation.ConditionalAttribute();

                    context.RegisterSyntaxNodeAction(
                        c => AnalyzeSyntax(c, expressionType, conditionalAttributeType),
                        SyntaxKind.SimpleLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.AnonymousMethodExpression);
                }
            });
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context, INamedTypeSymbol? expressionType, INamedTypeSymbol? conditionalAttributeType)
        {
            var cancellationToken = context.CancellationToken;
            var semanticModel = context.SemanticModel;
            var syntaxTree = semanticModel.SyntaxTree;

            var preference = context.GetCSharpAnalyzerOptions().PreferMethodGroupConversion;
            if (ShouldSkipAnalysis(context, preference.Notification))
            {
                // User doesn't care about this rule.
                return;
            }

            var anonymousFunction = (AnonymousFunctionExpressionSyntax)context.Node;

            // Syntax checks first.

            // Don't simplify static lambdas.  The user made them explicitly static to make it clear it must only cause
            // a single allocation for the cached delegate. If we get rid of the lambda (and thus the static-keyword) it
            // won't be clear anymore if the member-group-conversion allocation is cached or not.
            if (anonymousFunction.Modifiers.Any(SyntaxKind.StaticKeyword))
                return;

            if (!TryGetAnonymousFunctionInvocation(anonymousFunction, out var invocation, out var wasAwaited))
                return;

            // If we had an async function, but we didn't await the expression inside then we can't convert this. The
            // underlying value was wrapped into a task, and that won't work if directly referencing the function.
            if (wasAwaited != anonymousFunction.Modifiers.Any(SyntaxKind.AsyncKeyword))
                return;

            // We have to have an invocation in the lambda like `() => X()` or `() => expr.X()`.
            var invokedExpression = invocation.Expression;
            if (invokedExpression is not SimpleNameSyntax and not MemberAccessExpressionSyntax)
                return;

            // lambda and invocation have to agree on number of parameters.
            var parameters = GetParameters(anonymousFunction);
            if (parameters.Count != invocation.ArgumentList.Arguments.Count)
                return;

            // parameters must be passed 1:1 from lambda to invocation.
            for (int i = 0, n = parameters.Count; i < n; i++)
            {
                var parameter = parameters[i];
                var argument = invocation.ArgumentList.Arguments[i];

                if (argument.Expression is not IdentifierNameSyntax argumentIdentifier)
                    return;

                if (parameter.Identifier.ValueText != argumentIdentifier.Identifier.ValueText)
                    return;
            }

            // if we have `() => new C().X()` then converting to `new C().X` very much changes the meaning.
            if (MayHaveSideEffects(invokedExpression))
                return;

            // Looks like a reasonable candidate to simplify.  Now switch to semantics to check for sure.

            if (CSharpSemanticFacts.Instance.IsInExpressionTree(semanticModel, anonymousFunction, expressionType, cancellationToken))
                return;

            // If we have `object obj = x => Goo(x);` we don't want to simplify.  The compiler warns if you write
            // `object obj = Goo;` because of the conversion to a non-delegate type. While we could insert a cast here
            // to make this work, that goes against the spirit of this analyzer/fixer just removing code.
            var lambdaTypeInfo = semanticModel.GetTypeInfo(anonymousFunction, cancellationToken);
            if (lambdaTypeInfo.ConvertedType == null || lambdaTypeInfo.ConvertedType.SpecialType is SpecialType.System_Object)
                return;

            var lambdaSymbolInfo = semanticModel.GetSymbolInfo(anonymousFunction, cancellationToken);
            if (lambdaSymbolInfo.Symbol is not IMethodSymbol lambdaMethod)
                return;

            var invokedSymbolInfo = semanticModel.GetSymbolInfo(invokedExpression, cancellationToken);
            if (invokedSymbolInfo.Symbol is not IMethodSymbol invokedMethod)
                return;

            // cannot convert a partial-definition to a delegate (unless there's an existing implementation part that can be used).
            if (invokedMethod.IsPartialDefinition && invokedMethod.PartialImplementationPart is null)
                return;

            // If we're calling a generic method, we have to have supplied type arguments.  They cannot be inferred once
            // we remove the arguments during simplification.
            var invokedTypeArguments = invokedExpression.GetRightmostName() is GenericNameSyntax genericName
                ? genericName.TypeArgumentList.Arguments
                : default;

            if (invokedMethod.TypeArguments.Length != invokedTypeArguments.Count)
                return;

            // Methods have to be complimentary.  That means the same number of parameters, with proper
            // co-contravariance for the parameters and return type.
            if (lambdaMethod.Parameters.Length != invokedMethod.Parameters.Length)
                return;

            var compilation = semanticModel.Compilation;

            // Must be able to convert the invoked method return type to the lambda's return type.
            if (!IsIdentityOrImplicitConversion(compilation, invokedMethod.ReturnType, lambdaMethod.ReturnType))
                return;

            for (int i = 0, n = lambdaMethod.Parameters.Length; i < n; i++)
            {
                var lambdaParameter = lambdaMethod.Parameters[i];
                var invokedParameter = invokedMethod.Parameters[i];

                if (lambdaParameter.RefKind != invokedParameter.RefKind)
                    return;

                // All the lambda parameters must be convertible to the invoked method parameters.
                if (!IsIdentityOrImplicitConversion(compilation, lambdaParameter.Type, invokedParameter.Type))
                    return;
            }

            // If invoked method is conditional, converting lambda to method group produces compiler error
            if (invokedMethod.GetAttributes().Any(a => Equals(a.AttributeClass, conditionalAttributeType)))
                return;

            // In the case where we have `() => expr.m()`, check if `expr` is overwritten anywhere. If so then we do not
            // want to remove the lambda, as that will bind eagerly to the original `expr` and will not see the write
            // that later happens
            if (invokedExpression is MemberAccessExpressionSyntax { Expression: var accessedExpression })
            {
                // Limit the search space to the outermost code block that could contain references to this expr (or
                // fall back to compilation unit for top level statements).
                var outermostBody = invokedExpression.AncestorsAndSelf().LastOrDefault(
                    n => n is BlockSyntax or ArrowExpressionClauseSyntax or AnonymousFunctionExpressionSyntax or GlobalStatementSyntax);
                if (outermostBody is null or GlobalStatementSyntax)
                    outermostBody = syntaxTree.GetRoot(cancellationToken);

                foreach (var candidate in outermostBody.DescendantNodes().OfType<ExpressionSyntax>())
                {
                    if (candidate != accessedExpression &&
                        SemanticEquivalence.AreEquivalent(semanticModel, candidate, accessedExpression) &&
                        candidate.IsWrittenTo(semanticModel, cancellationToken))
                    {
                        return;
                    }
                }
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

            if (OverloadsChanged(
                    semanticModel, anonymousFunction.GetRequiredParent(),
                    rewrittenSemanticModel, rewrittenExpression.GetRequiredParent(), cancellationToken))
            {
                return;
            }

            var startReportSpan = TextSpan.FromBounds(anonymousFunction.SpanStart, invokedExpression.SpanStart);
            var endReportSpan = TextSpan.FromBounds(invokedExpression.Span.End, anonymousFunction.Span.End);

            context.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
                Descriptor,
                syntaxTree.GetLocation(startReportSpan),
                preference.Notification,
                additionalLocations: ImmutableArray.Create(anonymousFunction.GetLocation()),
                additionalUnnecessaryLocations: ImmutableArray.Create(
                    syntaxTree.GetLocation(startReportSpan),
                    syntaxTree.GetLocation(endReportSpan))));
        }

        private static bool OverloadsChanged(
            SemanticModel semanticModel1,
            SyntaxNode? node1,
            SemanticModel semanticModel2,
            SyntaxNode? node2,
            CancellationToken cancellationToken)
        {
            while (node1 != null && node2 != null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var method1 = semanticModel1.GetSymbolInfo(node1, cancellationToken).Symbol as IMethodSymbol;
                var method2 = semanticModel2.GetSymbolInfo(node2, cancellationToken).Symbol as IMethodSymbol;

                if (method1 is null != method2 is null)
                    return true;

                if (method1 is not null && !method1.Equals(method2, SymbolEqualityComparer.IncludeNullability))
                    return true;

                node1 = node1.Parent;
                node2 = node2.Parent;
            }

            return false;
        }

        private static bool IsIdentityOrImplicitConversion(Compilation compilation, ITypeSymbol type1, ITypeSymbol type2)
        {
            // Dynamic can have an identity conversion between types.  But it can have a very different effect on the
            // generated code.  Do not allow the change if these are not in agreement.
            if (type1 is IDynamicTypeSymbol != type2 is IDynamicTypeSymbol)
                return false;

            var conversion = compilation.ClassifyConversion(type1, type2);
            return conversion.IsIdentityOrImplicitReference();
        }

        private static bool MayHaveSideEffects(ExpressionSyntax expression)
        {
            // Checks to see if the expression being invoked looks side-effect free.  If so, changing from executing
            // each time in the lambda to only executing it once could have impact on the program.

            return !expression.DescendantNodesAndSelf().All(
                n => n is TypeSyntax or
                          TypeArgumentListSyntax or
                          MemberAccessExpressionSyntax or
                          InstanceExpressionSyntax or
                          LiteralExpressionSyntax);
        }

        private static SeparatedSyntaxList<ParameterSyntax> GetParameters(AnonymousFunctionExpressionSyntax expression)
            => expression switch
            {
                AnonymousMethodExpressionSyntax anonymousMethod => anonymousMethod.ParameterList?.Parameters ?? default,
                SimpleLambdaExpressionSyntax simpleLambda => SyntaxFactory.SingletonSeparatedList(simpleLambda.Parameter),
                ParenthesizedLambdaExpressionSyntax parenthesizedLambda => parenthesizedLambda.ParameterList.Parameters,
                _ => throw ExceptionUtilities.UnexpectedValue(expression.Kind()),
            };

        public static bool TryGetAnonymousFunctionInvocation(
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

        private static bool TryGetInvocation(
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
