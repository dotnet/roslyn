' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic.DocumentationComments
    Friend Class DocumentationCommentUtilities
        Private Shared ReadOnly s_newLineStrings As String() = {vbCrLf, vbCr, vbLf}

        Public Shared Function ExtractXMLFragment(docComment As String) As String
            Dim splitLines = docComment.Split(s_newLineStrings, StringSplitOptions.None)

            For i = 0 To splitLines.Length - 1
                If splitLines(i).StartsWith("'''", StringComparison.Ordinal) Then
                    splitLines(i) = splitLines(i).Substring(3)
                End If
            Next

            Return splitLines.Join(vbCrLf)
        End Function
    End Class
End Namespace
