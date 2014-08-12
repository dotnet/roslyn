' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Class SynthesizedLocal
        Inherits LocalSymbol

        Private ReadOnly m_kind As SynthesizedLocalKind
        Private ReadOnly m_isByRef As Boolean
        Private ReadOnly m_Name As String
        Private ReadOnly m_syntax As StatementSyntax

        Friend Sub New(container As Symbol, type As TypeSymbol, kind As SynthesizedLocalKind, Optional syntax As StatementSyntax = Nothing, Optional isByRef As Boolean = False)
            MyBase.New(container, LocalDeclarationKind.None, type)

            Me.m_kind = kind
            Me.m_syntax = syntax
            Me.m_isByRef = isByRef
            Me.m_Name = GeneratedNames.GenerateTempName(kind)
        End Sub

        Friend Sub New(container As Symbol, type As TypeSymbol, kind As SynthesizedLocalKind, index As Integer)
            MyBase.New(container, LocalDeclarationKind.None, type)

            Me.m_kind = kind
            Me.m_syntax = Nothing
            Me.m_isByRef = False
            Me.m_Name = GeneratedNames.GenerateTempName(kind, type, index)
        End Sub

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return If(m_syntax Is Nothing, ImmutableArray(Of Location).Empty, ImmutableArray.Create(m_syntax.GetLocation()))
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return If(m_syntax Is Nothing, ImmutableArray(Of SyntaxReference).Empty, ImmutableArray.Create(m_syntax.GetReference()))
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property IdentifierToken As SyntaxToken
            Get
                Return Nothing
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property IdentifierLocation As Location
            Get
                Return NoLocation.Singleton
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return m_Name
            End Get
        End Property

        Friend Overrides ReadOnly Property IsByRef As Boolean
            Get
                Return m_isByRef
            End Get
        End Property

        Friend Overrides ReadOnly Property SynthesizedLocalKind As SynthesizedLocalKind
            Get
                Return m_kind
            End Get
        End Property
    End Class

End Namespace