' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit.NoPia

    Friend NotInheritable Class EmbeddedTypeParameter
        Inherits EmbeddedTypesManager.CommonEmbeddedTypeParameter

        Public Sub New(containingMethod As EmbeddedMethod, underlyingTypeParameter As TypeParameterSymbol)
            MyBase.New(containingMethod, underlyingTypeParameter)
            Debug.Assert(underlyingTypeParameter.IsDefinition)
        End Sub

        Protected Overrides Function GetConstraints(context As EmitContext) As IEnumerable(Of Cci.TypeReferenceWithAttributes)
            Return DirectCast(UnderlyingTypeParameter, Cci.IGenericParameter).GetConstraints(context)
        End Function

        Protected Overrides ReadOnly Property MustBeReferenceType As Boolean
            Get
                Return UnderlyingTypeParameter.HasReferenceTypeConstraint
            End Get
        End Property

        Protected Overrides ReadOnly Property MustBeValueType As Boolean
            Get
                Return UnderlyingTypeParameter.HasValueTypeConstraint
            End Get
        End Property

        Protected Overrides ReadOnly Property MustHaveDefaultConstructor As Boolean
            Get
                Return UnderlyingTypeParameter.HasConstructorConstraint
            End Get
        End Property

        Protected Overrides ReadOnly Property Name As String
            Get
                Return UnderlyingTypeParameter.MetadataName
            End Get
        End Property

        Protected Overrides ReadOnly Property Index As UShort
            Get
                Return CUShort(UnderlyingTypeParameter.Ordinal)
            End Get
        End Property

    End Class

End Namespace
