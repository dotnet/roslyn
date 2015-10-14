' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticEndConstructCorrection
    Friend Class XmlElementTagCorrector
        Inherits AbstractCorrector

        Public Sub New(subjectBuffer As ITextBuffer, _waitIndicator As IWaitIndicator)
            MyBase.New(subjectBuffer, _waitIndicator)
        End Sub

        Protected Overrides Function ShouldReplaceText(text As String) As Boolean
            Return True
        End Function

        Protected Overrides Function IsAllowableWordAtIndex(lineText As String, wordStartIndex As Integer, wordLength As Integer) As Boolean
            ' needs to look like   "<foo>" or "<foo "

            If wordStartIndex <= 0 Then
                Return False
            End If

            Dim ch = lineText(wordStartIndex - 1)
            If ch <> "<"c Then
                Return False
            End If

            Dim wordEnd = wordStartIndex + wordLength
            If wordEnd >= lineText.Length Then
                Return False
            End If

            ch = lineText(wordEnd)
            If ch <> " "c AndAlso ch <> ">"c Then
                Return False
            End If

            Return True
        End Function

        Protected Overrides Function TryGetValidTokens(wordStartIndex As Integer,
                                                       ByRef startToken As SyntaxToken,
                                                       ByRef endToken As SyntaxToken,
                                                       cancellationToken As CancellationToken) As Boolean
            Dim root = Me.PreviousDocument.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken)
            startToken = root.FindToken(wordStartIndex, findInsideTrivia:=True)

            If Not startToken.IsKind(SyntaxKind.XmlNameToken) OrElse
               Not startToken.Parent.IsKind(SyntaxKind.XmlName) OrElse
               Not startToken.Parent.Parent.IsKind(SyntaxKind.XmlElementStartTag) OrElse
               Not startToken.Parent.Parent.Parent.IsKind(SyntaxKind.XmlElement) Then

                Return False
            End If

            Dim node = DirectCast(startToken.Parent.Parent, XmlElementStartTagSyntax)
            If node.Name IsNot startToken.Parent Then
                Return False
            End If

            Dim element = DirectCast(startToken.Parent.Parent.Parent, XmlElementSyntax)
            If element.EndTag Is Nothing OrElse
               element.EndTag.Name Is Nothing OrElse
               element.EndTag.Name.LocalName.IsMissing Then
                Return False
            End If

            endToken = element.EndTag.Name.LocalName
            Return startToken.ValueText = endToken.ValueText
        End Function
    End Class
End Namespace
