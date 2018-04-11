// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.ExtractMethod;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternMatching
{
    /// <summary>
    /// Looks for code of the forms:
    /// 
    ///     var x = o as Type;
    ///     if (x != null) ...
    /// 
    /// and converts it to:
    /// 
    ///     if (o is Type x) ...
    ///     
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpAsAndNullCheckDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        public override bool OpenFileOnly(Workspace workspace) => false;

        public CSharpAsAndNullCheckDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.InlineAsTypeCheckId,
                    new LocalizableResourceString(
                        nameof(FeaturesResources.Use_pattern_matching), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(SyntaxNodeAction,
                SyntaxKind.EqualsExpression,
                SyntaxKind.NotEqualsExpression);

        private void SyntaxNodeAction(SyntaxNodeAnalysisContext syntaxContext)
        {
            var node = syntaxContext.Node;
            var syntaxTree = node.SyntaxTree;

            // "x is Type y" is only available in C# 7.0 and above. Don't offer this refactoring
            // in projects targeting a lesser version.
            if (((CSharpParseOptions)syntaxTree.Options).LanguageVersion < LanguageVersion.CSharp7)
            {
                return;
            }

            var options = syntaxContext.Options;
            var cancellationToken = syntaxContext.CancellationToken;
            var optionSet = options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var styleOption = optionSet.GetOption(CSharpCodeStyleOptions.PreferPatternMatchingOverAsWithNullCheck);
            if (!styleOption.Value)
            {
                // Bail immediately if the user has disabled this feature.
                return;
            }

            var comparison = (BinaryExpressionSyntax)node;
            var operand = GetNullCheckOperand(comparison.Left, comparison.Right)?.WalkDownParentheses();
            if (operand == null)
            {
                return;
            }

            var semanticModel = syntaxContext.SemanticModel;
            if (operand.IsKind(SyntaxKind.CastExpression, out CastExpressionSyntax castExpression))
            {
                // Unwrap object cast
                var castType = semanticModel.GetTypeInfo(castExpression.Type).Type;
                if (castType.IsObjectType())
                {
                    operand = castExpression.Expression;
                }
            }

            if (!TryGetTypeCheckParts(operand, semanticModel,
                   out var declarator,
                   out var asExpression,
                   out var localSymbol))
            {
                return;
            }

            var localStatement = declarator.Parent?.Parent;
            var enclosingBlock = localStatement?.Parent;
            if (localStatement == null || 
                enclosingBlock == null)
            {
                return;
            }

            if (semanticModel.GetSymbolInfo(comparison).GetAnySymbol().IsUserDefinedOperator())
            {
                return;
            }

            var typeNode = ((BinaryExpressionSyntax)asExpression).Right;
            var asType = semanticModel.GetTypeInfo(typeNode, cancellationToken).Type;
            if (asType.IsNullable())
            {
                // Not legal to write "x is int? y"
                return;
            }

            if (asType?.TypeKind == TypeKind.Dynamic)
            {
                // Not legal to use dynamic in a pattern.
                return;
            }

            if (!((ILocalSymbol)localSymbol).Type.Equals(asType))
            {
                // We have something like:
                //
                //      BaseType b = x as DerivedType;
                //      if (b != null) { ... }
                //
                // It's not necessarily safe to convert this to:
                //
                //      if (x is DerivedType b) { ... }
                //
                // That's because there may be later code that wants to do something like assign a
                // 'BaseType' into 'b'.  As we've now claimed that it must be DerivedType, that
                // won't work.  This might also cause unintended changes like changing overload
                // resolution.  So, we conservatively do not offer the change in a situation like this.
                return;
            }

            if (!CanSafelyConvertToPatternMatching())
            {
                return;
            }

            // Looks good!
            var additionalLocations = ImmutableArray.Create(
                localStatement.GetLocation(),
                comparison.GetAncestor<StatementSyntax>().GetLocation(),
                comparison.GetLocation(),
                asExpression.GetLocation());

            // Put a diagnostic with the appropriate severity on the declaration-statement itself.
            syntaxContext.ReportDiagnostic(Diagnostic.Create(
                GetDescriptorWithSeverity(styleOption.Notification.Value),
                localStatement.GetLocation(),
                additionalLocations));

            bool CanSafelyConvertToPatternMatching()
            {
                bool DataFlowsIn(SyntaxNode statementOrExpression)
                {
                    return statementOrExpression != null
                        && semanticModel.AnalyzeDataFlow(statementOrExpression).DataFlowsIn.Contains(localSymbol);
                }

                var defAssignedWhenTrue = comparison.Kind() == SyntaxKind.NotEqualsExpression;
                for (var current = comparison.Parent; current != null; current = current.Parent)
                {
                    switch (current.Kind())
                    {
                        case SyntaxKind.ParenthesizedExpression:
                        case SyntaxKind.CastExpression:
                            // Parentheses and cast expressions do not contribute to the flow analysis.
                            continue;

                        case SyntaxKind.LogicalNotExpression:
                            // The !-operator reverses the definitive assignment state.
                            defAssignedWhenTrue = !defAssignedWhenTrue;
                            continue;

                        case SyntaxKind.LogicalAndExpression:
                            if (!defAssignedWhenTrue)
                            {
                                return false;
                            }

                            continue;

                        case SyntaxKind.LogicalOrExpression:
                            if (defAssignedWhenTrue)
                            {
                                return false;
                            }

                            continue;

                        case SyntaxKind.ConditionalExpression:
                            var conditionalExpression = (ConditionalExpressionSyntax)current;
                            if (DataFlowsIn(defAssignedWhenTrue
                                    ? conditionalExpression.WhenFalse
                                    : conditionalExpression.WhenTrue))
                            {
                                return false;
                            }

                            return CheckExpression(conditionalExpression);

                        case SyntaxKind.ForStatement:
                            var forStatement = (ForStatementSyntax)current;
                            if (!forStatement.Condition.Span.Contains(comparison.Span))
                            {
                                return false;
                            }

                            return CheckLoop(loopBody: forStatement.Statement);

                        case SyntaxKind.WhileStatement:
                            var whileStatement = (WhileStatementSyntax)current;
                            return CheckLoop(loopBody: whileStatement.Statement);

                        case SyntaxKind.IfStatement:
                            var ifStatement = (IfStatementSyntax)current;
                            var oppositeStatement = defAssignedWhenTrue
                                ? ifStatement.Else?.Statement
                                : ifStatement.Statement;

                            if (oppositeStatement != null)
                            {
                                var dataFlow = semanticModel.AnalyzeDataFlow(oppositeStatement);
                                if (dataFlow.DataFlowsIn.Contains(localSymbol))
                                {
                                    // Access before assignment is not safe in the opposite branch
                                    // as the variable is not definitely assgined at this point.
                                    // For example:
                                    //
                                    //    if (o is string x) { }
                                    //    else { Use(x); }
                                    //
                                    return false;
                                }

                                if (dataFlow.AlwaysAssigned.Contains(localSymbol))
                                {
                                    // If the variable is always assigned here, we don't need to check
                                    // subsequent statements as it's definitely assigned afterwards.
                                    // For example:
                                    //
                                    //     if (o is string x) { }
                                    //     else { x = null; }
                                    //
                                    return true;
                                }
                            }

                            if (!defAssignedWhenTrue && 
                                !semanticModel.AnalyzeControlFlow(ifStatement.Statement).EndPointIsReachable)
                            {
                                // Access before assignment here is only valid if we have a negative
                                // pattern-matching in an if-statement with an unreachable endpoint.
                                // For example:
                                //
                                //      if (!(o is string x)) {
                                //        return;
                                //      }
                                //
                                //      // The 'return' statement above ensures x is definitely assigned here
                                //      Console.WriteLine(x);
                                //
                                return true;
                            }

                            return CheckStatement(ifStatement);
                    }

                    switch (current)
                    {
                        case ExpressionSyntax expression:
                            return CheckExpression(expression);
                        case StatementSyntax statement:
                            return CheckStatement(statement);
                        default:
                            return CheckStatement(current.GetAncestor<StatementSyntax>());
                    }

                    bool CheckLoop(StatementSyntax loopBody)
                    {
                        if (operand.Kind() == SyntaxKind.IdentifierName)
                        {
                            // We have something like:
                            //
                            //      var x = e as T;
                            //      while (b != null) { ... }
                            //
                            // It's not necessarily safe to convert this to:
                            //
                            //      while (x is T b) { ... }
                            //
                            // That's because in this case, unlike the original code, we're type-checking in every iteration
                            // so we do not replace a simple null check with the "is" operator if it's in a loop.
                            return false;
                        }

                        if (!defAssignedWhenTrue && DataFlowsIn(loopBody))
                        {
                            return false;
                        }

                        return !IsAccessedOutOfScope(current);
                    }

                    bool CheckExpression(ExpressionSyntax exprsesion)
                    {
                        // It wouldn't be safe to read after the pattern variable is
                        // declared inside a sub-expression, because it would not be
                        // definitely assigned after this point. It's possible to allow
                        // use after assignment but it's rather unlikely to happen.
                        return !IsAccessedOutOfScope(exprsesion);
                    }

                    bool CheckStatement(StatementSyntax statement)
                    {
                        if (statement.Parent.IsKind(SyntaxKind.Block, out BlockSyntax block))
                        {
                            if (IsAccessedOutOfScope(block))
                            {
                                return false;
                            }

                            var nextStatement = statement.GetNextStatement();
                            if (nextStatement != null)
                            {
                                var dataFlow = semanticModel.AnalyzeDataFlow(nextStatement, block.Statements.Last());
                                return !dataFlow.DataFlowsIn.Contains(localSymbol);
                            }

                            return true;
                        }
                        else
                        {
                            return !IsAccessedOutOfScope(statement);
                        }
                    }

                    bool IsAccessedOutOfScope(SyntaxNode scope)
                    {
                        var localStatementStart = localStatement.Span.Start;
                        var variableName = localSymbol.Name;

                        foreach (var descendentNode in enclosingBlock.DescendantNodes())
                        {
                            var descendentStart = descendentNode.Span.Start;
                            if (descendentStart <= localStatementStart)
                            {
                                // We're not interested in nodes that are apeared before
                                // the local declaration statement. It's either an error
                                // or not the local reference we're looking for.
                                continue;
                            }

                            if (scope.Span.Contains(descendentNode.Span))
                            {
                                // If this is in the scope, we don't bother checking the symbol.
                                continue;
                            }

                            if (descendentNode.IsKind(SyntaxKind.IdentifierName, out IdentifierNameSyntax identifierName) &&
                                identifierName.Identifier.ValueText == variableName && 
                                localSymbol.Equals(semanticModel.GetSymbolInfo(identifierName, cancellationToken).Symbol))
                            {
                                return true;
                            }
                        }

                        return false;
                    }
                }

                return false;
            }
        }

        private static bool TryGetTypeCheckParts(
            SyntaxNode operand,
            SemanticModel semanticModel,
            out VariableDeclaratorSyntax declarator,
            out SyntaxNode asExpression,
            out ISymbol localSymbol)
        {
            switch (operand.Kind())
            {
                case SyntaxKind.IdentifierName:
                    {
                        // var x = e as T;
                        // if (x != null) F(x);
                        var identifier = (IdentifierNameSyntax)operand;
                        if (!TryFindVariableDeclarator(identifier, semanticModel, out localSymbol, out declarator))
                        {
                            break;
                        }

                        var initializerValue = declarator.Initializer?.Value;
                        if (!initializerValue.IsKind(SyntaxKind.AsExpression, out asExpression))
                        {
                            break;
                        }

                        return true;
                    }

                case SyntaxKind.SimpleAssignmentExpression:
                    {
                        // T x;
                        // if ((x = e as T) != null) F(x);
                        var assignment = (AssignmentExpressionSyntax)operand;
                        if (!assignment.Right.IsKind(SyntaxKind.AsExpression, out asExpression) ||
                            !assignment.Left.IsKind(SyntaxKind.IdentifierName, out IdentifierNameSyntax identifier))
                        {
                            break;
                        }

                        if (!TryFindVariableDeclarator(identifier, semanticModel, out localSymbol, out declarator))
                        {
                            break;
                        }

                        return true;
                    }
            }

            declarator = null;
            asExpression = null;
            localSymbol = null;
            return false;
        }

        private static bool TryFindVariableDeclarator(
            IdentifierNameSyntax identifier, SemanticModel semanticModel,
            out ISymbol localSymbol,
            out VariableDeclaratorSyntax declarator)
        {
            localSymbol = semanticModel.GetSymbolInfo(identifier).Symbol;
            declarator = localSymbol?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as VariableDeclaratorSyntax;
            return declarator != null;
        }

        private static ExpressionSyntax GetNullCheckOperand(ExpressionSyntax left, ExpressionSyntax right)
        {
            if (left.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return right;
            }

            if (right.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return left;
            }

            return null;
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
    }
}
