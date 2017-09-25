﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Semantics

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend MustInherit Class BaseVisualBasicConversionExpression
        Inherits BaseConversionExpression

        Protected Sub New(conversion As Conversion, isExplicitInCode As Boolean, isTryCast As Boolean, isChecked As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(isExplicitInCode, isTryCast, isChecked, semanticModel, syntax, type, constantValue, isImplicit)

            ConversionInternal = conversion
        End Sub

        Friend ReadOnly Property ConversionInternal As Conversion

        Public Overrides ReadOnly Property Conversion As CommonConversion
            Get
                Return ConversionInternal.ToCommonConversion()
            End Get
        End Property
    End Class

    Friend NotInheritable Class VisualBasicConversionExpression
        Inherits BaseVisualBasicConversionExpression

        Public Sub New(operand As IOperation, conversion As Conversion, isExplicitInCode As Boolean, isTryCast As Boolean, isChecked As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(conversion, isExplicitInCode, isTryCast, isChecked, semanticModel, syntax, type, constantValue, isImplicit)

            Me.OperandImpl = operand
        End Sub

        Public Overrides ReadOnly Property OperandImpl As IOperation
    End Class

    Friend NotInheritable Class LazyVisualBasicConversionExpression
        Inherits BaseVisualBasicConversionExpression

        Private ReadOnly _operandLazy As Lazy(Of IOperation)

        Public Sub New(operandLazy As Lazy(Of IOperation), conversion As Conversion, isExplicitInCode As Boolean, isTryCast As Boolean, isChecked As Boolean, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(conversion, isExplicitInCode, isTryCast, isChecked, semanticModel, syntax, type, constantValue, isImplicit)

            _operandLazy = operandLazy
        End Sub

        Public Overrides ReadOnly Property OperandImpl As IOperation
            Get
                Return _operandLazy.Value
            End Get
        End Property
    End Class
End Namespace
