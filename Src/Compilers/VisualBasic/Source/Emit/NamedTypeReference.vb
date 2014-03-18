' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Reflection.Metadata
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    Friend MustInherit Class NamedTypeReference
        Implements Microsoft.Cci.INamedTypeReference

        Protected ReadOnly m_UnderlyingNamedType As NamedTypeSymbol

        Public Sub New(underlyingNamedType As NamedTypeSymbol)
            Debug.Assert(underlyingNamedType IsNot Nothing)
            Me.m_UnderlyingNamedType = underlyingNamedType
        End Sub

        Private ReadOnly Property INamedTypeReferenceGenericParameterCount As UShort Implements Microsoft.Cci.INamedTypeReference.GenericParameterCount
            Get
                Return CType(m_UnderlyingNamedType.Arity, UShort)
            End Get
        End Property

        Private ReadOnly Property INamedTypeReferenceMangleName As Boolean Implements Microsoft.Cci.INamedTypeReference.MangleName
            Get
                Return m_UnderlyingNamedType.MangleName
            End Get
        End Property

        Private ReadOnly Property INamedEntityName As String Implements Microsoft.Cci.INamedEntity.Name
            Get
                ' CCI automatically handles type suffix, so use Name instead of MetadataName
                Return m_UnderlyingNamedType.Name
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceIsEnum As Boolean Implements Microsoft.Cci.ITypeReference.IsEnum
            Get
                Return m_UnderlyingNamedType.TypeKind = TypeKind.Enum
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceIsValueType As Boolean Implements Microsoft.Cci.ITypeReference.IsValueType
            Get
                Return m_UnderlyingNamedType.IsValueType
            End Get
        End Property

        Private Function ITypeReferenceGetResolvedType(context As Microsoft.CodeAnalysis.Emit.Context) As Microsoft.Cci.ITypeDefinition Implements Microsoft.Cci.ITypeReference.GetResolvedType
            Return Nothing
        End Function

        Private Function ITypeReferenceTypeCode(context As Microsoft.CodeAnalysis.Emit.Context) As Microsoft.Cci.PrimitiveTypeCode Implements Microsoft.Cci.ITypeReference.TypeCode
            Return Microsoft.Cci.PrimitiveTypeCode.NotPrimitive
        End Function

        Private ReadOnly Property ITypeReferenceTypeDef As TypeHandle Implements Microsoft.Cci.ITypeReference.TypeDef
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceAsGenericMethodParameterReference As Microsoft.Cci.IGenericMethodParameterReference Implements Microsoft.Cci.ITypeReference.AsGenericMethodParameterReference
            Get
                Return Nothing
            End Get
        End Property

        Public MustOverride ReadOnly Property AsGenericTypeInstanceReference As Microsoft.Cci.IGenericTypeInstanceReference Implements Microsoft.Cci.ITypeReference.AsGenericTypeInstanceReference

        Private ReadOnly Property ITypeReferenceAsGenericTypeParameterReference As Microsoft.Cci.IGenericTypeParameterReference Implements Microsoft.Cci.ITypeReference.AsGenericTypeParameterReference
            Get
                Return Nothing
            End Get
        End Property

        Private Function ITypeReferenceAsNamespaceTypeDefinition(context As Microsoft.CodeAnalysis.Emit.Context) As Microsoft.Cci.INamespaceTypeDefinition Implements Microsoft.Cci.ITypeReference.AsNamespaceTypeDefinition
            Return Nothing
        End Function

        Public MustOverride ReadOnly Property AsNamespaceTypeReference As Microsoft.Cci.INamespaceTypeReference Implements Microsoft.Cci.ITypeReference.AsNamespaceTypeReference

        Private Function ITypeReferenceAsNestedTypeDefinition(context As Microsoft.CodeAnalysis.Emit.Context) As Microsoft.Cci.INestedTypeDefinition Implements Microsoft.Cci.ITypeReference.AsNestedTypeDefinition
            Return Nothing
        End Function

        Public MustOverride ReadOnly Property AsNestedTypeReference As Microsoft.Cci.INestedTypeReference Implements Microsoft.Cci.ITypeReference.AsNestedTypeReference

        Public MustOverride ReadOnly Property AsSpecializedNestedTypeReference As Microsoft.Cci.ISpecializedNestedTypeReference Implements Microsoft.Cci.ITypeReference.AsSpecializedNestedTypeReference

        Private Function ITypeReferenceAsTypeDefinition(context As Microsoft.CodeAnalysis.Emit.Context) As Microsoft.Cci.ITypeDefinition Implements Microsoft.Cci.ITypeReference.AsTypeDefinition
            Return Nothing
        End Function

        Public Overrides Function ToString() As String
            Return m_UnderlyingNamedType.ToString()
        End Function

        Private Function IReferenceAttributes(context As Microsoft.CodeAnalysis.Emit.Context) As IEnumerable(Of Microsoft.Cci.ICustomAttribute) Implements Microsoft.Cci.IReference.GetAttributes
            Return SpecializedCollections.EmptyEnumerable(Of Microsoft.Cci.ICustomAttribute)()
        End Function

        Public MustOverride Sub Dispatch(visitor As Microsoft.Cci.MetadataVisitor) Implements Microsoft.Cci.IReference.Dispatch

        Private Function IReferenceAsDefinition(context As Microsoft.CodeAnalysis.Emit.Context) As Microsoft.Cci.IDefinition Implements Microsoft.Cci.IReference.AsDefinition
            Return Nothing
        End Function
    End Class
End Namespace
