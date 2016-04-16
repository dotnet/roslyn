' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Reflection.Metadata
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Symbols
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGen
    Friend NotInheritable Class CodeGenerator
        Private ReadOnly _method As MethodSymbol
        Private ReadOnly _block As BoundStatement
        Private ReadOnly _builder As ILBuilder
        Private ReadOnly _module As PEModuleBuilder
        Private ReadOnly _diagnostics As DiagnosticBag
        Private ReadOnly _ilEmitStyle As ILEmitStyle
        Private ReadOnly _emitPdbSequencePoints As Boolean

        Private ReadOnly _stackLocals As HashSet(Of LocalSymbol) = Nothing

        ''' <summary> Keeps track on current nesting level of try statements </summary>
        Private _tryNestingLevel As Integer = 0

        ''' <summary> Current enclosing Catch block if there is any. </summary>
        Private _currentCatchBlock As BoundCatchBlock = Nothing

        Private ReadOnly _synthesizedLocalOrdinals As SynthesizedLocalOrdinalsDispenser = New SynthesizedLocalOrdinalsDispenser()
        Private _uniqueNameId As Integer

        ' label used when return is emitted in a form of store/goto
        Private Shared ReadOnly s_returnLabel As New Object

        Private _unhandledReturn As Boolean

        Private _checkCallsForUnsafeJITOptimization As Boolean

        Private _asyncCatchHandlerOffset As Integer = -1
        Private _asyncYieldPoints As ArrayBuilder(Of Integer) = Nothing
        Private _asyncResumePoints As ArrayBuilder(Of Integer) = Nothing

        Public Sub New(method As MethodSymbol,
                       boundBody As BoundStatement,
                       builder As ILBuilder,
                       moduleBuilder As PEModuleBuilder,
                       diagnostics As DiagnosticBag,
                       optimizations As OptimizationLevel,
                       emittingPdb As Boolean)

            Debug.Assert(method IsNot Nothing)
            Debug.Assert(boundBody IsNot Nothing)
            Debug.Assert(builder IsNot Nothing)
            Debug.Assert(moduleBuilder IsNot Nothing)
            Debug.Assert(diagnostics IsNot Nothing)

            _method = method
            _block = boundBody
            _builder = builder
            _module = moduleBuilder
            _diagnostics = diagnostics

            'Always optimize synthesized methods that don't contain user code.
            If Not method.GenerateDebugInfo Then
                _ilEmitStyle = ILEmitStyle.Release

            Else
                If optimizations = OptimizationLevel.Debug Then
                    _ilEmitStyle = ILEmitStyle.Debug
                Else
                    _ilEmitStyle = If(IsDebugPlus(),
                                        ILEmitStyle.DebugFriendlyRelease,
                                        ILEmitStyle.Release)
                End If
            End If

            ' Emit sequence points unless
            ' - the PDBs are not being generated
            ' - debug information for the method is not generated since the method does not contain
            '   user code that can be stepped through, or changed during EnC.
            ' 
            ' This setting only affects generating PDB sequence points, it shall Not affect generated IL in any way.
            _emitPdbSequencePoints = emittingPdb AndAlso method.GenerateDebugInfo

            Try
                _block = Optimizer.Optimize(method, boundBody, debugFriendly:=_ilEmitStyle <> ILEmitStyle.Release, stackLocals:=_stackLocals)
            Catch ex As BoundTreeVisitor.CancelledByStackGuardException
                ex.AddAnError(diagnostics)
                _block = boundBody
            End Try

            _checkCallsForUnsafeJITOptimization = (_method.ImplementationAttributes And MethodSymbol.DisableJITOptimizationFlags) <> MethodSymbol.DisableJITOptimizationFlags
            Debug.Assert(Not _module.JITOptimizationIsDisabled(_method))
        End Sub

        Private Function IsDebugPlus() As Boolean
            Return Me._module.Compilation.Options.DebugPlusMode
        End Function

        Public Sub Generate()
            Debug.Assert(_asyncYieldPoints Is Nothing)
            Debug.Assert(_asyncResumePoints Is Nothing)
            Debug.Assert(_asyncCatchHandlerOffset < 0)

            GenerateImpl()
        End Sub

        Public Sub Generate(<Out> ByRef asyncCatchHandlerOffset As Integer,
                            <Out> ByRef asyncYieldPoints As ImmutableArray(Of Integer),
                            <Out> ByRef asyncResumePoints As ImmutableArray(Of Integer))
            GenerateImpl()

            Debug.Assert(_asyncCatchHandlerOffset >= 0)
            asyncCatchHandlerOffset = _builder.GetILOffsetFromMarker(_asyncCatchHandlerOffset)

            Dim yieldPoints As ArrayBuilder(Of Integer) = _asyncYieldPoints
            Dim resumePoints As ArrayBuilder(Of Integer) = _asyncResumePoints

            Debug.Assert((yieldPoints Is Nothing) = (resumePoints Is Nothing))

            If yieldPoints Is Nothing Then
                asyncYieldPoints = ImmutableArray(Of Integer).Empty
                asyncResumePoints = ImmutableArray(Of Integer).Empty
            Else
                Debug.Assert(yieldPoints.Count > 0, "Why it was allocated?")

                Dim yieldPointsBuilder = ArrayBuilder(Of Integer).GetInstance
                Dim resumePointsBuilder = ArrayBuilder(Of Integer).GetInstance

                For i = 0 To yieldPoints.Count - 1
                    Dim yieldOffset = _builder.GetILOffsetFromMarker(yieldPoints(i))
                    Dim resumeOffset = _builder.GetILOffsetFromMarker(resumePoints(i))

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
            End If
        End Sub

        Private Sub GenerateImpl()
            SetInitialDebugDocument()

            ' Synthesized methods should have a sequence point
            ' at offset 0 to ensure correct stepping behavior.
            If _emitPdbSequencePoints AndAlso _method.IsImplicitlyDeclared Then
                _builder.DefineInitialHiddenSequencePoint()
            End If

            Try
                EmitStatement(_block)

                If _unhandledReturn Then
                    HandleReturn()
                End If

                If Not _diagnostics.HasAnyErrors Then
                    _builder.Realize()
                End If

            Catch e As EmitCancelledException
                Debug.Assert(_diagnostics.HasAnyErrors())
            End Try

            _synthesizedLocalOrdinals.Free()
        End Sub

        Private Sub HandleReturn()
            _builder.MarkLabel(s_returnLabel)
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
            Dim syntax = node.Syntax
            If _emitPdbSequencePoints Then
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
            Dim syntax = node.Syntax
            If _emitPdbSequencePoints Then
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
            Dim syntax = node.Syntax
            If _emitPdbSequencePoints Then
                If syntax Is Nothing Then
                    EmitHiddenSequencePoint()
                Else
                    EmitSequencePoint(syntax)
                End If
            End If

            Dim statement = node.StatementOpt
            Dim instructionsEmitted As Integer = 0
            If statement IsNot Nothing Then
                instructionsEmitted = EmitStatementAndCountInstructions(statement)
            End If

            If instructionsEmitted = 0 AndAlso syntax IsNot Nothing AndAlso _ilEmitStyle = ILEmitStyle.Debug Then
                ' if there was no code emitted, then emit nop 
                ' otherwise this point could get associated with some random statement, possibly in a wrong scope
                _builder.EmitOpCode(ILOpCode.Nop)
            End If
        End Sub

        Private Sub EmitSequencePointStatement(node As BoundSequencePointWithSpan)
            Dim span = node.Span
            If span <> Nothing AndAlso _emitPdbSequencePoints Then
                EmitSequencePoint(node.SyntaxTree, span)
            End If

            Dim statement = node.StatementOpt
            Dim instructionsEmitted As Integer = 0
            If statement IsNot Nothing Then
                instructionsEmitted = EmitStatementAndCountInstructions(statement)
            End If

            If instructionsEmitted = 0 AndAlso span <> Nothing AndAlso _ilEmitStyle = ILEmitStyle.Debug Then
                ' if there was no code emitted, then emit nop 
                ' otherwise this point could get associated with some random statement, possibly in a wrong scope
                _builder.EmitOpCode(ILOpCode.Nop)
            End If
        End Sub

        Private Sub SetInitialDebugDocument()
            Dim methodBlockSyntax = Me._method.Syntax
            If _emitPdbSequencePoints AndAlso methodBlockSyntax IsNot Nothing Then
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
            Debug.Assert(_emitPdbSequencePoints)
            _builder.DefineHiddenSequencePoint()
        End Sub

        Private Sub EmitSequencePoint(syntax As VisualBasicSyntaxNode)
            EmitSequencePoint(syntax.SyntaxTree, syntax.Span)
        End Sub

        Private Function EmitSequencePoint(tree As SyntaxTree, span As TextSpan) As TextSpan
            Debug.Assert(tree IsNot Nothing)
            Debug.Assert(_emitPdbSequencePoints)
            _builder.DefineSequencePoint(tree, span)
            Return span
        End Function
    End Class
End Namespace
