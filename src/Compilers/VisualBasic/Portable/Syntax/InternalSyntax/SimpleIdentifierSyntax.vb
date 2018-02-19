﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.ObjectModel
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend NotInheritable Class SimpleIdentifierSyntax
        Inherits IdentifierTokenSyntax

        Friend Sub New(kind As SyntaxKind, errors As DiagnosticInfo(), annotations As SyntaxAnnotation(), text As String, precedingTrivia As GreenNode, followingTrivia As GreenNode)
            MyBase.New(kind, errors, annotations, text, precedingTrivia, followingTrivia)
        End Sub

        Friend Shared CreateInstance As Func(Of ObjectReader, Object) = Function(o) New SimpleIdentifierSyntax(o)

        Friend Sub New(reader As ObjectReader)
            MyBase.New(reader)
        End Sub

        Shared Sub New()
            ObjectBinder.RegisterTypeReader(GetType(SimpleIdentifierSyntax), Function(r) New SimpleIdentifierSyntax(r))
        End Sub

        ''' <summary>
        ''' Contextual Nodekind
        ''' </summary>
        Friend Overrides ReadOnly Property PossibleKeywordKind As SyntaxKind
            Get
                Return SyntaxKind.IdentifierToken
            End Get
        End Property

        ''' <summary>
        ''' If true, the identifier was enclosed in brackets, such as "[End]".
        ''' </summary>
        Friend Overrides ReadOnly Property IsBracketed As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' The text of the identifier, not including the brackets or type character.
        ''' </summary>
        Friend Overrides ReadOnly Property IdentifierText As String
            Get
                Return Me.Text
            End Get
        End Property

        ''' <summary>
        ''' The type character suffix, if present. Returns TypeCharacter.None if no type
        ''' character was present. The only allowed values are None, Integer, Long,
        ''' Decimal, Single, Double, and String.
        ''' </summary>
        Friend Overrides ReadOnly Property TypeCharacter As TypeCharacter
            Get
                Return TypeCharacter.None
            End Get
        End Property

        Public Overrides Function WithLeadingTrivia(trivia As GreenNode) As GreenNode
            Return New SimpleIdentifierSyntax(Kind, GetDiagnostics, GetAnnotations, Text, trivia, GetTrailingTrivia)
        End Function

        Public Overrides Function WithTrailingTrivia(trivia As GreenNode) As GreenNode
            Return New SimpleIdentifierSyntax(Kind, GetDiagnostics, GetAnnotations, Text, GetLeadingTrivia, trivia)
        End Function

        Friend Overrides Function SetDiagnostics(newErrors As DiagnosticInfo()) As GreenNode
            Return New SimpleIdentifierSyntax(Kind, newErrors, GetAnnotations, Text, GetLeadingTrivia, GetTrailingTrivia)
        End Function

        Friend Overrides Function SetAnnotations(annotations As SyntaxAnnotation()) As GreenNode
            Return New SimpleIdentifierSyntax(Kind, GetDiagnostics, annotations, Text, GetLeadingTrivia, GetTrailingTrivia)
        End Function
    End Class

End Namespace
