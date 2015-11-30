' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Partial Class FieldSymbol
        Implements IFieldReference
        Implements IFieldDefinition
        Implements ITypeMemberReference
        Implements ITypeDefinitionMember
        Implements ISpecializedFieldReference

        Private Function IFieldReferenceGetType(context As EmitContext) As ITypeReference Implements IFieldReference.GetType
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Dim customModifiers = Me.CustomModifiers
            Dim type = moduleBeingBuilt.Translate(Me.Type, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
            If customModifiers.Length = 0 Then
                Return type
            Else
                Return New ModifiedTypeReference(type, customModifiers.As(Of Cci.ICustomModifier))
            End If
        End Function

        Private Function IFieldReferenceGetResolvedField(context As EmitContext) As IFieldDefinition Implements IFieldReference.GetResolvedField
            Return ResolvedFieldImpl(DirectCast(context.Module, PEModuleBuilder))
        End Function

        Private Function ResolvedFieldImpl(moduleBeingBuilt As PEModuleBuilder) As IFieldDefinition
            Debug.Assert(IsDefinitionOrDistinct())

            If IsDefinition AndAlso ContainingModule = moduleBeingBuilt.SourceModule Then
                Return Me
            End If

            Return Nothing
        End Function

        Private ReadOnly Property IFieldReferenceAsSpecializedFieldReference As ISpecializedFieldReference Implements IFieldReference.AsSpecializedFieldReference
            Get
                Debug.Assert(IsDefinitionOrDistinct())

                If Not IsDefinition Then
                    Return Me
                End If

                Return Nothing
            End Get
        End Property

        Private Function ITypeMemberReferenceGetContainingType(context As EmitContext) As ITypeReference Implements ITypeMemberReference.GetContainingType
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Debug.Assert(IsDefinitionOrDistinct())

            If Not IsDefinition Then
                Return moduleBeingBuilt.Translate(ContainingType, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
            End If

            Return ContainingType
        End Function

        Friend NotOverridable Overrides Sub IReferenceDispatch(visitor As MetadataVisitor) ' Implements IReference.Dispatch
            Debug.Assert(IsDefinitionOrDistinct())

            If Not IsDefinition Then
                visitor.Visit(DirectCast(Me, ISpecializedFieldReference))
            Else
                If ContainingModule = (DirectCast(visitor.Context.Module, PEModuleBuilder)).SourceModule Then
                    visitor.Visit(DirectCast(Me, IFieldDefinition))
                Else
                    visitor.Visit(DirectCast(Me, IFieldReference))
                End If
            End If
        End Sub

        Friend NotOverridable Overrides Function IReferenceAsDefinition(context As EmitContext) As IDefinition ' Implements IReference.AsDefinition
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Return ResolvedFieldImpl(moduleBeingBuilt)
        End Function

        Private ReadOnly Property INamedEntityName As String Implements INamedEntity.Name
            Get
                Return MetadataName
            End Get
        End Property

        Friend Overridable ReadOnly Property IFieldReferenceIsContextualNamedEntity As Boolean Implements IFieldReference.IsContextualNamedEntity
            Get
                Return False
            End Get
        End Property

        Private Function IFieldDefinition_GetCompileTimeValue(context As EmitContext) As IMetadataConstant Implements IFieldDefinition.GetCompileTimeValue
            CheckDefinitionInvariant()

            Return GetMetadataConstantValue(context)
        End Function

        Friend Function GetMetadataConstantValue(context As EmitContext) As IMetadataConstant
            ' do not return a compile time value for const fields of types DateTime or Decimal because they
            ' are only const from a VB point of view
            If IsMetadataConstant Then
                Return DirectCast(context.Module, PEModuleBuilder).CreateConstant(Type, ConstantValue, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
            End If

            Return Nothing
        End Function

        Private ReadOnly Property IFieldDefinitionFieldMapping As ImmutableArray(Of Byte) Implements IFieldDefinition.MappedData
            Get
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionIsCompileTimeConstant As Boolean Implements IFieldDefinition.IsCompileTimeConstant
            Get
                CheckDefinitionInvariant()

                ' const fields of types DateTime or Decimal are not compile time constant, because they are only const 
                ' from a VB point of view
                Return IsMetadataConstant
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionIsNotSerialized As Boolean Implements IFieldDefinition.IsNotSerialized
            Get
                CheckDefinitionInvariant()
                Return IsNotSerialized
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionIsReadOnly As Boolean Implements IFieldDefinition.IsReadOnly
            Get
                CheckDefinitionInvariant()

                ' a const field of type DateTime or Decimal is ReadOnly in IL.
                Return IsReadOnly OrElse IsConstButNotMetadataConstant
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionIsRuntimeSpecial As Boolean Implements IFieldDefinition.IsRuntimeSpecial
            Get
                CheckDefinitionInvariant()
                Return HasRuntimeSpecialName
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionIsSpecialName As Boolean Implements IFieldDefinition.IsSpecialName
            Get
                CheckDefinitionInvariant()
                Return HasSpecialName
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionIsStatic As Boolean Implements IFieldDefinition.IsStatic
            Get
                CheckDefinitionInvariant()
                Return IsShared
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionIsMarshalledExplicitly As Boolean Implements IFieldDefinition.IsMarshalledExplicitly
            Get
                CheckDefinitionInvariant()
                Return IsMarshalledExplicitly
            End Get
        End Property

        Friend Overridable ReadOnly Property IsMarshalledExplicitly As Boolean
            Get
                CheckDefinitionInvariant()
                Return MarshallingInformation IsNot Nothing
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionMarshallingInformation As IMarshallingInformation Implements IFieldDefinition.MarshallingInformation
            Get
                CheckDefinitionInvariant()
                Return MarshallingInformation
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionMarshallingDescriptor As ImmutableArray(Of Byte) Implements IFieldDefinition.MarshallingDescriptor
            Get
                CheckDefinitionInvariant()
                Return MarshallingDescriptor
            End Get
        End Property

        Friend Overridable ReadOnly Property MarshallingDescriptor As ImmutableArray(Of Byte)
            Get
                CheckDefinitionInvariant()
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionOffset As UInteger Implements IFieldDefinition.Offset
            Get
                CheckDefinitionInvariant()
                Dim offset = TypeLayoutOffset
                Return CUInt(If(offset, 0))
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionMemberContainingTypeDefinition As ITypeDefinition Implements ITypeDefinitionMember.ContainingTypeDefinition
            Get
                CheckDefinitionInvariant()
                Return ContainingType
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionMemberVisibility As TypeMemberVisibility Implements ITypeDefinitionMember.Visibility
            Get
                CheckDefinitionInvariant()
                Return PEModuleBuilder.MemberVisibility(Me)
            End Get
        End Property

        Private ReadOnly Property ISpecializedFieldReferenceUnspecializedVersion As IFieldReference Implements ISpecializedFieldReference.UnspecializedVersion
            Get
                Debug.Assert(Not IsDefinition)
                Return OriginalDefinition
            End Get
        End Property
    End Class
End Namespace
