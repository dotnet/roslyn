' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Options

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.Options
    <ComVisible(True)>
    Partial Public Class AutomationObject
        Private ReadOnly _workspace As Workspace

        Friend Sub New(workspace As Workspace)
            _workspace = workspace
        End Sub

        Private Function GetBooleanOption(key As [PerLanguageOption2](Of Boolean)) As Boolean
            Return _workspace.Options.GetOption(key, LanguageNames.VisualBasic)
        End Function

        Private Function GetXmlOption(key As PerLanguageOption2(Of CodeStyleOption2(Of Boolean))) As String
            Return _workspace.Options.GetOption(key, LanguageNames.VisualBasic).ToXElement().ToString()
        End Function

        Private Sub SetBooleanOption(key As [PerLanguageOption2](Of Boolean), value As Boolean)
            _workspace.TryApplyChanges(_workspace.CurrentSolution.WithOptions(_workspace.Options _
                .WithChangedOption(key, LanguageNames.VisualBasic, value)))
        End Sub

        Private Function GetBooleanOption(key As PerLanguageOption2(Of Boolean?)) As Integer
            Dim [option] = _workspace.Options.GetOption(key, LanguageNames.VisualBasic)
            If Not [option].HasValue Then
                Return -1
            End If

            Return If([option].Value, 1, 0)
        End Function

        Private Sub SetBooleanOption(key As PerLanguageOption2(Of Boolean?), value As Integer)
            Dim boolValue As Boolean? = If(value < 0, Nothing, value > 0)
            _workspace.TryApplyChanges(_workspace.CurrentSolution.WithOptions(_workspace.Options _
                .WithChangedOption(key, LanguageNames.VisualBasic, boolValue)))
        End Sub

        Private Sub SetXmlOption(key As PerLanguageOption2(Of CodeStyleOption2(Of Boolean)), value As String)
            Dim convertedValue = CodeStyleOption2(Of Boolean).FromXElement(XElement.Parse(value))
            _workspace.TryApplyChanges(_workspace.CurrentSolution.WithOptions(_workspace.Options _
                .WithChangedOption(key, LanguageNames.VisualBasic, convertedValue)))
        End Sub
    End Class
End Namespace
