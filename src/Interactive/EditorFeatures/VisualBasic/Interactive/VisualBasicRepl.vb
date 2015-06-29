' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Interactive
Imports Microsoft.CodeAnalysis.Scripting
Imports Microsoft.CodeAnalysis.Scripting.VisualBasic

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Interactive

    Friend Class VisualBasicRepl
        Implements IRepl

        Public Sub New()
        End Sub

        Public Function CreateObjectFormatter() As ObjectFormatter Implements IRepl.CreateObjectFormatter
            Return VisualBasicObjectFormatter.Instance
        End Function

        Public Function CreateScript(code As String) As Script Implements IRepl.CreateScript
            Return VisualBasicScript.Create(code)
        End Function

        Public Function GetLogo() As String Implements IRepl.GetLogo
            Return String.Format(VBInteractiveEditorResources.VBReplLogo,
                                 FileVersionInfo.GetVersionInfo(GetType(VisualBasicCommandLineArguments).Assembly.Location).FileVersion)
        End Function

        Public Function GetCommandLineParser() As CommandLineParser Implements IRepl.GetCommandLineParser
#If SCRIPTING Then
            Return VisualBasicCommandLineParser.Interactive
#Else
            Return VisualBasicCommandLineParser.Default
#End If
        End Function

        Public Function GetDiagnosticFormatter() As DiagnosticFormatter Implements IRepl.GetDiagnosticFormatter
            Return VisualBasicDiagnosticFormatter.Instance
        End Function
    End Class
End Namespace

