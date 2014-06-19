// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Roslyn.Compilers.CSharp
{
    using Roslyn.Compilers.Internal;
    using Symbols.Source;
    internal sealed partial class SemanticAnalyzer
    {
        public BoundWhileStatement BindWhile(WhileStatementSyntax node)
        {
            Debug.Assert(node != null);

            var condition = BindBooleanExpression(node.Condition);
            var loopContext = this.containingMethod.BlockMap.GetValueOrDefault(node);
            Debug.Assert(loopContext != null);
            var analyzer = new SemanticAnalyzer(this.containingMethod, loopContext, this.diagnostics);
            var body = analyzer.BindStatement(node.Statement);
            return new BoundWhileStatement(node, condition, body, loopContext.GetBreakLabel(), loopContext.GetContinueLabel());
        }

        public BoundDoStatement BindDo(DoStatementSyntax node)
        {
            var condition = BindBooleanExpression(node.Condition);
            var loopContext = this.containingMethod.BlockMap.GetValueOrDefault(node);
            Debug.Assert(loopContext != null);
            var analyzer = new SemanticAnalyzer(this.containingMethod, loopContext, this.diagnostics);
            var body = analyzer.BindStatement(node.Statement);
            return new BoundDoStatement(node, condition, body, loopContext.GetBreakLabel(), loopContext.GetContinueLabel());
        }

        public BoundBreakStatement BindBreak(BreakStatementSyntax node)
        {
            var target = this.context.GetBreakLabel();
            if (target == null)
            {
                Error(ErrorCode.ERR_NoBreakOrCont, node);
                return BoundBreakStatement.AsError(node, null);
            }
            return new BoundBreakStatement(node, target);
        }
        
        public BoundContinueStatement BindContinue(ContinueStatementSyntax node)
        {
            var target = this.context.GetContinueLabel();
            if (target == null)
            {
                Error(ErrorCode.ERR_NoBreakOrCont, node);
                return BoundContinueStatement.AsError(node, null);
            }
            return new BoundContinueStatement(node, target);
        }
    }

    internal static partial class Extensions
    {
        public static GeneratedLabelSymbol GetBreakLabel(this BinderContext context)
        {
            BlockBaseBinderContext blockContext = context as BlockBaseBinderContext;
            if (blockContext == null)
            {
                return null;
            }
            return blockContext.BreakLabel;
        }
        public static GeneratedLabelSymbol GetContinueLabel(this BinderContext context)
        {
            BlockBaseBinderContext blockContext = context as BlockBaseBinderContext;
            if (blockContext == null)
            {
                return null;
            }
            return blockContext.ContinueLabel;
        }
    }
}