' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    ' the default provider that will be called by the engine at the end of provider's chain.
    ' there is no way for a user to be remove this provider.
    '
    ' to reduce number of unnecessary heap allocations, most of them just return null.
    Friend NotInheritable Class DefaultOperationProvider
        Inherits CompatAbstractFormattingRule

        Public Shared ReadOnly Instance As New DefaultOperationProvider()

        Private ReadOnly _options As SyntaxFormattingOptions

        Private Sub New()
            MyClass.New(VisualBasicSyntaxFormattingOptions.Default)
        End Sub

        Private Sub New(options As SyntaxFormattingOptions)
            _options = options
        End Sub

        Public Overrides Function WithOptions(options As SyntaxFormattingOptions) As AbstractFormattingRule
            If _options.SeparateImportDirectiveGroups = options.SeparateImportDirectiveGroups Then
                Return Me
            End If

            Return New DefaultOperationProvider(options)
        End Function

        Public Overrides Sub AddSuppressOperationsSlow(operations As ArrayBuilder(Of SuppressOperation), node As SyntaxNode, ByRef nextAction As NextSuppressOperationAction)
        End Sub

        Public Overrides Sub AddAnchorIndentationOperationsSlow(operations As List(Of AnchorIndentationOperation), node As SyntaxNode, ByRef nextAction As NextAnchorIndentationOperationAction)
        End Sub

        Public Overrides Sub AddIndentBlockOperationsSlow(operations As List(Of IndentBlockOperation), node As SyntaxNode, ByRef nextAction As NextIndentBlockOperationAction)
        End Sub

        Public Overrides Sub AddAlignTokensOperationsSlow(operations As List(Of AlignTokensOperation), node As SyntaxNode, ByRef nextAction As NextAlignTokensOperationAction)
        End Sub

        <PerformanceSensitive("https://github.com/dotnet/roslyn/issues/30819", AllowCaptures:=False, AllowImplicitBoxing:=False)>
        Public Overrides Function GetAdjustNewLinesOperationSlow(
                ByRef previousToken As SyntaxToken,
                ByRef currentToken As SyntaxToken,
                ByRef nextOperation As NextGetAdjustNewLinesOperation) As AdjustNewLinesOperation
            If previousToken.Parent Is Nothing Then
                Return Nothing
            End If

            Dim combinedTrivia = (previousToken.TrailingTrivia, currentToken.LeadingTrivia)

            Dim lastTrivia = LastOrDefaultTrivia(
                combinedTrivia,
                Function(trivia As SyntaxTrivia) ColonOrLineContinuationTrivia(trivia))

            If lastTrivia.RawKind = SyntaxKind.ColonTrivia Then
                Return FormattingOperations.CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines)
            ElseIf lastTrivia.RawKind = SyntaxKind.LineContinuationTrivia AndAlso previousToken.Parent.GetAncestorsOrThis(Of SyntaxNode)().Any(Function(node As SyntaxNode) IsSingleLineIfOrElseClauseSyntax(node)) Then
                Return Nothing
            End If

            ' return line break operation after statement terminator token so that we can enforce
            ' indentation for the line
            Dim previousStatement As StatementSyntax = Nothing
            If previousToken.IsLastTokenOfStatement(statement:=previousStatement) AndAlso ContainEndOfLine(previousToken, currentToken) AndAlso currentToken.Kind <> SyntaxKind.EmptyToken Then
                Return AdjustNewLinesBetweenStatements(previousStatement, currentToken)
            End If

            If previousToken.Kind = SyntaxKind.GreaterThanToken AndAlso previousToken.Parent IsNot Nothing AndAlso TypeOf previousToken.Parent Is AttributeListSyntax Then

                ' This AttributeList is the last applied attribute
                ' If this AttributeList belongs to a parameter then apply no line operation
                If previousToken.Parent.Parent IsNot Nothing AndAlso TypeOf previousToken.Parent.Parent Is ParameterSyntax Then
                    Return Nothing
                End If

                Return FormattingOperations.CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines)
            End If

            If currentToken.Kind = SyntaxKind.LessThanToken AndAlso currentToken.Parent IsNot Nothing AndAlso TypeOf currentToken.Parent Is AttributeListSyntax Then

                ' The case of the previousToken belonging to another AttributeList is handled in the previous condition
                If (previousToken.Kind = SyntaxKind.CommaToken OrElse previousToken.Kind = SyntaxKind.OpenParenToken) AndAlso
                   currentToken.Parent.Parent IsNot Nothing AndAlso TypeOf currentToken.Parent.Parent Is ParameterSyntax Then
                    Return Nothing
                End If

                Return FormattingOperations.CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines)
            End If

            ' return line break operation after xml tag token so that we can enforce indentation for the xml tag
            ' the very first xml literal tag case
            If IsFirstXmlTag(currentToken) Then
                Return Nothing
            End If

            Dim xmlDeclaration = TryCast(previousToken.Parent, XmlDeclarationSyntax)
            If xmlDeclaration IsNot Nothing AndAlso xmlDeclaration.GetLastToken(includeZeroWidth:=True) = previousToken Then
                Return FormattingOperations.CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines)
            End If

            If TypeOf previousToken.Parent Is XmlNodeSyntax OrElse TypeOf currentToken.Parent Is XmlNodeSyntax Then
                Return FormattingOperations.CreateAdjustNewLinesOperation(0, AdjustNewLinesOption.PreserveLines)
            End If

            Return Nothing
        End Function

        Private Function AdjustNewLinesBetweenStatements(
                previousStatement As StatementSyntax,
                currentToken As SyntaxToken) As AdjustNewLinesOperation

            ' if the user is separating import-groups, And we're between two imports, and these
            ' imports *should* be separated, then do so (if the imports were already properly
            ' sorted).
            If currentToken.Kind() = SyntaxKind.ImportsKeyword AndAlso
               TypeOf currentToken.Parent Is ImportsStatementSyntax AndAlso
               TypeOf previousStatement Is ImportsStatementSyntax Then

                Dim previousImports = DirectCast(previousStatement, ImportsStatementSyntax)
                Dim currentImports = DirectCast(currentToken.Parent, ImportsStatementSyntax)

                If _options.SeparateImportDirectiveGroups AndAlso
                   ImportsOrganizer.NeedsGrouping(previousImports, currentImports) Then

                    Dim [imports] = DirectCast(previousImports.Parent, CompilationUnitSyntax).Imports
                    If [imports].IsSorted(ImportsStatementComparer.SystemFirstInstance) OrElse
                       [imports].IsSorted(ImportsStatementComparer.NormalInstance) Then

                        ' Force at least one blank line here.
                        Return FormattingOperations.CreateAdjustNewLinesOperation(2, AdjustNewLinesOption.PreserveLines)
                    End If
                End If
            End If

            ' For any other two statements we will normally ensure at least one new-line between
            ' them.
            Return FormattingOperations.CreateAdjustNewLinesOperation(1, AdjustNewLinesOption.PreserveLines)
        End Function

        Private Shared Function IsSingleLineIfOrElseClauseSyntax(node As SyntaxNode) As Boolean
            Return TypeOf node Is SingleLineIfStatementSyntax OrElse TypeOf node Is SingleLineElseClauseSyntax
        End Function

        Private Shared Function ColonOrLineContinuationTrivia(trivia As SyntaxTrivia) As Boolean
            Return trivia.RawKind = SyntaxKind.ColonTrivia OrElse trivia.RawKind = SyntaxKind.LineContinuationTrivia
        End Function

        <PerformanceSensitive("https://github.com/dotnet/roslyn/issues/30819", AllowCaptures:=False, AllowImplicitBoxing:=False)>
        Private Shared Function LastOrDefaultTrivia(triviaListPair As (SyntaxTriviaList, SyntaxTriviaList), predicate As Func(Of SyntaxTrivia, Boolean)) As SyntaxTrivia
            For Each trivia In triviaListPair.Item2.Reverse()
                If predicate(trivia) Then
                    Return trivia
                End If
            Next

            For Each trivia In triviaListPair.Item1.Reverse()
                If predicate(trivia) Then
                    Return trivia
                End If
            Next

            Return Nothing
        End Function

        Private Shared Function ContainEndOfLine(previousToken As SyntaxToken, nextToken As SyntaxToken) As Boolean
            Return previousToken.TrailingTrivia.Any(SyntaxKind.EndOfLineTrivia) OrElse nextToken.LeadingTrivia.Any(SyntaxKind.EndOfLineTrivia)
        End Function

        Private Shared Function IsFirstXmlTag(currentToken As SyntaxToken) As Boolean
            Dim xmlDeclaration = TryCast(currentToken.Parent, XmlDeclarationSyntax)
            If xmlDeclaration IsNot Nothing AndAlso
               xmlDeclaration.LessThanQuestionToken = currentToken AndAlso
               TypeOf xmlDeclaration.Parent Is XmlDocumentSyntax AndAlso
               Not TypeOf xmlDeclaration.Parent.Parent Is XmlNodeSyntax Then
                Return True
            End If

            Dim startTag = TryCast(currentToken.Parent, XmlElementStartTagSyntax)
            If startTag IsNot Nothing AndAlso
               startTag.LessThanToken = currentToken AndAlso
               TypeOf startTag.Parent Is XmlElementSyntax AndAlso
               Not TypeOf startTag.Parent.Parent Is XmlNodeSyntax Then
                Return True
            End If

            Dim emptyTag = TryCast(currentToken.Parent, XmlEmptyElementSyntax)
            If emptyTag IsNot Nothing AndAlso
               emptyTag.LessThanToken = currentToken AndAlso
               Not TypeOf emptyTag.Parent Is XmlNodeSyntax Then
                Return True
            End If

            Return False
        End Function

        ' return 1 space for every token pairs as a default operation
        Public Overrides Function GetAdjustSpacesOperationSlow(ByRef previousToken As SyntaxToken, ByRef currentToken As SyntaxToken, ByRef nextOperation As NextGetAdjustSpacesOperation) As AdjustSpacesOperation
            If previousToken.Kind = SyntaxKind.ColonToken AndAlso
               TypeOf previousToken.Parent Is LabelStatementSyntax AndAlso
               currentToken.Kind <> SyntaxKind.EndOfFileToken Then
                Return FormattingOperations.CreateAdjustSpacesOperation(1, AdjustSpacesOption.DynamicSpaceToIndentationIfOnSingleLine)
            End If

            Dim space As Integer = If(currentToken.Kind = SyntaxKind.EndOfFileToken, 0, 1)
            Return FormattingOperations.CreateAdjustSpacesOperation(space, AdjustSpacesOption.DefaultSpacesIfOnSingleLine)
        End Function
    End Class
End Namespace
