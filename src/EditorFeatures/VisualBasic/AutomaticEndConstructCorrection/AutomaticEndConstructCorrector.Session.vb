' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticEndConstructCorrection
    Partial Friend Class AbstractCorrector
        Private Class Session
            Private ReadOnly _subjectBuffer As ITextBuffer
            Private ReadOnly _shouldReplaceText As Func(Of String, Boolean)

            Private _linkedSession As LinkedEditsTracker

            Public Sub New(subjectBuffer As ITextBuffer, shouldReplaceText As Func(Of String, Boolean))
                Me._subjectBuffer = subjectBuffer
                Me._shouldReplaceText = shouldReplaceText
                Me._linkedSession = Nothing
            End Sub

            Public ReadOnly Property Alive As Boolean
                Get
                    Return Me._linkedSession IsNot Nothing
                End Get
            End Property

            Public Sub Start(linkedEditSpans As IEnumerable(Of ITrackingSpan), e As TextContentChangedEventArgs)
                Me._linkedSession = New LinkedEditsTracker(Me._subjectBuffer)
                Me._linkedSession.AddSpans(linkedEditSpans)

                Dim replacementText As String = Nothing
                If Not Me._linkedSession.TryGetTextChanged(e, replacementText) Then
                    Me._linkedSession = Nothing
                    Return
                End If

                If _shouldReplaceText(replacementText) Then
                    Me._linkedSession.ApplyReplacementText(replacementText)
                End If
            End Sub

            Public Function OnTextChange(e As TextContentChangedEventArgs) As Boolean
                If Not Me.Alive Then
                    Return False
                End If

                If Me._linkedSession.MyOwnChanges(e) Then
                    Return True
                End If

                Dim replacementText As String = Nothing
                If e.Changes.IncludesLineChanges OrElse
                    Not IsChangeOnSameLine(e.After, e.Changes(0)) OrElse
                    Not Me._linkedSession.TryGetTextChanged(e, replacementText) Then
                    ' session finished
                    Me._linkedSession = Nothing
                    Return False
                End If

                If _shouldReplaceText(replacementText) Then
                    Me._linkedSession.ApplyReplacementText(replacementText)
                End If

                Return True
            End Function
        End Class
    End Class
End Namespace