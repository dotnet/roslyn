' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions
Imports Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Features.EmbeddedLanguages

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EmbeddedLanguages
    Public Class ValidateRegexStringTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicRegexDiagnosticAnalyzer(), Nothing)
        End Function

        Private Function OptionOn() As IDictionary(Of OptionKey, Object)
            Dim values = New Dictionary(Of OptionKey, Object)
            values.Add(New OptionKey(RegularExpressionsOptions.ReportInvalidRegexPatterns, LanguageNames.VisualBasic), True)
            Return values
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateRegexString)>
        Public Async Function TestWarning1() As Task
            Await TestDiagnosticInfoAsync("
        imports System.Text.RegularExpressions

        class Program
            sub Main()
                var r = new Regex(""[|)|]"")
            end sub
        end class",
                        options:=OptionOn(),
                        diagnosticId:=AbstractRegexDiagnosticAnalyzer.DiagnosticId,
                        diagnosticSeverity:=DiagnosticSeverity.Warning,
                        diagnosticMessage:=String.Format(WorkspacesResources.Regex_issue_0, WorkspacesResources.Too_many_close_parens))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateRegexString)>
        Public Async Function TestWarning2() As Task
            Await TestDiagnosticInfoAsync("
        imports System.Text.RegularExpressions

        class Program
            sub Main()
                var r = new Regex(""""""[|)|]"")
            end sub
        end class",
                        options:=OptionOn(),
                        diagnosticId:=AbstractRegexDiagnosticAnalyzer.DiagnosticId,
                        diagnosticSeverity:=DiagnosticSeverity.Warning,
                        diagnosticMessage:=String.Format(WorkspacesResources.Regex_issue_0, WorkspacesResources.Too_many_close_parens))
        End Function
    End Class
End Namespace
