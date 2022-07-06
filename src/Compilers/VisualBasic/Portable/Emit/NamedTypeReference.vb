' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Reflection.Metadata
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Friend MustInherit Class NamedTypeReference
        Implements Cci.INamedTypeReference

        Protected ReadOnly m_UnderlyingNamedType As NamedTypeSymbol

        Public Sub New(underlyingNamedType As NamedTypeSymbol)
            Debug.Assert(underlyingNamedType IsNot Nothing)
            Me.m_UnderlyingNamedType = underlyingNamedType
        End Sub

        Private ReadOnly Property INamedTypeReferenceGenericParameterCount As UShort Implements Cci.INamedTypeReference.GenericParameterCount
            Get
                Return CType(m_UnderlyingNamedType.Arity, UShort)
            End Get
        End Property

        Private ReadOnly Property INamedTypeReferenceMangleName As Boolean Implements Cci.INamedTypeReference.MangleName
            Get
                Return m_UnderlyingNamedType.MangleName
            End Get
        End Property

        Private ReadOnly Property INamedTypeReferenceAssociatedFileIdentifier As String Implements Cci.INamedTypeReference.AssociatedFileIdentifier
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property INamedEntityName As String Implements Cci.INamedEntity.Name
            Get
                ' CCI automatically handles type suffix, so use Name instead of MetadataName
                Return m_UnderlyingNamedType.Name
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceIsEnum As Boolean Implements Cci.ITypeReference.IsEnum
            Get
                Return m_UnderlyingNamedType.TypeKind = TypeKind.Enum
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceIsValueType As Boolean Implements Cci.ITypeReference.IsValueType
            Get
                Return m_UnderlyingNamedType.IsValueType
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

        Public MustOverride ReadOnly Property AsGenericTypeInstanceReference As Cci.IGenericTypeInstanceReference Implements Cci.ITypeReference.AsGenericTypeInstanceReference

        Private ReadOnly Property ITypeReferenceAsGenericTypeParameterReference As Cci.IGenericTypeParameterReference Implements Cci.ITypeReference.AsGenericTypeParameterReference
            Get
                Return Nothing
            End Get
        End Property

        Private Function ITypeReferenceAsNamespaceTypeDefinition(context As EmitContext) As Cci.INamespaceTypeDefinition Implements Cci.ITypeReference.AsNamespaceTypeDefinition
            Return Nothing
        End Function

        Public MustOverride ReadOnly Property AsNamespaceTypeReference As Cci.INamespaceTypeReference Implements Cci.ITypeReference.AsNamespaceTypeReference

        Private Function ITypeReferenceAsNestedTypeDefinition(context As EmitContext) As Cci.INestedTypeDefinition Implements Cci.ITypeReference.AsNestedTypeDefinition
            Return Nothing
        End Function

        Public MustOverride ReadOnly Property AsNestedTypeReference As Cci.INestedTypeReference Implements Cci.ITypeReference.AsNestedTypeReference

        Public MustOverride ReadOnly Property AsSpecializedNestedTypeReference As Cci.ISpecializedNestedTypeReference Implements Cci.ITypeReference.AsSpecializedNestedTypeReference

        Private Function ITypeReferenceAsTypeDefinition(context As EmitContext) As Cci.ITypeDefinition Implements Cci.ITypeReference.AsTypeDefinition
            Return Nothing
        End Function

        Public Overrides Function ToString() As String
            Return m_UnderlyingNamedType.ToString()
        End Function

        Private Function IReferenceAttributes(context As EmitContext) As IEnumerable(Of Cci.ICustomAttribute) Implements Cci.IReference.GetAttributes
            Return SpecializedCollections.EmptyEnumerable(Of Cci.ICustomAttribute)()
        End Function

        Public MustOverride Sub Dispatch(visitor As Cci.MetadataVisitor) Implements Cci.IReference.Dispatch

        Private Function IReferenceAsDefinition(context As EmitContext) As Cci.IDefinition Implements Cci.IReference.AsDefinition
            Return Nothing
        End Function

        Private Function IReferenceGetInternalSymbol() As CodeAnalysis.Symbols.ISymbolInternal Implements Cci.IReference.GetInternalSymbol
            Return m_UnderlyingNamedType
        End Function

        Public NotOverridable Overrides Function Equals(obj As Object) As Boolean
            ' It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            Throw Roslyn.Utilities.ExceptionUtilities.Unreachable
        End Function

        Public NotOverridable Overrides Function GetHashCode() As Integer
            ' It is not supported to rely on default equality of these Cci objects, an explicit way to compare and hash them should be used.
            Throw Roslyn.Utilities.ExceptionUtilities.Unreachable
        End Function
    End Class
End Namespace
