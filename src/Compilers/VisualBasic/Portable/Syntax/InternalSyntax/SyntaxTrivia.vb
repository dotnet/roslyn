' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.ObjectModel
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Partial Friend Class SyntaxTrivia
        Inherits VisualBasicSyntaxNode

        Private ReadOnly _text As String

        Friend Sub New(kind As SyntaxKind, errors As DiagnosticInfo(), annotations As SyntaxAnnotation(), text As String)
            MyBase.New(kind, errors, annotations, text.Length)
            Me._text = text
            If text.Length > 0 Then
                Me.SetFlags(NodeFlags.IsNotMissing)
            End If
        End Sub

        Friend Sub New(kind As SyntaxKind, text As String, context As ISyntaxFactoryContext)
            Me.New(kind, text)
            Me.SetFactoryContext(context)
        End Sub

        Friend Sub New(kind As SyntaxKind, text As String)
            MyBase.New(kind, text.Length)
            Me._text = text
            If text.Length > 0 Then
                Me.SetFlags(NodeFlags.IsNotMissing)
            End If
        End Sub

        Friend Sub New(reader As ObjectReader)
            MyBase.New(reader)

            Me._text = reader.ReadString()
            Me.FullWidth = Me._text.Length
            If Text.Length > 0 Then
                Me.SetFlags(NodeFlags.IsNotMissing)
            End If
        End Sub

        Shared Sub New()
            ObjectBinder.RegisterTypeReader(GetType(SyntaxTrivia), Function(r) New SyntaxTrivia(r))
        End Sub

        Friend Overrides ReadOnly Property ShouldReuseInSerialization As Boolean
            Get
                Select Case Me.Kind
                    Case SyntaxKind.WhitespaceTrivia,
                        SyntaxKind.EndOfLineTrivia,
                        SyntaxKind.LineContinuationTrivia,
                        SyntaxKind.DocumentationCommentExteriorTrivia,
                        SyntaxKind.ColonTrivia

                        Return True
                    Case Else
                        Return False
                End Select
            End Get
        End Property

        Friend Overrides Sub WriteTo(writer As ObjectWriter)
            MyBase.WriteTo(writer)
            writer.WriteString(Me._text)
        End Sub

        Friend ReadOnly Property Text As String
            Get
                Return Me._text
            End Get
        End Property

        Public Overrides ReadOnly Property IsTrivia As Boolean
            Get
                Return True
            End Get
        End Property

        Friend NotOverridable Overrides Function GetSlot(index As Integer) As GreenNode
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend NotOverridable Overrides Function GetTrailingTrivia() As GreenNode
            Return Nothing
        End Function

        Public NotOverridable Overrides Function GetTrailingTriviaWidth() As Integer
            Return 0
        End Function

        Friend NotOverridable Overrides Function GetLeadingTrivia() As GreenNode
            Return Nothing
        End Function

        Public NotOverridable Overrides Function GetLeadingTriviaWidth() As Integer
            Return 0
        End Function

        Protected Overrides Sub WriteTriviaTo(writer As System.IO.TextWriter)
            writer.Write(Text) 'write text of trivia itself
        End Sub

        Public NotOverridable Overrides Function ToFullString() As String
            Return Me._text
        End Function

        Public Overrides Function ToString() As String
            Return Me._text
        End Function

        Friend NotOverridable Overrides Sub AddSyntaxErrors(accumulatedErrors As List(Of DiagnosticInfo))
            If Me.GetDiagnostics IsNot Nothing Then
                accumulatedErrors.AddRange(Me.GetDiagnostics)
            End If
        End Sub

        Public NotOverridable Overrides Function Accept(visitor As VisualBasicSyntaxVisitor) As VisualBasicSyntaxNode
            Return visitor.VisitSyntaxTrivia(Me)
        End Function

        Public Shared Narrowing Operator CType(trivia As SyntaxTrivia) As Microsoft.CodeAnalysis.SyntaxTrivia
            Return New Microsoft.CodeAnalysis.SyntaxTrivia(Nothing, trivia, position:=0, index:=0)
        End Operator

        Public Overrides Function IsEquivalentTo(other As GreenNode) As Boolean
            If Not MyBase.IsEquivalentTo(other) Then
                Return False
            End If

            Dim otherTrivia = DirectCast(other, SyntaxTrivia)
            Return String.Equals(Me.Text, otherTrivia.Text, StringComparison.Ordinal)
        End Function

        Friend Overrides Function CreateRed(parent As SyntaxNode, position As Integer) As SyntaxNode
            Throw ExceptionUtilities.Unreachable
        End Function
    End Class

End Namespace
