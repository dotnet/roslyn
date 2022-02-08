' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Class EvaluatedConstant
        Public Shared ReadOnly None As New EvaluatedConstant(Nothing, Nothing)

        Public Sub New(value As ConstantValue, type As TypeSymbol)
            ' If a value is provided, a corresponding type must be provided.
            Debug.Assert((value Is Nothing) OrElse value.IsNull OrElse (type IsNot Nothing))
            Me.Value = value
            Me.Type = type
        End Sub

        Public ReadOnly Value As ConstantValue
        Public ReadOnly Type As TypeSymbol
    End Class

    Friend Module ConstantValueUtils
        Public Function EvaluateFieldConstant(field As SourceFieldSymbol, equalsValueOrAsNewNodeRef As SyntaxReference, dependencies As ConstantFieldsInProgress.Dependencies, diagnostics As BindingDiagnosticBag) As EvaluatedConstant
#If DEBUG Then
            Debug.Assert(dependencies IsNot Nothing)
            Debug.Assert(equalsValueOrAsNewNodeRef IsNot Nothing)
#End If

            ' Set up a binder for this part of the type.
            Dim containingModule = field.ContainingSourceType.ContainingSourceModule
            Dim binder As Binder = BinderBuilder.CreateBinderForType(containingModule, equalsValueOrAsNewNodeRef.SyntaxTree, field.ContainingSourceType)
            Dim initValueSyntax As VisualBasicSyntaxNode = equalsValueOrAsNewNodeRef.GetVisualBasicSyntax()

            Dim inProgressBinder = New ConstantFieldsInProgressBinder(New ConstantFieldsInProgress(field, dependencies), binder, field)
            Dim constValue As ConstantValue = Nothing
            Dim boundValue = BindFieldOrEnumInitializer(inProgressBinder, field, initValueSyntax, diagnostics, constValue)

            Dim boundValueType As TypeSymbol

            ' if an untyped constant gets initialized with a nothing literal, it's type should be System.Object.
            If boundValue.IsNothingLiteral Then
                boundValueType = binder.GetSpecialType(SpecialType.System_Object, initValueSyntax, diagnostics)
            Else
                boundValueType = boundValue.Type
            End If

            Dim value As ConstantValue = If(constValue, ConstantValue.Bad)
            Return New EvaluatedConstant(value, boundValueType)
        End Function

        Private Function BindFieldOrEnumInitializer(binder As Binder,
                                                    fieldOrEnumSymbol As FieldSymbol,
                                                    equalsValueOrAsNewSyntax As VisualBasicSyntaxNode,
                                                    diagnostics As BindingDiagnosticBag,
                                                    <Out> ByRef constValue As ConstantValue) As BoundExpression

            Debug.Assert(TypeOf fieldOrEnumSymbol Is SourceEnumConstantSymbol OrElse TypeOf fieldOrEnumSymbol Is SourceFieldSymbol)

            Dim enumConstant = TryCast(fieldOrEnumSymbol, SourceEnumConstantSymbol)
            If enumConstant IsNot Nothing Then
                Return binder.BindFieldAndEnumConstantInitializer(enumConstant, equalsValueOrAsNewSyntax, isEnum:=True, diagnostics:=diagnostics, constValue:=constValue)
            Else
                Dim fieldConstant = DirectCast(fieldOrEnumSymbol, SourceFieldSymbol)
                Return binder.BindFieldAndEnumConstantInitializer(fieldConstant, equalsValueOrAsNewSyntax, isEnum:=False, diagnostics:=diagnostics, constValue:=constValue)
            End If
        End Function

        <DebuggerDisplay("{GetDebuggerDisplay(), nq}")>
        Friend Structure FieldInfo
            Public ReadOnly Field As SourceFieldSymbol
            Public ReadOnly StartsCycle As Boolean

            Public Sub New(field As SourceFieldSymbol, startsCycle As Boolean)
                Me.Field = field
                Me.StartsCycle = startsCycle
            End Sub

            Private Function GetDebuggerDisplay() As String
                Dim value = Field.ToString()
                If StartsCycle Then
                    value += " [cycle]"
                End If

                Return value
            End Function
        End Structure

    End Module

End Namespace

