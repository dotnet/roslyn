' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Text
Imports System.Text.RegularExpressions
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic
    <ExportLanguageService(GetType(ILinkedFileMergeConflictCommentAdditionService), LanguageNames.VisualBasic), [Shared]>
    Friend Class BasicLinkedFileMergeConflictCommentAdditionService
        Inherits AbstractLinkedFileMergeConflictCommentAdditionService

        Friend Overrides Function GetConflictCommentText(header As String, beforeString As String, afterString As String) As String
            If beforeString Is Nothing AndAlso afterString Is Nothing Then
                Return Nothing
            ElseIf beforeString Is Nothing Then
                ' Added code
                Return String.Format("
' {0} 
' {1}
{2}
",
                    header,
                    WorkspacesResources.AddedHeader,
                    GetCommentedText(afterString))
            ElseIf afterString Is Nothing Then
                ' Removed code
                Return String.Format("
' {0} 
' {1}
{2}
",
                    header,
                    WorkspacesResources.RemovedHeader,
                    GetCommentedText(beforeString))
            Else
                Return String.Format("
' {0} 
' {1}
{2}
' {3}
{4}
",
                    header,
                    WorkspacesResources.BeforeHeader,
                    GetCommentedText(beforeString),
                    WorkspacesResources.AfterHeader,
                    GetCommentedText(afterString))

            End If
        End Function

        Private Function GetCommentedText(text As String) As String
            Dim lines = Regex.Split(text, "\r\n|\r|\n")
            If Not lines.Any() Then
                Return text
            End If

            Dim newlines = Regex.Matches(text, "\r\n|\r|\n")
            Contract.Assert(newlines.Count = lines.Length - 1)

            Dim builder = New StringBuilder()

            For i = 0 To lines.Length - 2
                builder.Append(String.Format("' {0}{1}", lines(i), newlines(i)))
            Next

            builder.Append(String.Format("' {0}", lines.Last()))

            Return builder.ToString()
        End Function
    End Class
End Namespace
