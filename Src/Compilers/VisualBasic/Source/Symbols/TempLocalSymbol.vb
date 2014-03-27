' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Class TempLocalSymbol
        Inherits LocalSymbol

        Friend Sub New(container As Symbol, type As TypeSymbol)
            MyBase.New(container, LocalDeclarationKind.CompilerGenerated, type)
        End Sub

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray(Of Location).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
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
                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property IsByRef As Boolean
            Get
                Return False
            End Get
        End Property
    End Class

    ''' <summary>
    ''' The semantics of LHS ByRef local is roughly the same as of a ByRef parameter
    '''    EmitAssignment will load value of the local (not address of the local itself) for LHS and then will do indirect assignment.
    '''    To store reference in the local, use BoundReferenceAssignment node.
    '''                                     
    ''' The semantics of RHS ByRef local is roughly the same as of a ByRef parameter
    '''    EmitExpression   will load the value which local is referring to.
    '''    EmitAddress      will load the actual local.
    ''' </summary>
    Friend Class ByRefTempLocalSymbol
        Inherits TempLocalSymbol

        Friend Sub New(container As Symbol, type As TypeSymbol)
            MyBase.New(container, type)
        End Sub

        Friend Overrides ReadOnly Property IsByRef As Boolean
            Get
                Return True
            End Get
        End Property
    End Class

    ' A compiler generated local that has a name.
    ' A typical reasons for a generated local to have a name is when a compiler generated local
    ' must be handled in a special way during debugging.
    ' The most typical case is the locals whose values may survive across multiple 
    ' sequence points(statements). In such case to preserve continuity of the values, corresponding
    ' IL slots must be matched between EnC generations. And matching relies on locals having names.
    '
    ' NOTE: The actual name of a temp must be derived from their TempKind, and correspondingly
    '       the TempKind must be inferrable from the name.
    Friend Class NamedTempLocalSymbol
        Inherits TempLocalSymbol

        Private ReadOnly m_tempKind As TempKind
        Private ReadOnly m_isByRef As Boolean
        Private ReadOnly m_Name As String
        Private ReadOnly m_syntax As ImmutableArray(Of SyntaxReference)

        Friend Sub New(container As Symbol, type As TypeSymbol, tempKind As TempKind, syntax As StatementSyntax, Optional isByRef As Boolean = False)
            MyBase.New(container, type)

            Me.m_isByRef = isByRef
            Me.m_tempKind = tempKind
            Me.m_Name = GeneratedNames.GenerateTempName(tempKind)

            If syntax IsNot Nothing Then
                Me.m_syntax = ImmutableArray.Create(syntax.GetReference)
            Else
                Me.m_syntax = ImmutableArray(Of SyntaxReference).Empty
            End If
        End Sub

        Friend Sub New(container As Symbol, type As TypeSymbol, tempKind As TempKind, index As Integer)
            MyBase.New(container, type)

            Me.m_tempKind = tempKind
            Me.m_Name = GeneratedNames.GenerateTempName(tempKind, type, index)
        End Sub

        Friend Overrides ReadOnly Property TempKind As TempKind
            Get
                Return m_tempKind
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

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return m_syntax
            End Get
        End Property
    End Class
End Namespace

