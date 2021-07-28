' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports System.Text
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.EndConstructGeneration
    Friend Module EndConstructExtensions
        <Extension()>
        Public Function GetAligningWhitespace(snapshot As ITextSnapshot, position As Integer) As String
            If snapshot Is Nothing Then
                Throw New ArgumentNullException(NameOf(snapshot))
            End If

            Dim line = snapshot.GetLineFromPosition(position)
            Dim precedingText = snapshot.GetText(Span.FromBounds(line.Start, position))

            ' To generate the aligning whitespace, we take the preceding text and simply replace any non-tab with a
            ' space. This is to guarantee we behave properly in the case of tabs and spaces, as well as cases where the
            ' user is mixing tabs and spaces and expects it to align in any tab width. (Trust me, most other "obvious"
            ' ways to implement this are wrong.)

            Dim builder As New StringBuilder

            For Each c As Char In precedingText
                If c = vbTab Then
                    builder.Append(vbTab)
                Else
                    builder.Append(" ")
                End If
            Next

            Return builder.ToString()
        End Function
    End Module
End Namespace
