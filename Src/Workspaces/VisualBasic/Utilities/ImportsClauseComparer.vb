' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Utilities
    Class ImportsClauseComparer
        Implements IComparer(Of ImportsClauseSyntax)

        Public Shared ReadOnly Instance As IComparer(Of ImportsClauseSyntax) = New ImportsClauseComparer()

        Dim nameComparer As IComparer(Of NameSyntax)

        Private Sub New()
            nameComparer = NameSyntaxComparer.Create(TokenComparer.NormalInstance)
        End Sub

        Public Function Compare(x As ImportsClauseSyntax, y As ImportsClauseSyntax) As Integer Implements IComparer(Of ImportsClauseSyntax).Compare
            Return CompareClauses(x, y, nameComparer)
        End Function

        Friend Shared Function CompareClauses(x As ImportsClauseSyntax, y As ImportsClauseSyntax, nameComparer As IComparer(Of NameSyntax)) As Integer
            Dim imports1 = TryCast(x, MembersImportsClauseSyntax)
            Dim imports2 = TryCast(y, MembersImportsClauseSyntax)
            Dim xml1 = TryCast(x, XmlNamespaceImportsClauseSyntax)
            Dim xml2 = TryCast(y, XmlNamespaceImportsClauseSyntax)
            Dim alias1 = TryCast(x, AliasImportsClauseSyntax)
            Dim alias2 = TryCast(y, AliasImportsClauseSyntax)

            If xml1 IsNot Nothing AndAlso xml2 Is Nothing Then
                Return 1
            ElseIf xml1 Is Nothing AndAlso xml2 IsNot Nothing Then
                Return -1
            ElseIf xml1 IsNot Nothing AndAlso xml2 IsNot Nothing Then
                Return CompareXmlNames(
                    DirectCast(xml1.XmlNamespace.Name, XmlNameSyntax),
                    DirectCast(xml2.XmlNamespace.Name, XmlNameSyntax))
            ElseIf alias1 IsNot Nothing AndAlso alias2 Is Nothing Then
                Return 1
            ElseIf alias1 Is Nothing AndAlso alias2 IsNot Nothing Then
                Return -1
            ElseIf alias1 IsNot Nothing AndAlso alias2 IsNot Nothing Then
                Return TokenComparer.NormalInstance.Compare(alias1.Alias, alias2.Alias)
            ElseIf imports1 IsNot Nothing AndAlso imports2 IsNot Nothing Then
                Return nameComparer.Compare(imports1.Name, imports2.Name)
            End If

            Return 0
        End Function

        Private Shared Function CompareXmlNames(xmlName1 As XmlNameSyntax, xmlName2 As XmlNameSyntax) As Integer
            Dim tokens1 = xmlName1.DescendantTokens().Where(Function(t) t.VisualBasicKind = SyntaxKind.IdentifierToken).ToList()
            Dim tokens2 = xmlName2.DescendantTokens().Where(Function(t) t.VisualBasicKind = SyntaxKind.IdentifierToken).ToList()

            For i = 0 To Math.Min(tokens1.Count - 1, tokens2.Count - 1)
                Dim compare = TokenComparer.NormalInstance.Compare(tokens1(i), tokens2(i))
                If compare <> 0 Then
                    Return compare
                End If
            Next

            Return tokens1.Count - tokens2.Count
        End Function
    End Class
End Namespace