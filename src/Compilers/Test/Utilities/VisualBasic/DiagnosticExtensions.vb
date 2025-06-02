' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text

Namespace Global.Microsoft.CodeAnalysis.VisualBasic

    Friend Module DiagnosticsExtensions

        <Extension>
        Friend Function VerifyDiagnostics(c As VisualBasicCompilation, ParamArray expected As DiagnosticDescription()) As VisualBasicCompilation
            Dim diagnostics = c.GetDiagnostics(CompilationStage.Compile)
            diagnostics.Verify(expected)
            Return c
        End Function

        <Extension>
        Friend Function GetDiagnosticsForSyntaxTree(c As VisualBasicCompilation, stage As CompilationStage, tree As SyntaxTree, Optional filterSpan As TextSpan? = Nothing) As ImmutableArray(Of Diagnostic)
            Return c.GetDiagnosticsForSyntaxTree(stage, tree, filterSpan, includeEarlierStages:=True, cancellationToken:=CancellationToken.None)
        End Function

        ' TODO: Figure out how to return a localized message using VB
        '<Extension()>
        'Public Function ToLocalizedString(id As MessageID) As String
        '    Return New LocalizableErrorArgument(id).ToString(Nothing, Nothing)
        'End Function
    End Module

End Namespace
