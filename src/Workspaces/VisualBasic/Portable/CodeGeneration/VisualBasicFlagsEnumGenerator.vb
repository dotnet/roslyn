' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend Class VisualBasicFlagsEnumGenerator
        Inherits AbstractFlagsEnumGenerator

        Public Shared ReadOnly Instance As VisualBasicFlagsEnumGenerator = New VisualBasicFlagsEnumGenerator
        Private Shared ReadOnly s_syntaxGeneratorInstance As SyntaxGenerator = New VisualBasicSyntaxGenerator

        Private Sub New()
        End Sub

        Protected Overrides Function CreateExplicitlyCastedLiteralValue(enumType As INamedTypeSymbol,
                                                                        underlyingSpecialType As SpecialType,
                                                                        constantValue As Object) As SyntaxNode
            Dim expression = ExpressionGenerator.GenerateNonEnumValueExpression(
                enumType.EnumUnderlyingType, constantValue, canUseFieldReference:=True)
            Dim constantValueULong = EnumUtilities.ConvertEnumUnderlyingTypeToUInt64(constantValue, underlyingSpecialType)
            If constantValueULong = 0 Then
                Return expression
            End If

            Dim factory = New VisualBasicSyntaxGenerator()
            Return factory.ConvertExpression(enumType, expression)
        End Function

        Protected Overrides Function GetSyntaxGenerator() As SyntaxGenerator
            Return s_syntaxGeneratorInstance
        End Function

        Protected Overrides Function IsValidName(enumType As INamedTypeSymbol, name As String) As Boolean
            If name = "_" Then
                Return False
            End If

            If Not SyntaxFacts.IsValidIdentifier(name) Then
                Return False
            End If

            Return enumType.GetMembers(name).Length = 1
        End Function
    End Class
End Namespace
