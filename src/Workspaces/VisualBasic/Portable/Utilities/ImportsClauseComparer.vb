' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities
    Friend Class ImportsClauseComparer
        Implements IComparer(Of ImportsClauseSyntax)

        Public Shared ReadOnly NormalInstance As IComparer(Of ImportsClauseSyntax) = New ImportsClauseComparer()

        Private ReadOnly _nameComparer As IComparer(Of NameSyntax)
        Private ReadOnly _tokenComparer As IComparer(Of SyntaxToken)

        Private Sub New()
            _nameComparer = NameSyntaxComparer.Create(TokenComparer.NormalInstance)
        End Sub

        Public Sub New(tokenComparer As IComparer(Of SyntaxToken))
            _nameComparer = NameSyntaxComparer.Create(tokenComparer)
            _tokenComparer = tokenComparer
        End Sub

        Friend Function Compare(x As ImportsClauseSyntax, y As ImportsClauseSyntax) As Integer Implements IComparer(Of ImportsClauseSyntax).Compare
            Dim imports1 = TryCast(x, SimpleImportsClauseSyntax)
            Dim imports2 = TryCast(y, SimpleImportsClauseSyntax)
            Dim xml1 = TryCast(x, XmlNamespaceImportsClauseSyntax)
            Dim xml2 = TryCast(y, XmlNamespaceImportsClauseSyntax)

            If xml1 IsNot Nothing AndAlso xml2 Is Nothing Then
                Return 1
            ElseIf xml1 Is Nothing AndAlso xml2 IsNot Nothing Then
                Return -1
            ElseIf xml1 IsNot Nothing AndAlso xml2 IsNot Nothing Then
                Return CompareXmlNames(
                    DirectCast(xml1.XmlNamespace.Name, XmlNameSyntax),
                    DirectCast(xml2.XmlNamespace.Name, XmlNameSyntax))
            ElseIf imports1 IsNot Nothing AndAlso imports2 IsNot Nothing Then
                If imports1.Alias IsNot Nothing AndAlso imports2.Alias Is Nothing Then
                    Return 1
                ElseIf imports1.Alias Is Nothing AndAlso imports2.Alias IsNot Nothing Then
                    Return -1
                ElseIf imports1.Alias IsNot Nothing AndAlso imports2.Alias IsNot Nothing Then
                    Return _tokenComparer.Compare(imports1.Alias.Identifier, imports2.Alias.Identifier)
                Else
                    Return _nameComparer.Compare(imports1.Name, imports2.Name)
                End If
            End If

            Return 0
        End Function

        Private Function CompareXmlNames(xmlName1 As XmlNameSyntax, xmlName2 As XmlNameSyntax) As Integer
            Dim tokens1 = xmlName1.DescendantTokens().Where(Function(t) t.Kind = SyntaxKind.IdentifierToken).ToList()
            Dim tokens2 = xmlName2.DescendantTokens().Where(Function(t) t.Kind = SyntaxKind.IdentifierToken).ToList()

            For i = 0 To Math.Min(tokens1.Count - 1, tokens2.Count - 1)
                Dim compare = _tokenComparer.Compare(tokens1(i), tokens2(i))
                If compare <> 0 Then
                    Return compare
                End If
            Next

            Return tokens1.Count - tokens2.Count
        End Function
    End Class
End Namespace
