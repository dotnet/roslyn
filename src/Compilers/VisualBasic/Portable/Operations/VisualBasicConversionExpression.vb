' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Semantics

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend MustInherit Class BaseVisualBasicConversionExpression
        Inherits BaseConversionExpression

        Protected Sub New(conversion As Conversion, isExplicitInCode As Boolean, isTryCast As Boolean, isChecked As Boolean, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object))
            MyBase.New(isExplicitInCode, isTryCast, isChecked, syntax, type, constantValue)

            ConversionInternal = conversion
        End Sub

        Friend ReadOnly Property ConversionInternal As Conversion

        Public Overrides ReadOnly Property Conversion As CommonConversion = ConversionInternal.ToCommonConversion()

        Public Overrides ReadOnly Property LanguageName As String = LanguageNames.VisualBasic
    End Class

    Friend NotInheritable Class VisualBasicConversionExpression
        Inherits BaseVisualBasicConversionExpression

        Public Sub New(operand As IOperation, conversion As Conversion, isExplicitInCode As Boolean, isTryCast As Boolean, isChecked As Boolean, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object))
            MyBase.New(conversion, isExplicitInCode, isTryCast, isChecked, syntax, type, constantValue)

            Me.Operand = operand
        End Sub

        Public Overrides ReadOnly Property Operand As IOperation
    End Class

    Friend NotInheritable Class LazyVisualBasicConversionExpression
        Inherits BaseVisualBasicConversionExpression

        Private ReadOnly _operandLazy As Lazy(Of IOperation)

        Public Sub New(operand As Lazy(Of IOperation), conversion As Conversion, isExplicitInCode As Boolean, isTryCast As Boolean, isChecked As Boolean, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object))
            MyBase.New(conversion, isExplicitInCode, isTryCast, isChecked, syntax, type, constantValue)

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
            Dim basicConversionExpression = TryCast(conversionExpression, BaseVisualBasicConversionExpression)
            If basicConversionExpression IsNot Nothing Then
                Return basicConversionExpression.ConversionInternal
            Else
                Throw New ArgumentException(String.Format(VBResources.IConversionExpressionIsNotVisualBasicConversion,
                                                          NameOf(IConversionExpression)),
                                            NameOf(conversionExpression))
            End If
        End Function
    End Module
End Namespace
