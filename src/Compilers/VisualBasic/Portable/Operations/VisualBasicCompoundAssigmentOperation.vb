' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Operations

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend MustInherit Class BaseVisualBasicCompoundAssignmentOperation
        Inherits BaseCompoundAssignmentExpression

        Protected Sub New(inConversion As Conversion, outConversion As Conversion, operatorKind As Operations.BinaryOperatorKind, isLifted As Boolean, isChecked As Boolean, operatorMethod As IMethodSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(operatorKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)
            InConversionInternal = inConversion
            OutConversionInternal = outConversion
        End Sub

        Friend ReadOnly Property InConversionInternal As Conversion
        Friend ReadOnly Property OutConversionInternal As Conversion

        Public Overrides ReadOnly Property InConversion As CommonConversion
            Get
                Return InConversionInternal.ToCommonConversion()
            End Get
        End Property

        Public Overrides ReadOnly Property OutConversion As CommonConversion
            Get
                Return OutConversionInternal.ToCommonConversion()
            End Get
        End Property
    End Class

    Friend Class VisualBasicCompoundAssignmentOperation
        Inherits BaseVisualBasicCompoundAssignmentOperation

        Public Sub New(target As IOperation, value As IOperation, inConversion As Conversion, outConversion As Conversion, operatorKind As Operations.BinaryOperatorKind, isLifted As Boolean, isChecked As Boolean, operatorMethod As IMethodSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(inConversion, outConversion, operatorKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)

            TargetImpl = target
            ValueImpl = value
        End Sub

        Protected Overrides ReadOnly Property TargetImpl As IOperation

        Protected Overrides ReadOnly Property ValueImpl As IOperation
    End Class

    Friend Class LazyVisualBasicCompoundAssignmentOperation
        Inherits BaseVisualBasicCompoundAssignmentOperation

        Private ReadOnly _lazyTarget As Lazy(Of IOperation)
        Private ReadOnly _lazyValue As Lazy(Of IOperation)

        Public Sub New(target As Lazy(Of IOperation), value As Lazy(Of IOperation), inConversion As Conversion, outConversion As Conversion, operatorKind As Operations.BinaryOperatorKind, isLifted As Boolean, isChecked As Boolean, operatorMethod As IMethodSymbol, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(inConversion, outConversion, operatorKind, isLifted, isChecked, operatorMethod, semanticModel, syntax, type, constantValue, isImplicit)

            _lazyTarget = target
            _lazyValue = value
        End Sub

        Protected Overrides ReadOnly Property TargetImpl As IOperation
            Get
                Return _lazyTarget.Value
            End Get
        End Property

        Protected Overrides ReadOnly Property ValueImpl As IOperation
            Get
                Return _lazyValue.Value
            End Get
        End Property
    End Class
End Namespace
