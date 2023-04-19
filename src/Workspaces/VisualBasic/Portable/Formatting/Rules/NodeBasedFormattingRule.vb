' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
    Friend Class NodeBasedFormattingRule
        Inherits BaseFormattingRule
        Friend Const Name As String = "VisualBasic Node Based Formatting Rule"

        Public Overrides Sub AddAnchorIndentationOperationsSlow(operations As List(Of AnchorIndentationOperation),
                                                            node As SyntaxNode,
                                                            ByRef nextOperation As NextAnchorIndentationOperationAction)
            nextOperation.Invoke()

            If TypeOf node Is StatementSyntax AndAlso Not IsBlockSyntax(node) Then
                Dim baseToken = node.GetFirstToken(includeZeroWidth:=True)
                AddAnchorIndentationOperation(operations, baseToken, node.GetLastToken(includeZeroWidth:=True))
                Return
            End If

            Dim queryClause = TryCast(node, QueryClauseSyntax)
            If queryClause IsNot Nothing Then
                AddAnchorIndentationOperation(operations, queryClause.GetFirstToken(includeZeroWidth:=True), queryClause.GetLastToken(includeZeroWidth:=True))
                Return
            End If

            Dim xmlNode = TryCast(node, XmlNodeSyntax)
            If xmlNode IsNot Nothing Then
                Dim baseToken = xmlNode.GetFirstToken(includeZeroWidth:=True)
                AddAnchorIndentationOperation(operations, baseToken, xmlNode.GetLastToken(includeZeroWidth:=True))
                Return
            End If
        End Sub

        Private Shared Function IsBlockSyntax(node As SyntaxNode) As Boolean
            Dim pair = GetFirstAndLastMembers(node)
            If pair.Equals(Nothing) Then
                Return False
            End If

            Return True
        End Function

        Public Overrides Sub AddIndentBlockOperationsSlow(operations As List(Of IndentBlockOperation),
                                                      node As SyntaxNode,
                                                      ByRef nextOperation As NextIndentBlockOperationAction)
            nextOperation.Invoke()

            Dim xmlDocument = TryCast(node, XmlDocumentSyntax)
            If xmlDocument IsNot Nothing Then
                Dim baseToken = xmlDocument.Declaration.GetFirstToken(includeZeroWidth:=True)
                SetAlignmentBlockOperation(operations,
                                           baseToken,
                                           baseToken.GetNextToken(includeZeroWidth:=True),
                                           xmlDocument.GetLastToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True))
                Return
            End If

            Dim xmlEmptyElement = TryCast(node, XmlEmptyElementSyntax)
            If xmlEmptyElement IsNot Nothing Then
                Dim baseToken = xmlEmptyElement.GetFirstToken(includeZeroWidth:=True)
                Dim startToken = baseToken.GetNextToken(includeZeroWidth:=True)

                AddXmlEmptyElement(operations, xmlEmptyElement, baseToken, startToken, xmlEmptyElement.SlashGreaterThanToken)
                Return
            End If

            Dim xmlElementStartTag = TryCast(node, XmlElementStartTagSyntax)
            If xmlElementStartTag IsNot Nothing Then
                Dim baseToken = xmlElementStartTag.GetFirstToken(includeZeroWidth:=True)
                Dim startToken = baseToken.GetNextToken(includeZeroWidth:=True)
                Dim endToken = xmlElementStartTag.GreaterThanToken

                AddIndentBlockOperation(operations, baseToken, startToken, endToken, TextSpan.FromBounds(startToken.SpanStart, endToken.Span.End))
                Return
            End If

            Dim xmlNode = TryCast(node, XmlElementSyntax)
            If xmlNode IsNot Nothing Then
                Dim baseToken = xmlNode.StartTag.LessThanToken
                Dim startToken = baseToken.GetNextToken(includeZeroWidth:=True)

                AddXmlElementIndentBlockOperation(operations,
                                                  xmlNode,
                                                  baseToken,
                                                  xmlNode.StartTag.LessThanToken.GetNextToken(includeZeroWidth:=True),
                                                  xmlNode.EndTag.GreaterThanToken,
                                                  xmlNode.StartTag.GreaterThanToken.GetNextToken(includeZeroWidth:=True),
                                                  xmlNode.EndTag.LessThanSlashToken.GetPreviousToken(includeZeroWidth:=True))
                Return
            End If

            Dim xmlEmbeddedExpression = TryCast(node, XmlEmbeddedExpressionSyntax)
            If xmlEmbeddedExpression IsNot Nothing Then
                SetAlignmentBlockOperation(operations,
                                           xmlEmbeddedExpression.LessThanPercentEqualsToken,
                                           xmlEmbeddedExpression.Expression.GetFirstToken(includeZeroWidth:=True),
                                           xmlEmbeddedExpression.PercentGreaterThanToken)

                AddIndentBlockOperation(operations,
                                        xmlEmbeddedExpression.LessThanPercentEqualsToken,
                                        xmlEmbeddedExpression.LessThanPercentEqualsToken.GetNextToken(includeZeroWidth:=True),
                                        xmlEmbeddedExpression.PercentGreaterThanToken.GetPreviousToken(includeZeroWidth:=True))
                Return
            End If

            Dim multiLineLambda = TryCast(node, MultiLineLambdaExpressionSyntax)
            If multiLineLambda IsNot Nothing Then
                ' unlike C#, we need to consider statement terminator token when setting range for indentation
                Dim baseToken = multiLineLambda.SubOrFunctionHeader.GetFirstToken(includeZeroWidth:=True)
                Dim lastBeginningToken = If(multiLineLambda.SubOrFunctionHeader.GetLastToken().Kind = SyntaxKind.None, multiLineLambda.SubOrFunctionHeader.GetLastToken(includeZeroWidth:=True), multiLineLambda.SubOrFunctionHeader.GetLastToken())

                SetAlignmentBlockOperation(operations, baseToken,
                                        baseToken.GetNextToken(includeZeroWidth:=True),
                                        multiLineLambda.GetLastToken(includeZeroWidth:=True))

                AddIndentBlockOperation(operations, baseToken,
                                        lastBeginningToken.GetNextToken(includeZeroWidth:=True),
                                        multiLineLambda.EndSubOrFunctionStatement.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True))
                Return
            End If

            Dim label = TryCast(node, LabelStatementSyntax)
            If label IsNot Nothing Then
                AddAbsolutePositionIndentBlockOperation(operations, label.LabelToken, label.LabelToken, 0, label.LabelToken.Span)
                Return
            End If

            Dim pair = GetFirstAndLastMembers(node)
            If pair.Equals(Nothing) Then
                Return
            End If

            Dim Item1PreviousToken = pair.Item1.GetPreviousToken()
            If (Item1PreviousToken.IsKind(SyntaxKind.GreaterThanToken) AndAlso
                Item1PreviousToken.Parent.IsKind(SyntaxKind.XmlElementEndTag)) Then

                Dim outerBlockWithBaseToken = GetOuterBlockWithDifferentStartTokenUsingXmlElement(pair.Item1)
                If outerBlockWithBaseToken IsNot Nothing Then
                    AddIndentBlockOperation(operations, outerBlockWithBaseToken.GetFirstToken(), pair.Item1, pair.Item2)
                    Return
                End If
            End If

            Dim caseBlock = TryCast(node, CaseBlockSyntax)
            If caseBlock IsNot Nothing Then
                Dim nextTokenAfterCase = pair.Item2.GetNextToken()
                If nextTokenAfterCase.IsKind(SyntaxKind.CaseKeyword) Then
                    ' Make sure the comments in the empty case block are indented
                    If caseBlock.Statements.Count = 0 Then
                        Dim caseBlockLastToken = caseBlock.GetLastToken()
                        operations.Add(FormattingOperations.CreateIndentBlockOperation(caseBlockLastToken, nextTokenAfterCase, TextSpan.FromBounds(caseBlockLastToken.Span.End, nextTokenAfterCase.SpanStart), 1, IndentBlockOption.RelativePosition))
                        Return
                    End If

                    AddIndentBlockOperation(operations, pair.Item1, pair.Item2)
                    Return
                End If
            End If

            AddIndentBlockOperation(operations, pair.Item1, pair.Item2)
        End Sub

        ' In the below cases, we want the block for the first token (Return here) inside the block
        ' This handles the cases of the block are 
        ' 1. WithBlock
        ' 2. SyncLockBlock
        ' 3. UsingBlock
        ' 4. ForEachBlock
        ' 1. With <a>
        '         </a>

        '    End With

        ' 2. SyncLock <b>
        '             </b>

        '    	Return
        '    End SyncLock

        ' 3. Using <c>
        '          </c>

        '    	Return

        '    End Using

        ' 4. For Each reallyReallyReallyLongIdentifierNameHere In <d>
        '                                                         </d>

        '    	Return
        '    Next

        Private Shared Function GetOuterBlockWithDifferentStartTokenUsingXmlElement(firstTokenOfInnerBlock As SyntaxToken) As SyntaxNode
            Dim outerBlock = firstTokenOfInnerBlock.Parent
            Dim outerBlockGetFirstToken = outerBlock.GetFirstToken()
            While outerBlock IsNot Nothing AndAlso
                outerBlockGetFirstToken = firstTokenOfInnerBlock AndAlso
                (outerBlock.Kind <> SyntaxKind.UsingBlock OrElse
                outerBlock.Kind <> SyntaxKind.SyncLockBlock OrElse
                outerBlock.Kind <> SyntaxKind.WithBlock OrElse
                outerBlock.Kind <> SyntaxKind.ForEachBlock)
                outerBlock = outerBlock.Parent
                outerBlockGetFirstToken = outerBlock.GetFirstToken()
            End While

            If outerBlock IsNot Nothing AndAlso
                (ReferenceEquals(outerBlock, firstTokenOfInnerBlock.Parent) OrElse
                (outerBlock.Kind <> SyntaxKind.UsingBlock AndAlso
                outerBlock.Kind <> SyntaxKind.SyncLockBlock AndAlso
                outerBlock.Kind <> SyntaxKind.WithBlock AndAlso
                outerBlock.Kind <> SyntaxKind.ForEachBlock)) Then
                Return Nothing
            End If

            Return outerBlock
        End Function

        Private Shared Sub AddXmlEmptyElement(operations As List(Of IndentBlockOperation),
                                       node As XmlNodeSyntax,
                                       baseToken As SyntaxToken,
                                       startToken As SyntaxToken,
                                       endToken As SyntaxToken)
            If Not TypeOf node.Parent Is XmlNodeSyntax Then
                SetAlignmentBlockOperation(operations, baseToken, startToken, endToken)
            End If

            Dim token = endToken.GetPreviousToken(includeZeroWidth:=True)
            AddIndentBlockOperation(operations, startToken, token)
        End Sub

        Private Shared Sub AddXmlElementIndentBlockOperation(operations As List(Of IndentBlockOperation),
                                                      xmlNode As XmlNodeSyntax,
                                                      baseToken As SyntaxToken,
                                                      alignmentStartToken As SyntaxToken,
                                                      alignmentEndToken As SyntaxToken,
                                                      indentationStartToken As SyntaxToken,
                                                      indentationEndToken As SyntaxToken)
            If Not TypeOf xmlNode.Parent Is XmlNodeSyntax Then
                SetAlignmentBlockOperation(operations, baseToken, alignmentStartToken, alignmentEndToken)
            End If

            ' if parent is not xml node
            If Not TypeOf xmlNode.Parent Is XmlNodeSyntax Then
                AddIndentBlockOperation(operations, baseToken, indentationStartToken, indentationEndToken)
                Return
            End If

            ' parent is xml node but embedded expression, then always set the indentation
            If TypeOf xmlNode.Parent Is XmlEmbeddedExpressionSyntax Then
                AddIndentBlockOperation(operations, baseToken, indentationStartToken, indentationEndToken)
                Return
            End If

            ' parent is xml node and the base token is the first token on line
            If IsFirstXmlElementTokenOnLine(baseToken) Then
                AddIndentBlockOperation(operations, baseToken, indentationStartToken, indentationEndToken)
                Return
            End If

            ' if it is not part of another xml element, then do nothing
            Dim element = TryCast(xmlNode.Parent, XmlElementSyntax)
            If element Is Nothing Then
                Return
            End If

            ' if base token is first token of the content of the parent element, don't do anything
            If element.Content.First().GetFirstToken(includeZeroWidth:=True) = baseToken Then
                Return
            End If

            ' now we do expensive stuff to find out whether we are the first xml element on the line
            Dim foundXmlElement = False
            Dim previousToken = baseToken.GetPreviousToken(includeZeroWidth:=True)
            While (Not IsFirstXmlElementTokenOnLine(previousToken))
                If TypeOf previousToken.Parent Is XmlElementSyntax Then
                    foundXmlElement = True
                    Exit While
                End If

                previousToken = previousToken.GetPreviousToken(includeZeroWidth:=True)
            End While

            ' if there is no preceding xml element, then add regular indentation
            If Not foundXmlElement Then
                AddIndentBlockOperation(operations, indentationStartToken, indentationEndToken)
                Return
            End If
        End Sub

        Private Shared Function IsFirstXmlElementTokenOnLine(xmlToken As SyntaxToken) As Boolean
            If xmlToken.LeadingTrivia.Any(Function(t) t.Kind = SyntaxKind.EndOfLineTrivia) Then
                Return True
            End If

            Dim previousToken = xmlToken.GetPreviousToken(includeZeroWidth:=True)
            If previousToken.Kind = SyntaxKind.None OrElse
               previousToken.IsLastTokenOfStatementWithEndOfLine() Then
                Return True
            End If

            Return previousToken.TrailingTrivia.Any(Function(t) t.Kind = SyntaxKind.EndOfLineTrivia)
        End Function

        Private Shared Function GetFirstAndLastMembers(node As SyntaxNode) As ValueTuple(Of SyntaxToken, SyntaxToken)
            Dim [namespace] = TryCast(node, NamespaceBlockSyntax)
            If [namespace] IsNot Nothing Then
                Return ValueTuple.Create(
                    [namespace].NamespaceStatement.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [namespace].EndNamespaceStatement.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True))
            End If

            Dim [module] = TryCast(node, ModuleBlockSyntax)
            If [module] IsNot Nothing Then
                Return ValueTuple.Create(
                    [module].BlockStatement.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [module].EndBlockStatement.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True))
            End If

            Dim [class] = TryCast(node, ClassBlockSyntax)
            If [class] IsNot Nothing Then
                Return ValueTuple.Create(
                    [class].BlockStatement.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [class].EndBlockStatement.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True))
            End If

            Dim [struct] = TryCast(node, StructureBlockSyntax)
            If [struct] IsNot Nothing Then
                Return ValueTuple.Create(
                    [struct].BlockStatement.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [struct].EndBlockStatement.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True))
            End If

            Dim [interface] = TryCast(node, InterfaceBlockSyntax)
            If [interface] IsNot Nothing Then
                Return ValueTuple.Create(
                    [interface].BlockStatement.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [interface].EndBlockStatement.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True))
            End If

            Dim [enum] = TryCast(node, EnumBlockSyntax)
            If [enum] IsNot Nothing Then
                Return ValueTuple.Create(
                    [enum].EnumStatement.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [enum].EndEnumStatement.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True))
            End If

            Dim [method] = TryCast(node, MethodBlockBaseSyntax)
            If [method] IsNot Nothing Then
                Return ValueTuple.Create(
                    [method].BlockStatement.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [method].EndBlockStatement.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True))
            End If

            Dim [property] = TryCast(node, PropertyBlockSyntax)
            If [property] IsNot Nothing Then
                Return ValueTuple.Create(
                    [property].PropertyStatement.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [property].EndPropertyStatement.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True))
            End If

            Dim [event] = TryCast(node, EventBlockSyntax)
            If [event] IsNot Nothing Then
                Return ValueTuple.Create(
                    [event].EventStatement.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [event].EndEventStatement.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True))
            End If

            Dim [while] = TryCast(node, WhileBlockSyntax)
            If [while] IsNot Nothing Then
                Return ValueTuple.Create(
                    [while].WhileStatement.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [while].EndWhileStatement.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True))
            End If

            Dim [using] = TryCast(node, UsingBlockSyntax)
            If [using] IsNot Nothing Then
                Return ValueTuple.Create(
                    [using].UsingStatement.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [using].EndUsingStatement.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True))
            End If

            Dim [sync] = TryCast(node, SyncLockBlockSyntax)
            If [sync] IsNot Nothing Then
                Return ValueTuple.Create(
                    [sync].SyncLockStatement.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [sync].EndSyncLockStatement.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True))
            End If

            Dim [with] = TryCast(node, WithBlockSyntax)
            If [with] IsNot Nothing Then
                Return ValueTuple.Create(
                    [with].WithStatement.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [with].EndWithStatement.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True))
            End If

            Dim [ifBlock] = TryCast(node, MultiLineIfBlockSyntax)
            If [ifBlock] IsNot Nothing Then
                If ifBlock.Statements.Count > 0 Then
                    Return ValueTuple.Create(
                        [ifBlock].IfStatement.GetLastToken().GetNextToken(includeZeroWidth:=True),
                        [ifBlock].Statements.Last().GetLastToken(includeZeroWidth:=True))
                Else
                    Return ValueTuple.Create(
                        [ifBlock].IfStatement.GetLastToken().GetNextToken(includeZeroWidth:=True),
                        [ifBlock].IfStatement.GetLastToken(includeZeroWidth:=True))
                End If
            End If

            Dim [elseif] = TryCast(node, ElseIfBlockSyntax)
            If [elseif] IsNot Nothing Then
                Return ValueTuple.Create(
                    [elseif].ElseIfStatement.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [elseif].GetLastToken(includeZeroWidth:=True))
            End If

            Dim [else] = TryCast(node, ElseBlockSyntax)
            If [else] IsNot Nothing Then
                Return ValueTuple.Create(
                    [else].ElseStatement.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [else].GetLastToken(includeZeroWidth:=True))
            End If

            Dim [try] = TryCast(node, TryBlockSyntax)
            If [try] IsNot Nothing Then
                If [try].Statements.Count > 0 Then
                    Return ValueTuple.Create(
                        [try].TryStatement.GetLastToken().GetNextToken(includeZeroWidth:=True),
                        [try].Statements.Last().GetLastToken(includeZeroWidth:=True))
                Else
                    Return ValueTuple.Create(
                        [try].TryStatement.GetLastToken().GetNextToken(includeZeroWidth:=True),
                        [try].TryStatement.GetLastToken(includeZeroWidth:=True))
                End If
            End If

            Dim [catch] = TryCast(node, CatchBlockSyntax)
            If [catch] IsNot Nothing Then
                Return ValueTuple.Create(
                    [catch].CatchStatement.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [catch].GetLastToken(includeZeroWidth:=True))
            End If

            Dim [finally] = TryCast(node, FinallyBlockSyntax)
            If [finally] IsNot Nothing Then
                Return ValueTuple.Create(
                    [finally].FinallyStatement.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [finally].GetLastToken(includeZeroWidth:=True))
            End If

            Dim [select] = TryCast(node, SelectBlockSyntax)
            If [select] IsNot Nothing Then
                Return ValueTuple.Create(
                    [select].SelectStatement.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [select].EndSelectStatement.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True))
            End If

            Dim [case] = TryCast(node, CaseBlockSyntax)
            If [case] IsNot Nothing Then
                Return ValueTuple.Create(
                    [case].CaseStatement.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [case].GetLastToken(includeZeroWidth:=True))
            End If

            Dim [do] = TryCast(node, DoLoopBlockSyntax)
            If [do] IsNot Nothing Then
                Return ValueTuple.Create(
                    [do].DoStatement.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [do].LoopStatement.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True))
            End If

            Dim [for] = TryCast(node, ForOrForEachBlockSyntax)
            If [for] IsNot Nothing Then
                Return ValueTuple.Create([for].ForOrForEachStatement.GetLastToken().GetNextToken(includeZeroWidth:=True), GetEndTokenForForBlock([for]))
            End If

            Return Nothing
        End Function

        Private Shared Function GetEndTokenForForBlock(node As ForOrForEachBlockSyntax) As SyntaxToken
            If node.NextStatement IsNot Nothing Then
                Return node.NextStatement.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True)
            End If

            ' containing forBlock contains next statement with multiple control variables
            Dim lastToken = node.GetLastToken(includeZeroWidth:=True)

            ' somehow, there is no next statement. probably malformed code
            Dim nextStatement = lastToken.GetAncestor(Of NextStatementSyntax)()
            If nextStatement Is Nothing Then
                Return node.GetLastToken(includeZeroWidth:=True)
            End If

            ' get all enclosing for block statements
            Dim forBlocks = nextStatement.GetAncestors(Of ForOrForEachBlockSyntax)()

            ' get count of the for blocks
            Dim forCount = GetForBlockCount(node, forBlocks)

            If forCount <= nextStatement.ControlVariables.Count Then
                Return nextStatement.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True)
            End If

            ' again, looks like malformed code
            Return node.GetLastToken(includeZeroWidth:=True)
        End Function

        Private Shared Function GetForBlockCount(node As ForOrForEachBlockSyntax, forBlocks As IEnumerable(Of ForOrForEachBlockSyntax)) As Integer
            Dim count As Integer = 0
            For Each forBlock In forBlocks
                If forBlock Is node Then
                    Return count + 1
                End If

                count = count + 1
            Next

            Return count
        End Function
    End Class
End Namespace
