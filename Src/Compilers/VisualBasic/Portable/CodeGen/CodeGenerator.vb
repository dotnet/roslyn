' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Runtime.InteropServices

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGen
    Friend NotInheritable Class CodeGenerator
        Private ReadOnly _method As MethodSymbol
        Private ReadOnly _block As BoundStatement
        Private ReadOnly _builder As ILBuilder
        Private ReadOnly _module As PEModuleBuilder
        Private ReadOnly _diagnostics As DiagnosticBag

        Private ReadOnly _stackLocals As HashSet(Of LocalSymbol) = Nothing

        ''' <summary> Keeps track on current nesting level of try statements </summary>
        Private _tryNestingLevel As Integer = 0

        ''' <summary> Current enclosing Catch block if there is any. </summary>
        Private _currentCatchBlock As BoundCatchBlock = Nothing

        'native compiler does a number of special things when this flag is set.
        'TODO: consider if it still makes sense to do these things.
        Private ReadOnly _noOptimizations As Boolean

        Private ReadOnly _emitSequencePoints As Boolean

        ' label used when when return is emitted in a form of store/goto
        Private Shared ReadOnly ReturnLabel As New Object

        Private _unhandledReturn As Boolean

        Private _checkCallsForUnsafeJITOptimization As Boolean

        Private _asyncCatchHandlerOffset As Integer = -1
        Private _asyncYieldPoints As ArrayBuilder(Of Integer) = Nothing
        Private _asyncResumePoints As ArrayBuilder(Of Integer) = Nothing

        Private Sub New(meth As MethodSymbol, block As BoundStatement, builder As ILBuilder, emitModule As PEModuleBuilder, diagnostics As DiagnosticBag, optimize As Boolean, emitSequencePoints As Boolean)
            Me._method = meth
            Me._block = block
            Me._builder = builder
            Me._module = emitModule
            Me._diagnostics = diagnostics

            ' TODO: this doesn't work for
            ' - partial methods
            ' - methods with no location (global code container)

            Me._noOptimizations = Not optimize
            Me._emitSequencePoints = emitSequencePoints

            If Not Me._noOptimizations Then
                Me._block = Optimizer.Optimize(meth, block, Me._stackLocals)
            End If

            Me._checkCallsForUnsafeJITOptimization = ((Me._method.ImplementationAttributes And
                                                        MethodSymbol.DisableJITOptimizationFlags) <> MethodSymbol.DisableJITOptimizationFlags)
            Debug.Assert(Not Me._module.JITOptimizationIsDisabled(Me._method))

            Debug.Assert(meth IsNot Nothing)
            Debug.Assert(block IsNot Nothing)
            Debug.Assert(builder IsNot Nothing)
            Debug.Assert(emitModule IsNot Nothing)
        End Sub

        Public Shared Sub Run(meth As MethodSymbol,
                              block As BoundStatement,
                              builder As ILBuilder,
                              [module] As PEModuleBuilder,
                              diagnostics As DiagnosticBag,
                              optimize As Boolean,
                              emitSequencePoints As Boolean)
            Dim generator As CodeGenerator = New CodeGenerator(meth, block, builder, [module], diagnostics, optimize, emitSequencePoints)
            generator.Generate()
            Debug.Assert(generator._asyncYieldPoints Is Nothing)
            Debug.Assert(generator._asyncResumePoints Is Nothing)
            Debug.Assert(generator._asyncCatchHandlerOffset < 0)

            If Not diagnostics.HasAnyErrors Then
                builder.Realize()
            End If

        End Sub

        Public Shared Sub Run(meth As MethodSymbol,
                              block As BoundStatement,
                              builder As ILBuilder,
                              [module] As PEModuleBuilder,
                              diagnostics As DiagnosticBag,
                              optimize As Boolean,
                              emitSequencePoints As Boolean,
                              <Out> ByRef asyncCatchHandlerOffset As Integer,
                              <Out> ByRef asyncYieldPoints As ImmutableArray(Of Integer),
                              <Out> ByRef asyncResumePoints As ImmutableArray(Of Integer))
            Dim generator As CodeGenerator = New CodeGenerator(meth, block, builder, [module], diagnostics, optimize, emitSequencePoints)
            generator.Generate()

            If Not diagnostics.HasAnyErrors Then
                builder.Realize()
            End If

            asyncCatchHandlerOffset = If(generator._asyncCatchHandlerOffset < 0, -1,
                                         generator._builder.GetILOffsetFromMarker(generator._asyncCatchHandlerOffset))

            Dim yieldPoints As ArrayBuilder(Of Integer) = generator._asyncYieldPoints
            Dim resumePoints As ArrayBuilder(Of Integer) = generator._asyncResumePoints

            Debug.Assert((yieldPoints Is Nothing) = (resumePoints Is Nothing))
            If yieldPoints IsNot Nothing Then
                Debug.Assert(yieldPoints.Count > 0, "Why it was allocated?")

                Dim yieldPointsBuilder = ArrayBuilder(Of Integer).GetInstance
                Dim resumePointsBuilder = ArrayBuilder(Of Integer).GetInstance

                For i = 0 To yieldPoints.Count - 1
                    Dim yieldOffset = generator._builder.GetILOffsetFromMarker(yieldPoints(i))
                    Dim resumeOffset = generator._builder.GetILOffsetFromMarker(resumePoints(i))

                    Debug.Assert(resumeOffset >= 0) ' resume marker should always be reachable from dispatch

                    ' yield point may not be reachable if the whole 
                    ' await is not reachable; we just ignore such awaits
                    If yieldOffset > 0 Then
                        yieldPointsBuilder.Add(yieldOffset)
                        resumePointsBuilder.Add(resumeOffset)
                    End If
                Next

                asyncYieldPoints = yieldPointsBuilder.ToImmutableAndFree()
                asyncResumePoints = resumePointsBuilder.ToImmutableAndFree()

                yieldPoints.Free()
                resumePoints.Free()

            Else
                asyncYieldPoints = ImmutableArray(Of Integer).Empty
                asyncResumePoints = ImmutableArray(Of Integer).Empty
            End If

        End Sub

        Private Sub Generate()
            SetInitialDebugDocument()

            ' Synthesized methods should have a sequence point
            ' at offset 0 to ensure correct stepping behavior.
            If _emitSequencePoints AndAlso _method.IsImplicitlyDeclared Then
                _builder.DefineInitialHiddenSeqPoint()
            End If

            EmitStatement(_block)
            If _unhandledReturn Then
                HandleReturn()
            End If
        End Sub

        Private Sub HandleReturn()
            _builder.MarkLabel(ReturnLabel)
            _builder.EmitRet(True)
            _unhandledReturn = False
        End Sub

        Private Sub EmitFieldAccess(fieldAccess As BoundFieldAccess)
            ' TODO: combination load/store for +=; addresses for ref
            Dim field As FieldSymbol = fieldAccess.FieldSymbol
            If Not field.IsShared Then
                EmitExpression(fieldAccess.ReceiverOpt, True)
            End If

            If field.IsShared Then
                _builder.EmitOpCode(ILOpCode.Ldsfld)
            Else
                _builder.EmitOpCode(ILOpCode.Ldfld)
            End If
            EmitSymbolToken(field, fieldAccess.Syntax)
        End Sub

        Private Function IsStackLocal(local As LocalSymbol) As Boolean
            Return _stackLocals IsNot Nothing AndAlso _stackLocals.Contains(local)
        End Function

        Private Sub EmitLocalStore(local As BoundLocal)
            ' TODO: combination load/store for +=; addresses for ref
            Dim slot = GetLocal(local)
            _builder.EmitLocalStore(slot)
        End Sub

        Private Sub EmitSymbolToken(symbol As FieldSymbol, syntaxNode As VisualBasicSyntaxNode)
            _builder.EmitToken(_module.Translate(symbol, syntaxNode, _diagnostics), syntaxNode, _diagnostics)
        End Sub

        Private Sub EmitSymbolToken(symbol As MethodSymbol, syntaxNode As VisualBasicSyntaxNode)
            _builder.EmitToken(_module.Translate(symbol, syntaxNode, _diagnostics), syntaxNode, _diagnostics)
        End Sub

        Private Sub EmitSymbolToken(symbol As TypeSymbol, syntaxNode As VisualBasicSyntaxNode)
            _builder.EmitToken(_module.Translate(symbol, syntaxNode, _diagnostics), syntaxNode, _diagnostics)
        End Sub

        Private Sub EmitSequencePointExpression(node As BoundSequencePointExpression, used As Boolean)
            AssertExplicitSequencePointAllowed(node.Expression)

            Dim syntax = node.Syntax
            If _emitSequencePoints Then
                If syntax Is Nothing Then
                    EmitHiddenSequencePoint()
                Else
                    EmitSequencePoint(syntax)
                End If
            End If

            ' used is true to ensure that something is emitted
            EmitExpression(node.Expression, used:=True)
            EmitPopIfUnused(used)
        End Sub

        Private Sub EmitSequencePointExpressionAddress(node As BoundSequencePointExpression, addressKind As AddressKind)
            AssertExplicitSequencePointAllowed(node.Expression)

            Dim syntax = node.Syntax
            If _emitSequencePoints Then
                If syntax Is Nothing Then
                    EmitHiddenSequencePoint()
                Else
                    EmitSequencePoint(syntax)
                End If
            End If

            Dim temp = EmitAddress(node.Expression, addressKind)
            Debug.Assert(temp Is Nothing, "we should not be taking ref of a sequence if value needs a temp")
        End Sub

        Private Sub EmitSequencePointStatement(node As BoundSequencePoint)
            AssertExplicitSequencePointAllowed(node.StatementOpt)

            Dim syntax = node.Syntax
            If _emitSequencePoints Then
                If syntax Is Nothing Then
                    EmitHiddenSequencePoint()
                Else
                    EmitSequencePoint(syntax)
                End If
            End If

            Dim statement = node.StatementOpt
            Dim instructionsEmitted As Integer = 0

            If statement IsNot Nothing Then
                instructionsEmitted = -_builder.InstructionsEmitted
                EmitStatement(statement)
                instructionsEmitted += _builder.InstructionsEmitted
            End If

            If instructionsEmitted = 0 AndAlso syntax IsNot Nothing AndAlso _noOptimizations Then
                ' if there was no code emitted, then emit nop 
                ' otherwise this point could get associated with some random statement, possibly in a wrong scope
                _builder.EmitOpCode(ILOpCode.Nop)
            End If
        End Sub

        Private Sub EmitSequencePointStatement(node As BoundSequencePointWithSpan)
            AssertExplicitSequencePointAllowed(node.StatementOpt)

            Dim span = node.SequenceSpan
            If span <> Nothing AndAlso _emitSequencePoints Then
                EmitSequencePoint(node.SyntaxTree, span)
            End If

            Dim statement = node.StatementOpt
            Dim instructionsEmitted As Integer = 0

            If statement IsNot Nothing Then
                instructionsEmitted = -_builder.InstructionsEmitted
                EmitStatement(statement)
                instructionsEmitted += _builder.InstructionsEmitted
            End If

            If instructionsEmitted = 0 AndAlso span <> Nothing AndAlso _noOptimizations Then
                ' if there was no code emitted, then emit nop 
                ' otherwise this point could get associated with some random statement, possibly in a wrong scope
                _builder.EmitOpCode(ILOpCode.Nop)
            End If
        End Sub

        Private Sub SetInitialDebugDocument()
            Dim methodBlockSyntax = Me._method.Syntax
            If _emitSequencePoints AndAlso methodBlockSyntax IsNot Nothing Then
                ' If methodBlockSyntax is available (i.e. we're in a SourceMethodSymbol), then
                ' provide the IL builder with our best guess at the appropriate debug document.
                ' If we don't and this is hidden sequence point precedes all non-hidden sequence
                ' points, then the IL Builder will drop the sequence point for lack of a document.
                ' This negatively impacts the scenario where we insert hidden sequence points at
                ' the beginnings of methods so that step-into (F11) will handle them correctly.
                _builder.SetInitialDebugDocument(methodBlockSyntax.SyntaxTree)
            End If
        End Sub

        Private Sub EmitHiddenSequencePoint()
            Debug.Assert(_emitSequencePoints)
            _builder.DefineHiddenSeqPoint()
        End Sub

        Private Sub EmitSequencePoint(syntax As VisualBasicSyntaxNode)
            Dim tree = syntax.SyntaxTree
            Dim span = syntax.Span

            span = EmitSequencePoint(tree, span)
        End Sub

        Private Function EmitSequencePoint(tree As SyntaxTree, span As TextSpan) As TextSpan
            Debug.Assert(tree IsNot Nothing)
            Debug.Assert(_emitSequencePoints)
            If Not tree.IsMyTemplate Then
                _builder.DefineSeqPoint(tree, span)
            End If
            Return span
        End Function

        <Conditional("DEBUG")>
        Private Sub AssertExplicitSequencePointAllowed(underlyingNode As BoundNode)
            ' If we are not going to emit debug info for the method we shouldn't be creating sequence points.
            ' We allow them for embedded methods even though we are not emitting debug info for them to avoid complicating the code.
            ' We also allow sequence points without underlying nodes (typically hidden points).
            ' When SP wraps something caller could just have used what was wrapped, it is not so easy when there was nothing to wrap.
            Debug.Assert(_method.GenerateDebugInfo OrElse underlyingNode Is Nothing OrElse _method.IsEmbedded)
        End Sub
    End Class
End Namespace
