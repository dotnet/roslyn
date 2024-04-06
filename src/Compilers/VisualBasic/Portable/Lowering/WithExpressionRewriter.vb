' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class WithExpressionRewriter
        Private ReadOnly _withSyntax As WithStatementSyntax

        Public Structure Result
            Public Sub New(expression As BoundExpression, locals As ImmutableArray(Of LocalSymbol), initializers As ImmutableArray(Of BoundExpression), capturedLvalueByRefCallOrProperty As BoundExpression)
                Me.Expression = expression
                Me.Locals = locals
                Me.Initializers = initializers
                Me.CapturedLvalueByRefCallOrProperty = capturedLvalueByRefCallOrProperty
            End Sub

            ''' <summary> Expression to be used instead of With statement expression placeholder </summary>
            Public ReadOnly Expression As BoundExpression

            ''' <summary> Locals being used </summary>
            Public ReadOnly Locals As ImmutableArray(Of LocalSymbol)

            ''' <summary> Locals initialization expressions </summary>
            Public ReadOnly Initializers As ImmutableArray(Of BoundExpression)

            Public ReadOnly CapturedLvalueByRefCallOrProperty As BoundExpression
        End Structure

        Friend Sub New(withSyntax As WithStatementSyntax)
            Me._withSyntax = withSyntax
        End Sub

#Region "State"

        Private Class State
            Public ReadOnly ContainingMember As Symbol
            Public ReadOnly DoNotUseByRefLocal As Boolean
            Public ReadOnly Binder As Binder
            Public ReadOnly PreserveIdentityOfLValues As Boolean
            Public ReadOnly IsDraftRewrite As Boolean

            Private _locals As ArrayBuilder(Of LocalSymbol) = Nothing
            Private _initializers As ArrayBuilder(Of BoundExpression) = Nothing
            Public _capturedLvalueByRefCallOrProperty As BoundExpression = Nothing

            Public Sub New(containingMember As Symbol, doNotUseByRefLocal As Boolean, binder As Binder, preserveIdentityOfLValues As Boolean, isDraftRewrite As Boolean)
                Me.ContainingMember = containingMember
                Me.DoNotUseByRefLocal = doNotUseByRefLocal
                Me.Binder = binder
                Me.PreserveIdentityOfLValues = preserveIdentityOfLValues
                Me.IsDraftRewrite = isDraftRewrite
            End Sub

            Public Sub AddLocal(local As LocalSymbol, initializer As BoundExpression)
                Debug.Assert(local IsNot Nothing)
                Debug.Assert(initializer IsNot Nothing)

                If Me._locals Is Nothing Then
                    Debug.Assert(Me._initializers Is Nothing)

                    Me._locals = ArrayBuilder(Of LocalSymbol).GetInstance()
                    Me._initializers = ArrayBuilder(Of BoundExpression).GetInstance()
                End If

                Me._locals.Add(local)
                Me._initializers.Add(initializer)
            End Sub

            Public Function CreateResult(expression As BoundExpression) As Result
                Return New Result(expression,
                                  If(Me._locals Is Nothing, ImmutableArray(Of LocalSymbol).Empty, Me._locals.ToImmutableAndFree()),
                                  If(Me._initializers Is Nothing, ImmutableArray(Of BoundExpression).Empty, Me._initializers.ToImmutableAndFree()),
                                  Me._capturedLvalueByRefCallOrProperty)

            End Function
        End Class

#End Region

#Region "Implementation"

        Private Function CaptureInATemp(value As BoundExpression, state As State) As BoundLocal
            Dim type As TypeSymbol = value.Type
            Debug.Assert(type IsNot Nothing AndAlso Not type.IsVoidType())

            Dim local As New SynthesizedLocal(state.ContainingMember, type, SynthesizedLocalKind.With, _withSyntax)

            Dim boundLocal = New BoundLocal(value.Syntax, local, isLValue:=True, type:=type).MakeCompilerGenerated()

            If value.IsLValue Then
                ' Region analysis depends on node identity, let's preserve it by using a wrapper.
                If state.PreserveIdentityOfLValues Then
                    value = New BoundLValueToRValueWrapper(value.Syntax, value, value.Type).MakeCompilerGenerated()
                Else
                    value = value.MakeRValue()
                End If
            End If
            state.AddLocal(local,
                            New BoundAssignmentOperator(
                                value.Syntax, boundLocal, value, suppressObjectClone:=True, type:=type).MakeCompilerGenerated())

            Return boundLocal
        End Function

        Private Function CaptureInAByRefTemp(value As BoundExpression, state As State) As BoundExpression
            Debug.Assert(value.IsLValue())

            Dim type As TypeSymbol = value.Type
            Debug.Assert(type IsNot Nothing AndAlso Not type.IsVoidType())

            Dim local As New SynthesizedLocal(state.ContainingMember, type, SynthesizedLocalKind.With, _withSyntax, isByRef:=True)

            Dim boundLocal = New BoundLocal(value.Syntax, local, isLValue:=True, type:=type).MakeCompilerGenerated()

            state.AddLocal(local,
                            New BoundReferenceAssignment(
                                value.Syntax, boundLocal, value, True, type:=type).MakeCompilerGenerated())

            Return boundLocal
        End Function

        Private Function CaptureArrayAccess(value As BoundArrayAccess, state As State) As BoundExpression
            Debug.Assert(value.IsLValue)

            Dim boundArrayTemp As BoundExpression = CaptureInATemp(value.Expression, state).MakeRValue()

            Dim n = value.Indices.Length
            Dim indices(n - 1) As BoundExpression
            For i = 0 To n - 1
                indices(i) = CaptureRValue(value.Indices(i), state)
            Next

            Return value.Update(boundArrayTemp, indices.AsImmutableOrNull(), value.IsLValue, value.Type)
        End Function

        Private Function CaptureRValue(value As BoundExpression, state As State) As BoundExpression
            Dim kind As BoundKind = value.Kind

            Select Case kind
                Case BoundKind.BadVariable,
                     BoundKind.Literal,
                     BoundKind.MeReference,
                     BoundKind.MyClassReference,
                     BoundKind.MyBaseReference

                    Return value
            End Select

            If value.IsValue AndAlso value.Type IsNot Nothing AndAlso Not value.Type.IsVoidType() Then

                Debug.Assert(Not value.IsLValue)

                Dim constantValue As ConstantValue = value.ConstantValueOpt

                If constantValue IsNot Nothing Then
                    Debug.Assert(value.Kind <> BoundKind.Literal)
                    Return value
                End If

                ' TODO: Might need to do some optimization for compiler generated locals.
                '       For example, no reason to recapture a local that is already a capture.
                Return CaptureInATemp(value, state).MakeRValue()
            End If

            Throw ExceptionUtilities.Unreachable
        End Function

        Private Function CaptureFieldAccess(value As BoundFieldAccess, state As State) As BoundExpression
            Debug.Assert(value.IsLValue)

            Dim fieldSymbol = value.FieldSymbol
            If fieldSymbol.IsShared AndAlso value.ReceiverOpt IsNot Nothing Then
                Return value.Update(Nothing, fieldSymbol, value.IsLValue, value.SuppressVirtualCalls, value.ConstantsInProgressOpt, value.Type)

            ElseIf value.ReceiverOpt Is Nothing Then
                Return value

            Else
                Dim receiver As BoundExpression = CaptureReceiver(value.ReceiverOpt, state)
                Return value.Update(receiver, fieldSymbol, value.IsLValue, value.SuppressVirtualCalls, value.ConstantsInProgressOpt, value.Type)
            End If

        End Function

        Private Function CaptureReceiver(value As BoundExpression, state As State) As BoundExpression
            Debug.Assert(value IsNot Nothing)

            If value.IsLValue AndAlso value.Type.IsReferenceType Then
                Return CaptureInATemp(value, state)
            Else
                Return CaptureExpression(value, state)
            End If
        End Function

        Private Function CaptureExpression(value As BoundExpression, state As State) As BoundExpression
            If Not value.IsLValue Then
                Return CaptureRValue(value, state)
            End If

            Select Case value.Kind
                Case BoundKind.ArrayAccess
                    Return CaptureArrayAccess(DirectCast(value, BoundArrayAccess), state)

                Case BoundKind.FieldAccess
                    Return CaptureFieldAccess(DirectCast(value, BoundFieldAccess), state)

                Case BoundKind.Local,
                     BoundKind.Parameter
                    Return value

                Case BoundKind.WithLValueExpressionPlaceholder
                    ' NOTE: this may only happen in case of calling this rewriter from initial 
                    '       binding, in lowering phase all placeholders must already be substituted

                    ' We need to replace the placeholder with 'draft substitute' to make sure
                    ' we properly analyze it in flow analysis. This is important, for example, 
                    ' if we pass the original value typed local to substitute like in the following
                    ' example:
                    '
                    '       Dim s As New StructureType(...)
                    '       With s
                    '           .Field1 = 1
                    '           ...
                    '
                    ' Note that we cannot just replace the placeholder with the 'original' expression
                    ' because we should predict and use the expression which will actually be used 
                    ' in lowering, for example in the following scenario:
                    '
                    '       With {expr}
                    '           With .Field
                    '               ...
                    '
                    ' {expr} is of reference type we don't want to replace the placeholder with the 
                    ' outer expression, because otherwise it will get to initializers of the nested 
                    ' With statement and will take part in flow analysis
                    '
                    ' Try and get the substitute from the binder, or leave placeholder 'as-is' to 
                    ' be replaced with proper substitute by flow analysis code when needed
                    Dim substitute As BoundExpression =
                        state.Binder.GetWithStatementPlaceholderSubstitute(DirectCast(value, BoundValuePlaceholderBase))
                    Return If(substitute IsNot Nothing, CaptureExpression(substitute, state), value)

                Case Else
                    Return CaptureRValue(value, state)
            End Select
        End Function

#End Region

        ''' <summary>
        ''' Given an expression specified for With statement produces:
        '''   1) Expression - an expression to be used instead of expression placeholder
        '''   2) Locals - a set of locals used to capture parts of Expression
        '''   3) Initializers - initializers for Locals
        ''' 
        ''' To be used in With statement only!
        ''' </summary>
        Public Function AnalyzeWithExpression(
            containingMember As Symbol,
            value As BoundExpression,
            doNotUseByRefLocal As Boolean,
            isDraftRewrite As Boolean,
            binder As Binder,
            Optional preserveIdentityOfLValues As Boolean = False
        ) As Result
            Dim state As New State(containingMember, doNotUseByRefLocal, binder, preserveIdentityOfLValues, isDraftRewrite)
            Return state.CreateResult(CaptureWithExpression(value, state))
        End Function

        Private Function CaptureWithExpression(value As BoundExpression, state As State) As BoundExpression
            Dim type As TypeSymbol = value.Type
            Debug.Assert(type IsNot Nothing)

            Dim kind = value.Kind

            If kind = BoundKind.MeReference OrElse kind = BoundKind.MyClassReference OrElse kind = BoundKind.MyBaseReference Then
                ' Me reference can simply be reused instead of placeholder
                ' NOTE: MyClass & MyBase references may only get here in erroneous scenarios
                Return value
            End If

            If type.IsReferenceType Then
                ' Expressions of reference type are to be captured using a simple local
                Dim result As BoundLocal = CaptureInATemp(value, state)

                If Not value.IsLValue Then
                    result = result.MakeRValue()
                End If

                Return result
            End If

            ' NOTE: Only structures and type parameters should reach this point
            Debug.Assert(value.Type.IsStructureType OrElse value.Type.IsTypeParameter)

            If Not value.IsLValue() Then
                ' All R-value value typed and type parameter typed 
                ' expressions should be captured in simple locals
                Return CaptureInATemp(value, state).MakeRValue()
            End If

            ' NOTE: Only L-value expressions of value type or type parameter type

            If kind = BoundKind.Local OrElse kind = BoundKind.Parameter Then
                ' Locals and parameters of value or type parameter type can 
                ' simply be reused instead of placeholder
                Debug.Assert(type.IsValueType OrElse type.IsTypeParameter)
                Return value
            End If

            ' If the expression is to be captured in lambda do not capture in a ref local.
            ' If the expression is a generic array element, getting a writable reference may fail
            ' and readonly reference cannot be stored in a temp, so do not capture in a ref.
            If Not (state.DoNotUseByRefLocal OrElse (value.Kind = BoundKind.ArrayAccess AndAlso value.Type.Kind = SymbolKind.TypeParameter)) Then
                Return CaptureInAByRefTemp(value, state)
            End If

            ' Otherwise, we need to capture parts of the expression in a set of non-ByRef locals 
            Dim expression As BoundExpression = Nothing
            Select Case value.Kind
                Case BoundKind.ArrayAccess
                    expression = CaptureArrayAccess(DirectCast(value, BoundArrayAccess), state)

                Case BoundKind.FieldAccess
                    expression = CaptureFieldAccess(DirectCast(value, BoundFieldAccess), state)

                Case BoundKind.PropertyAccess
                    If Not state.IsDraftRewrite OrElse Not DirectCast(value, BoundPropertyAccess).PropertySymbol.ReturnsByRef Then
                        Throw ExceptionUtilities.UnexpectedValue(value.Kind)
                    End If

                    Debug.Assert(state.DoNotUseByRefLocal)
                    If state._capturedLvalueByRefCallOrProperty Is Nothing Then
                        state._capturedLvalueByRefCallOrProperty = value
                    End If

                    expression = CaptureInATemp(value, state) ' Capture by value for the purpose of draft rewrite

                Case BoundKind.Call
                    If Not state.IsDraftRewrite OrElse Not DirectCast(value, BoundCall).Method.ReturnsByRef Then
                        Throw ExceptionUtilities.UnexpectedValue(value.Kind)
                    End If

                    Debug.Assert(state.DoNotUseByRefLocal)
                    If state._capturedLvalueByRefCallOrProperty Is Nothing Then
                        state._capturedLvalueByRefCallOrProperty = value
                    End If

                    expression = CaptureInATemp(value, state) ' Capture by value for the purpose of draft rewrite

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(value.Kind)
            End Select

            ' LValue-ness of expressions must be preserved
            Debug.Assert(expression IsNot Nothing)
            Debug.Assert(expression.IsLValue)
            Return expression
        End Function

    End Class

End Namespace
