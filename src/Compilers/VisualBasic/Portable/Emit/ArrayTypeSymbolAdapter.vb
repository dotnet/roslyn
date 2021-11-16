' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Reflection.Metadata
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
#If DEBUG Then
    Partial Friend Class ArrayTypeSymbolAdapter
        Inherits SymbolAdapter
#Else
    Partial Friend Class ArrayTypeSymbol
#End If
        Implements Cci.IArrayTypeReference

        Private Function IArrayTypeReferenceGetElementType(context As EmitContext) As Cci.ITypeReference Implements Cci.IArrayTypeReference.GetElementType
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Dim customModifiers As ImmutableArray(Of CustomModifier) = AdaptedArrayTypeSymbol.CustomModifiers
            Dim type = moduleBeingBuilt.Translate(AdaptedArrayTypeSymbol.ElementType, syntaxNodeOpt:=DirectCast(context.SyntaxNode, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)

            If customModifiers.Length = 0 Then
                Return type
            Else
                Return New Cci.ModifiedTypeReference(type, customModifiers.As(Of Cci.ICustomModifier))
            End If
        End Function

        Private ReadOnly Property IArrayTypeReferenceIsSZArray As Boolean Implements Cci.IArrayTypeReference.IsSZArray
            Get
                Return AdaptedArrayTypeSymbol.IsSZArray
            End Get
        End Property

        Private ReadOnly Property IArrayTypeReferenceLowerBounds As ImmutableArray(Of Integer) Implements Cci.IArrayTypeReference.LowerBounds
            Get
                Return AdaptedArrayTypeSymbol.LowerBounds
            End Get
        End Property

        Private ReadOnly Property IArrayTypeReferenceRank As Integer Implements Cci.IArrayTypeReference.Rank
            Get
                Return AdaptedArrayTypeSymbol.Rank
            End Get
        End Property

        Private ReadOnly Property IArrayTypeReferenceSizes As ImmutableArray(Of Integer) Implements Cci.IArrayTypeReference.Sizes
            Get
                Return AdaptedArrayTypeSymbol.Sizes
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceIsEnum As Boolean Implements Cci.ITypeReference.IsEnum
            Get
                Return False
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceIsValueType As Boolean Implements Cci.ITypeReference.IsValueType
            Get
                Return False
            End Get
        End Property

        Private Function ITypeReferenceGetResolvedType(context As EmitContext) As Cci.ITypeDefinition Implements Cci.ITypeReference.GetResolvedType
            Return Nothing
        End Function

        Private ReadOnly Property ITypeReferenceTypeCode As Cci.PrimitiveTypeCode Implements Cci.ITypeReference.TypeCode
            Get
                Return Cci.PrimitiveTypeCode.NotPrimitive
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceTypeDef As TypeDefinitionHandle Implements Cci.ITypeReference.TypeDef
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceAsGenericMethodParameterReference As Cci.IGenericMethodParameterReference Implements Cci.ITypeReference.AsGenericMethodParameterReference
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceAsGenericTypeInstanceReference As Cci.IGenericTypeInstanceReference Implements Cci.ITypeReference.AsGenericTypeInstanceReference
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceAsGenericTypeParameterReference As Cci.IGenericTypeParameterReference Implements Cci.ITypeReference.AsGenericTypeParameterReference
            Get
                Return Nothing
            End Get
        End Property

        Private Function ITypeReferenceAsNamespaceTypeDefinition(context As EmitContext) As Cci.INamespaceTypeDefinition Implements Cci.ITypeReference.AsNamespaceTypeDefinition
            Return Nothing
        End Function

        Private ReadOnly Property ITypeReferenceAsNamespaceTypeReference As Cci.INamespaceTypeReference Implements Cci.ITypeReference.AsNamespaceTypeReference
            Get
                Return Nothing
            End Get
        End Property

        Private Function ITypeReferenceAsNestedTypeDefinition(context As EmitContext) As Cci.INestedTypeDefinition Implements Cci.ITypeReference.AsNestedTypeDefinition
            Return Nothing
        End Function

        Private ReadOnly Property ITypeReferenceAsNestedTypeReference As Cci.INestedTypeReference Implements Cci.ITypeReference.AsNestedTypeReference
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceAsSpecializedNestedTypeReference As Cci.ISpecializedNestedTypeReference Implements Cci.ITypeReference.AsSpecializedNestedTypeReference
            Get
                Return Nothing
            End Get
        End Property

        Private Function ITypeReferenceAsTypeDefinition(context As EmitContext) As Cci.ITypeDefinition Implements Cci.ITypeReference.AsTypeDefinition
            Return Nothing
        End Function

        Friend NotOverridable Overrides Sub IReferenceDispatch(visitor As Cci.MetadataVisitor) ' Implements IReference.Dispatch
            visitor.Visit(DirectCast(Me, Cci.IArrayTypeReference))
        End Sub

        Friend NotOverridable Overrides Function IReferenceAsDefinition(context As EmitContext) As Cci.IDefinition ' Implements IReference.AsDefinition
            Return Nothing
        End Function
    End Class

    Partial Friend Class ArrayTypeSymbol
#If DEBUG Then
        Private _lazyAdapter As ArrayTypeSymbolAdapter

        Protected Overrides Function GetCciAdapterImpl() As SymbolAdapter
            Return GetCciAdapter()
        End Function

        Friend Shadows Function GetCciAdapter() As ArrayTypeSymbolAdapter
            If _lazyAdapter Is Nothing Then
                Return InterlockedOperations.Initialize(_lazyAdapter, New ArrayTypeSymbolAdapter(Me))
            End If

            Return _lazyAdapter
        End Function
#Else
        Friend ReadOnly Property AdaptedArrayTypeSymbol As ArrayTypeSymbol
            Get
                Return Me
            End Get
        End Property

        Friend Shadows Function GetCciAdapter() As ArrayTypeSymbol
            Return Me
        End Function
#End If
    End Class

#If DEBUG Then
    Partial Friend NotInheritable Class ArrayTypeSymbolAdapter
        Friend ReadOnly Property AdaptedArrayTypeSymbol As ArrayTypeSymbol

        Friend Sub New(underlyingArrayTypeSymbol As ArrayTypeSymbol)
            AdaptedArrayTypeSymbol = underlyingArrayTypeSymbol
        End Sub

        Friend Overrides ReadOnly Property AdaptedSymbol As Symbol
            Get
                Return AdaptedArrayTypeSymbol
            End Get
        End Property
    End Class
#End If
End Namespace
