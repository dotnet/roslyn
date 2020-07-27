' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.DocumentationComments
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.DocumentationComments
    <ExportLanguageService(GetType(IDocumentationCommentSnippetService), LanguageNames.VisualBasic), [Shared]>
    Friend Class DocumentationCommentSnippetService
        Inherits AbstractDocumentationCommentSnippetService(Of DocumentationCommentTriviaSyntax, DeclarationStatementSyntax)

        Public Overrides ReadOnly Property DocumentationCommentCharacter As String
            Get
                Return "'"
            End Get
        End Property

        Protected Overrides ReadOnly Property ExteriorTriviaText As String
            Get
                Return "'''"
            End Get
        End Property

        Protected Overrides ReadOnly Property AddIndent As Boolean
            Get
                Return False
            End Get
        End Property

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function GetContainingMember(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As DeclarationStatementSyntax
            Return syntaxTree.GetRoot(cancellationToken).FindToken(position).GetContainingMember()
        End Function

        Protected Overrides Function SupportsDocumentationComments(member As DeclarationStatementSyntax) As Boolean
            If member Is Nothing Then
                Return False
            End If

            Select Case member.Kind
                Case SyntaxKind.ClassBlock,
                    SyntaxKind.InterfaceBlock,
                    SyntaxKind.ModuleBlock,
                    SyntaxKind.StructureBlock,
                    SyntaxKind.EnumBlock,
                    SyntaxKind.SubBlock,
                    SyntaxKind.FunctionBlock,
                    SyntaxKind.ConstructorBlock,
                    SyntaxKind.OperatorBlock,
                    SyntaxKind.PropertyBlock,
                    SyntaxKind.EventBlock,
                    SyntaxKind.ClassStatement,
                    SyntaxKind.InterfaceStatement,
                    SyntaxKind.ModuleStatement,
                    SyntaxKind.StructureStatement,
                    SyntaxKind.EnumStatement,
                    SyntaxKind.EnumMemberDeclaration,
                    SyntaxKind.DelegateSubStatement,
                    SyntaxKind.DelegateFunctionStatement,
                    SyntaxKind.FieldDeclaration,
                    SyntaxKind.EventStatement,
                    SyntaxKind.PropertyStatement,
                    SyntaxKind.OperatorStatement,
                    SyntaxKind.FunctionStatement,
                    SyntaxKind.SubStatement,
                    SyntaxKind.SubNewStatement,
                    SyntaxKind.DeclareFunctionStatement,
                    SyntaxKind.DeclareSubStatement
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        Protected Overrides Function HasDocumentationComment(member As DeclarationStatementSyntax) As Boolean
            If member Is Nothing Then
                Return False
            End If

            Return member.GetFirstToken().LeadingTrivia.Any(SyntaxKind.DocumentationCommentTrivia)
        End Function

        Private Shared Function SupportsDocumentationCommentReturnsClause(member As DeclarationStatementSyntax) As Boolean
            If member Is Nothing Then
                Return False
            End If

            Select Case member.Kind
                Case SyntaxKind.FunctionBlock,
                    SyntaxKind.OperatorBlock,
                    SyntaxKind.PropertyBlock,
                    SyntaxKind.DelegateFunctionStatement,
                    SyntaxKind.OperatorStatement,
                    SyntaxKind.FunctionStatement,
                    SyntaxKind.DeclareFunctionStatement
                    Return True
                Case SyntaxKind.PropertyStatement
                    Return Not DirectCast(member, PropertyStatementSyntax).Modifiers.Any(SyntaxKind.WriteOnlyKeyword)
                Case Else
                    Return False
            End Select
        End Function

        Protected Overrides Function GetPrecedingDocumentationCommentCount(member As DeclarationStatementSyntax) As Integer
            Dim firstToken = member.GetFirstToken()

            Dim count = firstToken.LeadingTrivia.Sum(Function(t) If(t.Kind = SyntaxKind.DocumentationCommentTrivia, 1, 0))

            Dim previousToken = firstToken.GetPreviousToken()
            If previousToken.Kind <> SyntaxKind.None Then
                count += previousToken.TrailingTrivia.Sum(Function(t) If(t.Kind = SyntaxKind.DocumentationCommentTrivia, 1, 0))
            End If

            Return count
        End Function

        Protected Overrides Function IsMemberDeclaration(member As DeclarationStatementSyntax) As Boolean
            Return member.IsMemberDeclaration()
        End Function

        Protected Overrides Function GetDocumentationCommentStubLines(member As DeclarationStatementSyntax) As List(Of String)
            Dim list = New List(Of String) From {
                "''' <summary>",
                "''' ",
                "''' </summary>"
            }

            Dim typeParameterList = member.GetTypeParameterList()
            If typeParameterList IsNot Nothing Then
                For Each typeParam In typeParameterList.Parameters
                    list.Add("''' <typeparam name=""" & typeParam.Identifier.ToString() & """></typeparam>")
                Next
            End If

            Dim parameterList = member.GetParameterList()
            If parameterList IsNot Nothing Then
                For Each param In parameterList.Parameters
                    list.Add("''' <param name=""" & param.Identifier.Identifier.ToString() & """></param>")
                Next
            End If

            If SupportsDocumentationCommentReturnsClause(member) Then
                list.Add("''' <returns></returns>")
            End If

            Return list
        End Function

        Protected Overrides Function IsSingleExteriorTrivia(documentationComment As DocumentationCommentTriviaSyntax, Optional allowWhitespace As Boolean = False) As Boolean
            If documentationComment Is Nothing Then
                Return False
            End If

            If documentationComment.Content.Count <> 1 Then
                Return False
            End If

            Dim xmlText = TryCast(documentationComment.Content(0), XmlTextSyntax)
            If xmlText Is Nothing Then
                Return False
            End If

            Dim textTokens = xmlText.TextTokens

            If Not textTokens.Any Then
                Return False
            End If

            If Not allowWhitespace AndAlso textTokens.Count <> 1 Then
                Return False
            End If

            If textTokens.Any(Function(t) Not String.IsNullOrWhiteSpace(t.ToString())) Then
                Return False
            End If

            Dim lastTextToken = textTokens.Last()
            Dim firstTextToken = textTokens.First()

            Return lastTextToken.Kind = SyntaxKind.DocumentationCommentLineBreakToken AndAlso
                   firstTextToken.LeadingTrivia.Count = 1 AndAlso
                   firstTextToken.LeadingTrivia.ElementAt(0).Kind = SyntaxKind.DocumentationCommentExteriorTrivia AndAlso
                   firstTextToken.LeadingTrivia.ElementAt(0).ToString() = "'''" AndAlso
                   lastTextToken.TrailingTrivia.Count = 0
        End Function

        Private Shared Function GetTextTokensFollowingExteriorTrivia(xmlText As XmlTextSyntax) As IList(Of SyntaxToken)
            Dim result = New List(Of SyntaxToken)

            Dim tokenList = xmlText.TextTokens
            For Each token In tokenList.Reverse()
                result.Add(token)

                If token.LeadingTrivia.Any(SyntaxKind.DocumentationCommentExteriorTrivia) Then
                    Exit For
                End If
            Next

            result.Reverse()

            Return result
        End Function

        Protected Overrides Function EndsWithSingleExteriorTrivia(documentationComment As DocumentationCommentTriviaSyntax) As Boolean
            If documentationComment Is Nothing Then
                Return False
            End If

            Dim xmlText = TryCast(documentationComment.Content.LastOrDefault(), XmlTextSyntax)
            If xmlText Is Nothing Then
                Return False
            End If

            Dim textTokens = GetTextTokensFollowingExteriorTrivia(xmlText)

            If textTokens.Any(Function(t) Not String.IsNullOrWhiteSpace(t.ToString())) Then
                Return False
            End If

            Dim lastTextToken = textTokens.LastOrDefault()
            Dim firstTextToken = textTokens.FirstOrDefault()

            Return lastTextToken.Kind = SyntaxKind.DocumentationCommentLineBreakToken AndAlso
                   firstTextToken.LeadingTrivia.Count = 1 AndAlso
                   firstTextToken.LeadingTrivia.ElementAt(0).Kind = SyntaxKind.DocumentationCommentExteriorTrivia AndAlso
                   firstTextToken.LeadingTrivia.ElementAt(0).ToString() = "'''" AndAlso
                   lastTextToken.TrailingTrivia.Count = 0
        End Function

        Protected Overrides Function IsMultilineDocComment(documentationComment As DocumentationCommentTriviaSyntax) As Boolean
            Return False
        End Function

        Protected Overrides Function GetTokenToRight(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As SyntaxToken
            If position >= syntaxTree.GetText(cancellationToken).Length Then
                Return Nothing
            End If

            Return syntaxTree.GetRoot(cancellationToken).FindTokenOnRightOfPosition(
                position, includeDirectives:=True, includeDocumentationComments:=True)
        End Function

        Protected Overrides Function GetTokenToLeft(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As SyntaxToken
            If position < 1 Then
                Return Nothing
            End If

            Return syntaxTree.GetRoot(cancellationToken).FindTokenOnLeftOfPosition(
                position - 1, includeDirectives:=True, includeDocumentationComments:=True)
        End Function

        Protected Overrides Function IsDocCommentNewLine(token As SyntaxToken) As Boolean
            Return token.Kind = SyntaxKind.DocumentationCommentLineBreakToken
        End Function

        Protected Overrides Function IsEndOfLineTrivia(trivia As SyntaxTrivia) As Boolean
            Return trivia.RawKind = SyntaxKind.EndOfLineTrivia
        End Function
        Protected Overrides Function HasSkippedTrailingTrivia(token As SyntaxToken) As Boolean
            Return token.TrailingTrivia.Any(Function(t) t.Kind() = SyntaxKind.SkippedTokensTrivia)
        End Function
    End Class
End Namespace
