' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Semantics

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend MustInherit Class BaseVisualBasicConversionExpression
        Inherits BaseConversionExpression(Of Conversion)

        Protected Sub New(conversion As Conversion, isExplicitInCode As Boolean, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object))
            MyBase.New(conversion, isExplicitInCode, syntax, type, constantValue)
        End Sub

        Public Overrides ReadOnly Property LanguageName As String = LanguageNames.VisualBasic
    End Class

    Friend NotInheritable Class VisualBasicConversionExpression
        Inherits BaseVisualBasicConversionExpression

        Public Sub New(operand As IOperation, conversion As Conversion, isExplicitInCode As Boolean, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object))
            MyBase.New(conversion, isExplicitInCode, syntax, type, constantValue)

            Me.Operand = operand
        End Sub

        Public Overrides ReadOnly Property Operand As IOperation
    End Class

    Friend NotInheritable Class LazyVisualBasicConversionExpression
        Inherits BaseVisualBasicConversionExpression

        Private _operandLazy As Lazy(Of IOperation)

        Public Sub New(operand As Lazy(Of IOperation), conversion As Conversion, isExplicitInCode As Boolean, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object))
            MyBase.New(conversion, isExplicitInCode, syntax, type, constantValue)

            _operandLazy = operand
        End Sub

        Public Overrides ReadOnly Property Operand As IOperation = _operandLazy.Value
    End Class

    Public Module IConversionExpressionExtensions
        <Extension>
        Public Function GetVisualBasicConversion(conversionExpression As IConversionExpression) As Conversion
            If TypeOf conversionExpression Is BaseVisualBasicConversionExpression Then
                Return DirectCast(conversionExpression, BaseVisualBasicConversionExpression).ConversionInternal
            Else
                Throw New InvalidCastException(String.Format(VBResources.IConversionExpression_Is_Not_Visual_Basic_Conversion, conversionExpression))
            End If
        End Function
    End Module

End Namespace
