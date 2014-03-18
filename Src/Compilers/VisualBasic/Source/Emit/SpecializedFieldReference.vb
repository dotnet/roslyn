' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Emit

    ''' <summary>
    ''' Represents a reference to a field of a generic type instantiation.
    ''' e.g.
    ''' A{int}.Field
    ''' A{int}.B{string}.C.Field
    ''' </summary>
    Friend NotInheritable Class SpecializedFieldReference
        Inherits TypeMemberReference
        Implements Microsoft.Cci.ISpecializedFieldReference
        Implements Microsoft.Cci.IContextualNamedEntity

        Private ReadOnly m_UnderlyingField As FieldSymbol

        Public Sub New(underlyingField As FieldSymbol)
            Debug.Assert(underlyingField IsNot Nothing)
            Me.m_UnderlyingField = underlyingField
        End Sub

        Protected Overrides ReadOnly Property UnderlyingSymbol As Symbol
            Get
                Return m_UnderlyingField
            End Get
        End Property

        Public Overrides Sub Dispatch(visitor As Microsoft.Cci.MetadataVisitor)
            visitor.Visit(DirectCast(Me, Microsoft.Cci.ISpecializedFieldReference))
        End Sub

        Private ReadOnly Property ISpecializedFieldReferenceUnspecializedVersion As Microsoft.Cci.IFieldReference Implements Microsoft.Cci.ISpecializedFieldReference.UnspecializedVersion
            Get
                Debug.Assert(m_UnderlyingField.OriginalDefinition Is m_UnderlyingField.OriginalDefinition.OriginalDefinition)
                Return m_UnderlyingField.OriginalDefinition
            End Get
        End Property

        Private ReadOnly Property IFieldReferenceAsSpecializedFieldReference As Microsoft.Cci.ISpecializedFieldReference Implements Microsoft.Cci.IFieldReference.AsSpecializedFieldReference
            Get
                Return Me
            End Get
        End Property

        Private Function IFieldReferenceGetType(context As Microsoft.CodeAnalysis.Emit.Context) As Microsoft.Cci.ITypeReference Implements Microsoft.Cci.IFieldReference.GetType
            Dim customModifiers = m_UnderlyingField.CustomModifiers
            Dim type = (DirectCast(context.Module, PEModuleBuilder)).Translate(m_UnderlyingField.Type, syntaxNodeOpt:=DirectCast(context.SyntaxNodeOpt, VisualBasicSyntaxNode), diagnostics:=context.Diagnostics)

            If customModifiers.Length = 0 Then
                Return type
            Else
                Return New Microsoft.Cci.ModifiedTypeReference(type, customModifiers)
            End If
        End Function

        Private Function IFieldReferenceGetResolvedField(context As Microsoft.CodeAnalysis.Emit.Context) As Microsoft.Cci.IFieldDefinition Implements Microsoft.Cci.IFieldReference.GetResolvedField
            Return Nothing
        End Function

        Private Sub AssociateWithPeWriter(peWriter As Microsoft.Cci.PeWriter) Implements Microsoft.Cci.IContextualNamedEntity.AssociateWithPeWriter
            DirectCast(m_UnderlyingField, Microsoft.Cci.IContextualNamedEntity).AssociateWithPeWriter(peWriter)
        End Sub

        Private ReadOnly Property IsContextualNamedEntity As Boolean Implements Microsoft.Cci.IFieldReference.IsContextualNamedEntity
            Get
                Return m_UnderlyingField.IFieldReferenceIsContextualNamedEntity
            End Get
        End Property
    End Class
End Namespace
