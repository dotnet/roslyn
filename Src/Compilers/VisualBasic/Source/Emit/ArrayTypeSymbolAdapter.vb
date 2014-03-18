' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Linq
Imports System.Text
Imports System.Reflection.Metadata
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Partial Class ArrayTypeSymbol
        Implements IArrayTypeReference

        Private Function IArrayTypeReferenceGetElementType(context As Microsoft.CodeAnalysis.Emit.Context) As ITypeReference Implements IArrayTypeReference.GetElementType
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Dim customModifiers As ImmutableArray(Of CustomModifier) = Me.CustomModifiers
            Dim type = moduleBeingBuilt.Translate(Me.ElementType, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)

            If customModifiers.Length = 0 Then
                Return type
            Else
                Return New ModifiedTypeReference(type, customModifiers)
            End If
        End Function

        Private ReadOnly Property IArrayTypeReferenceIsVector As Boolean Implements IArrayTypeReference.IsVector
            Get
                Return Me.Rank = 1
            End Get
        End Property

        Private ReadOnly Property IArrayTypeReferenceLowerBounds As IEnumerable(Of Integer) Implements IArrayTypeReference.LowerBounds
            Get
                Return Linq.Enumerable.Repeat(0, Me.Rank)
            End Get
        End Property

        Private ReadOnly Property IArrayTypeReferenceRank As UInteger Implements IArrayTypeReference.Rank
            Get
                Return CType(Me.Rank, UInteger)
            End Get
        End Property

        Private ReadOnly Property IArrayTypeReferenceSizes As IEnumerable(Of ULong) Implements IArrayTypeReference.Sizes
            Get
                Return SpecializedCollections.EmptyEnumerable(Of ULong)()
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceIsEnum As Boolean Implements ITypeReference.IsEnum
            Get
                Return False
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceIsValueType As Boolean Implements ITypeReference.IsValueType
            Get
                Return False
            End Get
        End Property

        Private Function ITypeReferenceGetResolvedType(context As Microsoft.CodeAnalysis.Emit.Context) As ITypeDefinition Implements ITypeReference.GetResolvedType
            Return Nothing
        End Function

        Private Function ITypeReferenceTypeCode(context As Microsoft.CodeAnalysis.Emit.Context) As PrimitiveTypeCode Implements ITypeReference.TypeCode
            Return PrimitiveTypeCode.NotPrimitive
        End Function

        Private ReadOnly Property ITypeReferenceTypeDef As TypeHandle Implements ITypeReference.TypeDef
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceAsGenericMethodParameterReference As IGenericMethodParameterReference Implements ITypeReference.AsGenericMethodParameterReference
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceAsGenericTypeInstanceReference As IGenericTypeInstanceReference Implements ITypeReference.AsGenericTypeInstanceReference
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceAsGenericTypeParameterReference As IGenericTypeParameterReference Implements ITypeReference.AsGenericTypeParameterReference
            Get
                Return Nothing
            End Get
        End Property

        Private Function ITypeReferenceAsNamespaceTypeDefinition(context As Microsoft.CodeAnalysis.Emit.Context) As INamespaceTypeDefinition Implements ITypeReference.AsNamespaceTypeDefinition
            Return Nothing
        End Function

        Private ReadOnly Property ITypeReferenceAsNamespaceTypeReference As INamespaceTypeReference Implements ITypeReference.AsNamespaceTypeReference
            Get
                Return Nothing
            End Get
        End Property

        Private Function ITypeReferenceAsNestedTypeDefinition(context As Microsoft.CodeAnalysis.Emit.Context) As INestedTypeDefinition Implements ITypeReference.AsNestedTypeDefinition
            Return Nothing
        End Function

        Private ReadOnly Property ITypeReferenceAsNestedTypeReference As INestedTypeReference Implements ITypeReference.AsNestedTypeReference
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property ITypeReferenceAsSpecializedNestedTypeReference As ISpecializedNestedTypeReference Implements ITypeReference.AsSpecializedNestedTypeReference
            Get
                Return Nothing
            End Get
        End Property

        Private Function ITypeReferenceAsTypeDefinition(context As Microsoft.CodeAnalysis.Emit.Context) As ITypeDefinition Implements ITypeReference.AsTypeDefinition
            Return Nothing
        End Function

        Friend NotOverridable Overrides Sub IReferenceDispatch(visitor As MetadataVisitor) ' Implements IReference.Dispatch
            visitor.Visit(DirectCast(Me, IArrayTypeReference))
        End Sub

        Friend NotOverridable Overrides Function IReferenceAsDefinition(context As Microsoft.CodeAnalysis.Emit.Context) As IDefinition ' Implements IReference.AsDefinition
            Return Nothing
        End Function
    End Class
End Namespace
