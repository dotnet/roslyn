' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.AutomaticEndConstructCorrection
    Partial Friend Class AutomaticEndConstructCorrector
        Private Class Session
            Private ReadOnly _subjectBuffer As ITextBuffer

            Private _linkedSession As LinkedEditsTracker

            Public Sub New(subjectBuffer As ITextBuffer)
                Me._subjectBuffer = subjectBuffer

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

                If AutomaticEndConstructSet.Contains(replacementText) Then
                    Me._linkedSession.ApplyReplacementText(replacementText)
                End If

            End Sub

            Public Function OnTextChange(e As TextContentChangedEventArgs) As Boolean
                If Not Me.Alive Then
                    Return False
                End If

                If LinkedEditsTracker.MyOwnChanges(e) Then
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

                If AutomaticEndConstructSet.Contains(replacementText) Then
                    Me._linkedSession.ApplyReplacementText(replacementText)
                End If

                Return True
            End Function
        End Class
    End Class
End Namespace
