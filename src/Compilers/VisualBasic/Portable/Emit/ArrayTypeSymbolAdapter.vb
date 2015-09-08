' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Reflection.Metadata
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend Partial Class ArrayTypeSymbol
        Implements Cci.IArrayTypeReference

        Private Function IArrayTypeReferenceGetElementType(context As EmitContext) As Cci.ITypeReference Implements Cci.IArrayTypeReference.GetElementType
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Dim customModifiers As ImmutableArray(Of CustomModifier) = Me.CustomModifiers
            Dim type = moduleBeingBuilt.Translate(Me.ElementType, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)

            If customModifiers.Length = 0 Then
                Return type
            Else
                Return New Cci.ModifiedTypeReference(type, customModifiers.As(Of Cci.ICustomModifier))
            End If
        End Function

        Private ReadOnly Property IArrayTypeReferenceIsSZArray As Boolean Implements Cci.IArrayTypeReference.IsSZArray
            Get
                Return Me.IsSZArray
            End Get
        End Property

        Private ReadOnly Property IArrayTypeReferenceLowerBounds As IEnumerable(Of Integer) Implements Cci.IArrayTypeReference.LowerBounds
            Get
                Dim lowerBounds = Me.LowerBounds

                If lowerBounds.IsDefault Then
                    Return Linq.Enumerable.Repeat(0, Me.Rank)
                End If

                Return lowerBounds
            End Get
        End Property

        Private ReadOnly Property IArrayTypeReferenceRank As UInteger Implements Cci.IArrayTypeReference.Rank
            Get
                Return CType(Me.Rank, UInteger)
            End Get
        End Property

        Private ReadOnly Property IArrayTypeReferenceSizes As IEnumerable(Of ULong) Implements Cci.IArrayTypeReference.Sizes
            Get
                If Me.Sizes.IsEmpty Then
                    Return SpecializedCollections.EmptyEnumerable(Of ULong)()
                End If

                Return GetSizes()
            End Get
        End Property

        Private Iterator Function GetSizes() As IEnumerable(Of ULong)
            For Each size In Me.Sizes
                Yield CType(size, ULong)
            Next
        End Function

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

        Private Function ITypeReferenceTypeCode(context As EmitContext) As Cci.PrimitiveTypeCode Implements Cci.ITypeReference.TypeCode
            Return Cci.PrimitiveTypeCode.NotPrimitive
        End Function

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
End Namespace
