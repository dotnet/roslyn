' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend Class SingleTypeDeclaration
        Inherits SingleNamespaceOrTypeDeclaration

        Private ReadOnly m_children As ImmutableArray(Of SingleTypeDeclaration)

        ' CONSIDER: It may be possible to merge the flags and arity into a single
        ' 32-bit quantity if space is needed.

        Private ReadOnly m_kind As DeclarationKind
        Private ReadOnly m_flags As TypeDeclarationFlags
        Private ReadOnly m_arity As UShort
        Private ReadOnly m_modifiers As DeclarationModifiers
        Private ReadOnly m_memberNames As ICollection(Of String)

        Friend Enum TypeDeclarationFlags As Byte
            None = 0
            HasAnyAttributes = 1 << 1
            HasBaseDeclarations = 1 << 2
            AnyMemberHasAttributes = 1 << 3
            HasAnyNontypeMembers = 1 << 4
        End Enum

        Public Sub New(kind As DeclarationKind,
                       name As String,
                       arity As Integer,
                       modifiers As DeclarationModifiers,
                       declFlags As TypeDeclarationFlags,
                       syntaxReference As SyntaxReference,
                       nameLocation As Location,
                       memberNames As ICollection(Of String),
                       children As ImmutableArray(Of SingleTypeDeclaration))
            MyBase.New(name, syntaxReference, nameLocation)

            Debug.Assert(kind <> DeclarationKind.Namespace)

            Me.m_kind = kind
            Me.m_arity = CUShort(arity)
            Me.m_flags = declFlags
            Me.m_modifiers = modifiers
            Me.m_memberNames = memberNames
            Me.m_children = children
        End Sub

        Public Overrides ReadOnly Property Kind As DeclarationKind
            Get
                Return Me.m_kind
            End Get
        End Property

        Public ReadOnly Property Arity As Integer
            Get
                Return m_arity
            End Get
        End Property

        Public ReadOnly Property HasAnyAttributes As Boolean
            Get
                Return (m_flags And TypeDeclarationFlags.HasAnyAttributes) <> 0
            End Get
        End Property

        Public ReadOnly Property HasBaseDeclarations As Boolean
            Get
                Return (m_flags And TypeDeclarationFlags.HasBaseDeclarations) <> 0
            End Get
        End Property

        Public ReadOnly Property HasAnyNontypeMembers As Boolean
            Get
                Return (m_flags And TypeDeclarationFlags.HasAnyNontypeMembers) <> 0
            End Get
        End Property

        Public ReadOnly Property AnyMemberHasAttributes As Boolean
            Get
                Return (m_flags And TypeDeclarationFlags.AnyMemberHasAttributes) <> 0
            End Get
        End Property

        Public Overloads ReadOnly Property Children As ImmutableArray(Of SingleTypeDeclaration)
            Get
                Return m_children
            End Get
        End Property

        Public ReadOnly Property Modifiers As DeclarationModifiers
            Get
                Return Me.m_modifiers
            End Get
        End Property

        Public ReadOnly Property MemberNames As ICollection(Of String)
            Get
                Return Me.m_memberNames
            End Get
        End Property

        Protected Overrides Function GetNamespaceOrTypeDeclarationChildren() As ImmutableArray(Of SingleNamespaceOrTypeDeclaration)
            Return StaticCast(Of SingleNamespaceOrTypeDeclaration).From(m_children)
        End Function

        Private Function GetEmbeddedSymbolKind() As EmbeddedSymbolKind
            Return SyntaxReference.SyntaxTree.GetEmbeddedKind()
        End Function

        Private NotInheritable Class Comparer
            Implements IEqualityComparer(Of SingleTypeDeclaration)

            Private Shadows Function Equals(decl1 As SingleTypeDeclaration, decl2 As SingleTypeDeclaration) As Boolean Implements IEqualityComparer(Of SingleTypeDeclaration).Equals
                ' We should not merge types across embedded code / user code boundary.
                Return IdentifierComparison.Equals(decl1.Name, decl2.Name) _
                        AndAlso decl1.Kind = decl2.Kind _
                        AndAlso decl1.Kind <> DeclarationKind.Enum _
                        AndAlso decl1.Arity = decl2.Arity _
                        AndAlso decl1.GetEmbeddedSymbolKind() = decl2.GetEmbeddedSymbolKind()
            End Function

            Private Shadows Function GetHashCode(decl1 As SingleTypeDeclaration) As Integer Implements IEqualityComparer(Of SingleTypeDeclaration).GetHashCode
                Return Hash.Combine(IdentifierComparison.GetHashCode(decl1.Name), Hash.Combine(decl1.Arity.GetHashCode(), CType(decl1.Kind, Integer)))
            End Function
        End Class

        Public Shared ReadOnly EqualityComparer As IEqualityComparer(Of SingleTypeDeclaration) = New Comparer()
    End Class
End Namespace