' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Operations

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend MustInherit Class BaseVisualBasicArgument
        Inherits BaseArgument

        Protected Sub New(argumentKind As ArgumentKind, parameter As IParameterSymbol, inConversion As Conversion, outConversion As Conversion, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(argumentKind, parameter, semanticModel, syntax, type, constantValue, isImplicit)

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

    Friend NotInheritable Class VisualBasicArgument
        Inherits BaseVisualBasicArgument

        Public Sub New(argumentKind As ArgumentKind, parameter As IParameterSymbol, value As IOperation, inConversion As Conversion, outConversion As Conversion, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(argumentKind, parameter, inConversion, outConversion, semanticModel, syntax, type, constantValue, isImplicit)

            Me.ValueImpl = value
        End Sub

        Protected Overrides ReadOnly Property ValueImpl As IOperation
    End Class

    Friend NotInheritable Class LazyVisualBasicArgument
        Inherits BaseVisualBasicArgument

        Private ReadOnly _valueLazy As Lazy(Of IOperation)

        Public Sub New(argumentKind As ArgumentKind, parameter As IParameterSymbol, valueLazy As Lazy(Of IOperation), inConversion As Conversion, outConversion As Conversion, semanticModel As SemanticModel, syntax As SyntaxNode, type As ITypeSymbol, constantValue As [Optional](Of Object), isImplicit As Boolean)
            MyBase.New(argumentKind, parameter, inConversion, outConversion, semanticModel, syntax, type, constantValue, isImplicit)

            _valueLazy = valueLazy
        End Sub

        Protected Overrides ReadOnly Property ValueImpl As IOperation
            Get
                Return _valueLazy.Value
            End Get
        End Property
    End Class
End Namespace
