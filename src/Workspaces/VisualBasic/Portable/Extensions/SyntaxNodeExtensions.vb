﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Shared.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions
    Friend Module SyntaxNodeExtensions
        <Extension()>
        Public Function IsParentKind(node As SyntaxNode, kind As SyntaxKind) As Boolean
            Return node IsNot Nothing AndAlso
                   node.Parent.IsKind(kind)
        End Function

        <Extension()>
        Public Function IsKind(node As SyntaxNode, kind1 As SyntaxKind, kind2 As SyntaxKind) As Boolean
            If node Is Nothing Then
                Return False
            End If

            Return node.Kind = kind1 OrElse
                   node.Kind = kind2
        End Function

        <Extension()>
        Public Function IsKind(node As SyntaxNode, kind1 As SyntaxKind, kind2 As SyntaxKind, kind3 As SyntaxKind) As Boolean
            If node Is Nothing Then
                Return False
            End If

            Return node.Kind = kind1 OrElse
                   node.Kind = kind2 OrElse
                   node.Kind = kind3
        End Function

        <Extension()>
        Public Function IsKind(node As SyntaxNode, ParamArray kinds As SyntaxKind()) As Boolean
            If node Is Nothing Then
                Return False
            End If

            Return kinds.Contains(node.Kind())
        End Function

        <Extension()>
        Public Function IsInConstantContext(expression As SyntaxNode) As Boolean
            If expression.GetAncestor(Of ParameterSyntax)() IsNot Nothing Then
                Return True
            End If

            ' TODO(cyrusn): Add more cases
            Return False
        End Function

        <Extension()>
        Public Function IsInStaticContext(node As SyntaxNode) As Boolean
            Dim containingType = node.GetAncestorOrThis(Of TypeBlockSyntax)()
            If containingType.IsKind(SyntaxKind.ModuleBlock) Then
                Return True
            End If

            Return node.GetAncestorsOrThis(Of StatementSyntax)().
                        SelectMany(Function(s) s.GetModifiers()).
                        Any(Function(t) t.Kind = SyntaxKind.SharedKeyword)
        End Function

        <Extension()>
        Public Function IsStatementContainerNode(node As SyntaxNode) As Boolean
            If node.IsExecutableBlock() Then
                Return True
            End If

            Dim singleLineLambdaExpression = TryCast(node, SingleLineLambdaExpressionSyntax)
            If singleLineLambdaExpression IsNot Nothing AndAlso TypeOf singleLineLambdaExpression.Body Is ExecutableStatementSyntax Then
                Return True
            End If

            Return False
        End Function

        <Extension()>
        Public Function GetStatements(node As SyntaxNode) As SyntaxList(Of StatementSyntax)
            Contract.ThrowIfNull(node)
            Contract.ThrowIfFalse(node.IsStatementContainerNode())

            Dim methodBlock = TryCast(node, MethodBlockBaseSyntax)
            If methodBlock IsNot Nothing Then
                Return methodBlock.Statements
            End If

            Dim whileBlock = TryCast(node, WhileBlockSyntax)
            If whileBlock IsNot Nothing Then
                Return whileBlock.Statements
            End If

            Dim usingBlock = TryCast(node, UsingBlockSyntax)
            If usingBlock IsNot Nothing Then
                Return usingBlock.Statements
            End If

            Dim syncLockBlock = TryCast(node, SyncLockBlockSyntax)
            If syncLockBlock IsNot Nothing Then
                Return syncLockBlock.Statements
            End If

            Dim withBlock = TryCast(node, WithBlockSyntax)
            If withBlock IsNot Nothing Then
                Return withBlock.Statements
            End If

            Dim singleLineIfStatement = TryCast(node, SingleLineIfStatementSyntax)
            If singleLineIfStatement IsNot Nothing Then
                Return singleLineIfStatement.Statements
            End If

            Dim singleLineElseClause = TryCast(node, SingleLineElseClauseSyntax)
            If singleLineElseClause IsNot Nothing Then
                Return singleLineElseClause.Statements
            End If

            Dim ifBlock = TryCast(node, MultiLineIfBlockSyntax)
            If ifBlock IsNot Nothing Then
                Return ifBlock.Statements
            End If

            Dim elseBlock = TryCast(node, ElseBlockSyntax)
            If elseBlock IsNot Nothing Then
                Return elseBlock.Statements
            End If

            Dim tryBlock = TryCast(node, TryBlockSyntax)
            If tryBlock IsNot Nothing Then
                Return tryBlock.Statements
            End If

            Dim catchBlock = TryCast(node, CatchBlockSyntax)
            If catchBlock IsNot Nothing Then
                Return catchBlock.Statements
            End If

            Dim finallyBlock = TryCast(node, FinallyBlockSyntax)
            If finallyBlock IsNot Nothing Then
                Return finallyBlock.Statements
            End If

            Dim caseBlock = TryCast(node, CaseBlockSyntax)
            If caseBlock IsNot Nothing Then
                Return caseBlock.Statements
            End If

            Dim doLoopBlock = TryCast(node, DoLoopBlockSyntax)
            If doLoopBlock IsNot Nothing Then
                Return doLoopBlock.Statements
            End If

            Dim forBlock = TryCast(node, ForOrForEachBlockSyntax)
            If forBlock IsNot Nothing Then
                Return forBlock.Statements
            End If

            Dim singleLineLambdaExpression = TryCast(node, SingleLineLambdaExpressionSyntax)
            If singleLineLambdaExpression IsNot Nothing Then
                Return If(TypeOf singleLineLambdaExpression.Body Is StatementSyntax,
                            SyntaxFactory.SingletonList(DirectCast(singleLineLambdaExpression.Body, StatementSyntax)),
                            Nothing)
            End If

            Dim multiLineLambdaExpression = TryCast(node, MultiLineLambdaExpressionSyntax)
            If multiLineLambdaExpression IsNot Nothing Then
                Return multiLineLambdaExpression.Statements
            End If

            Return Contract.FailWithReturn(Of SyntaxList(Of StatementSyntax))("unknown statements container!")
        End Function

        ' Matches the following:
        '
        ' (whitespace* newline)+ 
        Private ReadOnly s_oneOrMoreBlankLines As Matcher(Of SyntaxTrivia)

        ' Matches the following:
        '
        ' (whitespace* comment whitespace* newline)+ OneOrMoreBlankLines
        Private ReadOnly s_bannerMatcher As Matcher(Of SyntaxTrivia)

        ' Used to match the following:
        '
        ' <start-of-file> (whitespace* comment whitespace* newline)+ blankLine*
        Private ReadOnly s_fileBannerMatcher As Matcher(Of SyntaxTrivia)

        Sub New()
            Dim whitespace = Matcher.Repeat(Match(SyntaxKind.WhitespaceTrivia, "\\b"))
            Dim endOfLine = Match(SyntaxKind.EndOfLineTrivia, "\\n")
            Dim singleBlankLine = Matcher.Sequence(whitespace, endOfLine)

            Dim comment = Match(SyntaxKind.CommentTrivia, "'")
            Dim commentLine = Matcher.Sequence(whitespace, comment, whitespace, endOfLine)

            s_oneOrMoreBlankLines = Matcher.OneOrMore(singleBlankLine)
            s_bannerMatcher =
                Matcher.Sequence(
                    Matcher.OneOrMore(commentLine),
                    s_oneOrMoreBlankLines)
            s_fileBannerMatcher =
                Matcher.Sequence(
                    Matcher.OneOrMore(commentLine),
                    Matcher.Repeat(singleBlankLine))
        End Sub

        Private Function Match(kind As SyntaxKind, description As String) As Matcher(Of SyntaxTrivia)
            Return Matcher.Single(Of SyntaxTrivia)(Function(t) t.Kind = kind, description)
        End Function

        <Extension()>
        Friend Function IsMultiLineLambda(lambda As LambdaExpressionSyntax) As Boolean
            Return lambda.Kind = SyntaxKind.MultiLineSubLambdaExpression OrElse
                   lambda.Kind = SyntaxKind.MultiLineFunctionLambdaExpression
        End Function

        <Extension()>
        Friend Function WithPrependedLeadingTrivia(Of T As SyntaxNode)(node As T, ParamArray trivia As SyntaxTrivia()) As T
            Return node.WithPrependedLeadingTrivia(DirectCast(trivia, IEnumerable(Of SyntaxTrivia)))
        End Function

        <Extension()>
        Friend Function WithPrependedLeadingTrivia(Of T As SyntaxNode)(node As T, trivia As IEnumerable(Of SyntaxTrivia)) As T
            Return DirectCast(node.WithLeadingTrivia(trivia.Concat(node.GetLeadingTrivia())), T)
        End Function

        <Extension()>
        Friend Function WithAppendedTrailingTrivia(Of T As SyntaxNode)(node As T, ParamArray trivia As SyntaxTrivia()) As T
            Return node.WithAppendedTrailingTrivia(DirectCast(trivia, IEnumerable(Of SyntaxTrivia)))
        End Function

        <Extension()>
        Friend Function WithAppendedTrailingTrivia(Of T As SyntaxNode)(node As T, trivia As IEnumerable(Of SyntaxTrivia)) As T
            Return DirectCast(node.WithTrailingTrivia(node.GetTrailingTrivia().Concat(trivia)), T)
        End Function

        <Extension()>
        Friend Function GetTypeCharacterString(type As TypeCharacter) As String
            Select Case type
                Case TypeCharacter.Integer
                    Return "%"
                Case TypeCharacter.Long
                    Return "&"
                Case TypeCharacter.Decimal
                    Return "@"
                Case TypeCharacter.Single
                    Return "!"
                Case TypeCharacter.Double
                    Return "#"
                Case TypeCharacter.String
                    Return "$"
                Case Else
                    Throw New ArgumentException("Unexpected TypeCharacter.", NameOf(type))
            End Select
        End Function

        <Extension()>
        Public Function SpansPreprocessorDirective(Of TSyntaxNode As SyntaxNode)(list As IEnumerable(Of TSyntaxNode)) As Boolean
            If list Is Nothing OrElse Not list.Any() Then
                Return False
            End If

            Dim tokens = list.SelectMany(Function(n) n.DescendantTokens())

            Return tokens.SpansPreprocessorDirective()
        End Function

        <Extension()>
        Public Function ConvertToSingleLine(Of TNode As SyntaxNode)(node As TNode) As TNode
            If node Is Nothing Then
                Return node
            End If

            Dim rewriter = New SingleLineRewriter()
            Return DirectCast(rewriter.Visit(node), TNode)
        End Function

        ''' <summary>
        ''' Breaks up the list of provided nodes, based on how they are 
        ''' interspersed with pp directives, into groups.  Within these groups
        ''' nodes can be moved around safely, without breaking any pp 
        ''' constructs.
        ''' </summary>
        <Extension()>
        Public Function SplitNodesOnPreprocessorBoundaries(Of TSyntaxNode As SyntaxNode)(
            nodes As IEnumerable(Of TSyntaxNode),
            cancellationToken As CancellationToken) As IList(Of IList(Of TSyntaxNode))

            Dim result = New List(Of IList(Of TSyntaxNode))()

            Dim currentGroup = New List(Of TSyntaxNode)()
            For Each node In nodes
                Dim hasUnmatchedInteriorDirective = node.ContainsInterleavedDirective(cancellationToken)
                Dim hasLeadingDirective = node.GetLeadingTrivia().Any(Function(t) SyntaxFacts.IsPreprocessorDirective(t.Kind))

                If hasUnmatchedInteriorDirective Then
                    ' we have a #if/#endif/#region/#endregion/#else/#elif in
                    ' this node that belongs to a span of pp directives that
                    ' is not entirely contained within the node.  i.e.:
                    '
                    '   void Foo() {
                    '      #if ...
                    '   }
                    '
                    ' This node cannot be moved at all.  It is in a group that
                    ' only contains itself (and thus can never be moved).

                    ' add whatever group we've built up to now. And reset the 
                    ' next group to empty.
                    result.Add(currentGroup)
                    currentGroup = New List(Of TSyntaxNode)()

                    result.Add(New List(Of TSyntaxNode) From {node})
                ElseIf hasLeadingDirective Then
                    ' We have a PP directive before us.  i.e.:
                    ' 
                    '   #if ...
                    '      void Foo() {
                    '
                    ' That means we start a new group that is contained between
                    ' the above directive and the following directive.

                    ' add whatever group we've built up to now. And reset the 
                    ' next group to empty.
                    result.Add(currentGroup)
                    currentGroup = New List(Of TSyntaxNode)()

                    currentGroup.Add(node)
                Else
                    ' simple case.  just add ourselves to the current group
                    currentGroup.Add(node)
                End If
            Next

            ' add the remainder of the final group.
            result.Add(currentGroup)

            ' Now, filter out any empty groups.
            result = result.Where(Function(group) Not group.IsEmpty()).ToList()
            Return result
        End Function

        ''' <summary>
        ''' Returns true if the passed in node contains an interleaved pp 
        ''' directive.
        ''' 
        ''' i.e. The following returns false:
        ''' 
        '''   void Foo() {
        ''' #if true
        ''' #endif
        '''   }
        ''' 
        ''' #if true
        '''   void Foo() {
        '''   }
        ''' #endif
        ''' 
        ''' but these return true:
        ''' 
        ''' #if true
        '''   void Foo() {
        ''' #endif
        '''   }
        ''' 
        '''   void Foo() {
        ''' #if true
        '''   }
        ''' #endif
        ''' 
        ''' #if true
        '''   void Foo() {
        ''' #else
        '''   }
        ''' #endif
        ''' 
        ''' i.e. the method returns true if it contains a PP directive that 
        ''' belongs to a grouping constructs (like #if/#endif or 
        ''' #region/#endregion), but the grouping construct isn't entirely c
        ''' contained within the span of the node.
        ''' </summary>
        <Extension()>
        Public Function ContainsInterleavedDirective(node As SyntaxNode, cancellationToken As CancellationToken) As Boolean
            ' Check if this node contains a start or end pp construct whose
            ' matching construct is not contained within this node.  If so, 
            ' this node must be pinned and cannot move.
            '
            ' Also, keep track of those spans so that if we see #else/#elif we
            ' can tell if they belong to a pp span that is entirely within the
            ' node.
            Dim span = node.Span
            Return node.DescendantTokens().Any(Function(token) ContainsInterleavedDirective(span, token, cancellationToken))
        End Function

        Private Function ContainsInterleavedDirective(
            textSpan As TextSpan,
            token As SyntaxToken,
            cancellationToken As CancellationToken) As Boolean

            Return ContainsInterleavedDirective(textSpan, token.LeadingTrivia, cancellationToken) OrElse
                ContainsInterleavedDirective(textSpan, token.TrailingTrivia, cancellationToken)
        End Function

        Private Function ContainsInterleavedDirective(
            textSpan As TextSpan,
            list As SyntaxTriviaList,
            cancellationToken As CancellationToken) As Boolean

            For Each trivia In list
                If textSpan.Contains(trivia.Span) Then
                    If ContainsInterleavedDirective(textSpan, trivia, cancellationToken) Then
                        Return True
                    End If
                End If
            Next trivia

            Return False
        End Function

        Private Function ContainsInterleavedDirective(
            textSpan As TextSpan,
            trivia As SyntaxTrivia,
            cancellationToken As CancellationToken) As Boolean

            If trivia.HasStructure AndAlso TypeOf trivia.GetStructure() Is DirectiveTriviaSyntax Then
                Dim parentSpan = trivia.GetStructure().Span
                Dim directiveSyntax = DirectCast(trivia.GetStructure(), DirectiveTriviaSyntax)
                If directiveSyntax.IsKind(SyntaxKind.RegionDirectiveTrivia, SyntaxKind.EndRegionDirectiveTrivia, SyntaxKind.IfDirectiveTrivia, SyntaxKind.EndIfDirectiveTrivia) Then
                    Dim match = directiveSyntax.GetMatchingStartOrEndDirective(cancellationToken)
                    If match IsNot Nothing Then
                        Dim matchSpan = match.Span
                        If Not textSpan.Contains(matchSpan.Start) Then
                            ' The match for this pp directive is outside
                            ' this node.
                            Return True
                        End If
                    End If
                ElseIf directiveSyntax.IsKind(SyntaxKind.ElseDirectiveTrivia, SyntaxKind.ElseIfDirectiveTrivia) Then
                    Dim directives = directiveSyntax.GetMatchingConditionalDirectives(cancellationToken)
                    If directives IsNot Nothing AndAlso directives.Count > 0 Then
                        If Not textSpan.Contains(directives(0).SpanStart) OrElse
                           Not textSpan.Contains(directives(directives.Count - 1).SpanStart) Then
                            ' This else/elif belongs to a pp span that isn't 
                            ' entirely within this node.
                            Return True
                        End If
                    End If
                End If
            End If

            Return False
        End Function


        <Extension()>
        Public Function GetLeadingBlankLines(Of TSyntaxNode As SyntaxNode)(node As TSyntaxNode) As IEnumerable(Of SyntaxTrivia)
            Dim blankLines As IEnumerable(Of SyntaxTrivia) = Nothing
            node.GetNodeWithoutLeadingBlankLines(blankLines)
            Return blankLines
        End Function

        <Extension()>
        Public Function GetNodeWithoutLeadingBlankLines(Of TSyntaxNode As SyntaxNode)(node As TSyntaxNode) As TSyntaxNode
            Dim blankLines As IEnumerable(Of SyntaxTrivia) = Nothing
            Return node.GetNodeWithoutLeadingBlankLines(blankLines)
        End Function

        <Extension()>
        Public Function GetNodeWithoutLeadingBlankLines(Of TSyntaxNode As SyntaxNode)(
            node As TSyntaxNode, ByRef strippedTrivia As IEnumerable(Of SyntaxTrivia)) As TSyntaxNode

            Dim leadingTriviaToKeep = New List(Of SyntaxTrivia)(node.GetLeadingTrivia())

            Dim index = 0
            s_oneOrMoreBlankLines.TryMatch(leadingTriviaToKeep, index)

            strippedTrivia = New List(Of SyntaxTrivia)(leadingTriviaToKeep.Take(index))

            Return DirectCast(node.WithLeadingTrivia(leadingTriviaToKeep.Skip(index)), TSyntaxNode)
        End Function

        <Extension()>
        Public Function GetLeadingBannerAndPreprocessorDirectives(Of TSyntaxNode As SyntaxNode)(node As TSyntaxNode) As IEnumerable(Of SyntaxTrivia)
            Dim leadingTrivia As IEnumerable(Of SyntaxTrivia) = Nothing
            node.GetNodeWithoutLeadingBannerAndPreprocessorDirectives(leadingTrivia)
            Return leadingTrivia
        End Function

        <Extension()>
        Public Function GetNodeWithoutLeadingBannerAndPreprocessorDirectives(Of TSyntaxNode As SyntaxNode)(node As TSyntaxNode) As TSyntaxNode
            Dim strippedTrivia As IEnumerable(Of SyntaxTrivia) = Nothing
            Return node.GetNodeWithoutLeadingBannerAndPreprocessorDirectives(strippedTrivia)
        End Function

        <Extension()>
        Public Function GetNodeWithoutLeadingBannerAndPreprocessorDirectives(Of TSyntaxNode As SyntaxNode)(
            node As TSyntaxNode,
            ByRef strippedTrivia As IEnumerable(Of SyntaxTrivia)) As TSyntaxNode

            Dim leadingTrivia = node.GetLeadingTrivia()

            ' Rules for stripping trivia: 
            ' 1) If there is a pp directive, then it (and all preceding trivia) *must* be stripped.
            '    This rule supersedes all other rules.
            ' 2) If there is a doc comment, it cannot be stripped.  Even if there is a doc comment,
            '    followed by 5 new lines, then the doc comment still must stay with the node.  This
            '    rule does *not* supersede rule 1.
            ' 3) Single line comments in a group (i.e. with no blank lines between them) belong to
            '    the node *iff* there is no blank line between it and the following trivia.

            Dim leadingTriviaToStrip, leadingTriviaToKeep As List(Of SyntaxTrivia)

            Dim ppIndex = -1
            For i = leadingTrivia.Count - 1 To 0 Step -1
                If SyntaxFacts.IsPreprocessorDirective(leadingTrivia(i).Kind) Then
                    ppIndex = i
                    Exit For
                End If
            Next

            If ppIndex <> -1 Then
                ' We have a pp directive.  it (and all previous trivia) must be stripped.
                leadingTriviaToStrip = New List(Of SyntaxTrivia)(leadingTrivia.Take(ppIndex + 1))
                leadingTriviaToKeep = New List(Of SyntaxTrivia)(leadingTrivia.Skip(ppIndex + 1))
            Else
                leadingTriviaToKeep = New List(Of SyntaxTrivia)(leadingTrivia)
                leadingTriviaToStrip = New List(Of SyntaxTrivia)()
            End If

            ' Now, consume as many banners as we can.  s_fileBannerMatcher will only be matched at
            ' the start of the file.
            Dim index = 0
            While (
                s_oneOrMoreBlankLines.TryMatch(leadingTriviaToKeep, index) OrElse
                s_bannerMatcher.TryMatch(leadingTriviaToKeep, index) OrElse
                (node.FullSpan.Start = 0 AndAlso s_fileBannerMatcher.TryMatch(leadingTriviaToKeep, index)))
            End While

            leadingTriviaToStrip.AddRange(leadingTriviaToKeep.Take(index))

            strippedTrivia = leadingTriviaToStrip
            Return DirectCast(node.WithLeadingTrivia(leadingTriviaToKeep.Skip(index)), TSyntaxNode)
        End Function

        ''' <summary>
        ''' Returns true if this is a block that can contain multiple executable statements.  i.e.
        ''' this node is the VB equivalent of a BlockSyntax in C#.
        ''' </summary>
        <Extension()>
        Public Function IsExecutableBlock(node As SyntaxNode) As Boolean
            If node IsNot Nothing Then
                If TypeOf node Is MethodBlockBaseSyntax OrElse
                   TypeOf node Is DoLoopBlockSyntax OrElse
                   TypeOf node Is ForOrForEachBlockSyntax OrElse
                   TypeOf node Is MultiLineLambdaExpressionSyntax Then
                    Return True
                End If

                Select Case node.Kind
                    Case SyntaxKind.WhileBlock,
                         SyntaxKind.UsingBlock,
                         SyntaxKind.SyncLockBlock,
                         SyntaxKind.WithBlock,
                         SyntaxKind.SingleLineIfStatement,
                         SyntaxKind.SingleLineElseClause,
                         SyntaxKind.SingleLineSubLambdaExpression,
                         SyntaxKind.MultiLineIfBlock,
                         SyntaxKind.ElseBlock,
                         SyntaxKind.TryBlock,
                         SyntaxKind.CatchBlock,
                         SyntaxKind.FinallyBlock,
                         SyntaxKind.CaseBlock,
                         SyntaxKind.CaseElseBlock
                        Return True
                End Select
            End If

            Return False
        End Function

        <Extension()>
        Public Function GetContainingExecutableBlocks(node As SyntaxNode) As IEnumerable(Of SyntaxNode)
            Return node.GetAncestorsOrThis(Of StatementSyntax).
                        Where(Function(s) s.Parent.IsExecutableBlock() AndAlso s.Parent.GetExecutableBlockStatements().Contains(s)).
                        Select(Function(s) s.Parent)
        End Function

        <Extension()>
        Public Function GetContainingMultiLineExecutableBlocks(node As SyntaxNode) As IEnumerable(Of SyntaxNode)
            Return node.GetAncestorsOrThis(Of StatementSyntax).
                        Where(Function(s) s.Parent.IsMultiLineExecutableBlock AndAlso s.Parent.GetExecutableBlockStatements().Contains(s)).
                        Select(Function(s) s.Parent)
        End Function

        <Extension()>
        Public Function FindInnermostCommonExecutableBlock(nodes As IEnumerable(Of SyntaxNode)) As SyntaxNode
            Dim blocks As IEnumerable(Of SyntaxNode) = Nothing
            For Each node In nodes
                blocks = If(blocks Is Nothing,
                             node.GetContainingExecutableBlocks(),
                             blocks.Intersect(node.GetContainingExecutableBlocks()))
            Next

            Return If(blocks Is Nothing, Nothing, blocks.FirstOrDefault())
        End Function

        <Extension()>
        Public Function GetExecutableBlockStatements(node As SyntaxNode) As SyntaxList(Of StatementSyntax)
            If node IsNot Nothing Then
                If TypeOf node Is MethodBlockBaseSyntax Then
                    Return DirectCast(node, MethodBlockBaseSyntax).Statements
                ElseIf TypeOf node Is DoLoopBlockSyntax Then
                    Return DirectCast(node, DoLoopBlockSyntax).Statements
                ElseIf TypeOf node Is ForOrForEachBlockSyntax Then
                    Return DirectCast(node, ForOrForEachBlockSyntax).Statements
                ElseIf TypeOf node Is MultiLineLambdaExpressionSyntax Then
                    Return DirectCast(node, MultiLineLambdaExpressionSyntax).Statements
                End If

                Select Case node.Kind
                    Case SyntaxKind.WhileBlock
                        Return DirectCast(node, WhileBlockSyntax).Statements
                    Case SyntaxKind.UsingBlock
                        Return DirectCast(node, UsingBlockSyntax).Statements
                    Case SyntaxKind.SyncLockBlock
                        Return DirectCast(node, SyncLockBlockSyntax).Statements
                    Case SyntaxKind.WithBlock
                        Return DirectCast(node, WithBlockSyntax).Statements
                    Case SyntaxKind.SingleLineIfStatement
                        Return DirectCast(node, SingleLineIfStatementSyntax).Statements
                    Case SyntaxKind.SingleLineElseClause
                        Return DirectCast(node, SingleLineElseClauseSyntax).Statements
                    Case SyntaxKind.SingleLineSubLambdaExpression
                        Return SyntaxFactory.SingletonList(DirectCast(DirectCast(node, SingleLineLambdaExpressionSyntax).Body, StatementSyntax))
                    Case SyntaxKind.MultiLineIfBlock
                        Return DirectCast(node, MultiLineIfBlockSyntax).Statements
                    Case SyntaxKind.ElseBlock
                        Return DirectCast(node, ElseBlockSyntax).Statements
                    Case SyntaxKind.TryBlock
                        Return DirectCast(node, TryBlockSyntax).Statements
                    Case SyntaxKind.CatchBlock
                        Return DirectCast(node, CatchBlockSyntax).Statements
                    Case SyntaxKind.FinallyBlock
                        Return DirectCast(node, FinallyBlockSyntax).Statements
                    Case SyntaxKind.CaseBlock, SyntaxKind.CaseElseBlock
                        Return DirectCast(node, CaseBlockSyntax).Statements
                End Select
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' If the position is inside of token, return that token; otherwise, return the token to right.
        ''' </summary>
        <Extension()>
        Public Function FindTokenOnRightOfPosition(
            root As SyntaxNode,
            position As Integer,
            Optional includeSkipped As Boolean = True,
            Optional includeDirectives As Boolean = False,
            Optional includeDocumentationComments As Boolean = False) As SyntaxToken

            Dim skippedTokenFinder As Func(Of SyntaxTriviaList, Integer, SyntaxToken) = Nothing

            skippedTokenFinder =
                If(includeSkipped, s_findSkippedTokenForward, CType(Nothing, Func(Of SyntaxTriviaList, Integer, SyntaxToken)))

            Return FindTokenHelper.FindTokenOnRightOfPosition(Of CompilationUnitSyntax)(
                    root, position, skippedTokenFinder, includeSkipped, includeDirectives, includeDocumentationComments)
        End Function

        ''' <summary>
        ''' If the position is inside of token, return that token; otherwise, return the token to left. 
        ''' </summary>
        <Extension()>
        Public Function FindTokenOnLeftOfPosition(
            root As SyntaxNode,
            position As Integer,
            Optional includeSkipped As Boolean = True,
            Optional includeDirectives As Boolean = False,
            Optional includeDocumentationComments As Boolean = False) As SyntaxToken

            Dim skippedTokenFinder As Func(Of SyntaxTriviaList, Integer, SyntaxToken) = Nothing

            skippedTokenFinder =
                If(includeSkipped, s_findSkippedTokenBackward, CType(Nothing, Func(Of SyntaxTriviaList, Integer, SyntaxToken)))

            Return FindTokenHelper.FindTokenOnLeftOfPosition(Of CompilationUnitSyntax)(
                    root, position, skippedTokenFinder, includeSkipped, includeDirectives, includeDocumentationComments)
        End Function

        ''' <summary>
        ''' Returns child node or token that contains given position.
        ''' </summary>
        ''' <remarks>
        ''' This is a copy of <see cref="SyntaxNode.ChildThatContainsPosition"/>  that also returns the index of the child node.
        ''' </remarks>
        <Extension>
        Friend Function ChildThatContainsPosition(self As SyntaxNode, position As Integer, ByRef childIndex As Integer) As SyntaxNodeOrToken
            Dim childList = self.ChildNodesAndTokens()
            Dim left As Integer = 0
            Dim right As Integer = childList.Count - 1
            While left <= right
                Dim middle As Integer = left + (right - left) \ 2
                Dim node As SyntaxNodeOrToken = childList(middle)
                Dim span = node.FullSpan
                If position < span.Start Then
                    right = middle - 1
                ElseIf position >= span.End Then
                    left = middle + 1
                Else
                    childIndex = middle
                    Return node
                End If
            End While

            Debug.Assert(Not self.FullSpan.Contains(position), "Position is valid. How could we not find a child?")
            Throw New ArgumentOutOfRangeException(NameOf(position))
        End Function


        ''' <summary>
        ''' Look inside a trivia list for a skipped token that contains the given position.
        ''' </summary>
        Private ReadOnly s_findSkippedTokenForward As Func(Of SyntaxTriviaList, Integer, SyntaxToken) =
            Function(l, p) FindTokenHelper.FindSkippedTokenForward(GetSkippedTokens(l), p)

        ''' <summary>
        ''' Look inside a trivia list for a skipped token that contains the given position.
        ''' </summary>
        Private ReadOnly s_findSkippedTokenBackward As Func(Of SyntaxTriviaList, Integer, SyntaxToken) =
            Function(l, p) FindTokenHelper.FindSkippedTokenBackward(GetSkippedTokens(l), p)

        ''' <summary>
        ''' get skipped tokens from the trivia list
        ''' </summary>
        Private Function GetSkippedTokens(list As SyntaxTriviaList) As IEnumerable(Of SyntaxToken)
            Return list.Where(Function(t) t.RawKind = SyntaxKind.SkippedTokensTrivia) _
                       .SelectMany(Function(t) DirectCast(t.GetStructure(), SkippedTokensTriviaSyntax).Tokens)
        End Function

        <Extension()>
        Public Function ReplaceStatements(node As SyntaxNode,
                                          statements As SyntaxList(Of StatementSyntax),
                                          ParamArray annotations As SyntaxAnnotation()) As SyntaxNode
            Return node.TypeSwitch(
                Function(x As MethodBlockSyntax) x.WithStatements(statements),
                Function(x As ConstructorBlockSyntax) x.WithStatements(statements),
                Function(x As OperatorBlockSyntax) x.WithStatements(statements),
                Function(x As AccessorBlockSyntax) x.WithStatements(statements),
                Function(x As DoLoopBlockSyntax) x.WithStatements(statements),
                Function(x As ForBlockSyntax) x.WithStatements(statements),
                Function(x As ForEachBlockSyntax) x.WithStatements(statements),
                Function(x As MultiLineLambdaExpressionSyntax) x.WithStatements(statements),
                Function(x As WhileBlockSyntax) x.WithStatements(statements),
                Function(x As UsingBlockSyntax) x.WithStatements(statements),
                Function(x As SyncLockBlockSyntax) x.WithStatements(statements),
                Function(x As WithBlockSyntax) x.WithStatements(statements),
                Function(x As SingleLineIfStatementSyntax) x.WithStatements(statements),
                Function(x As SingleLineElseClauseSyntax) x.WithStatements(statements),
                Function(x As SingleLineLambdaExpressionSyntax) ReplaceSingleLineLambdaExpressionStatements(x, statements, annotations),
                Function(x As MultiLineIfBlockSyntax) x.WithStatements(statements),
                Function(x As ElseBlockSyntax) x.WithStatements(statements),
                Function(x As TryBlockSyntax) x.WithStatements(statements),
                Function(x As CatchBlockSyntax) x.WithStatements(statements),
                Function(x As FinallyBlockSyntax) x.WithStatements(statements),
                Function(x As CaseBlockSyntax) x.WithStatements(statements))
        End Function

        <Extension()>
        Public Function ReplaceSingleLineLambdaExpressionStatements(
                node As SingleLineLambdaExpressionSyntax,
                statements As SyntaxList(Of StatementSyntax),
                ParamArray annotations As SyntaxAnnotation()) As SyntaxNode
            If node.Kind = SyntaxKind.SingleLineSubLambdaExpression Then
                Dim singleLineLambda = DirectCast(node, SingleLineLambdaExpressionSyntax)
                If statements.Count = 1 Then
                    Return node.WithBody(statements.First())
                Else
#If False Then
                    Dim statementsAndSeparators = statements.GetWithSeparators()
                    If statementsAndSeparators.LastOrDefault().IsNode Then
                        statements = Syntax.SeparatedList(Of StatementSyntax)(
                            statementsAndSeparators.Concat(Syntax.Token(SyntaxKind.StatementTerminatorToken)))
                    End If
#End If

                    Return SyntaxFactory.MultiLineSubLambdaExpression(
                        singleLineLambda.SubOrFunctionHeader,
                        statements,
                        SyntaxFactory.EndSubStatement()).WithAdditionalAnnotations(annotations)
                End If
            End If

            ' Can't be called on a single line lambda (as it can't have statements for children)
            Throw New InvalidOperationException()
        End Function

        <Extension()>
        Public Function ReplaceStatements(tree As SyntaxTree,
                                          executableBlock As SyntaxNode,
                                          statements As SyntaxList(Of StatementSyntax),
                                          ParamArray annotations As SyntaxAnnotation()) As SyntaxNode
            If executableBlock.IsSingleLineExecutableBlock() Then
                Return ConvertSingleLineToMultiLineExecutableBlock(tree, executableBlock, statements, annotations)
            End If

            ' TODO(cyrusn): Implement this.
            Throw ExceptionUtilities.Unreachable
        End Function

        <Extension()>
        Public Function IsSingleLineExecutableBlock(executableBlock As SyntaxNode) As Boolean
            Select Case executableBlock.Kind
                Case SyntaxKind.SingleLineElseClause,
                     SyntaxKind.SingleLineIfStatement,
                     SyntaxKind.SingleLineSubLambdaExpression
                    Return executableBlock.GetExecutableBlockStatements().Count = 1
                Case Else
                    Return False
            End Select
        End Function

        <Extension()>
        Public Function IsMultiLineExecutableBlock(node As SyntaxNode) As Boolean
            Return node.IsExecutableBlock AndAlso Not node.IsSingleLineExecutableBlock
        End Function

        Private Sub UpdateStatements(executableBlock As SyntaxNode,
                                     newStatements As SyntaxList(Of StatementSyntax),
                                     annotations As SyntaxAnnotation(),
                                     ByRef singleLineIf As SingleLineIfStatementSyntax,
                                     ByRef multiLineIf As MultiLineIfBlockSyntax)

            Dim ifStatements As SyntaxList(Of StatementSyntax)
            Dim elseStatements As SyntaxList(Of StatementSyntax)

            Select Case executableBlock.Kind
                Case SyntaxKind.SingleLineIfStatement
                    singleLineIf = DirectCast(executableBlock, SingleLineIfStatementSyntax)
                    ifStatements = newStatements
                    elseStatements = If(singleLineIf.ElseClause Is Nothing, Nothing, singleLineIf.ElseClause.Statements)
                Case SyntaxKind.SingleLineElseClause
                    singleLineIf = DirectCast(executableBlock.Parent, SingleLineIfStatementSyntax)
                    ifStatements = singleLineIf.Statements
                    elseStatements = newStatements
                Case Else
                    Return
            End Select

            Dim ifStatement = SyntaxFactory.IfStatement(singleLineIf.IfKeyword, singleLineIf.Condition, singleLineIf.ThenKeyword) _
                                           .WithPrependedLeadingTrivia(SyntaxFactory.ElasticMarker).WithAppendedTrailingTrivia(SyntaxFactory.ElasticMarker)

            Dim elseBlockOpt = If(singleLineIf.ElseClause Is Nothing,
                                  Nothing,
                                  SyntaxFactory.ElseBlock(
                                                    SyntaxFactory.ElseStatement(singleLineIf.ElseClause.ElseKeyword).WithAppendedTrailingTrivia(SyntaxFactory.ElasticMarker),
                                                    elseStatements) _
                                               .WithPrependedLeadingTrivia(SyntaxFactory.ElasticMarker).WithAppendedTrailingTrivia(SyntaxFactory.ElasticMarker))

            Dim [endIf] = SyntaxFactory.EndIfStatement(SyntaxFactory.Token(SyntaxKind.EndKeyword), SyntaxFactory.Token(SyntaxKind.IfKeyword)) _
                                       .WithAdditionalAnnotations(annotations)

            multiLineIf = SyntaxFactory.MultiLineIfBlock(
                                            SyntaxFactory.IfStatement(singleLineIf.IfKeyword, singleLineIf.Condition, singleLineIf.ThenKeyword) _
                                                         .WithPrependedLeadingTrivia(SyntaxFactory.ElasticMarker).WithAppendedTrailingTrivia(SyntaxFactory.ElasticMarker),
                                            ifStatements,
                                            Nothing,
                                            elseBlockOpt,
                                            [endIf]) _
                                       .WithAdditionalAnnotations(annotations)
        End Sub

        <Extension()>
        Public Function ConvertSingleLineToMultiLineExecutableBlock(
                tree As SyntaxTree,
                block As SyntaxNode,
                statements As SyntaxList(Of StatementSyntax),
                ParamArray annotations As SyntaxAnnotation()) As SyntaxNode

            Dim oldBlock = block
            Dim newBlock = block

            Dim current = block
            While current.IsSingleLineExecutableBlock()
                If current.Kind = SyntaxKind.SingleLineIfStatement OrElse current.Kind = SyntaxKind.SingleLineElseClause Then
                    Dim singleLineIf As SingleLineIfStatementSyntax = Nothing
                    Dim multiLineIf As MultiLineIfBlockSyntax = Nothing
                    UpdateStatements(current, statements, annotations, singleLineIf, multiLineIf)

                    statements = SyntaxFactory.List({DirectCast(multiLineIf, StatementSyntax)})

                    current = singleLineIf.Parent

                    oldBlock = singleLineIf
                    newBlock = multiLineIf
                ElseIf current.Kind = SyntaxKind.SingleLineSubLambdaExpression Then
                    Dim singleLineLambda = DirectCast(current, SingleLineLambdaExpressionSyntax)
                    Dim multiLineLambda = SyntaxFactory.MultiLineSubLambdaExpression(
                        singleLineLambda.SubOrFunctionHeader,
                        statements,
                        SyntaxFactory.EndSubStatement()).WithAdditionalAnnotations(annotations)

                    current = singleLineLambda.Parent

                    oldBlock = singleLineLambda
                    newBlock = multiLineLambda

                    Exit While
                Else
                    Exit While
                End If
            End While

            Return tree.GetRoot().ReplaceNode(oldBlock, newBlock)
        End Function

        <Extension()>
        Public Function GetBraces(node As SyntaxNode) As ValueTuple(Of SyntaxToken, SyntaxToken)
            Return node.TypeSwitch(
                Function(n As TypeParameterMultipleConstraintClauseSyntax) ValueTuple.Create(n.OpenBraceToken, n.CloseBraceToken),
                Function(n As ObjectMemberInitializerSyntax) ValueTuple.Create(n.OpenBraceToken, n.CloseBraceToken),
                Function(n As CollectionInitializerSyntax) ValueTuple.Create(n.OpenBraceToken, n.CloseBraceToken),
                Function(n As SyntaxNode) CType(Nothing, ValueTuple(Of SyntaxToken, SyntaxToken)))
        End Function

        <Extension()>
        Public Function GetParentheses(node As SyntaxNode) As ValueTuple(Of SyntaxToken, SyntaxToken)
            Return node.TypeSwitch(
                Function(n As TypeParameterListSyntax) ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                Function(n As ParameterListSyntax) ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                Function(n As ArrayRankSpecifierSyntax) ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                Function(n As ParenthesizedExpressionSyntax) ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                Function(n As GetTypeExpressionSyntax) ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                Function(n As GetXmlNamespaceExpressionSyntax) ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                Function(n As CTypeExpressionSyntax) ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                Function(n As DirectCastExpressionSyntax) ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                Function(n As TryCastExpressionSyntax) ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                Function(n As PredefinedCastExpressionSyntax) ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                Function(n As BinaryConditionalExpressionSyntax) ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                Function(n As TernaryConditionalExpressionSyntax) ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                Function(n As ArgumentListSyntax) ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                Function(n As FunctionAggregationSyntax) ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                Function(n As TypeArgumentListSyntax) ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                Function(n As ExternalSourceDirectiveTriviaSyntax) ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                Function(n As ExternalChecksumDirectiveTriviaSyntax) ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                Function(n As SyntaxNode) CType(Nothing, ValueTuple(Of SyntaxToken, SyntaxToken)))
        End Function

        <Extension>
        Public Function IsLeftSideOfAnyAssignStatement(node As SyntaxNode) As Boolean
            Return node.IsParentKind(SyntaxKind.SimpleAssignmentStatement) AndAlso
                DirectCast(node.Parent, AssignmentStatementSyntax).Left Is node
        End Function

        <Extension>
        Public Function ParentingNodeContainsDiagnostics(node As SyntaxNode) As Boolean
            Dim topMostStatement = node _
                .AncestorsAndSelf() _
                .OfType(Of ExecutableStatementSyntax) _
                .LastOrDefault()

            If topMostStatement IsNot Nothing Then
                Return topMostStatement.ContainsDiagnostics
            End If

            Dim topMostExpression = node _
                .AncestorsAndSelf() _
                .TakeWhile(Function(n) Not TypeOf n Is StatementSyntax) _
                .OfType(Of ExpressionSyntax) _
                .LastOrDefault()

            If topMostExpression.Parent IsNot Nothing Then
                Return topMostExpression.Parent.ContainsDiagnostics
            End If

            Return False
        End Function

        <Extension()>
        Public Function CheckTopLevel(node As SyntaxNode, span As TextSpan) As Boolean
            Dim block = TryCast(node, MethodBlockBaseSyntax)
            If block IsNot Nothing AndAlso block.ContainsInMethodBlockBody(span) Then
                Return True
            End If

            Dim field = TryCast(node, FieldDeclarationSyntax)
            If field IsNot Nothing Then
                For Each declaration In field.Declarators
                    If declaration.Initializer IsNot Nothing AndAlso declaration.Initializer.Span.Contains(span) Then
                        Return True
                    End If
                Next
            End If

            Dim [property] = TryCast(node, PropertyStatementSyntax)
            If [property] IsNot Nothing AndAlso [property].Initializer IsNot Nothing AndAlso [property].Initializer.Span.Contains(span) Then
                Return True
            End If

            Return False
        End Function

        <Extension()>
        Public Function ContainsInMethodBlockBody(block As MethodBlockBaseSyntax, textSpan As TextSpan) As Boolean
            If block Is Nothing Then
                Return False
            End If

            Dim blockSpan = TextSpan.FromBounds(block.BlockStatement.Span.End, block.EndBlockStatement.SpanStart)
            Return blockSpan.Contains(textSpan)
        End Function

        <Extension()>
        Public Function GetMembers(node As SyntaxNode) As SyntaxList(Of StatementSyntax)
            Dim compilation = TryCast(node, CompilationUnitSyntax)
            If compilation IsNot Nothing Then
                Return compilation.Members
            End If

            Dim [namespace] = TryCast(node, NamespaceBlockSyntax)
            If [namespace] IsNot Nothing Then
                Return [namespace].Members
            End If

            Dim type = TryCast(node, TypeBlockSyntax)
            If type IsNot Nothing Then
                Return type.Members
            End If

            Dim [enum] = TryCast(node, EnumBlockSyntax)
            If [enum] IsNot Nothing Then
                Return [enum].Members
            End If

            Return Nothing
        End Function

        <Extension()>
        Public Function GetBodies(node As SyntaxNode) As IEnumerable(Of SyntaxNode)
            Dim method = TryCast(node, MethodBlockBaseSyntax)
            If method IsNot Nothing Then
                Return method.Statements
            End If

            Dim [event] = TryCast(node, EventBlockSyntax)
            If [event] IsNot Nothing Then
                Return [event].Accessors.SelectMany(Function(a) a.Statements)
            End If

            Dim [property] = TryCast(node, PropertyBlockSyntax)
            If [property] IsNot Nothing Then
                Return [property].Accessors.SelectMany(Function(a) a.Statements)
            End If

            Dim field = TryCast(node, FieldDeclarationSyntax)
            If field IsNot Nothing Then
                Return field.Declarators.Where(Function(d) d.Initializer IsNot Nothing).Select(Function(d) d.Initializer.Value).WhereNotNull()
            End If

            Dim initializer As EqualsValueSyntax = Nothing
            Dim [enum] = TryCast(node, EnumMemberDeclarationSyntax)
            If [enum] IsNot Nothing Then
                initializer = [enum].Initializer
            End If

            Dim propStatement = TryCast(node, PropertyStatementSyntax)
            If propStatement IsNot Nothing Then
                initializer = propStatement.Initializer
            End If

            If initializer IsNot Nothing AndAlso initializer.Value IsNot Nothing Then
                Return SpecializedCollections.SingletonEnumerable(initializer.Value)
            End If

            Return SpecializedCollections.EmptyEnumerable(Of SyntaxNode)()
        End Function

        <Extension()>
        Public Iterator Function GetAliasImportsClauses(root As CompilationUnitSyntax) As IEnumerable(Of SimpleImportsClauseSyntax)
            For i = 0 To root.Imports.Count - 1
                Dim statement = root.Imports(i)

                For j = 0 To statement.ImportsClauses.Count - 1
                    Dim importsClause = statement.ImportsClauses(j)

                    If importsClause.Kind = SyntaxKind.SimpleImportsClause Then
                        Dim simpleImportsClause = DirectCast(importsClause, SimpleImportsClauseSyntax)

                        If simpleImportsClause.Alias IsNot Nothing Then
                            Yield simpleImportsClause
                        End If
                    End If
                Next
            Next
        End Function

        ''' <summary>
        ''' Given an expression within a tree of <see cref="ConditionalAccessExpressionSyntax"/>s, 
        ''' finds the <see cref="ConditionalAccessExpressionSyntax"/> that it is part of.
        ''' </summary>
        ''' <param name="node"></param>
        ''' <returns></returns>
        <Extension>
        Friend Function GetCorrespondingConditionalAccessExpression(node As ExpressionSyntax) As ConditionalAccessExpressionSyntax
            Dim access As SyntaxNode = node
            Dim parent As SyntaxNode = access.Parent

            While parent IsNot Nothing
                Select Case parent.Kind
                    Case SyntaxKind.DictionaryAccessExpression,
                         SyntaxKind.SimpleMemberAccessExpression

                        If DirectCast(parent, MemberAccessExpressionSyntax).Expression IsNot access Then
                            Return Nothing
                        End If

                    Case SyntaxKind.XmlElementAccessExpression,
                         SyntaxKind.XmlDescendantAccessExpression,
                         SyntaxKind.XmlAttributeAccessExpression

                        If DirectCast(parent, XmlMemberAccessExpressionSyntax).Base IsNot access Then
                            Return Nothing
                        End If

                    Case SyntaxKind.InvocationExpression

                        If DirectCast(parent, InvocationExpressionSyntax).Expression IsNot access Then
                            Return Nothing
                        End If

                    Case SyntaxKind.ConditionalAccessExpression

                        Dim conditional = DirectCast(parent, ConditionalAccessExpressionSyntax)
                        If conditional.WhenNotNull Is access Then
                            Return conditional
                        ElseIf conditional.Expression IsNot access Then
                            Return Nothing
                        End If

                    Case Else
                        Return Nothing
                End Select

                access = parent
                parent = access.Parent
            End While

            Return Nothing
        End Function
    End Module
End Namespace
