' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.DocumentationComments
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Operations
Imports Microsoft.VisualStudio.Utilities
Imports Microsoft.VisualStudio.Text.Editor
Imports VSCommanding = Microsoft.VisualStudio.Commanding
Imports Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.DocumentationComments
    <Export(GetType(VSCommanding.ICommandHandler))>
    <ContentType(ContentTypeNames.VisualBasicContentType)>
    <Name("XmlTagCompletionCommandHandler")>
    <Order(Before:=PredefinedCompletionNames.CompletionCommandHandler)>
    Friend Class XmlTagCompletionCommandHandler
        Inherits AbstractXmlTagCompletionCommandHandler

        <ImportingConstructor>
        Public Sub New(undoHistory As ITextUndoHistoryRegistry)
            MyBase.New(undoHistory)
        End Sub

        Protected Overrides Sub TryCompleteTag(textView As ITextView, subjectBuffer As ITextBuffer, document As Document, position As SnapshotPoint, cancellationToken As CancellationToken)
            Dim tree = document.GetSyntaxTreeSynchronously(cancellationToken)
            Dim token = tree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDocumentationComments:=True)

            Dim parentTrivia = token.GetAncestor(Of DocumentationCommentTriviaSyntax)()

            If parentTrivia Is Nothing Then
                Return
            End If

            If token.IsKind(SyntaxKind.GreaterThanToken) AndAlso
               token.Parent.IsKind(SyntaxKind.XmlElementStartTag) AndAlso
               token.Parent.Parent.IsKind(SyntaxKind.XmlElement) Then

                Dim element = DirectCast(token.Parent.Parent, XmlElementSyntax)
                Dim elementName = DirectCast(element.StartTag.Name, XmlNameSyntax).LocalName.ValueText

                ' '''<blah><blah$$</blah>
                If HasMatchingEndTag(element) AndAlso HasUnmatchedIdenticalParentStart(element, elementName) Then
                    InsertTextAndMoveCaret(textView, subjectBuffer, position, "</" + elementName + ">", position)
                End If

                Dim endTag = element.EndTag
                Dim endTagText = If(endTag.Name IsNot Nothing, endTag.Name.LocalName.ValueText, String.Empty)

                If elementName.Length > 0 AndAlso elementName <> endTagText Then
                    InsertTextAndMoveCaret(textView, subjectBuffer, position, "</" + elementName + ">", position)
                End If
            End If
        End Sub

        Private Function AlreadyHasEndTag(element As XmlElementSyntax, elementName As String) As Boolean
            If element.IsParentKind(SyntaxKind.DocumentationCommentTrivia) Then
                Dim trivia = DirectCast(element.Parent, DocumentationCommentTriviaSyntax)
                Dim index = trivia.Content.IndexOf(element)

                While index < trivia.Content.Count
                    Dim nextItem = trivia.Content(index)
                    If nextItem.IsKind(SyntaxKind.XmlElementEndTag) Then
                        Dim name = DirectCast(nextItem, XmlElementEndTagSyntax).Name.LocalName.ValueText
                        If name = elementName Then
                            Return True
                        End If
                    End If

                    index += 1
                End While
            End If

            Return False
        End Function

        Private Function HasUnmatchedIdenticalParentStart(element As XmlElementSyntax, expectedName As String) As Boolean
            If element Is Nothing Then
                Return False
            End If

            Dim startTag = element.StartTag
            Dim elementName = DirectCast(startTag.Name, XmlNameSyntax).LocalName.ValueText

            If elementName = expectedName Then
                If HasMatchingEndTag(element) Then
                    Return HasUnmatchedIdenticalParentStart(TryCast(element.Parent, XmlElementSyntax), expectedName)
                End If

                Return True
            End If

            Return False
        End Function

        Private Function HasMatchingEndTag(element As XmlElementSyntax) As Boolean
            Dim startTag = element.StartTag
            Dim endTag = element.EndTag

            If startTag IsNot Nothing AndAlso endTag IsNot Nothing Then
                Dim name = DirectCast(startTag.Name, XmlNameSyntax).LocalName.ValueText
                Return Not endTag.IsMissing AndAlso endTag.Name.LocalName.ValueText = name
            End If

            Return False
        End Function
    End Class
End Namespace
