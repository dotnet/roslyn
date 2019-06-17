using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Lowering
{
    class NullCheckRewriter : BoundTreeRewriterWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
    {
        readonly MethodSymbol _method;
        SyntheticBoundNodeFactory _fact;
        public NullCheckRewriter(
            MethodSymbol method,
            SyntaxNode syntax,
            TypeCompilationState compilationState,
            DiagnosticBag diagnostics)
        {
            _method = method;
            _fact = new SyntheticBoundNodeFactory(method, syntax, compilationState, diagnostics);
        }

        internal static BoundNode Rewrite(
            BoundStatement body,
            MethodSymbol method,
            int methodOrdinal,
            VariableSlotAllocator slotAllocatorOpt,
            TypeCompilationState compilationState,
            DiagnosticBag diagnostics) //,
                                       //out IteratorStateMachine stateMachineType)
        {
            var rewriter = new NullCheckRewriter(method, body.Syntax, compilationState, diagnostics);
            return rewriter.Visit(body);
        }

        public override BoundNode VisitBlock(BoundBlock node)
        {
            List<SourceParameterSymbolBase> toCheck = new List<SourceParameterSymbolBase>();
            for (int i = 0; i < _method.Parameters.Length; i++)
            {
                var param = (SourceParameterSymbolBase)_method.Parameters[i];
                if (param.IsNullChecked)
                {
                    toCheck.Add(param);
                }
            }
            return AddNullChecksToBody(node, toCheck);
        }

        private BoundNode AddNullChecksToBody(BoundBlock body, List<SourceParameterSymbolBase> toCheck)
        {
            BoundBlock prependedBody = body;
            var statementList = new List<BoundStatement>();
            foreach (SourceParameterSymbolBase param in toCheck)
            {
                statementList.Add(PrependBodyWithNullCheck(body, param));
            }
            statementList.AddRange(body.Statements);

            return _fact.Block(body.Locals, statementList.ToImmutableArray());
        }

        private BoundStatement PrependBodyWithNullCheck(BoundBlock body, SourceParameterSymbolBase parameter)
        {
            if (parameter is null)
                return body;

            // If condition
            BoundExpression paramIsNullCondition = _fact.ObjectEqual(_fact.Parameter(parameter), _fact.Literal(ConstantValue.Null, parameter.Type));

            // If consequences
            BoundThrowStatement throwArgNullStatement = _fact.Throw(_fact.New(_fact.WellKnownType(WellKnownType.System_Exception)));

            // Assembles the two statements above into if-statement
            return _fact.HiddenSequencePoint(_fact.If(paramIsNullCondition, body.Locals, throwArgNullStatement));
        }
    }
}
