// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitIfStatement(BoundIfStatement node)
        {
            Debug.Assert(node != null);
            var rewrittenCondition = VisitExpression(node.Condition);
            var rewrittenConsequence = VisitStatement(node.Consequence);
            var rewrittenAlternative = VisitStatement(node.AlternativeOpt);
            var syntax = (IfStatementSyntax)node.Syntax;

            // EnC: We need to insert a hidden sequence point to handle function remapping in case 
            // the containing method is edited while methods invoked in the condition are being executed.
            if (this.Instrument && !node.WasCompilerGenerated)
            {
                rewrittenCondition = _instrumenter.InstrumentIfStatementCondition(node, rewrittenCondition, _factory);
            }

            var result = RewriteIfStatement(syntax, node.Locals, rewrittenCondition, rewrittenConsequence, rewrittenAlternative, node.HasErrors);

            // add sequence point before the whole statement
            if (this.Instrument && !node.WasCompilerGenerated)
            {
                result = _instrumenter.InstrumentIfStatement(node, result);
            }

            return result;
        }

        private static BoundStatement RewriteIfStatement(
            CSharpSyntaxNode syntax,
            BoundExpression rewrittenCondition,
            BoundStatement rewrittenConsequence,
            BoundStatement rewrittenAlternativeOpt,
            bool hasErrors)
        {
            return RewriteIfStatement(syntax, ImmutableArray<LocalSymbol>.Empty, rewrittenCondition, rewrittenConsequence, rewrittenAlternativeOpt, hasErrors);
        }

        private static BoundStatement RewriteIfStatement(
            CSharpSyntaxNode syntax,
            ImmutableArray<LocalSymbol> patternVariables,
            BoundExpression rewrittenCondition,
            BoundStatement rewrittenConsequence,
            BoundStatement rewrittenAlternativeOpt,
            bool hasErrors)
        {
            var afterif = new GeneratedLabelSymbol("afterif");
            var builder = ArrayBuilder<BoundStatement>.GetInstance();

            if (rewrittenAlternativeOpt == null)
            {
                // if (condition) 
                //   consequence;  
                //
                // becomes
                //
                // GotoIfFalse condition afterif;
                // consequence;
                // afterif:

                builder.Add(new BoundConditionalGoto(rewrittenCondition.Syntax, rewrittenCondition, false, afterif));
                builder.Add(rewrittenConsequence);
                builder.Add(new BoundLabelStatement(syntax, afterif));
                var statements = builder.ToImmutableAndFree();
                return (patternVariables.IsDefaultOrEmpty)
                    ? new BoundStatementList(syntax, statements, hasErrors)
                    : new BoundBlock(syntax, patternVariables, ImmutableArray<LocalFunctionSymbol>.Empty, statements, hasErrors) { WasCompilerGenerated = true };
            }
            else
            {
                // if (condition)
                //     consequence;
                // else 
                //     alternative
                //
                // becomes
                //
                // GotoIfFalse condition alt;
                // consequence
                // goto afterif;
                // alt:
                // alternative;
                // afterif:

                var alt = new GeneratedLabelSymbol("alternative");

                builder.Add(new BoundConditionalGoto(rewrittenCondition.Syntax, rewrittenCondition, false, alt));
                builder.Add(rewrittenConsequence);
                builder.Add(new BoundGotoStatement(syntax, afterif));
                builder.Add(new BoundLabelStatement(syntax, alt));
                if (!patternVariables.IsDefaultOrEmpty)
                {
                    // pattern variables are not in scope in the else part
                    var firstPart = new BoundBlock(syntax, patternVariables, ImmutableArray<LocalFunctionSymbol>.Empty, builder.ToImmutableAndFree(), hasErrors) { WasCompilerGenerated = true };
                    builder = ArrayBuilder<BoundStatement>.GetInstance();
                    builder.Add(firstPart);
                }
                builder.Add(rewrittenAlternativeOpt);
                builder.Add(new BoundLabelStatement(syntax, afterif));
                return new BoundStatementList(syntax, builder.ToImmutableAndFree(), hasErrors);
            }

        }
    }
}
