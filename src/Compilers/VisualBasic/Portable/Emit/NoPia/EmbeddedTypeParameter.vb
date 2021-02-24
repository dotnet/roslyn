' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

#If Not DEBUG Then
Imports TypeParameterSymbolAdapter = Microsoft.CodeAnalysis.VisualBasic.Symbols.TypeParameterSymbol
#End If

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit.NoPia

    Friend NotInheritable Class EmbeddedTypeParameter
        Inherits EmbeddedTypesManager.CommonEmbeddedTypeParameter

        Public Sub New(containingMethod As EmbeddedMethod, underlyingTypeParameter As TypeParameterSymbolAdapter)
            MyBase.New(containingMethod, underlyingTypeParameter)
            Debug.Assert(underlyingTypeParameter.AdaptedTypeParameterSymbol.IsDefinition)
        End Sub

        Protected Overrides Function GetConstraints(context As EmitContext) As IEnumerable(Of Cci.TypeReferenceWithAttributes)
            Return DirectCast(UnderlyingTypeParameter, Cci.IGenericParameter).GetConstraints(context)
        End Function

        Protected Overrides ReadOnly Property MustBeReferenceType As Boolean
            Get
                Return UnderlyingTypeParameter.AdaptedTypeParameterSymbol.HasReferenceTypeConstraint
            End Get
        End Property

        Protected Overrides ReadOnly Property MustBeValueType As Boolean
            Get
                Return UnderlyingTypeParameter.AdaptedTypeParameterSymbol.HasValueTypeConstraint
            End Get
        End Property

        Protected Overrides ReadOnly Property MustHaveDefaultConstructor As Boolean
            Get
                Return UnderlyingTypeParameter.AdaptedTypeParameterSymbol.HasConstructorConstraint
            End Get
        End Property

        Protected Overrides ReadOnly Property Name As String
            Get
                Return UnderlyingTypeParameter.AdaptedTypeParameterSymbol.MetadataName
            End Get
        End Property

        Protected Overrides ReadOnly Property Index As UShort
            Get
                Return CUShort(UnderlyingTypeParameter.AdaptedTypeParameterSymbol.Ordinal)
            End Get
        End Property

    End Class

End Namespace
