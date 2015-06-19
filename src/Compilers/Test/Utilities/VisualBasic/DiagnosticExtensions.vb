' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Global.Microsoft.CodeAnalysis.VisualBasic

    Friend Module DiagnosticsExtensions

        <Extension>
        Friend Function VerifyDiagnostics(c As VisualBasicCompilation, ParamArray expected As DiagnosticDescription()) As VisualBasicCompilation
            Dim diagnostics = c.GetDiagnostics(CompilationStage.Compile)
            diagnostics.Verify(expected)
            Return c
        End Function

        ' TODO: Figure out how to return a localized message using VB
        '<Extension()>
        'Public Function ToLocalizedString(id As MessageID) As String
        '    Return New LocalizableErrorArgument(id).ToString(Nothing, Nothing)
        'End Function
    End Module

End Namespace
