' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Formatting
#If MEF Then
    <ExportFormattingRule(NodeBasedFormattingRule.Name, LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=AlignTokensFormattingRule.Name)>
    Friend Class NodeBasedFormattingRule
#Else
    Friend Class NodeBasedFormattingRule
#End If
        Inherits BaseFormattingRule
        Friend Const Name As String = "VisualBasic Node Based Formatting Rule"

        Public Overrides Sub AddAnchorIndentationOperations(operations As List(Of AnchorIndentationOperation),
                                                            node As SyntaxNode,
                                                            optionSet As OptionSet,
                                                            nextOperation As NextAction(Of AnchorIndentationOperation))
            nextOperation.Invoke(operations)

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

        Private Function IsBlockSyntax(node As SyntaxNode) As Boolean
            Dim pair = GetFirstAndLastMembers(node)
            If pair.Equals(Nothing) Then
                Return False
            End If

            Return True
        End Function

        Public Overrides Sub AddIndentBlockOperations(operations As List(Of IndentBlockOperation),
                                                      node As SyntaxNode,
                                                      optionSet As OptionSet,
                                                      nextOperation As NextAction(Of IndentBlockOperation))
            nextOperation.Invoke(operations)

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
                Dim baseToken = multiLineLambda.Begin.GetFirstToken(includeZeroWidth:=True)
                Dim lastBeginningToken = If(multiLineLambda.Begin.GetLastToken().VisualBasicKind = SyntaxKind.None, multiLineLambda.Begin.GetLastToken(includeZeroWidth:=True), multiLineLambda.Begin.GetLastToken())

                AddIndentBlockOperation(operations, baseToken,
                                        lastBeginningToken.GetNextToken(includeZeroWidth:=True),
                                        multiLineLambda.End.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True))
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
            If (Item1PreviousToken.VisualBasicKind = SyntaxKind.GreaterThanToken AndAlso
                Item1PreviousToken.IsParentKind(SyntaxKind.XmlElementEndTag)) Then

                Dim outerBlockWithBaseToken = GetOuterBlockWithDifferentStartTokenUsingXmlElement(pair.Item1)
                If outerBlockWithBaseToken IsNot Nothing Then
                    AddIndentBlockOperation(operations, outerBlockWithBaseToken.GetFirstToken(), pair.Item1, pair.Item2)
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

        Private Function GetOuterBlockWithDifferentStartTokenUsingXmlElement(firstTokenOfInnerBlock As SyntaxToken) As SyntaxNode
            Dim outerBlock = firstTokenOfInnerBlock.Parent
            Dim outerBlockGetFirstToken = outerBlock.GetFirstToken()
            While outerBlock IsNot Nothing AndAlso
                outerBlockGetFirstToken = firstTokenOfInnerBlock AndAlso
                (outerBlock.VisualBasicKind <> SyntaxKind.UsingBlock OrElse
                outerBlock.VisualBasicKind <> SyntaxKind.SyncLockBlock OrElse
                outerBlock.VisualBasicKind <> SyntaxKind.WithBlock OrElse
                outerBlock.VisualBasicKind <> SyntaxKind.ForEachBlock)
                outerBlock = outerBlock.Parent
                outerBlockGetFirstToken = outerBlock.GetFirstToken()
            End While

            If outerBlock IsNot Nothing AndAlso
                (ReferenceEquals(outerBlock, firstTokenOfInnerBlock.Parent) OrElse
                (outerBlock.VisualBasicKind <> SyntaxKind.UsingBlock AndAlso
                outerBlock.VisualBasicKind <> SyntaxKind.SyncLockBlock AndAlso
                outerBlock.VisualBasicKind <> SyntaxKind.WithBlock AndAlso
                outerBlock.VisualBasicKind <> SyntaxKind.ForEachBlock)) Then
                Return Nothing
            End If

            Return outerBlock
        End Function

        Private Sub AddXmlEmptyElement(operations As List(Of IndentBlockOperation),
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

        Private Sub AddXmlElementIndentBlockOperation(operations As List(Of IndentBlockOperation),
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

            ' if base token is first token of the conent of the parent element, don't do anything
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

        Private Function IsFirstXmlElementTokenOnLine(xmlToken As SyntaxToken) As Boolean
            If xmlToken.LeadingTrivia.Any(Function(t) t.VisualBasicKind = SyntaxKind.EndOfLineTrivia) Then
                Return True
            End If

            Dim previousToken = xmlToken.GetPreviousToken(includeZeroWidth:=True)
            If previousToken.VisualBasicKind = SyntaxKind.None OrElse
               previousToken.IsLastTokenOfStatementWithEndOfLine() Then
                Return True
            End If

            Return previousToken.TrailingTrivia.Any(Function(t) t.VisualBasicKind = SyntaxKind.EndOfLineTrivia)
        End Function

        Private Function GetFirstAndLastMembers(node As SyntaxNode) As ValueTuple(Of SyntaxToken, SyntaxToken)
            Dim [namespace] = TryCast(node, NamespaceBlockSyntax)
            If [namespace] IsNot Nothing Then
                Return ValueTuple.Create(
                    [namespace].NamespaceStatement.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [namespace].EndNamespaceStatement.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True))
            End If

            Dim [module] = TryCast(node, ModuleBlockSyntax)
            If [module] IsNot Nothing Then
                Return ValueTuple.Create(
                    [module].Begin.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [module].End.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True))
            End If

            Dim [class] = TryCast(node, ClassBlockSyntax)
            If [class] IsNot Nothing Then
                Return ValueTuple.Create(
                    [class].Begin.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [class].End.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True))
            End If

            Dim [struct] = TryCast(node, StructureBlockSyntax)
            If [struct] IsNot Nothing Then
                Return ValueTuple.Create(
                    [struct].Begin.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [struct].End.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True))
            End If

            Dim [interface] = TryCast(node, InterfaceBlockSyntax)
            If [interface] IsNot Nothing Then
                Return ValueTuple.Create(
                    [interface].Begin.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [interface].End.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True))
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
                    [method].Begin.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [method].End.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True))
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

            Dim [ifpart] = TryCast(node, IfPartSyntax)
            If [ifpart] IsNot Nothing Then
                Return ValueTuple.Create(
                    [ifpart].Begin.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [ifpart].GetLastToken(includeZeroWidth:=True))
            End If

            Dim [elsepart] = TryCast(node, ElsePartSyntax)
            If [elsepart] IsNot Nothing Then
                Return ValueTuple.Create(
                    [elsepart].Begin.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [elsepart].GetLastToken(includeZeroWidth:=True))
            End If

            Dim [trypart] = TryCast(node, TryPartSyntax)
            If [trypart] IsNot Nothing Then
                Return ValueTuple.Create(
                    [trypart].Begin.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [trypart].GetLastToken(includeZeroWidth:=True))
            End If

            Dim [catch] = TryCast(node, CatchPartSyntax)
            If [catch] IsNot Nothing Then
                Return ValueTuple.Create(
                    [catch].Begin.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [catch].GetLastToken(includeZeroWidth:=True))
            End If

            Dim [finally] = TryCast(node, FinallyPartSyntax)
            If [finally] IsNot Nothing Then
                Return ValueTuple.Create(
                    [finally].Begin.GetLastToken().GetNextToken(includeZeroWidth:=True),
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
                    [case].Begin.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [case].GetLastToken(includeZeroWidth:=True))
            End If

            Dim [do] = TryCast(node, DoLoopBlockSyntax)
            If [do] IsNot Nothing Then
                Return ValueTuple.Create(
                    [do].DoStatement.GetLastToken().GetNextToken(includeZeroWidth:=True),
                    [do].LoopStatement.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True))
            End If

            Dim [for] = TryCast(node, ForBlockSyntax)
            If [for] IsNot Nothing Then
                Return ValueTuple.Create([for].Begin.GetLastToken().GetNextToken(includeZeroWidth:=True), GetEndTokenForForBlock([for]))
            End If

            Return Nothing
        End Function

        Private Function GetEndTokenForForBlock(node As ForBlockSyntax) As SyntaxToken
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

            ' get all enclosing forblock statements
            Dim forBlocks = nextStatement.GetAncestors(Of ForBlockSyntax)()

            ' get count of the for blocks
            Dim forCount = GetForBlockCount(node, forBlocks)

            If forCount <= nextStatement.ControlVariables.Count Then
                Return nextStatement.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True)
            End If

            ' again, looks like malformed code
            Return node.GetLastToken(includeZeroWidth:=True)
        End Function

        Private Function GetForBlockCount(node As ForBlockSyntax, forBlocks As IEnumerable(Of ForBlockSyntax)) As Integer
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