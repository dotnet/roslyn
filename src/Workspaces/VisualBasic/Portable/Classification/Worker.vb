' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Classification
    Partial Friend Class Worker
        Private ReadOnly _list As List(Of ClassifiedSpan)
        Private ReadOnly _textSpan As TextSpan
        Private ReadOnly _docCommentClassifier As DocumentationCommentClassifier
        Private ReadOnly _xmlClassifier As XmlClassifier
        Private ReadOnly _cancellationToken As CancellationToken

        Private Sub New(textSpan As TextSpan, list As List(Of ClassifiedSpan), cancellationToken As CancellationToken)
            _textSpan = textSpan
            _list = list
            _docCommentClassifier = New DocumentationCommentClassifier(Me)
            _xmlClassifier = New XmlClassifier(Me)
            _cancellationToken = cancellationToken
        End Sub

        Friend Shared Sub CollectClassifiedSpans(
            tokens As IEnumerable(Of SyntaxToken), textSpan As TextSpan, list As List(Of ClassifiedSpan), cancellationToken As CancellationToken)
            Dim worker = New Worker(textSpan, list, cancellationToken)

            For Each token In tokens
                worker.ClassifyToken(token)
            Next
        End Sub

        Friend Shared Sub CollectClassifiedSpans(
            node As SyntaxNode, textSpan As TextSpan, list As List(Of ClassifiedSpan), cancellationToken As CancellationToken)
            Dim worker = New Worker(textSpan, list, cancellationToken)
            worker.ClassifyNode(node)
        End Sub

        Private Sub AddClassification(textSpan As TextSpan, classificationType As String)
            _list.Add(New ClassifiedSpan(classificationType, textSpan))
        End Sub

        Private Sub AddClassification(token As SyntaxToken, classificationType As String)
            If token.Width() > 0 AndAlso _textSpan.OverlapsWith(token.Span) Then
                AddClassification(token.Span, classificationType)
            End If
        End Sub

        Private Sub AddClassification(trivia As SyntaxTrivia, classificationType As String)
            If trivia.Width() > 0 AndAlso _textSpan.OverlapsWith(trivia.Span) Then
                AddClassification(trivia.Span, classificationType)
            End If
        End Sub

        Friend Sub ClassifyNode(node As SyntaxNode)
            For Each nodeOrToken In node.DescendantNodesAndTokensAndSelf(span:=_textSpan, descendIntoChildren:=Function(t) Not IsXmlNode(t), descendIntoTrivia:=False)
                _cancellationToken.ThrowIfCancellationRequested()

                If nodeOrToken.IsNode Then
                    ClassifyXmlNode(nodeOrToken.AsNode())
                Else
                    ClassifyToken(nodeOrToken.AsToken())
                End If
            Next
        End Sub

        Private Function IsXmlNode(node As SyntaxNode) As Boolean
            Return TypeOf node Is XmlNodeSyntax OrElse
                   TypeOf node Is XmlNamespaceImportsClauseSyntax OrElse
                   TypeOf node Is XmlMemberAccessExpressionSyntax OrElse
                   TypeOf node Is GetXmlNamespaceExpressionSyntax
        End Function

        Private Sub ClassifyXmlNode(node As SyntaxNode)
            If IsXmlNode(node) Then
                _xmlClassifier.ClassifyNode(node)
            End If
        End Sub

        Friend Sub ClassifyToken(token As SyntaxToken, Optional type As String = Nothing)
            Dim span = token.Span
            If span.Length <> 0 AndAlso _textSpan.OverlapsWith(span) Then
                type = If(type, ClassificationHelpers.GetClassification(token))

                If type IsNot Nothing Then
                    AddClassification(token.Span, type)
                End If
            End If

            ClassifyTrivia(token)
        End Sub

        Private Sub ClassifyTrivia(token As SyntaxToken)
            For Each trivia In token.LeadingTrivia
                _cancellationToken.ThrowIfCancellationRequested()
                ClassifyTrivia(trivia)
            Next

            For Each trivia In token.TrailingTrivia
                _cancellationToken.ThrowIfCancellationRequested()
                ClassifyTrivia(trivia)
            Next
        End Sub

        Private Sub ClassifyTrivia(trivia As SyntaxTrivia)
            If trivia.HasStructure Then
                Select Case trivia.GetStructure().Kind
                    Case SyntaxKind.DocumentationCommentTrivia
                        _docCommentClassifier.Classify(DirectCast(trivia.GetStructure(), DocumentationCommentTriviaSyntax))
                    Case SyntaxKind.IfDirectiveTrivia,
                        SyntaxKind.ElseIfDirectiveTrivia,
                        SyntaxKind.ElseDirectiveTrivia,
                        SyntaxKind.EndIfDirectiveTrivia,
                        SyntaxKind.RegionDirectiveTrivia,
                        SyntaxKind.EndRegionDirectiveTrivia,
                        SyntaxKind.ConstDirectiveTrivia,
                        SyntaxKind.ExternalSourceDirectiveTrivia,
                        SyntaxKind.EndExternalSourceDirectiveTrivia,
                        SyntaxKind.ExternalChecksumDirectiveTrivia,
                        SyntaxKind.ReferenceDirectiveTrivia,
                        SyntaxKind.EnableWarningDirectiveTrivia,
                        SyntaxKind.DisableWarningDirectiveTrivia,
                        SyntaxKind.BadDirectiveTrivia

                        ClassifyDirectiveSyntax(DirectCast(trivia.GetStructure(), DirectiveTriviaSyntax))
                    Case SyntaxKind.SkippedTokensTrivia
                        ClassifySkippedTokens(DirectCast(trivia.GetStructure(), SkippedTokensTriviaSyntax))
                End Select
            ElseIf trivia.Kind = SyntaxKind.CommentTrivia Then
                AddClassification(trivia, ClassificationTypeNames.Comment)
            ElseIf trivia.Kind = SyntaxKind.DisabledTextTrivia Then
                AddClassification(trivia, ClassificationTypeNames.ExcludedCode)
            ElseIf trivia.Kind = SyntaxKind.ColonTrivia Then
                AddClassification(trivia, ClassificationTypeNames.Punctuation)
            ElseIf trivia.Kind = SyntaxKind.LineContinuationTrivia Then
                AddClassification(New TextSpan(trivia.SpanStart, 1), ClassificationTypeNames.Punctuation)
            End If
        End Sub

        Private Sub ClassifySkippedTokens(skippedTokens As SkippedTokensTriviaSyntax)
            If Not _textSpan.OverlapsWith(skippedTokens.Span) Then
                Return
            End If

            Dim tokens = skippedTokens.Tokens
            For Each tk In tokens
                ClassifyToken(tk)
            Next
        End Sub

        Private Sub ClassifyDirectiveSyntax(directiveSyntax As SyntaxNode)
            If Not _textSpan.OverlapsWith(directiveSyntax.FullSpan) Then
                Return
            End If

            For Each child As SyntaxNodeOrToken In directiveSyntax.ChildNodesAndTokens()
                If child.IsToken Then
                    Select Case child.Kind()
                        Case SyntaxKind.HashToken,
                             SyntaxKind.IfKeyword,
                             SyntaxKind.EndKeyword,
                             SyntaxKind.ElseKeyword,
                             SyntaxKind.ElseIfKeyword,
                             SyntaxKind.RegionKeyword,
                             SyntaxKind.ThenKeyword,
                             SyntaxKind.ConstKeyword,
                             SyntaxKind.ExternalSourceKeyword,
                             SyntaxKind.ExternalChecksumKeyword,
                             SyntaxKind.EnableKeyword,
                             SyntaxKind.WarningKeyword,
                             SyntaxKind.DisableKeyword

                            ClassifyToken(child.AsToken(), ClassificationTypeNames.PreprocessorKeyword)
                        Case Else
                            ClassifyToken(child.AsToken())
                    End Select
                Else
                    ClassifyNode(child.AsNode())
                End If
            Next
        End Sub
    End Class
End Namespace