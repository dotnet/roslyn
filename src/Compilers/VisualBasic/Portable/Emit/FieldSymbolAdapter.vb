' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.CodeGen
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
            Debug.Assert(Me.IsDefinitionOrDistinct())

            If Me.IsDefinition AndAlso Me.ContainingModule = moduleBeingBuilt.SourceModule Then
                Return Me
            End If

            Return Nothing
        End Function

        Private ReadOnly Property IFieldReferenceAsSpecializedFieldReference As ISpecializedFieldReference Implements IFieldReference.AsSpecializedFieldReference
            Get
                Debug.Assert(Me.IsDefinitionOrDistinct())

                If Not Me.IsDefinition Then
                    Return Me
                End If

                Return Nothing
            End Get
        End Property

        Private Function ITypeMemberReferenceGetContainingType(context As EmitContext) As ITypeReference Implements ITypeMemberReference.GetContainingType
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Debug.Assert(Me.IsDefinitionOrDistinct())

            Return moduleBeingBuilt.Translate(Me.ContainingType, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics, needDeclaration:=Me.IsDefinition)
        End Function

        Friend NotOverridable Overrides Sub IReferenceDispatch(visitor As MetadataVisitor) ' Implements IReference.Dispatch
            Debug.Assert(Me.IsDefinitionOrDistinct())

            If Not Me.IsDefinition Then
                visitor.Visit(DirectCast(Me, ISpecializedFieldReference))
            Else
                If Me.ContainingModule = (DirectCast(visitor.Context.Module, PEModuleBuilder)).SourceModule Then
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
                Return Me.MetadataName
            End Get
        End Property

        Friend Overridable ReadOnly Property IFieldReferenceIsContextualNamedEntity As Boolean Implements IFieldReference.IsContextualNamedEntity
            Get
                Return False
            End Get
        End Property

        Private Function IFieldDefinition_GetCompileTimeValue(context As EmitContext) As MetadataConstant Implements IFieldDefinition.GetCompileTimeValue
            CheckDefinitionInvariant()

            Return GetMetadataConstantValue(context)
        End Function

        Friend Function GetMetadataConstantValue(context As EmitContext) As MetadataConstant
            ' do not return a compile time value for const fields of types DateTime or Decimal because they
            ' are only const from a VB point of view
            If Me.IsMetadataConstant Then
                Return DirectCast(context.Module, PEModuleBuilder).CreateConstant(Me.Type, Me.ConstantValue, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
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
                If Me.IsMetadataConstant Then
                    Return True
                End If

                Return False
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionIsNotSerialized As Boolean Implements IFieldDefinition.IsNotSerialized
            Get
                CheckDefinitionInvariant()
                Return Me.IsNotSerialized
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionIsReadOnly As Boolean Implements IFieldDefinition.IsReadOnly
            Get
                CheckDefinitionInvariant()

                ' a const field of type DateTime or Decimal is ReadOnly in IL.
                Return Me.IsReadOnly OrElse
                        Me.IsConstButNotMetadataConstant
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionIsRuntimeSpecial As Boolean Implements IFieldDefinition.IsRuntimeSpecial
            Get
                CheckDefinitionInvariant()
                Return Me.HasRuntimeSpecialName
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionIsSpecialName As Boolean Implements IFieldDefinition.IsSpecialName
            Get
                CheckDefinitionInvariant()
                Return Me.HasSpecialName
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionIsStatic As Boolean Implements IFieldDefinition.IsStatic
            Get
                CheckDefinitionInvariant()
                Return Me.IsShared
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionIsMarshalledExplicitly As Boolean Implements IFieldDefinition.IsMarshalledExplicitly
            Get
                CheckDefinitionInvariant()
                Return Me.IsMarshalledExplicitly
            End Get
        End Property

        Friend Overridable ReadOnly Property IsMarshalledExplicitly As Boolean
            Get
                CheckDefinitionInvariant()
                Return Me.MarshallingInformation IsNot Nothing
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionMarshallingInformation As IMarshallingInformation Implements IFieldDefinition.MarshallingInformation
            Get
                CheckDefinitionInvariant()
                Return Me.MarshallingInformation
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionMarshallingDescriptor As ImmutableArray(Of Byte) Implements IFieldDefinition.MarshallingDescriptor
            Get
                CheckDefinitionInvariant()
                Return Me.MarshallingDescriptor
            End Get
        End Property

        Friend Overridable ReadOnly Property MarshallingDescriptor As ImmutableArray(Of Byte)
            Get
                CheckDefinitionInvariant()
                Return Nothing
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionOffset As Integer Implements IFieldDefinition.Offset
            Get
                CheckDefinitionInvariant()
                Return If(TypeLayoutOffset, 0)
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionMemberContainingTypeDefinition As ITypeDefinition Implements ITypeDefinitionMember.ContainingTypeDefinition
            Get
                CheckDefinitionInvariant()
                Return Me.ContainingType
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
                Debug.Assert(Not Me.IsDefinition)
                Return Me.OriginalDefinition
            End Get
        End Property
    End Class
End Namespace
