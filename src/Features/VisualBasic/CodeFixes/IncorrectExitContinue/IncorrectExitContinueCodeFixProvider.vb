' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.CodeActions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.IncorrectExitContinue

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.FixIncorrectExitContinue), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.ImplementInterface)>
    Partial Friend Class IncorrectExitContinueCodeFixProvider
        Inherits CodeFixProvider

        Friend Const BC30781 As String = "BC30781" ' 'Continue' must be followed by 'Do', 'For' or 'While'.
        Friend Const BC30782 As String = "BC30782" ' 'Continue Do' can only appear inside a 'Do' statement.
        Friend Const BC30783 As String = "BC30783" ' 'Continue For' can only appear inside a 'For' statement.
        Friend Const BC30784 As String = "BC30784" ' 'Continue While' can only appear inside a 'While' statement.

        Friend Const BC30240 As String = "BC30240" ' 'Exit' must be followed by 'Sub', 'Function', 'Property', 'Do', 'For', 'While', 'Select', or 'Try'.
        Friend Const BC30065 As String = "BC30065" ' 'Exit Sub' is not valid in a Function or Property.
        Friend Const BC30066 As String = "BC30066" ' 'Exit Property' is not valid in a Function or Sub.
        Friend Const BC30067 As String = "BC30067" ' 'Exit Function' is not valid in a Sub or Property.
        Friend Const BC30089 As String = "BC30089" ' 'Exit Do' can only appear inside a 'Do' statement.
        Friend Const BC30096 As String = "BC30096" ' 'Exit For' can only appear inside a 'For' statement.
        Friend Const BC30097 As String = "BC30097" ' 'Exit While' can only appear inside a 'While' statement.
        Friend Const BC30099 As String = "BC30099" ' 'Exit Select' can only appear inside a 'Select' statement.
        Friend Const BC30393 As String = "BC30393" ' 'Exit Try' can only appear inside a 'Try' statement.

        Friend Const BC30689 As String = "BC30689" ' Statement cannot appear outside of a method body.

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC30781, BC30782, BC30783, BC30784, BC30240, BC30065, BC30066, BC30067, BC30089, BC30096, BC30097, BC30099, BC30393, BC30689)
            End Get
        End Property

        Public NotOverridable Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim document = context.Document
            Dim span = context.Span
            Dim cancellationToken = context.CancellationToken
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            Dim token = root.FindToken(span.Start)
            If Not token.Span.IntersectsWith(span) Then
                Return
            End If

            Dim node = token.GetAncestors(Of SyntaxNode) _
                       .FirstOrDefault(Function(c)
                                           Return c.Span.IntersectsWith(span) AndAlso (
                                               TypeOf (c) Is ContinueStatementSyntax OrElse
                                               TypeOf (c) Is ExitStatementSyntax)
                                       End Function)
            If node Is Nothing Then
                Return
            End If

            Dim enclosingblocks = node.GetContainingExecutableBlocks()
            If Not enclosingblocks.Any() Then
                context.RegisterCodeFix(New RemoveStatementCodeAction(document, node, CreateDeleteString(node)), context.Diagnostics)
                Return
            End If

            Dim codeActions As List(Of CodeAction) = Nothing

            Dim semanticDoc = Await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(False)
            CreateExitCodeActions(semanticDoc, node, enclosingblocks, codeActions, cancellationToken)
            CreateContinueCodeActions(semanticDoc, node, enclosingblocks, codeActions, cancellationToken)

            context.RegisterFixes(codeActions, context.Diagnostics)
        End Function

        Private Sub CreateContinueCodeActions(document As SemanticDocument, node As SyntaxNode, enclosingblocks As IEnumerable(Of SyntaxNode), ByRef codeActions As List(Of CodeAction), cancellationToken As CancellationToken)
            Dim continueStatement = TryCast(node, ContinueStatementSyntax)
            If continueStatement IsNot Nothing Then
                Dim enclosingDeclaration = document.SemanticModel.GetEnclosingSymbol(node.SpanStart, cancellationToken)

                If enclosingDeclaration Is Nothing Then
                    Return
                End If

                If codeActions Is Nothing Then
                    codeActions = New List(Of CodeAction)
                End If

                Dim blockKinds = GetEnclosingContinuableBlockKinds(enclosingblocks, enclosingDeclaration)

                Dim tokenAfterContinueToken = continueStatement.ContinueKeyword.GetNextToken(includeSkipped:=True)
                Dim text = document.Text

                If continueStatement.BlockKeyword.IsMissing Then
                    If tokenAfterContinueToken.IsSkipped() AndAlso text.Lines.IndexOf(tokenAfterContinueToken.SpanStart) = text.Lines.IndexOf(continueStatement.SpanStart) Then
                        CreateReplaceTokenKeywordActions(blockKinds, tokenAfterContinueToken, document.Document, codeActions)
                    Else
                        CreateAddKeywordActions(continueStatement, document.Document, enclosingblocks.First(), blockKinds, AddressOf CreateContinueStatement, codeActions)
                        codeActions.Add(New RemoveStatementCodeAction(document.Document, continueStatement, CreateDeleteString(continueStatement)))
                    End If
                ElseIf Not blockKinds.Any(Function(bk) KeywordAndBlockKindMatch(bk, continueStatement.BlockKeyword.Kind)) Then
                    CreateReplaceKeywordActions(blockKinds, tokenAfterContinueToken, continueStatement, enclosingblocks.First(), document.Document, AddressOf CreateContinueStatement, codeActions)
                    codeActions.Add(New RemoveStatementCodeAction(document.Document, continueStatement, CreateDeleteString(continueStatement)))
                End If
            End If
        End Sub

        Private Sub CreateExitCodeActions(document As SemanticDocument, node As SyntaxNode, enclosingblocks As IEnumerable(Of SyntaxNode), ByRef codeActions As List(Of CodeAction), cancellationToken As CancellationToken)
            Dim exitStatement = TryCast(node, ExitStatementSyntax)
            If exitStatement IsNot Nothing Then
                Dim enclosingDeclaration = document.SemanticModel.GetEnclosingSymbol(node.SpanStart, cancellationToken)

                If enclosingDeclaration Is Nothing Then
                    Return
                End If

                If codeActions Is Nothing Then
                    codeActions = New List(Of CodeAction)
                End If

                Dim blockKinds = GetEnclosingBlockKinds(enclosingblocks, enclosingDeclaration)

                Dim tokenAfterExitToken = exitStatement.ExitKeyword.GetNextToken(includeSkipped:=True)
                Dim text = document.Text

                If exitStatement.BlockKeyword.IsMissing Then
                    If tokenAfterExitToken.IsSkipped() AndAlso text.Lines.IndexOf(tokenAfterExitToken.SpanStart) = text.Lines.IndexOf(exitStatement.SpanStart) Then
                        CreateReplaceTokenKeywordActions(blockKinds, tokenAfterExitToken, document.Document, codeActions)
                    Else
                        CreateAddKeywordActions(exitStatement, document.Document, enclosingblocks.First(), blockKinds, AddressOf CreateExitStatement, codeActions)
                        codeActions.Add(New RemoveStatementCodeAction(document.Document, exitStatement, CreateDeleteString(exitStatement)))
                    End If
                ElseIf Not blockKinds.Any(Function(bk) KeywordAndBlockKindMatch(bk, exitStatement.BlockKeyword.Kind)) Then
                    CreateReplaceKeywordActions(blockKinds, tokenAfterExitToken, exitStatement, enclosingblocks.First(), document.Document, AddressOf CreateExitStatement, codeActions)
                    codeActions.Add(New RemoveStatementCodeAction(document.Document, exitStatement, CreateDeleteString(exitStatement)))
                End If
            End If
        End Sub

        Private Function GetEnclosingBlockKinds(enclosingblocks As IEnumerable(Of SyntaxNode), enclosingDeclaration As ISymbol) As IEnumerable(Of SyntaxKind)
            Dim kinds = New List(Of SyntaxKind)(enclosingblocks.Select(Function(b) b.Kind()).Where(Function(kind) BlockKindToKeywordKind(kind) <> Nothing OrElse kind = SyntaxKind.FinallyBlock))

            ' If we're inside a method declaration, we can only exit if it's a Function/Sub (lambda) or a property set or get.
            Dim methodSymbol = TryCast(enclosingDeclaration, IMethodSymbol)
            If methodSymbol IsNot Nothing Then
                If methodSymbol.MethodKind = MethodKind.PropertyGet Then
                    kinds.Add(SyntaxKind.GetAccessorBlock)
                ElseIf methodSymbol.MethodKind = MethodKind.PropertySet Then
                    kinds.Add(SyntaxKind.SetAccessorBlock)
                ElseIf methodSymbol.ReturnsVoid() Then
                    kinds.Add(SyntaxKind.SubBlock)
                Else
                    kinds.Add(SyntaxKind.FunctionBlock)
                End If
            End If

            ' For each enclosing-before-finally block, select block kinds that won't generate duplicate keyword kinds.
            Return kinds.TakeWhile(Function(k) k <> SyntaxKind.FinallyBlock).GroupBy(Function(k) BlockKindToKeywordKind(k)).Select(Function(g) g.First())
        End Function

        Private Function GetEnclosingContinuableBlockKinds(enclosingblocks As IEnumerable(Of SyntaxNode), enclosingDeclaration As ISymbol) As IEnumerable(Of SyntaxKind)
            Return enclosingblocks.TakeWhile(Function(eb) eb.Kind() <> SyntaxKind.FinallyBlock) _
                                  .Where(Function(eb) eb.IsKind(SyntaxKind.WhileBlock,
                                                                SyntaxKind.SimpleDoLoopBlock,
                                                                SyntaxKind.DoWhileLoopBlock, SyntaxKind.DoUntilLoopBlock,
                                                                SyntaxKind.DoLoopWhileBlock, SyntaxKind.DoLoopUntilBlock,
                                                                SyntaxKind.ForBlock,
                                                                SyntaxKind.ForEachBlock)) _
                                  .Select(Function(eb) eb.Kind()) _
                                  .Distinct()
        End Function

        Private Function CreateExitStatement(exitSyntax As SyntaxNode, containingBlock As SyntaxNode,
                                             createBlockKind As SyntaxKind, document As Document,
                                             cancellationToken As CancellationToken) As StatementSyntax
            Dim exitStatement = DirectCast(exitSyntax, ExitStatementSyntax)

            Dim keywordKind = BlockKindToKeywordKind(createBlockKind)
            Dim statementKind = BlockKindToStatementKind(createBlockKind)

            Dim newToken = SyntaxFactory.Token(keywordKind) _
                    .WithLeadingTrivia(exitStatement.BlockKeyword.LeadingTrivia) _
                    .WithTrailingTrivia(exitStatement.BlockKeyword.TrailingTrivia)

            Dim updatedSyntax = SyntaxFactory.ExitStatement(statementKind, newToken) _
                    .WithLeadingTrivia(exitStatement.GetLeadingTrivia()) _
                    .WithTrailingTrivia(exitStatement.GetTrailingTrivia()) _
                    .WithAdditionalAnnotations(Formatter.Annotation)
            Return updatedSyntax
        End Function

        Private Function CreateContinueStatement(continueSyntax As SyntaxNode, containingBlock As SyntaxNode,
                                                 createBlockKind As SyntaxKind, document As Document,
                                                 cancellationToken As CancellationToken) As StatementSyntax
            Dim keywordKind = BlockKindToKeywordKind(createBlockKind)
            Dim statementKind = BlockKindToContinuableStatementKind(createBlockKind)

            Dim continueStatement = DirectCast(continueSyntax, ContinueStatementSyntax)

            Dim newToken = SyntaxFactory.Token(keywordKind) _
                            .WithLeadingTrivia(continueStatement.BlockKeyword.LeadingTrivia) _
                            .WithTrailingTrivia(continueStatement.BlockKeyword.TrailingTrivia)

            Dim updatedSyntax = SyntaxFactory.ContinueStatement(statementKind, newToken) _
                    .WithLeadingTrivia(continueStatement.GetLeadingTrivia()) _
                    .WithTrailingTrivia(continueStatement.GetTrailingTrivia()) _
                    .WithAdditionalAnnotations(Formatter.Annotation)

            Return updatedSyntax
        End Function

        Private Shared Function KeywordAndBlockKindMatch(blockKind As SyntaxKind, keywordKind As SyntaxKind) As Boolean
            Return keywordKind = BlockKindToKeywordKind(blockKind)
        End Function

        Private Shared Function BlockKindToKeywordKind(blockKind As SyntaxKind) As SyntaxKind
            Select Case blockKind
                Case SyntaxKind.WhileBlock
                    Return SyntaxKind.WhileKeyword
                Case SyntaxKind.TryBlock, SyntaxKind.CatchBlock
                    Return SyntaxKind.TryKeyword
                Case SyntaxKind.SimpleDoLoopBlock,
                     SyntaxKind.DoWhileLoopBlock,
                     SyntaxKind.DoUntilLoopBlock,
                     SyntaxKind.DoLoopWhileBlock,
                     SyntaxKind.DoLoopUntilBlock
                    Return SyntaxKind.DoKeyword
                Case SyntaxKind.ForBlock, SyntaxKind.ForEachBlock
                    Return SyntaxKind.ForKeyword
                Case SyntaxKind.CaseBlock, SyntaxKind.CaseElseBlock
                    Return SyntaxKind.SelectKeyword
                Case SyntaxKind.SubBlock
                    Return SyntaxKind.SubKeyword
                Case SyntaxKind.FunctionBlock
                    Return SyntaxKind.FunctionKeyword
                Case SyntaxKind.GetAccessorBlock, SyntaxKind.SetAccessorBlock
                    Return SyntaxKind.PropertyKeyword
                Case Else
                    Return Nothing
            End Select
        End Function

        Private Shared Function BlockKindToStatementKind(blockKind As SyntaxKind) As SyntaxKind
            Select Case blockKind
                Case SyntaxKind.WhileBlock
                    Return SyntaxKind.ExitWhileStatement
                Case SyntaxKind.TryBlock, SyntaxKind.CatchBlock
                    Return SyntaxKind.ExitTryStatement
                Case SyntaxKind.SimpleDoLoopBlock,
                     SyntaxKind.DoWhileLoopBlock,
                     SyntaxKind.DoUntilLoopBlock,
                     SyntaxKind.DoLoopWhileBlock,
                     SyntaxKind.DoLoopUntilBlock
                    Return SyntaxKind.ExitDoStatement
                Case SyntaxKind.ForBlock, SyntaxKind.ForEachBlock
                    Return SyntaxKind.ExitForStatement
                Case SyntaxKind.CaseBlock, SyntaxKind.CaseElseBlock
                    Return SyntaxKind.ExitSelectStatement
                Case SyntaxKind.SubBlock
                    Return SyntaxKind.ExitSubStatement
                Case SyntaxKind.FunctionBlock
                    Return SyntaxKind.ExitFunctionStatement
                Case SyntaxKind.GetAccessorBlock, SyntaxKind.SetAccessorBlock
                    Return SyntaxKind.ExitPropertyStatement
                Case Else
                    Throw ExceptionUtilities.Unreachable
            End Select
        End Function

        Private Shared Function BlockKindToContinuableStatementKind(blockKind As SyntaxKind) As SyntaxKind
            Select Case blockKind
                Case SyntaxKind.SimpleDoLoopBlock,
                     SyntaxKind.DoWhileLoopBlock,
                     SyntaxKind.DoUntilLoopBlock,
                     SyntaxKind.DoLoopWhileBlock,
                     SyntaxKind.DoLoopUntilBlock
                    Return SyntaxKind.ContinueDoStatement
                Case SyntaxKind.ForBlock, SyntaxKind.ForEachBlock
                    Return SyntaxKind.ContinueForStatement
                Case SyntaxKind.WhileBlock
                    Return SyntaxKind.ContinueWhileStatement
                Case Else
                    Throw ExceptionUtilities.Unreachable
            End Select
        End Function

        Private Sub CreateAddKeywordActions(node As SyntaxNode,
                                            document As Document,
                                            enclosingBlock As SyntaxNode,
                                            blockKinds As IEnumerable(Of SyntaxKind),
                                            updateNode As Func(Of SyntaxNode, SyntaxNode, SyntaxKind, Document, CancellationToken, StatementSyntax),
                                            codeActions As IList(Of CodeAction))
            codeActions.AddRange(blockKinds.Select(Function(bk) New AddKeywordCodeAction(node, bk, enclosingBlock, document, updateNode)))
        End Sub

        Private Sub CreateReplaceKeywordActions(blockKinds As IEnumerable(Of SyntaxKind),
                                                invalidToken As SyntaxToken,
                                                node As SyntaxNode,
                                                enclosingBlock As SyntaxNode,
                                                document As Document,
                                                updateNode As Func(Of SyntaxNode, SyntaxNode, SyntaxKind, Document, CancellationToken, StatementSyntax),
                                                codeActions As IList(Of CodeAction))
            codeActions.AddRange(blockKinds.Select(Function(bk) New ReplaceKeywordCodeAction(bk, invalidToken,
                                                                                    node, enclosingBlock, document, updateNode)))
        End Sub

        Private Sub CreateReplaceTokenKeywordActions(blockKinds As IEnumerable(Of SyntaxKind), invalidToken As SyntaxToken, document As Document, codeActions As List(Of CodeAction))
            codeActions.AddRange(blockKinds.Select(Function(bk) New ReplaceTokenKeywordCodeAction(bk, invalidToken, document)))
        End Sub

        Private Function CreateDeleteString(node As SyntaxNode) As String
            Return String.Format(VBFeaturesResources.DeleteTheStatement, node.ToString().Trim())
        End Function
    End Class
End Namespace
