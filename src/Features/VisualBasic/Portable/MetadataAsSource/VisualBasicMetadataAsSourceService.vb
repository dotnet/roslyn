' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.DocumentationComments
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.MetadataAsSource
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.ChangeSignature
Imports Microsoft.CodeAnalysis.VisualBasic.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.MetadataAsSource
    Friend Class VisualBasicMetadataAsSourceService
        Inherits AbstractMetadataAsSourceService

        Private ReadOnly _memberSeparationRule As AbstractFormattingRule = New FormattingRule()
        Public Shared ReadOnly Instance As New VisualBasicMetadataAsSourceService()

        Private Sub New()
        End Sub

        Protected Overrides Async Function AddAssemblyInfoRegionAsync(document As Document, symbolCompilation As Compilation, symbol As ISymbol, cancellationToken As CancellationToken) As Task(Of Document)
            Dim assemblyInfo = MetadataAsSourceHelpers.GetAssemblyInfo(symbol.ContainingAssembly)
            Dim assemblyPath = MetadataAsSourceHelpers.GetAssemblyDisplay(symbolCompilation, symbol.ContainingAssembly)

            Dim regionTrivia = SyntaxFactory.RegionDirectiveTrivia(
                    SyntaxFactory.Token(SyntaxKind.HashToken),
                    SyntaxFactory.Token(SyntaxKind.RegionKeyword),
                    SyntaxFactory.StringLiteralToken(
                        SyntaxFactory.TriviaList(SyntaxFactory.Space),
                        """"c & assemblyInfo & """"c,
                        assemblyInfo,
                        SyntaxTriviaList.Create(SyntaxFactory.CarriageReturnLineFeed)))

            Dim oldRoot = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim newRoot = oldRoot.WithLeadingTrivia({
                SyntaxFactory.Trivia(regionTrivia),
                SyntaxFactory.CommentTrivia("' " & assemblyPath),
                SyntaxFactory.CarriageReturnLineFeed,
                SyntaxFactory.Trivia(
                    SyntaxFactory.EndRegionDirectiveTrivia()),
                SyntaxFactory.CarriageReturnLineFeed,
                SyntaxFactory.CarriageReturnLineFeed})

            Return document.WithSyntaxRoot(newRoot)
        End Function

        Protected Overrides Function AddNullableRegionsAsync(document As Document, cancellationToken As CancellationToken) As Task(Of Document)
            ' VB has no equivalent to #nullable enable
            Return Task.FromResult(document)
        End Function

        Protected Overrides Async Function ConvertDocCommentsToRegularCommentsAsync(document As Document, docCommentFormattingService As IDocumentationCommentFormattingService, cancellationToken As CancellationToken) As Task(Of Document)
            Dim syntaxRoot = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            Dim newSyntaxRoot = DocCommentConverter.ConvertToRegularComments(syntaxRoot, docCommentFormattingService, cancellationToken)

            Return document.WithSyntaxRoot(newSyntaxRoot)
        End Function

        Protected Overrides Function GetFormattingRules(document As Document) As ImmutableArray(Of AbstractFormattingRule)
            Dim coreRules = Formatter.GetDefaultFormattingRules(document)
            Dim result = New FixedSizeArrayBuilder(Of AbstractFormattingRule)(1 + coreRules.Length)
            result.Add(_memberSeparationRule)
            result.AddRange(coreRules)
            Return result.MoveToImmutable()
        End Function

        Protected Overrides Function GetReducers() As ImmutableArray(Of AbstractReducer)
            Return ImmutableArray.Create(Of AbstractReducer)(
                New VisualBasicNameReducer(),
                New VisualBasicEscapingReducer(),
                New VisualBasicParenthesesReducer())
        End Function

        Private Class FormattingRule
            Inherits CompatAbstractMetadataFormattingRule

            Protected Overrides Function GetAdjustNewLinesOperationBetweenMembersAndUsings(token1 As SyntaxToken, token2 As SyntaxToken) As AdjustNewLinesOperation
                If token1.Kind = SyntaxKind.None OrElse token2.Kind = SyntaxKind.None Then
                    Return Nothing
                End If

                If Not token1.IsLastTokenOfStatement() Then
                    Return Nothing
                End If

                Dim member1 = token1.Parent.FirstAncestorOrSelf(Of DeclarationStatementSyntax)()
                Dim member2 = token2.Parent.FirstAncestorOrSelf(Of DeclarationStatementSyntax)()

                If member1 Is Nothing OrElse member2 Is Nothing OrElse member1 Is member2 Then
                    Return Nothing
                End If

                ' If we have two members in a type or if we have an Imports statements followed by a type
                ' declaration, then we are interested - otherwise bail out.
                If Not (ValidTopLevelDeclaration(member1) AndAlso ValidTopLevelDeclaration(member2)) AndAlso
                   Not (member1.Kind = SyntaxKind.ImportsStatement AndAlso TypeOf member2 Is TypeStatementSyntax) Then
                    Return Nothing
                End If

                ' If we have two members of the same kind, we won't insert a blank line 
                If member1.Kind = member2.Kind Then
                    Return FormattingOperations.CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.ForceLines)
                End If

                ' Force a blank line between the two nodes by counting the number of lines of
                ' trivia and adding one to it.
                Dim triviaList = token1.TrailingTrivia.Concat(token2.LeadingTrivia)
                Return FormattingOperations.CreateAdjustNewLinesOperation(GetNumberOfLines(triviaList) + 1, AdjustNewLinesOption.ForceLines)
            End Function

            Public Overrides Sub AddAnchorIndentationOperationsSlow(list As List(Of AnchorIndentationOperation), node As SyntaxNode, ByRef nextOperation As NextAnchorIndentationOperationAction)
                Return
            End Sub

            Protected Overrides Function IsNewLine(c As Char) As Boolean
                Return c = vbCr OrElse c = vbLf OrElse SyntaxFacts.IsNewLine(c)
            End Function

            Private Shared Function ValidTopLevelDeclaration(node As DeclarationStatementSyntax) As Boolean
                Select Case node.Kind
                    Case SyntaxKind.SubStatement,
                         SyntaxKind.FunctionStatement,
                         SyntaxKind.SubNewStatement,
                         SyntaxKind.OperatorStatement,
                         SyntaxKind.PropertyStatement,
                         SyntaxKind.EventStatement,
                         SyntaxKind.RaiseEventStatement,
                         SyntaxKind.FieldDeclaration
                        Return True
                End Select

                Return False
            End Function
        End Class

        Private Class DocCommentConverter
            Inherits VisualBasicSyntaxRewriter

            Private ReadOnly _formattingService As IDocumentationCommentFormattingService
            Private ReadOnly _cancellationToken As CancellationToken

            Public Sub New(formattingService As IDocumentationCommentFormattingService, cancellationToken As CancellationToken)
                MyBase.New(visitIntoStructuredTrivia:=False)
                Me._formattingService = formattingService
                Me._cancellationToken = cancellationToken
            End Sub

            Public Shared Function ConvertToRegularComments(node As SyntaxNode, formattingService As IDocumentationCommentFormattingService, cancellationToken As CancellationToken) As SyntaxNode
                Dim converter = New DocCommentConverter(formattingService, cancellationToken)

                Return converter.Visit(node)
            End Function

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                Me._cancellationToken.ThrowIfCancellationRequested()

                If node Is Nothing Then
                    Return node
                End If

                ' Process children first
                node = MyBase.Visit(node)

                ' Check the leading trivia for doc comments
                If node.GetLeadingTrivia().Any(SyntaxKind.DocumentationCommentTrivia) Then
                    Dim newLeadingTrivia = New List(Of SyntaxTrivia)()

                    For Each trivia In node.GetLeadingTrivia()
                        If trivia.Kind = SyntaxKind.DocumentationCommentTrivia Then
                            newLeadingTrivia.Add(SyntaxFactory.CommentTrivia("'"))
                            newLeadingTrivia.Add(SyntaxFactory.ElasticCarriageReturnLineFeed)

                            Dim structuredTrivia = DirectCast(trivia.GetStructure(), DocumentationCommentTriviaSyntax)
                            newLeadingTrivia.AddRange(ConvertDocCommentToRegularComment(structuredTrivia))
                        Else
                            newLeadingTrivia.Add(trivia)
                        End If
                    Next

                    node = node.WithLeadingTrivia(newLeadingTrivia)
                End If

                Return node
            End Function

            Private Iterator Function ConvertDocCommentToRegularComment(structuredTrivia As DocumentationCommentTriviaSyntax) As IEnumerable(Of SyntaxTrivia)
                Dim xmlFragment = DocumentationCommentUtilities.ExtractXMLFragment(structuredTrivia.ToFullString(), "'''")

                Dim docComment = DocumentationComment.FromXmlFragment(xmlFragment)

                Dim commentLines = DocCommentFormatter.Format(Me._formattingService, docComment)

                For Each line In commentLines
                    If Not String.IsNullOrWhiteSpace(line) Then
                        Yield SyntaxFactory.CommentTrivia("' " + line)
                    Else
                        Yield SyntaxFactory.CommentTrivia("'")
                    End If

                    Yield SyntaxFactory.ElasticCarriageReturnLineFeed
                Next
            End Function
        End Class
    End Class
End Namespace
