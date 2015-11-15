// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class FlowAnalysisPass
    {
        /// <summary>
        /// The flow analysis pass.  This pass reports required diagnostics for unreachable
        /// statements and uninitialized variables (through the call to FlowAnalysisWalker.Analyze),
        /// and inserts a final return statement if the end of a void-returning method is reachable.
        /// </summary>
        /// <param name="method">the method to be analyzed</param>
        /// <param name="block">the method's body</param>
        /// <param name="diagnostics">the receiver of the reported diagnostics</param>
        /// <param name="hasTrailingExpression">indicates whether this Script had a trailing expression</param>
        /// <returns>the rewritten block for the method (with a return statement possibly inserted)</returns>
        public static BoundBlock Rewrite(
            MethodSymbol method,
            BoundBlock block,
            DiagnosticBag diagnostics,
            bool hasTrailingExpression)
        {
#if DEBUG
            // We should only see a trailingExpression if we're in a Script initializer.
            Debug.Assert(!hasTrailingExpression || method.IsScriptInitializer);
            var initialDiagnosticCount = diagnostics.ToReadOnly().Length;
#endif
            var compilation = method.DeclaringCompilation;

            if (method.ReturnsVoid || method.IsIterator ||
                (method.IsAsync && compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task) == method.ReturnType))
            {
                // we don't analyze synthesized void methods.
                if ((method.IsImplicitlyDeclared && !method.IsScriptInitializer) || Analyze(compilation, method, block, diagnostics))
                {
                    block = AppendImplicitReturn(block, method, (CSharpSyntaxNode)(method as SourceMethodSymbol)?.BodySyntax);
                }
            }
            else if (Analyze(compilation, method, block, diagnostics))
            {
                // If the method is a lambda expression being converted to a non-void delegate type
                // and the end point is reachable then suppress the error here; a special error
                // will be reported by the lambda binder.
                Debug.Assert(method.MethodKind != MethodKind.AnonymousFunction);

                // Add implicit "return default(T)" if this is a submission that does not have a trailing expression.
                var submissionResultType = (method as SynthesizedInteractiveInitializerMethod)?.ResultType;
                if (!hasTrailingExpression && ((object)submissionResultType != null))
                {
                    Debug.Assert(submissionResultType.SpecialType != SpecialType.System_Void);

                    var trailingExpression = new BoundDefaultOperator(method.GetNonNullSyntaxNode(), submissionResultType);
                    var newStatements = block.Statements.Add(new BoundReturnStatement(trailingExpression.Syntax, trailingExpression));
                    block = new BoundBlock(block.Syntax, ImmutableArray<LocalSymbol>.Empty, newStatements) { WasCompilerGenerated = true };
#if DEBUG
                    // It should not be necessary to repeat analysis after adding this node, because adding a trailing
                    // return in cases where one was missing should never produce different Diagnostics.
                    var flowAnalysisDiagnostics = DiagnosticBag.GetInstance();
                    Debug.Assert(!Analyze(compilation, method, block, flowAnalysisDiagnostics));
                    Debug.Assert(flowAnalysisDiagnostics.ToReadOnly().SequenceEqual(diagnostics.ToReadOnly().Skip(initialDiagnosticCount)));
                    flowAnalysisDiagnostics.Free();
#endif
                }
                // If there's more than one location, then the method is partial and we
                // have already reported a non-void partial method error.
                else if (method.Locations.Length == 1)
                {
                    diagnostics.Add(ErrorCode.ERR_ReturnExpected, method.Locations[0], method);
                }
            }

            return block;
        }

        // insert the implicit "return" statement at the end of the method body
        // Normally, we wouldn't bother attaching syntax trees to compiler-generated nodes, but these
        // ones are going to have sequence points.
        internal static BoundBlock AppendImplicitReturn(BoundBlock body, MethodSymbol method, CSharpSyntaxNode syntax = null)
        {
            Debug.Assert(body != null);
            Debug.Assert(method != null);

            if (syntax == null)
            {
                syntax = body.Syntax;
            }

            Debug.Assert(body.WasCompilerGenerated || syntax.IsKind(SyntaxKind.Block) || syntax.IsKind(SyntaxKind.ArrowExpressionClause));

            BoundStatement ret = method.IsIterator
                ? (BoundStatement)BoundYieldBreakStatement.Synthesized(syntax)
                : BoundReturnStatement.Synthesized(syntax, null);

            // Implicitly added return for async method does not need sequence points since lowering would add one.
            if (syntax.IsKind(SyntaxKind.Block) && !method.IsAsync)
            {
                var blockSyntax = (BlockSyntax)syntax;

                ret = new BoundSequencePointWithSpan(
                    blockSyntax,
                    ret,
                    blockSyntax.CloseBraceToken.Span)
                { WasCompilerGenerated = true };
            }

            switch (body.Kind)
            {
                case BoundKind.Block:
                    return body.Update(body.Locals, body.Statements.Add(ret));

                default:
                    return new BoundBlock(syntax, ImmutableArray<LocalSymbol>.Empty, ImmutableArray.Create(ret, body));
            }
        }

        private static bool Analyze(
            CSharpCompilation compilation,
            MethodSymbol method,
            BoundBlock block,
            DiagnosticBag diagnostics)
        {
            var result = ControlFlowPass.Analyze(compilation, method, block, diagnostics);
            DataFlowPass.Analyze(compilation, method, block, diagnostics);
            return result;
        }
    }
}
