' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    ''' <summary>
    ''' Represents a reference to a field of a generic type instantiation.
    ''' e.g.
    ''' A{int}.Field
    ''' A{int}.B{string}.C.Field
    ''' </summary>
    Friend NotInheritable Class SpecializedFieldReference
        Inherits TypeMemberReference
        Implements Cci.ISpecializedFieldReference
        Implements Cci.IContextualNamedEntity

        Private ReadOnly _underlyingField As FieldSymbol

        Public Sub New(underlyingField As FieldSymbol)
            Debug.Assert(underlyingField IsNot Nothing)
            Me._underlyingField = underlyingField
        End Sub

        Protected Overrides ReadOnly Property UnderlyingSymbol As Symbol
            Get
                Return _underlyingField
            End Get
        End Property

        Public Overrides Sub Dispatch(visitor As Cci.MetadataVisitor)
            visitor.Visit(DirectCast(Me, Cci.ISpecializedFieldReference))
        End Sub

        Private ReadOnly Property ISpecializedFieldReferenceUnspecializedVersion As Cci.IFieldReference Implements Cci.ISpecializedFieldReference.UnspecializedVersion
            Get
                Debug.Assert(_underlyingField.OriginalDefinition Is _underlyingField.OriginalDefinition.OriginalDefinition)
                Return _underlyingField.OriginalDefinition.GetCciAdapter()
            End Get
        End Property

        Private ReadOnly Property IFieldReferenceAsSpecializedFieldReference As Cci.ISpecializedFieldReference Implements Cci.IFieldReference.AsSpecializedFieldReference
            Get
                Return Me
            End Get
        End Property

        Private Function IFieldReferenceGetType(context As EmitContext) As Cci.ITypeReference Implements Cci.IFieldReference.GetType
            Dim customModifiers = _underlyingField.CustomModifiers
            Dim type = DirectCast(context.Module, PEModuleBuilder).Translate(_underlyingField.Type, syntaxNodeOpt:=DirectCast(context.SyntaxNode, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)

            If customModifiers.Length = 0 Then
                Return type
            Else
                Return New Cci.ModifiedTypeReference(type, customModifiers.As(Of Cci.ICustomModifier))
            End If
        End Function

        Private ReadOnly Property IFieldReferenceRefCustomModifiers As ImmutableArray(Of Cci.ICustomModifier) Implements Cci.IFieldReference.RefCustomModifiers
            Get
                Return ImmutableArray(Of Cci.ICustomModifier).Empty
            End Get
        End Property

        Private ReadOnly Property IFieldReferenceIsByReference As Boolean Implements Cci.IFieldReference.IsByReference
            Get
                Return False
            End Get
        End Property

        Private Function IFieldReferenceGetResolvedField(context As EmitContext) As Cci.IFieldDefinition Implements Cci.IFieldReference.GetResolvedField
            Return Nothing
        End Function

        Private Sub AssociateWithMetadataWriter(metadataWriter As Cci.MetadataWriter) Implements Cci.IContextualNamedEntity.AssociateWithMetadataWriter
            DirectCast(_underlyingField, SynthesizedStaticLocalBackingField).AssociateWithMetadataWriter(metadataWriter)
        End Sub

        Private ReadOnly Property IsContextualNamedEntity As Boolean Implements Cci.IFieldReference.IsContextualNamedEntity
            Get
                Return _underlyingField.IsContextualNamedEntity
            End Get
        End Property
    End Class
End Namespace
