' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Emit

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
#If DEBUG Then
    Partial Friend Class FieldSymbolAdapter
        Inherits SymbolAdapter
#Else
    Partial Friend Class FieldSymbol
#End If
        Implements IFieldReference
        Implements IFieldDefinition
        Implements ITypeMemberReference
        Implements ITypeDefinitionMember
        Implements ISpecializedFieldReference

        Private ReadOnly Property IDefinition_IsEncDeleted As Boolean Implements Cci.IDefinition.IsEncDeleted
            Get
                Return False
            End Get
        End Property

        Private Function IFieldReferenceGetType(context As EmitContext) As ITypeReference Implements IFieldReference.GetType
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Dim customModifiers = AdaptedFieldSymbol.CustomModifiers
            Dim type = moduleBeingBuilt.Translate(AdaptedFieldSymbol.Type, syntaxNodeOpt:=DirectCast(context.SyntaxNode, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
            If customModifiers.Length = 0 Then
                Return type
            Else
                Return New ModifiedTypeReference(type, customModifiers.As(Of Cci.ICustomModifier))
            End If
        End Function

        Private ReadOnly Property IFieldReferenceRefCustomModifiers As ImmutableArray(Of ICustomModifier) Implements IFieldReference.RefCustomModifiers
            Get
                Return ImmutableArray(Of ICustomModifier).Empty
            End Get
        End Property

        Private ReadOnly Property IFieldReferenceIsByReference As Boolean Implements IFieldReference.IsByReference
            Get
                Return False
            End Get
        End Property

        Private Function IFieldReferenceGetResolvedField(context As EmitContext) As IFieldDefinition Implements IFieldReference.GetResolvedField
            Return ResolvedFieldImpl(DirectCast(context.Module, PEModuleBuilder))
        End Function

        Private Function ResolvedFieldImpl(moduleBeingBuilt As PEModuleBuilder) As IFieldDefinition
            Debug.Assert(Me.IsDefinitionOrDistinct())

            If AdaptedFieldSymbol.IsDefinition AndAlso AdaptedFieldSymbol.ContainingModule = moduleBeingBuilt.SourceModule Then
                Return Me
            End If

            Return Nothing
        End Function

        Private ReadOnly Property IFieldReferenceAsSpecializedFieldReference As ISpecializedFieldReference Implements IFieldReference.AsSpecializedFieldReference
            Get
                Debug.Assert(Me.IsDefinitionOrDistinct())

                If Not AdaptedFieldSymbol.IsDefinition Then
                    Return Me
                End If

                Return Nothing
            End Get
        End Property

        Private Function ITypeMemberReferenceGetContainingType(context As EmitContext) As ITypeReference Implements ITypeMemberReference.GetContainingType
            Dim moduleBeingBuilt As PEModuleBuilder = DirectCast(context.Module, PEModuleBuilder)
            Debug.Assert(Me.IsDefinitionOrDistinct())

            Return moduleBeingBuilt.Translate(AdaptedFieldSymbol.ContainingType, syntaxNodeOpt:=DirectCast(context.SyntaxNode, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics, needDeclaration:=AdaptedFieldSymbol.IsDefinition)
        End Function

        Friend NotOverridable Overrides Sub IReferenceDispatch(visitor As MetadataVisitor) ' Implements IReference.Dispatch
            Debug.Assert(Me.IsDefinitionOrDistinct())

            If Not AdaptedFieldSymbol.IsDefinition Then
                visitor.Visit(DirectCast(Me, ISpecializedFieldReference))
            Else
                If AdaptedFieldSymbol.ContainingModule = (DirectCast(visitor.Context.Module, PEModuleBuilder)).SourceModule Then
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
                Return AdaptedFieldSymbol.MetadataName
            End Get
        End Property

        Private ReadOnly Property IFieldReference_IsContextualNamedEntity As Boolean Implements IFieldReference.IsContextualNamedEntity
            Get
                Return AdaptedFieldSymbol.IsContextualNamedEntity
            End Get
        End Property

        Private Function IFieldDefinition_GetCompileTimeValue(context As EmitContext) As MetadataConstant Implements IFieldDefinition.GetCompileTimeValue
            CheckDefinitionInvariant()

            Return GetMetadataConstantValue(context)
        End Function

        Friend Function GetMetadataConstantValue(context As EmitContext) As MetadataConstant
            ' do not return a compile time value for const fields of types DateTime or Decimal because they
            ' are only const from a VB point of view
            If AdaptedFieldSymbol.IsMetadataConstant Then
                Return DirectCast(context.Module, PEModuleBuilder).CreateConstant(AdaptedFieldSymbol.Type, AdaptedFieldSymbol.ConstantValue, syntaxNodeOpt:=DirectCast(context.SyntaxNode, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)
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
                If AdaptedFieldSymbol.IsMetadataConstant Then
                    Return True
                End If

                Return False
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionIsNotSerialized As Boolean Implements IFieldDefinition.IsNotSerialized
            Get
                CheckDefinitionInvariant()
                Return AdaptedFieldSymbol.IsNotSerialized
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionIsReadOnly As Boolean Implements IFieldDefinition.IsReadOnly
            Get
                CheckDefinitionInvariant()

                ' a const field of type DateTime or Decimal is ReadOnly in IL.
                Return AdaptedFieldSymbol.IsReadOnly OrElse
                        AdaptedFieldSymbol.IsConstButNotMetadataConstant
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionIsRuntimeSpecial As Boolean Implements IFieldDefinition.IsRuntimeSpecial
            Get
                CheckDefinitionInvariant()
                Return AdaptedFieldSymbol.HasRuntimeSpecialName
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionIsSpecialName As Boolean Implements IFieldDefinition.IsSpecialName
            Get
                CheckDefinitionInvariant()
                Return AdaptedFieldSymbol.HasSpecialName
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionIsStatic As Boolean Implements IFieldDefinition.IsStatic
            Get
                CheckDefinitionInvariant()
                Return AdaptedFieldSymbol.IsShared
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionIsMarshalledExplicitly As Boolean Implements IFieldDefinition.IsMarshalledExplicitly
            Get
                CheckDefinitionInvariant()
                Return AdaptedFieldSymbol.IsMarshalledExplicitly
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionMarshallingInformation As IMarshallingInformation Implements IFieldDefinition.MarshallingInformation
            Get
                CheckDefinitionInvariant()
                Return AdaptedFieldSymbol.MarshallingInformation
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionMarshallingDescriptor As ImmutableArray(Of Byte) Implements IFieldDefinition.MarshallingDescriptor
            Get
                CheckDefinitionInvariant()
                Return AdaptedFieldSymbol.MarshallingDescriptor
            End Get
        End Property

        Private ReadOnly Property IFieldDefinitionOffset As Integer Implements IFieldDefinition.Offset
            Get
                CheckDefinitionInvariant()
                Return If(AdaptedFieldSymbol.TypeLayoutOffset, 0)
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionMemberContainingTypeDefinition As ITypeDefinition Implements ITypeDefinitionMember.ContainingTypeDefinition
            Get
                CheckDefinitionInvariant()
                Return AdaptedFieldSymbol.ContainingType.GetCciAdapter()
            End Get
        End Property

        Private ReadOnly Property ITypeDefinitionMemberVisibility As TypeMemberVisibility Implements ITypeDefinitionMember.Visibility
            Get
                CheckDefinitionInvariant()
                Return AdaptedFieldSymbol.MetadataVisibility
            End Get
        End Property

        Private ReadOnly Property ISpecializedFieldReferenceUnspecializedVersion As IFieldReference Implements ISpecializedFieldReference.UnspecializedVersion
            Get
                Debug.Assert(Not AdaptedFieldSymbol.IsDefinition)
                Return AdaptedFieldSymbol.OriginalDefinition.GetCciAdapter()
            End Get
        End Property
    End Class

    Partial Friend Class FieldSymbol
#If DEBUG Then
        Private _lazyAdapter As FieldSymbolAdapter

        Protected Overrides Function GetCciAdapterImpl() As SymbolAdapter
            Return GetCciAdapter()
        End Function

        Friend Shadows Function GetCciAdapter() As FieldSymbolAdapter
            If _lazyAdapter Is Nothing Then
                Return InterlockedOperations.Initialize(_lazyAdapter, FieldSymbolAdapter.Create(Me))
            End If

            Return _lazyAdapter
        End Function
#Else
        Friend ReadOnly Property AdaptedFieldSymbol As FieldSymbol
            Get
                Return Me
            End Get
        End Property

        Friend Shadows Function GetCciAdapter() As FieldSymbol
            Return Me
        End Function
#End If

        Friend Overridable ReadOnly Property IsContextualNamedEntity As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overridable ReadOnly Property IsMarshalledExplicitly As Boolean
            Get
                CheckDefinitionInvariant()
                Return Me.MarshallingInformation IsNot Nothing
            End Get
        End Property

        Friend Overridable ReadOnly Property MarshallingDescriptor As ImmutableArray(Of Byte)
            Get
                CheckDefinitionInvariant()
                Return Nothing
            End Get
        End Property
    End Class

#If DEBUG Then
    Partial Friend Class FieldSymbolAdapter
        Friend ReadOnly Property AdaptedFieldSymbol As FieldSymbol

        Protected Sub New(underlyingFieldSymbol As FieldSymbol)
            AdaptedFieldSymbol = underlyingFieldSymbol
        End Sub

        Friend Shared Function Create(underlyingFieldSymbol As FieldSymbol) As FieldSymbolAdapter
            Dim synthesizedStaticLocalBackingField = TryCast(underlyingFieldSymbol, SynthesizedStaticLocalBackingField)

            If synthesizedStaticLocalBackingField IsNot Nothing Then
                Return New SynthesizedStaticLocalBackingFieldAdapter(synthesizedStaticLocalBackingField)
            End If

            Return New FieldSymbolAdapter(underlyingFieldSymbol)
        End Function

        Friend Overrides ReadOnly Property AdaptedSymbol As Symbol
            Get
                Return AdaptedFieldSymbol
            End Get
        End Property
    End Class
#End If
End Namespace
