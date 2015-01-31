' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        Public Function EvaluateFieldConstant(field As SourceFieldSymbol, equalsValueOrAsNewNodeRef As SyntaxReference, inProgress As SymbolsInProgress(Of FieldSymbol), diagnostics As DiagnosticBag) As EvaluatedConstant
            Debug.Assert(inProgress IsNot Nothing)
            Dim value As ConstantValue = Nothing
            Dim boundValueType As TypeSymbol = Nothing
            Dim errorField = inProgress.GetStartOfCycleIfAny(field)

            If errorField IsNot Nothing Then
                diagnostics.Add(ERRID.ERR_CircularEvaluation1,
                                errorField.Locations(0),
                                CustomSymbolDisplayFormatter.ShortErrorName(errorField))
                value = ConstantValue.Bad
                boundValueType = New ErrorTypeSymbol()
            Else
                ' Set up a binder for this part of the type.
                Dim containingModule = field.ContainingSourceType.ContainingSourceModule
                Dim binder As Binder = BinderBuilder.CreateBinderForType(containingModule, equalsValueOrAsNewNodeRef.SyntaxTree, field.ContainingSourceType)
                Dim initValueSyntax As VisualBasicSyntaxNode = equalsValueOrAsNewNodeRef.GetVisualBasicSyntax()

                If initValueSyntax Is Nothing Then
                    value = ConstantValue.Bad
                    boundValueType = New ErrorTypeSymbol()
                Else
                    Dim inProgressBinder = New ConstantFieldsInProgressBinder(inProgress.Add(field), binder, field)
                    Dim constValue As ConstantValue = Nothing
                    Dim boundValue = BindFieldOrEnumInitializer(inProgressBinder, field, initValueSyntax, diagnostics, constValue)

                    ' if an untyped constant gets initialized with a nothing literal, it's type should be System.Object.
                    If boundValue.IsNothingLiteral Then
                        boundValueType = binder.GetSpecialType(SpecialType.System_Object, initValueSyntax, diagnostics)
                    Else
                        boundValueType = boundValue.Type
                    End If

                    value = If(constValue Is Nothing, ConstantValue.Bad, constValue)
                End If
                Debug.Assert(value IsNot Nothing)
            End If

            Return New EvaluatedConstant(value, boundValueType)
        End Function

        Private Function BindFieldOrEnumInitializer(binder As Binder,
                                                    fieldOrEnumSymbol As FieldSymbol,
                                                    equalsValueOrAsNewSyntax As VisualBasicSyntaxNode,
                                                    diagnostics As DiagnosticBag,
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

    End Module

End Namespace

