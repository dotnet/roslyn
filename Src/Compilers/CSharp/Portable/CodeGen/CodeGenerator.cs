// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeGen
{
    internal sealed partial class CodeGenerator
    {
        private readonly MethodSymbol method;
        private readonly BlockSyntax methodBlockSyntax;
        private readonly BoundStatement block;
        private readonly ILBuilder builder;
        private readonly PEModuleBuilder module;
        private readonly DiagnosticBag diagnostics;

        private readonly HashSet<LocalSymbol> stackLocals;

        // not 0 when in a protected region with a handler. 
        private int tryNestingLevel = 0;

        //Native compiler does a number of special things when this flag is set.
        private readonly bool noOptimizations;

        private readonly DebugInformationKind debugInformationKind;

        // Emit sequence points?
        private readonly bool emitSequencePoints;

        // Unique id for generated local names.
        private int uniqueId;

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
        private LocalDefinition LazyReturnTemp
        {
            get
            {
                var result = returnTemp;
                if (result == null)
                {
                    Debug.Assert(!this.method.ReturnsVoid, "returning something from void method?");
                    result = AllocateTemp(this.method.ReturnType, block.Syntax);
                    returnTemp = result;
                }
                return result;
            }
        }

        private CodeGenerator(MethodSymbol method,
            BoundStatement block,
            ILBuilder builder,
            PEModuleBuilder module,
            DiagnosticBag diagnostics,
            bool optimize,
            bool emitSequencePoints)
        {
            this.method = method;
            this.block = block;
            this.builder = builder;
            this.module = module;
            this.diagnostics = diagnostics;

            this.noOptimizations = !optimize;
            this.debugInformationKind = module.Compilation.Options.DebugInformationKind;

            if (!this.debugInformationKind.IsValid())
            {
                this.debugInformationKind = DebugInformationKind.None;
            }

            // Special case: always optimize synthesized explicit interface implementation methods
            // (aka bridge methods) with by-ref returns because peverify produces errors if we
            // return a ref local (which the return local will be in such cases).
            if (this.noOptimizations && method.ReturnType is ByRefReturnErrorTypeSymbol)
            {
                Debug.Assert(method is SynthesizedExplicitImplementationForwardingMethod);
                this.noOptimizations = false;
            }

            this.emitSequencePoints = emitSequencePoints;

            if (!this.noOptimizations)
            {
                this.block = Optimizer.Optimize(block, out stackLocals);
            }

            Debug.Assert((object)method != null);
            Debug.Assert(block != null);
            Debug.Assert(builder != null);
            Debug.Assert(module != null);

            var asSourceMethod = method as SourceMethodSymbol;
            if ((object)asSourceMethod != null)
            {
                methodBlockSyntax = asSourceMethod.BlockSyntax;
            }
        }

        private bool IsStackLocal(LocalSymbol local)
        {
            return stackLocals != null && stackLocals.Contains(local);
        }

        public static void Run(
            MethodSymbol meth, BoundStatement block, ILBuilder builder, PEModuleBuilder module, DiagnosticBag diagnostics, bool optimize, bool emitSequencePoints)
        {
            CodeGenerator generator = new CodeGenerator(meth, block, builder, module, diagnostics, optimize, emitSequencePoints);
            generator.Generate();
            Debug.Assert(generator.asyncCatchHandlerOffset < 0);
            Debug.Assert(generator.asyncYieldPoints == null);
            Debug.Assert(generator.asyncResumePoints == null);

            if (!diagnostics.HasAnyErrors())
            {
                builder.Realize();
            }
        }

        public static void Run(
            MethodSymbol meth, BoundStatement block, ILBuilder builder, PEModuleBuilder module, DiagnosticBag diagnostics, bool optimize, bool emitSequencePoints,
            out int asyncCatchHandlerOffset, out ImmutableArray<int> asyncYieldPoints, out ImmutableArray<int> asyncResumePoints)
        {
            CodeGenerator generator = new CodeGenerator(meth, block, builder, module, diagnostics, optimize, emitSequencePoints);
            generator.Generate();

            if (!diagnostics.HasAnyErrors())
            {
                builder.Realize();
            }

            asyncCatchHandlerOffset = (generator.asyncCatchHandlerOffset < 0)
                ? -1
                : generator.builder.GetILOffsetFromMarker(generator.asyncCatchHandlerOffset);

            ArrayBuilder<int> yieldPoints = generator.asyncYieldPoints;
            ArrayBuilder<int> resumePoints = generator.asyncResumePoints;
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
                    int yieldOffset = generator.builder.GetILOffsetFromMarker(yieldPoints[i]);
                    int resumeOffset = generator.builder.GetILOffsetFromMarker(resumePoints[i]);
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

        private void Generate()
        {
            SetInitialDebugDocument();

            // Synthesized methods should have a sequence point
            // at offset 0 to ensure correct stepping behavior.
            if (this.emitSequencePoints && this.method.IsImplicitlyDeclared)
            {
                this.builder.DefineInitialHiddenSeqPoint();
            }

            EmitStatement(block);

            if (indirectReturnState == IndirectReturnState.Needed)
            {
                // it is unfortunate that return was not handled while we were in scope of the method
                // it can happen in rare cases involving exception handling (for example all returns were from a try)
                // in such case we can still handle return here.
                HandleReturn();
            }
        }

        private void HandleReturn()
        {
            builder.MarkLabel(ReturnLabel);

            Debug.Assert(method.ReturnsVoid == (returnTemp == null));

            // in debug mode user could set a breakpoint on the last "}" of the method and 
            // expect to hit it before exiting the method
            // we do it by rewriting all returns into a jump to an Exit label 
            // and mark the Exit sequence with sequence point for the span of the last "}"
            BlockSyntax blockSyntax = null;
            SourceMethodSymbol sourceMethod = method as SourceMethodSymbol;
            if ((object)sourceMethod != null && (object)sourceMethod.IteratorElementType == null)
            {
                blockSyntax = sourceMethod.BlockSyntax;
            }

            if (blockSyntax != null && this.emitSequencePoints)
            {
                EmitSequencePoint(block.SyntaxTree ?? sourceMethod.SyntaxTree, blockSyntax.CloseBraceToken.Span);
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
            AssertExplicitSequencePointAllowed();

            CSharpSyntaxNode syntax = node.Syntax;
            if (this.emitSequencePoints)
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
                instructionsEmitted = -builder.InstructionsEmitted;
                this.EmitStatement(statement);
                instructionsEmitted += builder.InstructionsEmitted;
            }

            if (instructionsEmitted == 0 && syntax != null && noOptimizations)
            {
                // if there was no code emitted, then emit nop 
                // otherwise this point could get associated with some random statement, possibly in a wrong scope
                builder.EmitOpCode(ILOpCode.Nop);
            }
        }

        private void EmitSequencePointStatement(BoundSequencePointWithSpan node)
        {
            AssertExplicitSequencePointAllowed();

            TextSpan span = node.Span;
            if (span != default(TextSpan) && this.emitSequencePoints)
            {
                this.EmitSequencePoint(node.SyntaxTree, span);
            }

            BoundStatement statement = node.StatementOpt;
            int instructionsEmitted = 0;

            if (statement != null)
            {
                instructionsEmitted = -builder.InstructionsEmitted;
                this.EmitStatement(statement);
                instructionsEmitted += builder.InstructionsEmitted;
            }

            if (instructionsEmitted == 0 && span != default(TextSpan) && noOptimizations)
            {
                // if there was no code emitted, then emit nop 
                // otherwise this point could get associated with some random statement, possibly in a wrong scope
                builder.EmitOpCode(ILOpCode.Nop);
            }
        }

        private void SetInitialDebugDocument()
        {
            if (emitSequencePoints && this.methodBlockSyntax != null)
            {
                // If methodBlockSyntax is available (i.e. we're in a SourceMethodSymbol), then
                // provide the IL builder with our best guess at the appropriate debug document.
                // If we don't and this is hidden sequence point precedes all non-hidden sequence
                // points, then the IL Builder will drop the sequence point for lack of a document.
                // This negatively impacts the scenario where we insert hidden sequence points at
                // the beginnings of methods so that step-into (F11) will handle them correctly.
                builder.SetInitialDebugDocument(this.methodBlockSyntax.SyntaxTree);
            }
        }

        private void EmitHiddenSequencePoint()
        {
            Debug.Assert(emitSequencePoints);
            builder.DefineHiddenSeqPoint();
        }

        private void EmitSequencePoint(CSharpSyntaxNode syntax)
        {
            EmitSequencePoint(syntax.SyntaxTree, syntax.Span);
        }

        private TextSpan EmitSequencePoint(SyntaxTree syntaxTree, TextSpan span)
        {
            Debug.Assert(syntaxTree != null);
            Debug.Assert(emitSequencePoints);

            builder.DefineSeqPoint(syntaxTree, span);
            return span;
        }

        [Conditional("DEBUG")]
        private void AssertExplicitSequencePointAllowed()
        {
            // If we are not going to emit debug info for the method we shouldn't be creating sequence points.
            Debug.Assert(method.GenerateDebugInfo);
        }
    }
}
