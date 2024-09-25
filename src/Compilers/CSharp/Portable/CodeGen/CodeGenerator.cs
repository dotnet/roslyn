// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

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
        private readonly BindingDiagnosticBag _diagnostics;
        private readonly ILEmitStyle _ilEmitStyle;
        private readonly bool _emitPdbSequencePoints;

        private readonly HashSet<LocalSymbol> _stackLocals;

        // There are scenarios where rvalues need to be passed to ref/in parameters
        // in such cases the values must be spilled into temps and retained for the entirety of
        // the most encompassing expression.
        private ArrayBuilder<LocalDefinition> _expressionTemps;

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

        /// <summary>
        /// Used to implement <see cref="BoundSavePreviousSequencePoint"/> and <see cref="BoundRestorePreviousSequencePoint"/>.
        /// </summary>
        private PooledDictionary<object, TextSpan> _savedSequencePoints;

        private enum IndirectReturnState : byte
        {
            NotNeeded = 0,  // did not see indirect returns
            Needed = 1,  // saw indirect return and need to emit return sequence
            Emitted = 2,  // return sequence has been emitted
        }

        private LocalDefinition _returnTemp;

        /// <summary>
        /// True if there was a <see cref="ILOpCode.Localloc"/> anywhere in the method. This will
        /// affect whether or not we require the locals init flag to be marked, since locals init
        /// affects <see cref="ILOpCode.Localloc"/>.
        /// </summary>
        private bool _sawStackalloc;

        public CodeGenerator(
            MethodSymbol method,
            BoundStatement boundBody,
            ILBuilder builder,
            PEModuleBuilder moduleBuilder,
            BindingDiagnosticBag diagnostics,
            OptimizationLevel optimizations,
            bool emittingPdb)
        {
            Debug.Assert((object)method != null);
            Debug.Assert(boundBody != null);
            Debug.Assert(builder != null);
            Debug.Assert(moduleBuilder != null);
            Debug.Assert(diagnostics != null);
            Debug.Assert(diagnostics.DiagnosticBag != null);

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

            var sourceMethod = method as SourceMemberMethodSymbol;
            (BlockSyntax blockBody, ArrowExpressionClauseSyntax expressionBody) = sourceMethod?.Bodies ?? default;
            _methodBodySyntaxOpt = (SyntaxNode)blockBody ?? expressionBody ?? sourceMethod?.SyntaxNode;
        }

        private bool IsDebugPlus()
        {
            return _module.Compilation.Options.DebugPlusMode;
        }

        private bool IsPeVerifyCompatEnabled() => _module.Compilation.IsPeVerifyCompatEnabled;

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
                        int syntaxOffset = _method.CalculateLocalSyntaxOffset(LambdaUtilities.GetDeclaratorPosition(bodySyntax), bodySyntax.SyntaxTree);
                        var localSymbol = new SynthesizedLocal(_method, _method.ReturnTypeWithAnnotations, SynthesizedLocalKind.FunctionReturnValue, bodySyntax);

                        result = _builder.LocalSlotManager.DeclareLocal(
                            type: _module.Translate(localSymbol.Type, bodySyntax, _diagnostics.DiagnosticBag),
                            symbol: localSymbol,
                            name: null,
                            kind: localSymbol.SynthesizedKind,
                            id: new LocalDebugId(syntaxOffset, ordinal: 0),
                            pdbAttributes: localSymbol.SynthesizedKind.PdbAttributes(),
                            constraints: slotConstraints,
                            dynamicTransformFlags: ImmutableArray<bool>.Empty,
                            tupleElementNames: ImmutableArray<string>.Empty,
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

        internal static bool IsStackLocal(LocalSymbol local, HashSet<LocalSymbol> stackLocalsOpt)
            => stackLocalsOpt?.Contains(local) ?? false;

        private bool IsStackLocal(LocalSymbol local) => IsStackLocal(local, _stackLocals);

        public void Generate(out bool hasStackalloc)
        {
            this.GenerateImpl();
            hasStackalloc = _sawStackalloc;

            Debug.Assert(_asyncCatchHandlerOffset < 0);
            Debug.Assert(_asyncYieldPoints == null);
            Debug.Assert(_asyncResumePoints == null);
        }

        public void Generate(
            out int asyncCatchHandlerOffset,
            out ImmutableArray<int> asyncYieldPoints,
            out ImmutableArray<int> asyncResumePoints,
            out bool hasStackAlloc)
        {
            this.GenerateImpl();
            hasStackAlloc = _sawStackalloc;
            Debug.Assert(_asyncCatchHandlerOffset >= 0);

            asyncCatchHandlerOffset = _diagnostics.HasAnyErrors() ? -1 : _builder.GetILOffsetFromMarker(_asyncCatchHandlerOffset);

            ArrayBuilder<int> yieldPoints = _asyncYieldPoints;
            ArrayBuilder<int> resumePoints = _asyncResumePoints;

            Debug.Assert((yieldPoints == null) == (resumePoints == null));

            if (yieldPoints == null || _diagnostics.HasAnyErrors())
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

            Debug.Assert(!(_expressionTemps?.Count > 0), "leaking expression temps?");
            _expressionTemps?.Free();
            _savedSequencePoints?.Free();
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

        private void EmitTypeReferenceToken(Cci.ITypeReference symbol, SyntaxNode syntaxNode)
        {
            _builder.EmitToken(symbol, syntaxNode, _diagnostics.DiagnosticBag);
        }

        private void EmitSymbolToken(TypeSymbol symbol, SyntaxNode syntaxNode)
        {
            EmitTypeReferenceToken(_module.Translate(symbol, syntaxNode, _diagnostics.DiagnosticBag), syntaxNode);
        }

        private void EmitSymbolToken(MethodSymbol method, SyntaxNode syntaxNode, BoundArgListOperator optArgList, bool encodeAsRawDefinitionToken = false)
        {
            var methodRef = _module.Translate(method, syntaxNode, _diagnostics.DiagnosticBag, optArgList, needDeclaration: encodeAsRawDefinitionToken);
            _builder.EmitToken(methodRef, syntaxNode, _diagnostics.DiagnosticBag, encodeAsRawDefinitionToken ? Cci.MetadataWriter.RawTokenEncoding.RowId : 0);
        }

        private void EmitSymbolToken(FieldSymbol symbol, SyntaxNode syntaxNode)
        {
            var fieldRef = _module.Translate(symbol, syntaxNode, _diagnostics.DiagnosticBag);
            _builder.EmitToken(fieldRef, syntaxNode, _diagnostics.DiagnosticBag);
        }

        private void EmitSignatureToken(FunctionPointerTypeSymbol symbol, SyntaxNode syntaxNode)
        {
            _builder.EmitToken(_module.Translate(symbol).Signature, syntaxNode, _diagnostics.DiagnosticBag);
        }

        private void EmitSequencePointStatement(BoundSequencePoint node)
        {
            SyntaxNode syntax = node.Syntax;
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

        private void EmitSequencePointStatementBegin(BoundSequencePointWithSpan node)
        {
            TextSpan span = node.Span;
            if (span != default(TextSpan) && _emitPdbSequencePoints)
            {
                this.EmitSequencePoint(node.SyntaxTree, span);
            }
        }

        private void EmitSequencePointStatementEnd(BoundSequencePointWithSpan node, int instructionsEmitted)
        {
            TextSpan span = node.Span;
            if (instructionsEmitted == 0 && span != default(TextSpan) && _ilEmitStyle == ILEmitStyle.Debug)
            {
                // if there was no code emitted, then emit nop 
                // otherwise this point could get associated with some random statement, possibly in a wrong scope
                _builder.EmitOpCode(ILOpCode.Nop);
            }
        }

        private void EmitSequencePointStatement(BoundSequencePointWithSpan node)
        {
            EmitSequencePointStatementBegin(node);

            BoundStatement statement = node.StatementOpt;
            int instructionsEmitted = 0;
            if (statement != null)
            {
                instructionsEmitted = this.EmitStatementAndCountInstructions(statement);
            }

            EmitSequencePointStatementEnd(node, instructionsEmitted);
        }

        private void EmitSavePreviousSequencePoint(BoundSavePreviousSequencePoint statement)
        {
            if (!_emitPdbSequencePoints)
                return;

            ArrayBuilder<RawSequencePoint> sequencePoints = _builder.SeqPointsOpt;
            if (sequencePoints is null)
                return;

            for (int i = sequencePoints.Count - 1; i >= 0; i--)
            {
                var span = sequencePoints[i].Span;
                if (span == RawSequencePoint.HiddenSequencePointSpan)
                    continue;

                // Found the previous non-hidden sequence point.  Save it.
                _savedSequencePoints ??= PooledDictionary<object, TextSpan>.GetInstance();
                _savedSequencePoints.Add(statement.Identifier, span);
                return;
            }
        }

        private void EmitRestorePreviousSequencePoint(BoundRestorePreviousSequencePoint node)
        {
            Debug.Assert(node.Syntax is { });
            if (_savedSequencePoints is null || !_savedSequencePoints.TryGetValue(node.Identifier, out var span))
                return;

            EmitStepThroughSequencePoint(node.Syntax.SyntaxTree, span);
        }

        private void EmitStepThroughSequencePoint(BoundStepThroughSequencePoint node)
        {
            EmitStepThroughSequencePoint(node.Syntax.SyntaxTree, node.Span);
        }

        private void EmitStepThroughSequencePoint(SyntaxTree syntaxTree, TextSpan span)
        {
            if (!_emitPdbSequencePoints)
                return;

            var label = new object();
            // The IL builder is eager to discard unreachable code, so
            // we fool it by branching on a condition that is always true at runtime.
            _builder.EmitConstantValue(ConstantValue.Create(true));
            _builder.EmitBranch(ILOpCode.Brtrue, label);
            EmitSequencePoint(syntaxTree, span);
            _builder.EmitOpCode(ILOpCode.Nop);
            _builder.MarkLabel(label);
            EmitHiddenSequencePoint();
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

        private void EmitSequencePoint(SyntaxNode syntax)
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

        private void AddExpressionTemp(LocalDefinition temp)
        {
            // in some cases like stack locals, there is no slot allocated.
            if (temp == null)
            {
                return;
            }

            ArrayBuilder<LocalDefinition> exprTemps = _expressionTemps;
            if (exprTemps == null)
            {
                exprTemps = ArrayBuilder<LocalDefinition>.GetInstance();
                _expressionTemps = exprTemps;
            }

            Debug.Assert(!exprTemps.Contains(temp));
            exprTemps.Add(temp);
        }

        private void ReleaseExpressionTemps()
        {
            if (_expressionTemps?.Count > 0)
            {
                // release in reverse order to keep same temps on top of the temp stack if possible
                for (int i = _expressionTemps.Count - 1; i >= 0; i--)
                {
                    var temp = _expressionTemps[i];
                    FreeTemp(temp);
                }

                _expressionTemps.Clear();
            }
        }
    }
}
