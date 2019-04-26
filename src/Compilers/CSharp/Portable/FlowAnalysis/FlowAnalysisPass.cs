// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

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
        /// <param name="originalBodyNested">the original method body is the last statement in the block</param>
        /// <returns>the rewritten block for the method (with a return statement possibly inserted)</returns>
        public static BoundBlock Rewrite(
            MethodSymbol method,
            BoundBlock block,
            DiagnosticBag diagnostics,
            bool hasTrailingExpression,
            bool originalBodyNested)
        {
#if DEBUG
            // We should only see a trailingExpression if we're in a Script initializer.
            Debug.Assert(!hasTrailingExpression || method.IsScriptInitializer);
            var initialDiagnosticCount = diagnostics.ToReadOnly().Length;
#endif
            var compilation = method.DeclaringCompilation;

            if (method.ReturnsVoid || method.IsIterator || method.IsTaskReturningAsync(compilation))
            {
                // we don't analyze synthesized void methods.
                if ((method.IsImplicitlyDeclared && !method.IsScriptInitializer) || Analyze(compilation, method, block, diagnostics))
                {
                    block = AppendImplicitReturn(block, method, originalBodyNested);
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
                    Debug.Assert(!submissionResultType.IsVoidType());

                    var trailingExpression = new BoundDefaultExpression(method.GetNonNullSyntaxNode(), submissionResultType);
                    var newStatements = block.Statements.Add(new BoundReturnStatement(trailingExpression.Syntax, RefKind.None, trailingExpression));
                    block = new BoundBlock(block.Syntax, ImmutableArray<LocalSymbol>.Empty, newStatements) { WasCompilerGenerated = true };
#if DEBUG
                    // It should not be necessary to repeat analysis after adding this node, because adding a trailing
                    // return in cases where one was missing should never produce different Diagnostics.
                    IEnumerable<Diagnostic> GetErrorsOnly(IEnumerable<Diagnostic> diags) => diags.Where(d => d.Severity == DiagnosticSeverity.Error);
                    var flowAnalysisDiagnostics = DiagnosticBag.GetInstance();
                    Debug.Assert(!Analyze(compilation, method, block, flowAnalysisDiagnostics));
                    // Ignore warnings since flow analysis reports nullability mismatches.
                    Debug.Assert(GetErrorsOnly(flowAnalysisDiagnostics.ToReadOnly()).SequenceEqual(GetErrorsOnly(diagnostics.ToReadOnly().Skip(initialDiagnosticCount))));
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

        private static BoundBlock AppendImplicitReturn(BoundBlock body, MethodSymbol method, bool originalBodyNested)
        {
            if (originalBodyNested)
            {
                var statements = body.Statements;
                int n = statements.Length;

                var builder = ArrayBuilder<BoundStatement>.GetInstance(n);
                builder.AddRange(statements, n - 1);
                builder.Add(AppendImplicitReturn((BoundBlock)statements[n - 1], method));

                return body.Update(body.Locals, ImmutableArray<LocalFunctionSymbol>.Empty, builder.ToImmutableAndFree());
            }
            else
            {
                return AppendImplicitReturn(body, method);
            }
        }

        // insert the implicit "return" statement at the end of the method body
        // Normally, we wouldn't bother attaching syntax trees to compiler-generated nodes, but these
        // ones are going to have sequence points.
        internal static BoundBlock AppendImplicitReturn(BoundBlock body, MethodSymbol method)
        {
            Debug.Assert(body != null);
            Debug.Assert(method != null);

            SyntaxNode syntax = body.Syntax;

            Debug.Assert(body.WasCompilerGenerated ||
                         syntax.IsKind(SyntaxKind.Block) ||
                         syntax.IsKind(SyntaxKind.ArrowExpressionClause) ||
                         syntax.IsKind(SyntaxKind.ConstructorDeclaration));

            BoundStatement ret = (method.IsIterator && !method.IsAsync)
                ? (BoundStatement)BoundYieldBreakStatement.Synthesized(syntax)
                : BoundReturnStatement.Synthesized(syntax, RefKind.None, null);

            return body.Update(body.Locals, body.LocalFunctions, body.Statements.Add(ret));
        }

        private static bool Analyze(
            CSharpCompilation compilation,
            MethodSymbol method,
            BoundBlock block,
            DiagnosticBag diagnostics)
        {
            var result = ControlFlowPass.Analyze(compilation, method, block, diagnostics);
            DefiniteAssignmentPass.Analyze(compilation, method, block, diagnostics);
            return result;
        }
    }
}
