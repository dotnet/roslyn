' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend NotInheritable Class SourceIncompleteFieldSymbol
        Inherits SourceFieldSymbol

        Private ReadOnly _syntax As IncompleteMemberSyntax

        Public Sub New(container As SourceMemberContainerTypeSymbol, memberFlags As SourceMemberFlags, syntax As IncompleteMemberSyntax)
            MyBase.New(container, syntax.GetReference(), name:="", memberFlags)

            _syntax = syntax
        End Sub

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return Binder.GetErrorSymbol(name:="", errorInfo:=Nothing)
            End Get
        End Property

        Friend Overrides ReadOnly Property DeclarationSyntax As VisualBasicSyntaxNode
            Get
                Return _syntax
            End Get
        End Property

        Friend Overrides ReadOnly Property GetAttributeDeclarations As OneOrMany(Of SyntaxList(Of AttributeListSyntax))
            Get
                Return OneOrMany.Create(_syntax.AttributeLists)
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray.Create(_syntax.Location)
            End Get
        End Property
    End Class
End Namespace
