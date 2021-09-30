' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend Class SingleTypeDeclaration
        Inherits SingleNamespaceOrTypeDeclaration

        Private ReadOnly _children As ImmutableArray(Of SingleTypeDeclaration)

        ' CONSIDER: It may be possible to merge the flags and arity into a single
        ' 32-bit quantity if space is needed.

        Private ReadOnly _kind As DeclarationKind
        Private ReadOnly _flags As TypeDeclarationFlags
        Private ReadOnly _arity As UShort
        Private ReadOnly _modifiers As DeclarationModifiers

        Public ReadOnly QuickAttributes As QuickAttributes

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
                       memberNames As ImmutableHashSet(Of String),
                       children As ImmutableArray(Of SingleTypeDeclaration),
                       quickAttributes As QuickAttributes)
            MyBase.New(name, syntaxReference, nameLocation)

            Debug.Assert(kind <> DeclarationKind.Namespace)

            Me._kind = kind
            Me._arity = CUShort(arity)
            Me._flags = declFlags
            Me._modifiers = modifiers
            Me.MemberNames = memberNames
            Me._children = children
            Me.QuickAttributes = quickAttributes
        End Sub

        Public Overrides ReadOnly Property Kind As DeclarationKind
            Get
                Return Me._kind
            End Get
        End Property

        Public ReadOnly Property Arity As Integer
            Get
                Return _arity
            End Get
        End Property

        Public ReadOnly Property HasAnyAttributes As Boolean
            Get
                Return (_flags And TypeDeclarationFlags.HasAnyAttributes) <> 0
            End Get
        End Property

        Public ReadOnly Property HasBaseDeclarations As Boolean
            Get
                Return (_flags And TypeDeclarationFlags.HasBaseDeclarations) <> 0
            End Get
        End Property

        Public ReadOnly Property HasAnyNontypeMembers As Boolean
            Get
                Return (_flags And TypeDeclarationFlags.HasAnyNontypeMembers) <> 0
            End Get
        End Property

        Public ReadOnly Property AnyMemberHasAttributes As Boolean
            Get
                Return (_flags And TypeDeclarationFlags.AnyMemberHasAttributes) <> 0
            End Get
        End Property

        Public Overloads ReadOnly Property Children As ImmutableArray(Of SingleTypeDeclaration)
            Get
                Return _children
            End Get
        End Property

        Public ReadOnly Property Modifiers As DeclarationModifiers
            Get
                Return Me._modifiers
            End Get
        End Property

        Public ReadOnly Property MemberNames As ImmutableHashSet(Of String)

        Protected Overrides Function GetNamespaceOrTypeDeclarationChildren() As ImmutableArray(Of SingleNamespaceOrTypeDeclaration)
            Return StaticCast(Of SingleNamespaceOrTypeDeclaration).From(_children)
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
