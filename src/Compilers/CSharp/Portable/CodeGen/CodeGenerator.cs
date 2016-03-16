// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
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
        private readonly MethodSymbol _method;

        // Syntax of the method body (block or an expression) being emitted, 
        // or null if the method being emitted isn't a source method.
        // If we are emitting a lambda this is its body.
        private readonly SyntaxNode _methodBodySyntaxOpt;

        private readonly BoundStatement _boundBody;
        private readonly ILBuilder _builder;
        private readonly PEModuleBuilder _module;
        private readonly DiagnosticBag _diagnostics;
        private readonly ILEmitStyle _ilEmitStyle;
        private readonly bool _emitPdbSequencePoints;

        private readonly HashSet<LocalSymbol> _stackLocals;

        // not 0 when in a protected region with a handler. 
        private int _tryNestingLevel;

        private readonly SynthesizedLocalOrdinalsDispenser _synthesizedLocalOrdinals = new SynthesizedLocalOrdinalsDispenser();
        private int _uniqueNameId;

        // label used when return is emitted in a form of store/goto
        private static readonly object s_returnLabel = new object();

        private int _asyncCatchHandlerOffset = -1;
        private ArrayBuilder<int> _asyncYieldPoints;
        private ArrayBuilder<int> _asyncResumePoints;

        /// <summary>
        /// In some cases returns are handled as gotos to return epilogue.
        /// This is used to track the state of the epilogue.
        /// </summary>
        private IndirectReturnState _indirectReturnState;

        private enum IndirectReturnState : byte
        {
            NotNeeded = 0,  // did not see indirect returns
            Needed = 1,  // saw indirect return and need to emit return sequence
            Emitted = 2,  // return sequence has been emitted
        }

        private LocalDefinition _returnTemp;

        public CodeGenerator(
            MethodSymbol method,
            BoundStatement boundBody,
            ILBuilder builder,
            PEModuleBuilder moduleBuilder,
            DiagnosticBag diagnostics,
            OptimizationLevel optimizations,
            bool emittingPdb)
        {
            Debug.Assert((object)method != null);
            Debug.Assert(boundBody != null);
            Debug.Assert(builder != null);
            Debug.Assert(moduleBuilder != null);
            Debug.Assert(diagnostics != null);

            _method = method;
            _boundBody = boundBody;
            _builder = builder;
            _module = moduleBuilder;
            _diagnostics = diagnostics;

            if (!method.GenerateDebugInfo)
            {
                // Always optimize synthesized methods that don't contain user code.
                // 
                // Specifically, always optimize synthesized explicit interface implementation methods
                // (aka bridge methods) with by-ref returns because peverify produces errors if we
                // return a ref local (which the return local will be in such cases).
                _ilEmitStyle = ILEmitStyle.Release;
            }
            else
            {
                if (optimizations == OptimizationLevel.Debug)
                {
                    _ilEmitStyle = ILEmitStyle.Debug;
                }
                else
                {
                    _ilEmitStyle = IsDebugPlus() ?
                        ILEmitStyle.DebugFriendlyRelease :
                        ILEmitStyle.Release;
                }
            }

            // Emit sequence points unless
            // - the PDBs are not being generated
            // - debug information for the method is not generated since the method does not contain
            //   user code that can be stepped through, or changed during EnC.
            // 
            // This setting only affects generating PDB sequence points, it shall not affect generated IL in any way.
            _emitPdbSequencePoints = emittingPdb && method.GenerateDebugInfo;

            try
            {
                _boundBody = Optimizer.Optimize(
                    boundBody,
                    debugFriendly: _ilEmitStyle != ILEmitStyle.Release,
                    stackLocals: out _stackLocals);
            }
            catch (BoundTreeVisitor.CancelledByStackGuardException ex)
            {
                ex.AddAnError(diagnostics);
                _boundBody = boundBody;
            }

            _methodBodySyntaxOpt = (method as SourceMethodSymbol)?.BodySyntax;
        }

        private bool IsDebugPlus()
        {
            return _module.Compilation.Options.DebugPlusMode;
        }

        private LocalDefinition LazyReturnTemp
        {
            get
            {
                var result = _returnTemp;
                if (result == null)
                {
                    Debug.Assert(!_method.ReturnsVoid, "returning something from void method?");
                    var slotConstraints = _method.RefKind == RefKind.None
                        ? LocalSlotConstraints.None
                        : LocalSlotConstraints.ByRef;


                    var bodySyntax = _methodBodySyntaxOpt;
                    if (_ilEmitStyle == ILEmitStyle.Debug && bodySyntax != null)
                    {
                        int syntaxOffset = _method.CalculateLocalSyntaxOffset(bodySyntax.SpanStart, bodySyntax.SyntaxTree);
                        var localSymbol = new SynthesizedLocal(_method, _method.ReturnType, SynthesizedLocalKind.FunctionReturnValue, bodySyntax);

                        result = _builder.LocalSlotManager.DeclareLocal(
                            type: _module.Translate(localSymbol.Type, bodySyntax, _diagnostics),
                            symbol: localSymbol,
                            name: null,
                            kind: localSymbol.SynthesizedKind,
                            id: new LocalDebugId(syntaxOffset, ordinal: 0),
                            pdbAttributes: localSymbol.SynthesizedKind.PdbAttributes(),
                            constraints: slotConstraints,
                            isDynamic: false,
                            dynamicTransformFlags: ImmutableArray<TypedConstant>.Empty,
                            isSlotReusable: false);
                    }
                    else
                    {
                        result = AllocateTemp(_method.ReturnType, _boundBody.Syntax, slotConstraints);
                    }

                    _returnTemp = result;
                }
                return result;
            }
        }

        private bool IsStackLocal(LocalSymbol local)
        {
            return _stackLocals != null && _stackLocals.Contains(local);
        }

        public void Generate()
        {
            this.GenerateImpl();

            Debug.Assert(_asyncCatchHandlerOffset < 0);
            Debug.Assert(_asyncYieldPoints == null);
            Debug.Assert(_asyncResumePoints == null);
        }

        public void Generate(out int asyncCatchHandlerOffset, out ImmutableArray<int> asyncYieldPoints, out ImmutableArray<int> asyncResumePoints)
        {
            this.GenerateImpl();
            Debug.Assert(_asyncCatchHandlerOffset >= 0);

            asyncCatchHandlerOffset = _builder.GetILOffsetFromMarker(_asyncCatchHandlerOffset);

            ArrayBuilder<int> yieldPoints = _asyncYieldPoints;
            ArrayBuilder<int> resumePoints = _asyncResumePoints;

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
                    int yieldOffset = _builder.GetILOffsetFromMarker(yieldPoints[i]);
                    int resumeOffset = _builder.GetILOffsetFromMarker(resumePoints[i]);
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
            if (_emitPdbSequencePoints && _method.IsImplicitlyDeclared)
            {
                _builder.DefineInitialHiddenSequencePoint();
            }

            try
            {
                EmitStatement(_boundBody);

                if (_indirectReturnState == IndirectReturnState.Needed)
                {
                    // it is unfortunate that return was not handled while we were in scope of the method
                    // it can happen in rare cases involving exception handling (for example all returns were from a try)
                    // in such case we can still handle return here.
                    HandleReturn();
                }

                if (!_diagnostics.HasAnyErrors())
                {
                    _builder.Realize();
                }
            }
            catch (EmitCancelledException)
            {
                Debug.Assert(_diagnostics.HasAnyErrors());
            }

            _synthesizedLocalOrdinals.Free();
        }

        private void HandleReturn()
        {
            _builder.MarkLabel(s_returnLabel);

            Debug.Assert(_method.ReturnsVoid == (_returnTemp == null));

            if (_emitPdbSequencePoints && !_method.IsIterator && !_method.IsAsync)
            {
                // In debug mode user could set a breakpoint on the last "}" of the method and 
                // expect to hit it before exiting the method.
                // We do it by rewriting all returns into a jump to an Exit label 
                // and mark the Exit sequence with sequence point for the span of the last "}".
                BlockSyntax blockSyntax = _methodBodySyntaxOpt as BlockSyntax;
                if (blockSyntax != null)
                {
                    EmitSequencePoint(blockSyntax.SyntaxTree, blockSyntax.CloseBraceToken.Span);
                }
            }

            if (_returnTemp != null)
            {
                _builder.EmitLocalLoad(LazyReturnTemp);
                _builder.EmitRet(false);
            }
            else
            {
                _builder.EmitRet(true);
            }

            _indirectReturnState = IndirectReturnState.Emitted;
        }

        private void EmitSymbolToken(TypeSymbol symbol, CSharpSyntaxNode syntaxNode)
        {
            _builder.EmitToken(_module.Translate(symbol, syntaxNode, _diagnostics), syntaxNode, _diagnostics);
        }

        private void EmitSymbolToken(MethodSymbol method, CSharpSyntaxNode syntaxNode, BoundArgListOperator optArgList, bool encodeAsRawToken = false)
        {
            _builder.EmitToken(_module.Translate(method, syntaxNode, _diagnostics, optArgList), syntaxNode, _diagnostics, encodeAsRawToken);
        }

        private void EmitSymbolToken(FieldSymbol symbol, CSharpSyntaxNode syntaxNode)
        {
            _builder.EmitToken(_module.Translate(symbol, syntaxNode, _diagnostics), syntaxNode, _diagnostics);
        }

        private void EmitSequencePointStatement(BoundSequencePoint node)
        {
            CSharpSyntaxNode syntax = node.Syntax;
            if (_emitPdbSequencePoints)
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

            if (instructionsEmitted == 0 && syntax != null && _ilEmitStyle == ILEmitStyle.Debug)
            {
                // if there was no code emitted, then emit nop 
                // otherwise this point could get associated with some random statement, possibly in a wrong scope
                _builder.EmitOpCode(ILOpCode.Nop);
            }
        }

        private void EmitSequencePointStatement(BoundSequencePointWithSpan node)
        {
            TextSpan span = node.Span;
            if (span != default(TextSpan) && _emitPdbSequencePoints)
            {
                this.EmitSequencePoint(node.SyntaxTree, span);
            }

            BoundStatement statement = node.StatementOpt;
            int instructionsEmitted = 0;
            if (statement != null)
            {
                instructionsEmitted = this.EmitStatementAndCountInstructions(statement);
            }

            if (instructionsEmitted == 0 && span != default(TextSpan) && _ilEmitStyle == ILEmitStyle.Debug)
            {
                // if there was no code emitted, then emit nop 
                // otherwise this point could get associated with some random statement, possibly in a wrong scope
                _builder.EmitOpCode(ILOpCode.Nop);
            }
        }

        private void SetInitialDebugDocument()
        {
            if (_emitPdbSequencePoints && _methodBodySyntaxOpt != null)
            {
                // If methodBlockSyntax is available (i.e. we're in a SourceMethodSymbol), then
                // provide the IL builder with our best guess at the appropriate debug document.
                // If we don't and this is hidden sequence point precedes all non-hidden sequence
                // points, then the IL Builder will drop the sequence point for lack of a document.
                // This negatively impacts the scenario where we insert hidden sequence points at
                // the beginnings of methods so that step-into (F11) will handle them correctly.
                _builder.SetInitialDebugDocument(_methodBodySyntaxOpt.SyntaxTree);
            }
        }

        private void EmitHiddenSequencePoint()
        {
            Debug.Assert(_emitPdbSequencePoints);
            _builder.DefineHiddenSequencePoint();
        }

        private void EmitSequencePoint(CSharpSyntaxNode syntax)
        {
            EmitSequencePoint(syntax.SyntaxTree, syntax.Span);
        }

        private TextSpan EmitSequencePoint(SyntaxTree syntaxTree, TextSpan span)
        {
            Debug.Assert(syntaxTree != null);
            Debug.Assert(_emitPdbSequencePoints);

            _builder.DefineSequencePoint(syntaxTree, span);
            return span;
        }
    }
}
