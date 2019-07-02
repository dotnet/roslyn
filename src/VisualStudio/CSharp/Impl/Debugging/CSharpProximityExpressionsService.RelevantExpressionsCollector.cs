// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Debugging
{
    internal partial class CSharpProximityExpressionsService
    {
        private class RelevantExpressionsCollector : CSharpSyntaxVisitor
        {
            private readonly bool _includeDeclarations;
            private readonly IList<string> _expressions;

            public RelevantExpressionsCollector(bool includeDeclarations, IList<string> expressions)
            {
                _includeDeclarations = includeDeclarations;
                _expressions = expressions;
            }

            public override void VisitLabeledStatement(LabeledStatementSyntax node)
            {
                AddRelevantExpressions(node.Statement, _expressions, _includeDeclarations);
            }

            public override void VisitExpressionStatement(ExpressionStatementSyntax node)
            {
                AddExpressionTerms(node.Expression, _expressions);
            }

            public override void VisitReturnStatement(ReturnStatementSyntax node)
            {
                AddExpressionTerms(node.Expression, _expressions);
            }

            public override void VisitThrowStatement(ThrowStatementSyntax node)
            {
                AddExpressionTerms(node.Expression, _expressions);
            }

            public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
            {
                // Here, we collect expression expressions from any/all initialization expressions
                AddVariableExpressions(node.Declaration.Variables, _expressions);
            }

            public override void VisitDoStatement(DoStatementSyntax node)
            {
                AddExpressionTerms(node.Condition, _expressions);
            }

            public override void VisitLockStatement(LockStatementSyntax node)
            {
                AddExpressionTerms(node.Expression, _expressions);
            }

            public override void VisitWhileStatement(WhileStatementSyntax node)
            {
                AddExpressionTerms(node.Condition, _expressions);
            }

            public override void VisitIfStatement(IfStatementSyntax node)
            {
                AddExpressionTerms(node.Condition, _expressions);
            }

            public override void VisitForStatement(ForStatementSyntax node)
            {
                if (node.Declaration != null)
                {
                    AddVariableExpressions(node.Declaration.Variables, _expressions);
                }

                node.Initializers.Do(i => AddExpressionTerms(i, _expressions));
                AddExpressionTerms(node.Condition, _expressions);
                node.Incrementors.Do(i => AddExpressionTerms(i, _expressions));
            }

            public override void VisitForEachStatement(ForEachStatementSyntax node)
            {
                _expressions.Add(node.Identifier.ValueText);
                AddExpressionTerms(node.Expression, _expressions);
            }

            public override void VisitForEachVariableStatement(ForEachVariableStatementSyntax node)
            {
                AddVariableExpressions(node.Variable, _expressions);
                AddExpressionTerms(node.Expression, _expressions);
            }

            public override void VisitUsingStatement(UsingStatementSyntax node)
            {
                if (node.Declaration != null)
                {
                    AddVariableExpressions(node.Declaration.Variables, _expressions);
                }

                AddExpressionTerms(node.Expression, _expressions);
            }

            public override void VisitSwitchStatement(SwitchStatementSyntax node)
            {
                AddExpressionTerms(node.Expression, _expressions);
            }

            private void AddVariableExpressions(
                SeparatedSyntaxList<VariableDeclaratorSyntax> declarators,
                IList<string> expressions)
            {
                foreach (var declarator in declarators)
                {
                    if (_includeDeclarations)
                    {
                        expressions.Add(declarator.Identifier.ValueText);
                    }

                    if (declarator.Initializer != null)
                    {
                        AddExpressionTerms(declarator.Initializer.Value, expressions);
                    }
                }
            }

            private void AddVariableExpressions(
                ExpressionSyntax component,
                IList<string> expressions)
            {
                if (!_includeDeclarations) return;

                switch (component.Kind())
                {
                    case SyntaxKind.TupleExpression:
                        {
                            var t = (TupleExpressionSyntax)component;
                            foreach (var a in t.Arguments)
                            {
                                AddVariableExpressions(a.Expression, expressions);
                            }

                            break;
                        }
                    case SyntaxKind.DeclarationExpression:
                        {
                            var t = (DeclarationExpressionSyntax)component;
                            AddVariableExpressions(t.Designation, expressions);
                            break;
                        }
                }
            }

            private void AddVariableExpressions(
                VariableDesignationSyntax component,
                IList<string> expressions)
            {
                if (!_includeDeclarations) return;

                switch (component.Kind())
                {
                    case SyntaxKind.ParenthesizedVariableDesignation:
                        {
                            var t = (ParenthesizedVariableDesignationSyntax)component;
                            foreach (var v in t.Variables) AddVariableExpressions(v, expressions);
                            break;
                        }
                    case SyntaxKind.SingleVariableDesignation:
                        {
                            var t = (SingleVariableDesignationSyntax)component;
                            expressions.Add(t.Identifier.ValueText);
                            break;
                        }
                }
            }
        }
    }
}
