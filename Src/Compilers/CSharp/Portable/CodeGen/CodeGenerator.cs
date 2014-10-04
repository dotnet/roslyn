// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Symbols;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeGen
{
    internal sealed partial class CodeGenerator
    {
        private readonly MethodSymbol method;

        // Syntax of the method body (block or an expression) being emitted, 
        // or null if the method being emitted isn't a source method.
        // If we are emitting a lambda this is its body.
        private readonly SyntaxNode methodBodySyntaxOpt;

        private readonly BoundStatement boundBody;
        private readonly ILBuilder builder;
        private readonly PEModuleBuilder module;
        private readonly DiagnosticBag diagnostics;
        private readonly OptimizationLevel optimizations;
        private readonly bool emitPdbSequencePoints;

        private readonly HashSet<LocalSymbol> stackLocals;

        // not 0 when in a protected region with a handler. 
        private int tryNestingLevel = 0;

        // Dispenser of unique ordinals for synthesized variable names that have the same kind and syntax offset.
        // The key is (local.SyntaxOffset << 8) | local.SynthesizedKind.
        private PooledDictionary<long, int> synthesizedLocalOrdinals;
        private int uniqueNameId;

        // label used when when return is emitted in a form of store/goto
        private static readonly object ReturnLabel = new object();

        private int asyncCatchHandlerOffset = -1;
        private ArrayBuilder<int> asyncYieldPoints = null;
        private ArrayBuilder<int> asyncResumePoints = null;

        /// <summary>
        /// In some cases returns are handled as gotos to return epilogue.
        /// This is used to track the state of the epilogue.
        /// </summary>
        private IndirectReturnState indirectReturnState;

        private enum IndirectReturnState : byte
        {
            NotNeeded = 0,  // did not see indirect returns
            Needed = 1,  // saw indirect return and need to emit return sequence
            Emitted = 2,  // return sequence has been emitted
        }

        private LocalDefinition returnTemp;

        public CodeGenerator(
            MethodSymbol method,
            BoundStatement boundBody,
            ILBuilder builder,
            PEModuleBuilder moduleBuilder,
            DiagnosticBag diagnostics,
            OptimizationLevel optimizations,
            bool emittingPdbs)
        {
            Debug.Assert((object)method != null);
            Debug.Assert(boundBody != null);
            Debug.Assert(builder != null);
            Debug.Assert(moduleBuilder != null);
            Debug.Assert(diagnostics != null);

            this.method = method;
            this.boundBody = boundBody;
            this.builder = builder;
            this.module = moduleBuilder;
            this.diagnostics = diagnostics;

            // Always optimize synthesized methods that don't contain user code.
            // 
            // Specifically, always optimize synthesized explicit interface implementation methods
            // (aka bridge methods) with by-ref returns because peverify produces errors if we
            // return a ref local (which the return local will be in such cases).

            this.optimizations = method.GenerateDebugInfo ? optimizations : OptimizationLevel.Release;

            // Emit sequence points unless
            // - the PDBs are not being generated
            // - debug information for the method is not generated since the method does not contain
            //   user code that can be stepped thru, or changed during EnC.
            // 
            // This setting only affects generating PDB sequence points, it shall not affect generated IL in any way.
            this.emitPdbSequencePoints = emittingPdbs && method.GenerateDebugInfo;

            if (this.optimizations == OptimizationLevel.Release)
            {
                this.boundBody = Optimizer.Optimize(boundBody, out stackLocals);
            }
            
            methodBodySyntaxOpt = (method as SourceMethodSymbol)?.BodySyntax;
        }

        private LocalDefinition LazyReturnTemp
        {
            get
            {
                var result = returnTemp;
                if (result == null)
                {
                    Debug.Assert(!this.method.ReturnsVoid, "returning something from void method?");
                    result = AllocateTemp(this.method.ReturnType, boundBody.Syntax);
                    returnTemp = result;
                }
                return result;
            }
        }

        private bool IsStackLocal(LocalSymbol local)
        {
            return stackLocals != null && stackLocals.Contains(local);
        }

        public void Generate()
        {
            this.GenerateImpl();

            Debug.Assert(this.asyncCatchHandlerOffset < 0);
            Debug.Assert(this.asyncYieldPoints == null);
            Debug.Assert(this.asyncResumePoints == null);
        }

        public void Generate(out int asyncCatchHandlerOffset, out ImmutableArray<int> asyncYieldPoints, out ImmutableArray<int> asyncResumePoints)
        {
            this.GenerateImpl();

            asyncCatchHandlerOffset = (this.asyncCatchHandlerOffset < 0)
                ? -1
                : this.builder.GetILOffsetFromMarker(this.asyncCatchHandlerOffset);

            ArrayBuilder<int> yieldPoints = this.asyncYieldPoints;
            ArrayBuilder<int> resumePoints = this.asyncResumePoints;

            Debug.Assert((yieldPoints == null) == (resumePoints == null));

            if (yieldPoints == null)
            {
                asyncYieldPoints = ImmutableArray<int>.Empty;
                asyncResumePoints = ImmutableArray<int>.Empty;
            }
            else
            {
                var yieldPointBuilder = ArrayBuilder<int>.GetInstance();
                var resumePointBuilder = ArrayBuilder<int>.GetInstance();
                int n = yieldPoints.Count;
                for (int i = 0; i < n; i++)
                {
                    int yieldOffset = this.builder.GetILOffsetFromMarker(yieldPoints[i]);
                    int resumeOffset = this.builder.GetILOffsetFromMarker(resumePoints[i]);
                    Debug.Assert(resumeOffset >= 0); // resume marker should always be reachable from dispatch

                    // yield point may not be reachable if the whole 
                    // await is not reachable; we just ignore such awaits
                    if (yieldOffset > 0)
                    {
                        yieldPointBuilder.Add(yieldOffset);
                        resumePointBuilder.Add(resumeOffset);
                    }
                }

                asyncYieldPoints = yieldPointBuilder.ToImmutableAndFree();
                asyncResumePoints = resumePointBuilder.ToImmutableAndFree();

                yieldPoints.Free();
                resumePoints.Free();
            }
        }

        private void GenerateImpl()
        {
            SetInitialDebugDocument();

            // Synthesized methods should have a sequence point
            // at offset 0 to ensure correct stepping behavior.
            if (this.emitPdbSequencePoints && this.method.IsImplicitlyDeclared)
            {
                this.builder.DefineInitialHiddenSequencePoint();
            }

            EmitStatement(boundBody);

            if (indirectReturnState == IndirectReturnState.Needed)
            {
                // it is unfortunate that return was not handled while we were in scope of the method
                // it can happen in rare cases involving exception handling (for example all returns were from a try)
                // in such case we can still handle return here.
                HandleReturn();
            }

            if (!diagnostics.HasAnyErrors())
            {
                builder.Realize();
            }

            if (this.synthesizedLocalOrdinals != null)
            {
                this.synthesizedLocalOrdinals.Free();
                this.synthesizedLocalOrdinals = null;
            }
        }

        private void HandleReturn()
        {
            builder.MarkLabel(ReturnLabel);

            Debug.Assert(method.ReturnsVoid == (returnTemp == null));

            if (this.emitPdbSequencePoints && (object)method.IteratorElementType == null && !method.IsAsync)
            {
                // In debug mode user could set a breakpoint on the last "}" of the method and 
                // expect to hit it before exiting the method.
                // We do it by rewriting all returns into a jump to an Exit label 
                // and mark the Exit sequence with sequence point for the span of the last "}".
                BlockSyntax blockSyntax = methodBodySyntaxOpt as BlockSyntax;
                if (blockSyntax != null)
                {
                    EmitSequencePoint(blockSyntax.SyntaxTree, blockSyntax.CloseBraceToken.Span);
                }
            }

            if (returnTemp != null)
            {
                builder.EmitLocalLoad(LazyReturnTemp);
                builder.EmitRet(false);
            }
            else
            {
                builder.EmitRet(true);
            }

            indirectReturnState = IndirectReturnState.Emitted;
        }

        private void EmitSymbolToken(TypeSymbol symbol, CSharpSyntaxNode syntaxNode)
        {
            builder.EmitToken(module.Translate(symbol, syntaxNode, diagnostics), syntaxNode, diagnostics);
        }

        private void EmitSymbolToken(MethodSymbol method, CSharpSyntaxNode syntaxNode, BoundArgListOperator optArgList)
        {
            builder.EmitToken(module.Translate(method, syntaxNode, diagnostics, optArgList), syntaxNode, diagnostics);
        }

        private void EmitSymbolToken(FieldSymbol symbol, CSharpSyntaxNode syntaxNode)
        {
            builder.EmitToken(module.Translate(symbol, syntaxNode, diagnostics), syntaxNode, diagnostics);
        }

        private void EmitSequencePointStatement(BoundSequencePoint node)
        {
            CSharpSyntaxNode syntax = node.Syntax;
            if (this.emitPdbSequencePoints)
            {
                if (syntax == null) //Null syntax indicates hidden sequence point (not equivalent to WasCompilerGenerated)
                {
                    EmitHiddenSequencePoint();
                }
                else
                {
                    EmitSequencePoint(syntax);
                }
            }

            BoundStatement statement = node.StatementOpt;
            int instructionsEmitted = 0;
            if (statement != null)
            {
                instructionsEmitted = this.EmitStatementAndCountInstructions(statement);
            }

            if (instructionsEmitted == 0 && syntax != null && optimizations == OptimizationLevel.Debug)
            {
                // if there was no code emitted, then emit nop 
                // otherwise this point could get associated with some random statement, possibly in a wrong scope
                builder.EmitOpCode(ILOpCode.Nop);
            }
        }

        private void EmitSequencePointStatement(BoundSequencePointWithSpan node)
        {
            TextSpan span = node.Span;
            if (span != default(TextSpan) && this.emitPdbSequencePoints)
            {
                this.EmitSequencePoint(node.SyntaxTree, span);
            }

            BoundStatement statement = node.StatementOpt;
            int instructionsEmitted = 0;
            if (statement != null)
            {
                instructionsEmitted = this.EmitStatementAndCountInstructions(statement);
            }

            if (instructionsEmitted == 0 && span != default(TextSpan) && optimizations == OptimizationLevel.Debug)
            {
                // if there was no code emitted, then emit nop 
                // otherwise this point could get associated with some random statement, possibly in a wrong scope
                builder.EmitOpCode(ILOpCode.Nop);
            }
        }

        private void SetInitialDebugDocument()
        {
            if (emitPdbSequencePoints && this.methodBodySyntaxOpt != null)
            {
                // If methodBlockSyntax is available (i.e. we're in a SourceMethodSymbol), then
                // provide the IL builder with our best guess at the appropriate debug document.
                // If we don't and this is hidden sequence point precedes all non-hidden sequence
                // points, then the IL Builder will drop the sequence point for lack of a document.
                // This negatively impacts the scenario where we insert hidden sequence points at
                // the beginnings of methods so that step-into (F11) will handle them correctly.
                builder.SetInitialDebugDocument(this.methodBodySyntaxOpt.SyntaxTree);
            }
        }

        private void EmitHiddenSequencePoint()
        {
            Debug.Assert(emitPdbSequencePoints);
            builder.DefineHiddenSequencePoint();
        }

        private void EmitSequencePoint(CSharpSyntaxNode syntax)
        {
            EmitSequencePoint(syntax.SyntaxTree, syntax.Span);
        }

        private TextSpan EmitSequencePoint(SyntaxTree syntaxTree, TextSpan span)
        {
            Debug.Assert(syntaxTree != null);
            Debug.Assert(emitPdbSequencePoints);

            builder.DefineSequencePoint(syntaxTree, span);
            return span;
        }
    }
}
