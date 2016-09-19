// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Semantics;
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
    /// Note: this analyzer can be udpated to run on VB once VB supports 'throw' 
    /// expressions as well.
    /// </summary>
    internal abstract class AbstractUseThrowExpressionDiagnosticAnalyzer : DiagnosticAnalyzer, IBuiltInAnalyzer
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(FeaturesResources.Use_throw_expression), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(FeaturesResources.Use_throw_expression), FeaturesResources.ResourceManager, typeof(FeaturesResources));

        private static DiagnosticDescriptor s_descriptor = 
            CreateDescriptor(DiagnosticSeverity.Hidden);

        private static DiagnosticDescriptor s_unnecessaryCodeDescriptor =
            CreateDescriptor(DiagnosticSeverity.Hidden, customTags: DiagnosticCustomTags.Unnecessary);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(s_descriptor, s_unnecessaryCodeDescriptor);

        public DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

        public bool OpenFileOnly(Workspace workspace) => false;

        private static MethodInfo s_registerOperationActionInfo =
            typeof(AnalysisContext).GetTypeInfo().GetDeclaredMethod("RegisterOperationActionImmutableArrayInternal");

        private static MethodInfo s_getOperationInfo =
            typeof(SemanticModel).GetTypeInfo().GetDeclaredMethod("GetOperationInternal");

        protected abstract bool IsSupported(ParseOptions options);

        private static DiagnosticDescriptor CreateDescriptor(DiagnosticSeverity severity, params string[] customTags)
            => new DiagnosticDescriptor(
                    IDEDiagnosticIds.UseThrowExpressionDiagnosticId,
                    s_localizableTitle,
                    s_localizableMessage,
                    DiagnosticCategory.Style,
                    DiagnosticSeverity.Hidden,
                    isEnabledByDefault: true,
                    customTags: customTags);

        public override void Initialize(AnalysisContext context)
        {
            s_registerOperationActionInfo.Invoke(context, new object[]
            {
                new Action<OperationAnalysisContext>(AnalyzeOperation),
                ImmutableArray.Create(OperationKind.ThrowStatement)
            });
        }

        private void AnalyzeOperation(OperationAnalysisContext context)
        {
            var syntaxTree = context.Operation.Syntax.SyntaxTree;
            if (!IsSupported(syntaxTree.Options))
            {
                return;
            }

            var cancellationToken = context.CancellationToken;

            var throwOperation = (IThrowStatement)context.Operation;
            var throwStatement = throwOperation.Syntax;

            var optionSet = context.Options.GetOptionSet();
            var option = optionSet.GetOption(CodeStyleOptions.PreferThrowExpression, throwStatement.Language);
            if (!option.Value)
            {
                return;
            }

            var compilation = context.Compilation;
            var semanticModel = compilation.GetSemanticModel(throwStatement.SyntaxTree);

            var ifOperation = GetContainingIfOperation(
                semanticModel, throwOperation, cancellationToken);

            // This throw statement isn't parented by an if-statement.  Nothing to
            // do here.
            if (ifOperation == null)
            {
                return;
            }

            var containingBlock = GetOperation(
                semanticModel, ifOperation.Syntax.Parent, cancellationToken) as IBlockStatement;
            if (containingBlock == null)
            {
                return;
            }

            ISymbol localOrParameter;
            if (!TryDecomposeIfCondition(ifOperation, out localOrParameter))
            {
                return;
            }

            IExpressionStatement expressionStatement;
            IAssignmentExpression assignmentExpression;
            if (!TryFindAssignmentExpression(containingBlock, ifOperation, localOrParameter,
                out expressionStatement, out assignmentExpression))
            {
                return;
            }

            // We found an assignment using this local/parameter.  Now, just make sure there
            // were no intervening writes between the check and the assignement.
            var dataFlow = semanticModel.AnalyzeDataFlow(
                ifOperation.Syntax, expressionStatement.Syntax);

            if (dataFlow.WrittenInside.Contains(localOrParameter))
            {
                return;
            }

            // Ok, there were no intervening writes.  This check+assignment can be simplified.

            var allLocations = ImmutableArray.Create(
                ifOperation.Syntax.GetLocation(),
                throwOperation.ThrownObject.Syntax.GetLocation(),
                assignmentExpression.Value.Syntax.GetLocation());

            var descriptor = CreateDescriptor(option.Notification.Value);

            context.ReportDiagnostic(
                Diagnostic.Create(descriptor, throwStatement.GetLocation(), additionalLocations: allLocations));

            // Fade out the rest of the if that surrounds the 'throw' exception.

            var tokenBeforeThrow = throwStatement.GetFirstToken().GetPreviousToken();
            var tokenAfterThrow = throwStatement.GetLastToken().GetNextToken();
            context.ReportDiagnostic(
                Diagnostic.Create(s_unnecessaryCodeDescriptor,
                    Location.Create(syntaxTree, TextSpan.FromBounds(
                        ifOperation.Syntax.SpanStart,
                        tokenBeforeThrow.Span.End)),
                    additionalLocations: allLocations));

            if (ifOperation.Syntax.Span.End > tokenAfterThrow.Span.Start)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(s_unnecessaryCodeDescriptor,
                        Location.Create(syntaxTree, TextSpan.FromBounds(
                            tokenAfterThrow.Span.Start,
                            ifOperation.Syntax.Span.End)),
                        additionalLocations: allLocations));
            }
        }

        private bool TryFindAssignmentExpression(
            IBlockStatement containingBlock, IIfStatement ifOperation, ISymbol localOrParameter,
            out IExpressionStatement expressionStatement, out IAssignmentExpression assignmentExpression)
        {
            var ifOperationIndex = containingBlock.Statements.IndexOf(ifOperation);

            // walk forward until we find an assignment of this local/parameter into
            // something else.
            for (var i = ifOperationIndex + 1; i < containingBlock.Statements.Length; i++)
            {
                expressionStatement = containingBlock.Statements[i] as IExpressionStatement;
                if (expressionStatement == null)
                {
                    continue;
                }

                assignmentExpression = expressionStatement.Expression as IAssignmentExpression;
                if (assignmentExpression == null)
                {
                    continue;
                }

                ISymbol assignmentValue;
                if (!TryGetLocalOrParameterSymbol(assignmentExpression.Value, out assignmentValue))
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
            IIfStatement ifStatement,
            out ISymbol localOrParameter)
        {
            localOrParameter = null;

            var condition = ifStatement.Condition;
            var binaryOperator = condition as IBinaryOperatorExpression;
            if (binaryOperator == null)
            {
                return false;
            }

            if (binaryOperator.GetSimpleBinaryOperationKind() != SimpleBinaryOperationKind.Equals)
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
            if (operation.Kind == OperationKind.ConversionExpression)
            {
                var conversionExpression = (IConversionExpression)operation;
                return TryGetLocalOrParameterSymbol(
                    conversionExpression.Operand, out localOrParameter);
            }

            if (operation.Kind == OperationKind.LocalReferenceExpression)
            {
                localOrParameter = ((ILocalReferenceExpression)operation).Local;
                return true;
            }
            else if (operation.Kind == OperationKind.ParameterReferenceExpression)
            {
                localOrParameter = ((IParameterReferenceExpression)operation).Parameter;
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

        private IIfStatement GetContainingIfOperation(
            SemanticModel semanticModel, IThrowStatement throwOperation,
            CancellationToken cancellationToken)
        {
            var throwStatement = throwOperation.Syntax;
            var containingOperation = GetOperation(
                semanticModel, throwStatement.Parent, cancellationToken);

            if (containingOperation?.Kind == OperationKind.BlockStatement)
            {
                // C# may have an intermediary block between the throw-statement
                // and the if-statement.  Walk up one operation higher in htat case.
                containingOperation = GetOperation(
                    semanticModel, throwStatement.Parent.Parent, cancellationToken);
            }

            return containingOperation as IIfStatement;
        }

        private static IOperation GetOperation(
            SemanticModel semanticModel,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            return (IOperation)s_getOperationInfo.Invoke(
                semanticModel, new object[] { node, cancellationToken });
        }
    }
}