// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UseThrowExpression
{
    /// <summary>
    /// Looks for patterns of the form:
    /// <code>
    /// if (a == null) {
    ///   throw SomeException();
    /// }
    ///
    /// x = a;
    /// </code>
    ///
    /// and offers to change it to
    ///
    /// <code>
    /// x = a ?? throw SomeException();
    /// </code>
    ///
    /// Note: this analyzer can be updated to run on VB once VB supports 'throw'
    /// expressions as well.
    /// </summary>
    internal abstract class AbstractUseThrowExpressionDiagnosticAnalyzer :
        AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        protected AbstractUseThrowExpressionDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseThrowExpressionDiagnosticId,
                   CodeStyleOptions.PreferThrowExpression,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_throw_expression), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Null_check_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected abstract bool IsSupported(ParseOptions options);

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(startContext =>
            {
                var expressionTypeOpt = startContext.Compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression`1");
                startContext.RegisterOperationAction(operationContext => AnalyzeOperation(operationContext, expressionTypeOpt), OperationKind.Throw);
            });
        }

        private void AnalyzeOperation(OperationAnalysisContext context, INamedTypeSymbol expressionTypeOpt)
        {
            var syntaxTree = context.Operation.Syntax.SyntaxTree;
            if (!IsSupported(syntaxTree.Options))
            {
                return;
            }

            var cancellationToken = context.CancellationToken;

            var throwOperation = (IThrowOperation)context.Operation;
            var throwStatementSyntax = throwOperation.Syntax;

            var compilation = context.Compilation;
            var semanticModel = compilation.GetSemanticModel(throwStatementSyntax.SyntaxTree);

            var ifOperation = GetContainingIfOperation(
                semanticModel, throwOperation, cancellationToken);

            // This throw statement isn't parented by an if-statement.  Nothing to
            // do here.
            if (ifOperation == null)
            {
                return;
            }

            if (ifOperation.WhenFalse != null)
            {
                // Can't offer this if the 'if-statement' has an 'else-clause'.
                return;
            }

            var options = context.Options;
            var optionSet = options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var option = optionSet.GetOption(CodeStyleOptions.PreferThrowExpression, throwStatementSyntax.Language);
            if (!option.Value)
            {
                return;
            }

            var semanticFacts = GetSemanticFactsService();
            if (semanticFacts.IsInExpressionTree(semanticModel, throwStatementSyntax, expressionTypeOpt, cancellationToken))
            {
                return;
            }

            var containingBlock = semanticModel.GetOperation(ifOperation.Syntax.Parent, cancellationToken) as IBlockOperation;
            if (containingBlock == null)
            {
                return;
            }

            if (!TryDecomposeIfCondition(ifOperation, out var localOrParameter))
            {
                return;
            }

            if (!TryFindAssignmentExpression(containingBlock, ifOperation, localOrParameter,
                    out var expressionStatement, out var assignmentExpression))
            {
                return;
            }

            if (!localOrParameter.GetSymbolType().CanAddNullCheck())
            {
                return;
            }

            // We found an assignment using this local/parameter.  Now, just make sure there
            // were no intervening accesses between the check and the assignment.
            if (ValueIsAccessed(
                    semanticModel, ifOperation, containingBlock,
                    localOrParameter, expressionStatement, assignmentExpression))
            {
                return;
            }

            // Ok, there were no intervening writes or accesses.  This check+assignment can be simplified.
            var allLocations = ImmutableArray.Create(
                ifOperation.Syntax.GetLocation(),
                throwOperation.Exception.Syntax.GetLocation(),
                assignmentExpression.Value.Syntax.GetLocation());

            context.ReportDiagnostic(
                DiagnosticHelper.Create(Descriptor, throwStatementSyntax.GetLocation(), option.Notification.Severity, additionalLocations: allLocations, properties: null));

            // Fade out the rest of the if that surrounds the 'throw' exception.

            var tokenBeforeThrow = throwStatementSyntax.GetFirstToken().GetPreviousToken();
            var tokenAfterThrow = throwStatementSyntax.GetLastToken().GetNextToken();
            context.ReportDiagnostic(
                Diagnostic.Create(UnnecessaryWithSuggestionDescriptor,
                    Location.Create(syntaxTree, TextSpan.FromBounds(
                        ifOperation.Syntax.SpanStart,
                        tokenBeforeThrow.Span.End)),
                    additionalLocations: allLocations));

            if (ifOperation.Syntax.Span.End > tokenAfterThrow.Span.Start)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(UnnecessaryWithSuggestionDescriptor,
                        Location.Create(syntaxTree, TextSpan.FromBounds(
                            tokenAfterThrow.Span.Start,
                            ifOperation.Syntax.Span.End)),
                        additionalLocations: allLocations));
            }
        }

        private static bool ValueIsAccessed(SemanticModel semanticModel, IConditionalOperation ifOperation, IBlockOperation containingBlock, ISymbol localOrParameter, IExpressionStatementOperation expressionStatement, IAssignmentOperation assignmentExpression)
        {
            var statements = containingBlock.Operations;
            var ifOperationIndex = statements.IndexOf(ifOperation);
            var expressionStatementIndex = statements.IndexOf(expressionStatement);

            if (expressionStatementIndex > ifOperationIndex + 1)
            {
                // There are intermediary statements between the check and the assignment.
                // Make sure they don't try to access the local.
                var dataFlow = semanticModel.AnalyzeDataFlow(
                    statements[ifOperationIndex + 1].Syntax,
                    statements[expressionStatementIndex - 1].Syntax);

                if (dataFlow.ReadInside.Contains(localOrParameter) ||
                    dataFlow.WrittenInside.Contains(localOrParameter))
                {
                    return true;
                }
            }

            // Also, have to make sure there is no read/write of the local/parameter on the left
            // of the assignment.  For example: map[val.Id] = val;
            var exprDataFlow = semanticModel.AnalyzeDataFlow(assignmentExpression.Target.Syntax);
            return exprDataFlow.ReadInside.Contains(localOrParameter) ||
                   exprDataFlow.WrittenInside.Contains(localOrParameter);
        }

        protected abstract ISyntaxFactsService GetSyntaxFactsService();
        protected abstract ISemanticFactsService GetSemanticFactsService();

        private bool TryFindAssignmentExpression(
            IBlockOperation containingBlock, IConditionalOperation ifOperation, ISymbol localOrParameter,
            out IExpressionStatementOperation expressionStatement, out IAssignmentOperation assignmentExpression)
        {
            var ifOperationIndex = containingBlock.Operations.IndexOf(ifOperation);

            // walk forward until we find an assignment of this local/parameter into
            // something else.
            for (var i = ifOperationIndex + 1; i < containingBlock.Operations.Length; i++)
            {
                expressionStatement = containingBlock.Operations[i] as IExpressionStatementOperation;
                if (expressionStatement == null)
                {
                    continue;
                }

                assignmentExpression = expressionStatement.Operation as IAssignmentOperation;
                if (assignmentExpression == null)
                {
                    continue;
                }

                if (!TryGetLocalOrParameterSymbol(assignmentExpression.Value, out var assignmentValue))
                {
                    continue;
                }

                if (!Equals(localOrParameter, assignmentValue))
                {
                    continue;
                }

                return true;
            }

            expressionStatement = null;
            assignmentExpression = null;
            return false;
        }

        private bool TryDecomposeIfCondition(
            IConditionalOperation ifStatement,
            out ISymbol localOrParameter)
        {
            localOrParameter = null;

            var condition = ifStatement.Condition;
            if (!(condition is IBinaryOperation binaryOperator))
            {
                return false;
            }

            if (binaryOperator.OperatorKind != BinaryOperatorKind.Equals)
            {
                return false;
            }

            if (IsNull(binaryOperator.LeftOperand))
            {
                return TryGetLocalOrParameterSymbol(
                    binaryOperator.RightOperand, out localOrParameter);
            }

            if (IsNull(binaryOperator.RightOperand))
            {
                return TryGetLocalOrParameterSymbol(
                    binaryOperator.LeftOperand, out localOrParameter);
            }

            return false;
        }

        private bool TryGetLocalOrParameterSymbol(
            IOperation operation, out ISymbol localOrParameter)
        {
            if (operation is IConversionOperation conversion && conversion.IsImplicit)
            {
                return TryGetLocalOrParameterSymbol(conversion.Operand, out localOrParameter);
            }
            else if (operation is ILocalReferenceOperation localReference)
            {
                localOrParameter = localReference.Local;
                return true;
            }
            else if (operation is IParameterReferenceOperation parameterReference)
            {
                localOrParameter = parameterReference.Parameter;
                return true;
            }

            localOrParameter = null;
            return false;
        }

        private bool IsNull(IOperation operation)
        {
            return operation.ConstantValue.HasValue &&
                   operation.ConstantValue.Value == null;
        }

        private IConditionalOperation GetContainingIfOperation(
            SemanticModel semanticModel, IThrowOperation throwOperation,
            CancellationToken cancellationToken)
        {
            var throwStatement = throwOperation.Syntax;
            var containingOperation = semanticModel.GetOperation(throwStatement.Parent, cancellationToken);

            if (containingOperation is IBlockOperation block)
            {
                if (block.Operations.Length != 1)
                {
                    // If we are in a block, then the block must only contain
                    // the throw statement.
                    return null;
                }

                // C# may have an intermediary block between the throw-statement
                // and the if-statement.  Walk up one operation higher in that case.
                containingOperation = semanticModel.GetOperation(throwStatement.Parent.Parent, cancellationToken);
            }

            if (containingOperation is IConditionalOperation conditionalOperation)
            {
                return conditionalOperation;
            }

            return null;
        }
    }
}
