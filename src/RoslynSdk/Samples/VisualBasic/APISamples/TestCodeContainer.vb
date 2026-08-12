' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

''' <summary> Helper class to bundle together information about a piece of analyzed test code. </summary>        
Class TestCodeContainer

    Public Property Position As Integer

    Public Property Text As String

    Public Property SyntaxTree As SyntaxTree

    Public Property Token As SyntaxToken

    Public Property SyntaxNode As SyntaxNode

    Public Property Compilation As Compilation

    Public Property SemanticModel As SemanticModel

    Public Sub New(textWithMarker As String)
        Position = textWithMarker.IndexOf("$"c)
        If Position <> -1 Then
            textWithMarker = textWithMarker.Remove(Position, 1)
        End If

        Text = textWithMarker
        SyntaxTree = SyntaxFactory.ParseSyntaxTree(Text)
        If Position <> -1 Then
            Token = SyntaxTree.GetRoot().FindToken(Position)
            SyntaxNode = Token.Parent
        End If

        ' Use the mscorlib from our current process
        Compilation = VisualBasicCompilation.Create(
            "test",
            syntaxTrees:={SyntaxTree},
            references:={MetadataReference.CreateFromFile(GetType(Object).Assembly.Location)})

        SemanticModel = Compilation.GetSemanticModel(SyntaxTree)
    End Sub

    Public Sub GetStatementsBetweenMarkers(ByRef firstStatement As StatementSyntax, ByRef lastStatement As StatementSyntax)
        Dim span As TextSpan = GetSpanBetweenMarkers()
        Dim statementsInside = SyntaxTree.GetRoot().DescendantNodes(span).OfType(Of StatementSyntax).Where(Function(s) span.Contains(s.Span))
        Dim first = statementsInside.First()
        firstStatement = first
        lastStatement = statementsInside.Where(Function(s) s.Parent Is first.Parent).Last()
    End Sub

    Public Function GetSpanBetweenMarkers() As TextSpan
        Dim startComment = SyntaxTree.GetRoot().DescendantTrivia().First(Function(t) t.ToString().Contains("start"))
        Dim endComment = SyntaxTree.GetRoot().DescendantTrivia().First(Function(t) t.ToString().Contains("end"))
        Dim span = TextSpan.FromBounds(startComment.FullSpan.End, endComment.FullSpan.Start)
        Return span
    End Function
End Class
