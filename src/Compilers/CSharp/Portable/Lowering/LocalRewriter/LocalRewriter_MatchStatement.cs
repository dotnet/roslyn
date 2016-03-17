// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class LocalRewriter
    {
        // Rewriting for pattern-matching switch statements.
        public override BoundNode VisitPatternSwitchStatement(BoundPatternSwitchStatement node)
        {
            var statements = ArrayBuilder<BoundStatement>.GetInstance();

            // copy the original switch expression into a temp
            BoundAssignmentOperator initialStore;
            var switchExpressionTemp = _factory.StoreToTemp(VisitExpression(node.Expression), out initialStore, syntaxOpt: node.Expression.Syntax);
            statements.Add(_factory.ExpressionStatement(initialStore));

            // save the default label, if and when we find it.
            LabelSymbol defaultLabel = null;

            foreach (var section in node.PatternSwitchSections)
            {
                BoundExpression sectionCondition = _factory.Literal(false);
                bool isDefaultSection = false;
                foreach (var label in section.PatternSwitchLabels)
                {
                    if (label.Syntax.Kind() == SyntaxKind.DefaultSwitchLabel)
                    {
                        // The default label was handled in initial tail, above
                        Debug.Assert(label.Pattern.Kind == BoundKind.WildcardPattern && label.Guard == null);
                        isDefaultSection = true;
                        defaultLabel = _factory.GenerateLabel("default");
                        continue;
                    }

                    var labelCondition = TranslatePattern(switchExpressionTemp, label.Pattern);
                    if (label.Guard != null)
                    {
                        labelCondition = _factory.LogicalAnd(labelCondition, VisitExpression(label.Guard));
                    }

                    sectionCondition = _factory.LogicalOr(sectionCondition, labelCondition);
                }

                var sectionBuilder = ArrayBuilder<BoundStatement>.GetInstance();
                if (isDefaultSection)
                {
                    sectionBuilder.Add(_factory.Label(defaultLabel));
                }
                sectionBuilder.AddRange(VisitList(section.Statements));
                sectionBuilder.Add(_factory.Goto(node.BreakLabel));
                statements.Add(_factory.If(sectionCondition, section.Locals, _factory.Block(sectionBuilder.ToImmutableAndFree())));
            }

            if (defaultLabel != null)
            {
                statements.Add(_factory.Goto(defaultLabel));
            }

            statements.Add(_factory.Label(node.BreakLabel));
            return _factory.Block(node.InnerLocals.Add(switchExpressionTemp.LocalSymbol), node.InnerLocalFunctions, statements.ToImmutableAndFree());
        }
    }
}
