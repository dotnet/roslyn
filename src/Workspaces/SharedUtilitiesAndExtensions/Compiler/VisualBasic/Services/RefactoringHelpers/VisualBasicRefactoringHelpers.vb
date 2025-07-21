' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings
    Friend NotInheritable Class VisualBasicRefactoringHelpers
        Inherits AbstractRefactoringHelpers(Of ExpressionSyntax, ArgumentSyntax, ExpressionStatementSyntax)

        Public Shared ReadOnly Instance As New VisualBasicRefactoringHelpers()

        Private Sub New()
        End Sub

        Protected Overrides ReadOnly Property HeaderFacts As IHeaderFacts = VisualBasicHeaderFacts.Instance
        Protected Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts = VisualBasicSyntaxFacts.Instance

        Public Overrides Function IsBetweenTypeMembers(sourceText As SourceText, root As SyntaxNode, position As Integer, ByRef typeDeclaration As SyntaxNode) As Boolean
            Dim token = root.FindToken(position)
            Dim typeDecl = token.GetAncestor(Of TypeBlockSyntax)
            typeDeclaration = typeDecl

            If typeDecl IsNot Nothing Then
                Dim start = If(typeDecl.Implements.LastOrDefault()?.Span.End,
                               If(typeDecl.Inherits.LastOrDefault()?.Span.End,
                                  typeDecl.BlockStatement.Span.End))

                If position >= start AndAlso
                   position <= typeDecl.EndBlockStatement.Span.Start Then

                    Dim line = sourceText.Lines.GetLineFromPosition(position)
                    If Not line.IsEmptyOrWhitespace() Then
                        Return False
                    End If

                    Dim member = typeDecl.Members.FirstOrDefault(Function(d) d.FullSpan.Contains(position))
                    If member Is Nothing Then
                        ' There are no members, Or we're after the last member.
                        Return True
                    Else
                        ' We're within a member.  Make sure we're in the leading whitespace of
                        ' the member.
                        If position < member.SpanStart Then
                            For Each trivia In member.GetLeadingTrivia()
                                If Not trivia.IsWhitespaceOrEndOfLine() Then
                                    Return False
                                End If

                                If trivia.FullSpan.Contains(position) Then
                                    Return True
                                End If
                            Next
                        End If
                    End If
                End If
            End If

            Return False
        End Function

        Protected Overrides Iterator Function ExtractNodesSimple(node As SyntaxNode, syntaxFacts As ISyntaxFacts) As IEnumerable(Of SyntaxNode)
            For Each baseExtraction In MyBase.ExtractNodesSimple(node, syntaxFacts)
                Yield baseExtraction
            Next

            ' VB's arguments can have identifiers nested in ModifiedArgument -> we want
            ' identifiers to represent parent node -> need to extract.
            If IsIdentifierOfParameter(node) Then
                Yield node.Parent
            End If

            ' In VB Statement both for/foreach are split into Statement (header) and the rest
            ' selecting the header should still count for the whole blockSyntax
            If TypeOf node Is ForEachStatementSyntax And TypeOf node.Parent Is ForEachBlockSyntax Then
                Dim foreachStatement = CType(node, ForEachStatementSyntax)
                Yield foreachStatement.Parent
            End If

            If TypeOf node Is ForStatementSyntax And TypeOf node.Parent Is ForBlockSyntax Then
                Dim forStatement = CType(node, ForStatementSyntax)
                Yield forStatement.Parent
            End If

            If TypeOf node Is VariableDeclaratorSyntax Then
                Dim declarator = CType(node, VariableDeclaratorSyntax)
                If TypeOf declarator.Parent Is LocalDeclarationStatementSyntax Then
                    Dim localDeclarationStatement = CType(declarator.Parent, LocalDeclarationStatementSyntax)
                    ' Only return the whole localDeclarationStatement if there's just one declarator with just one name
                    If localDeclarationStatement.Declarators.Count = 1 And localDeclarationStatement.Declarators.First().Names.Count = 1 Then
                        Yield localDeclarationStatement
                    End If
                End If
            End If

        End Function

        Public Shared Function IsIdentifierOfParameter(node As SyntaxNode) As Boolean
            Return (TypeOf node Is ModifiedIdentifierSyntax) AndAlso (TypeOf node.Parent Is ParameterSyntax) AndAlso (CType(node.Parent, ParameterSyntax).Identifier Is node)
        End Function
    End Class
End Namespace
