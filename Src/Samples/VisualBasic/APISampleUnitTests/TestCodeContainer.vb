' *********************************************************
'
' Copyright © Microsoft Corporation
'
' Licensed under the Apache License, Version 2.0 (the
' "License"); you may not use this file except in
' compliance with the License. You may obtain a copy of
' the License at
'
' http://www.apache.org/licenses/LICENSE-2.0 
'
' THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
' OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
' INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
' OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
' PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
'
' See the Apache 2 License for the specific language
' governing permissions and limitations under the License.
'
' *********************************************************

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
        Me.Position = textWithMarker.IndexOf("$"c)
        If Position <> -1 Then
            textWithMarker = textWithMarker.Remove(Position, 1)
        End If

        Me.Text = textWithMarker
        Me.SyntaxTree = VisualBasic.SyntaxFactory.ParseSyntaxTree(Text)
        If Position <> -1 Then
            Token = SyntaxTree.GetRoot().FindToken(Position)
            SyntaxNode = Token.Parent
        End If

        ' Use the mscorlib from our current process
        Me.Compilation = VisualBasicCompilation.Create(
            "test",
            syntaxTrees:={Me.SyntaxTree},
            references:={New MetadataFileReference(GetType(Object).Assembly.Location)})

        Me.SemanticModel = Compilation.GetSemanticModel(Me.SyntaxTree)
    End Sub

    Public Sub GetStatementsBetweenMarkers(ByRef firstStatement As StatementSyntax, ByRef lastStatement As StatementSyntax)
        Dim span As TextSpan = GetSpanBetweenMarkers()
        Dim statementsInside = Me.SyntaxTree.GetRoot().DescendantNodes(span).OfType(Of StatementSyntax).Where(Function(s) span.Contains(s.Span))
        Dim first = statementsInside.First()
        firstStatement = first
        lastStatement = statementsInside.Where(Function(s) s.Parent Is first.Parent).Last()
    End Sub

    Public Function GetSpanBetweenMarkers() As TextSpan
        Dim startComment = Me.SyntaxTree.GetRoot().DescendantTrivia().First(Function(t) t.ToString().Contains("start"))
        Dim endComment = Me.SyntaxTree.GetRoot().DescendantTrivia().First(Function(t) t.ToString().Contains("end"))
        Dim span = TextSpan.FromBounds(startComment.FullSpan.End, endComment.FullSpan.Start)
        Return span
    End Function
End Class