' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Semantics

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend MustInherit Class BaseVisualBasicConversionExpression
        Inherits BaseConversionExpression(Of Conversion)

        Protected Sub New(conversion As Conversion, isExplicitInCode As Boolean, throwsExceptionOnFailure As Boolean, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object))
            MyBase.New(conversion, isExplicitInCode, throwsExceptionOnFailure, syntax, type, constantValue)
        End Sub

        Public Overrides ReadOnly Property LanguageName As String = LanguageNames.VisualBasic
    End Class

    Friend NotInheritable Class VisualBasicConversionExpression
        Inherits BaseVisualBasicConversionExpression

        Public Sub New(operand As IOperation, conversion As Conversion, isExplicitInCode As Boolean, throwsExceptionOnFailure As Boolean, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object))
            MyBase.New(conversion, isExplicitInCode, throwsExceptionOnFailure, syntax, type, constantValue)

            Me.Operand = operand
        End Sub

        Public Overrides ReadOnly Property Operand As IOperation
    End Class

    Friend NotInheritable Class LazyVisualBasicConversionExpression
        Inherits BaseVisualBasicConversionExpression

        Private _operandLazy As Lazy(Of IOperation)

        Public Sub New(operand As Lazy(Of IOperation), conversion As Conversion, isExplicitInCode As Boolean, throwsExceptionOnFailure As Boolean, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object))
            MyBase.New(conversion, isExplicitInCode, throwsExceptionOnFailure, syntax, type, constantValue)

            _operandLazy = operand
        End Sub

        Public Overrides ReadOnly Property Operand As IOperation = _operandLazy.Value
    End Class

    Public Module IConversionExpressionExtensions
        ''' <summary>
        ''' Gets the underlying <see cref="Conversion"/> information from an <see cref="IConversionExpression"/> that was created from Visual Basic code.
        ''' </summary>
        ''' <param name="conversionExpression">The conversion expression to get original info from.</param>
        ''' <returns>The underlying <see cref="Conversion"/>.</returns>
        ''' <exception cref="InvalidCastException">If the <see cref="IConversionExpression"/> was not created from Visual Basic code.</exception>
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
