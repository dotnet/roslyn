' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend NotInheritable Class AsyncRewriter
        Inherits StateMachineRewriter(Of CapturedSymbolOrExpression)

        Friend MustInherit Class CapturedSymbolOrExpression
            ''' <summary>
            ''' Materialize the capture, e.g. return an expression to be used instead of captured symbol
            ''' </summary>
            Friend MustOverride Function Materialize(rewriter As AsyncRewriter.AsyncMethodToClassRewriter, isLValue As Boolean) As BoundExpression

            ''' <summary>
            ''' Add proxy field(s) if any to the array builder provided
            ''' 
            ''' Note: is used for binding BoundStateMachineScope with 
            '''       correspondent local/field references
            ''' </summary>
            Friend Overridable Sub AddProxyFieldsForStateMachineScope(proxyFields As ArrayBuilder(Of FieldSymbol))
                Debug.Assert(False, "This method should not be called for " + Me.GetType.Name)
            End Sub

            ''' <summary>
            ''' Create assignment expressions initializing for this capture, is only supposed to be
            ''' used for proper handling of reference assignments
            ''' </summary>
            Friend MustOverride Sub CreateCaptureInitializationCode(rewriter As AsyncRewriter.AsyncMethodToClassRewriter, prologue As ArrayBuilder(Of BoundExpression))
        End Class

        Private Class CapturedConstantExpression
            Inherits CapturedSymbolOrExpression

            Private ReadOnly _constValue As ConstantValue
            Private ReadOnly _type As TypeSymbol

            Public Sub New(constValue As ConstantValue, type As TypeSymbol)
                Debug.Assert(constValue IsNot Nothing)
                Debug.Assert(type IsNot Nothing)
                Me._constValue = constValue
                Me._type = type
            End Sub

            Friend Overloads Overrides Sub CreateCaptureInitializationCode(rewriter As AsyncRewriter.AsyncMethodToClassRewriter, prologue As ArrayBuilder(Of BoundExpression))
            End Sub

            Friend Overloads Overrides Function Materialize(rewriter As AsyncMethodToClassRewriter, isLValue As Boolean) As BoundExpression
                Debug.Assert(Not isLValue)
                Return New BoundLiteral(rewriter.F.Syntax, Me._constValue, Me._type)
            End Function
        End Class

        Private MustInherit Class SingleFieldCapture
            Inherits CapturedSymbolOrExpression

            Friend ReadOnly Field As FieldSymbol

            Public Sub New(field As FieldSymbol)
                Debug.Assert(field IsNot Nothing)
                Me.Field = field
            End Sub

            Friend Overloads Overrides Function Materialize(rewriter As AsyncRewriter.AsyncMethodToClassRewriter, isLValue As Boolean) As BoundExpression
                Dim syntax As SyntaxNode = rewriter.F.Syntax
                Dim framePointer As BoundExpression = rewriter.FramePointer(syntax, Me.Field.ContainingType)
                Dim proxyFieldParented = Me.Field.AsMember(DirectCast(framePointer.Type, NamedTypeSymbol))
                Return rewriter.F.Field(framePointer, proxyFieldParented, isLValue)
            End Function
        End Class

        Private Class CapturedParameterSymbol
            Inherits SingleFieldCapture

            Public Sub New(field As FieldSymbol)
                MyBase.New(field)
            End Sub

            Friend Overloads Overrides Sub CreateCaptureInitializationCode(rewriter As AsyncRewriter.AsyncMethodToClassRewriter, prologue As ArrayBuilder(Of BoundExpression))
                ' Don't anything, parameters' proxy fields should only be initialized once
                ' and it is done in AsyncRewriter.InitializeParameterWithProxy
            End Sub
        End Class

        Private Class CapturedLocalSymbol
            Inherits SingleFieldCapture

            Friend ReadOnly Local As LocalSymbol

            Public Sub New(field As FieldSymbol, local As LocalSymbol)
                MyBase.New(field)
                Me.Local = local
            End Sub

            Friend Overrides Sub AddProxyFieldsForStateMachineScope(proxyFields As ArrayBuilder(Of FieldSymbol))
                proxyFields.Add(Me.Field)
            End Sub

            Friend Overloads Overrides Sub CreateCaptureInitializationCode(rewriter As AsyncRewriter.AsyncMethodToClassRewriter, prologue As ArrayBuilder(Of BoundExpression))
                ' Don't anything, those fields are supposed to replace local in all the method's code
            End Sub
        End Class

        Private Class CapturedRValueExpression
            Inherits SingleFieldCapture

            Friend ReadOnly Expression As BoundExpression

            Public Sub New(field As FieldSymbol, expr As BoundExpression)
                MyBase.New(field)

                Debug.Assert(Not expr.IsLValue)
                Me.Expression = expr
            End Sub

            Friend Overloads Overrides Sub CreateCaptureInitializationCode(rewriter As AsyncRewriter.AsyncMethodToClassRewriter, prologue As ArrayBuilder(Of BoundExpression))
                ' Initialize with expression
                prologue.Add(
                    rewriter.ProcessRewrittenAssignmentOperator(
                        rewriter.F.AssignmentExpression(
                            Me.Materialize(rewriter, True),
                            rewriter.VisitExpression(Expression))))
            End Sub
        End Class

        Private Class CapturedFieldAccessExpression
            Inherits CapturedSymbolOrExpression

            Friend ReadOnly ReceiverOpt As CapturedSymbolOrExpression
            Friend ReadOnly Field As FieldSymbol

            Public Sub New(receiverOpt As CapturedSymbolOrExpression, field As FieldSymbol)
                Debug.Assert((field.IsShared) = (receiverOpt Is Nothing))
                Me.ReceiverOpt = receiverOpt
                Me.Field = field
            End Sub

            Friend Overloads Overrides Sub CreateCaptureInitializationCode(rewriter As AsyncRewriter.AsyncMethodToClassRewriter, prologue As ArrayBuilder(Of BoundExpression))
                If Me.ReceiverOpt IsNot Nothing Then
                    Me.ReceiverOpt.CreateCaptureInitializationCode(rewriter, prologue)
                End If
            End Sub

            Friend Overloads Overrides Function Materialize(rewriter As AsyncMethodToClassRewriter, isLValue As Boolean) As BoundExpression
                Dim newReceiverOpt As BoundExpression = Nothing
                If Me.ReceiverOpt IsNot Nothing Then
                    newReceiverOpt = Me.ReceiverOpt.Materialize(rewriter, Me.Field.ContainingType.IsValueType)
                End If
                Return rewriter.F.Field(newReceiverOpt, rewriter.VisitFieldSymbol(Me.Field), isLValue)
            End Function
        End Class

        Private Class CapturedArrayAccessExpression
            Inherits CapturedSymbolOrExpression

            Friend ReadOnly ArrayPointer As CapturedSymbolOrExpression
            Friend ReadOnly Indices As ImmutableArray(Of CapturedSymbolOrExpression)

            Public Sub New(arrayPointer As CapturedSymbolOrExpression, indices As ImmutableArray(Of CapturedSymbolOrExpression))
                Debug.Assert(arrayPointer IsNot Nothing)
                Debug.Assert(Not indices.IsEmpty)

                Me.ArrayPointer = arrayPointer
                Me.Indices = indices
            End Sub

            Friend Overloads Overrides Sub CreateCaptureInitializationCode(rewriter As AsyncRewriter.AsyncMethodToClassRewriter, prologue As ArrayBuilder(Of BoundExpression))
                Me.ArrayPointer.CreateCaptureInitializationCode(rewriter, prologue)
                For Each index In Me.Indices
                    index.CreateCaptureInitializationCode(rewriter, prologue)
                Next
            End Sub

            Friend Overloads Overrides Function Materialize(rewriter As AsyncMethodToClassRewriter, isLValue As Boolean) As BoundExpression
                Dim origIndices As ImmutableArray(Of CapturedSymbolOrExpression) = Me.Indices
                Dim indicesCount As Integer = origIndices.Length

                Dim indices(indicesCount - 1) As BoundExpression
                For i = 0 To indicesCount - 1
                    indices(i) = origIndices(i).Materialize(rewriter, False)
                Next

                Dim arrayPointer As BoundExpression = Me.ArrayPointer.Materialize(rewriter, False)
                Dim arrayElementType As TypeSymbol = DirectCast(arrayPointer.Type, ArrayTypeSymbol).ElementType

                Return rewriter.F.ArrayAccess(arrayPointer, isLValue, indices)
            End Function
        End Class

    End Class

End Namespace
