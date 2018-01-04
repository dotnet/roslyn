' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.RegularExpressions
Imports Microsoft.CodeAnalysis.VisualBasic.ValidateRegexString
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ValidateRegexString
    Public Class ValidateRegexStringTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicValidateRegexStringDiagnosticAnalyzer(), Nothing)
        End Function

        Private Function OptionOn() As IDictionary(Of OptionKey, Object)
            Dim values = New Dictionary(Of OptionKey, Object)
            values.Add(New OptionKey(RegularExpressionsOptions.ReportInvalidRegexPatterns, LanguageNames.CSharp), True)
            values.Add(New OptionKey(RegularExpressionsOptions.ReportInvalidRegexPatterns, LanguageNames.VisualBasic), True)
            Return values
        End Function

        Private Function OptionOff() As IDictionary(Of OptionKey, Object)
            Dim values = New Dictionary(Of OptionKey, Object)
            values.Add(New OptionKey(RegularExpressionsOptions.ReportInvalidRegexPatterns, LanguageNames.CSharp), False)
            values.Add(New OptionKey(RegularExpressionsOptions.ReportInvalidRegexPatterns, LanguageNames.VisualBasic), False)
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
                        diagnosticId:=IDEDiagnosticIds.RegexPatternDiagnosticId,
                        diagnosticSeverity:=DiagnosticSeverity.Warning,
                        diagnosticMessage:=String.Format(FeaturesResources.Regex_issue_0, WorkspacesResources.Too_many_close_parens))
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
                        diagnosticId:=IDEDiagnosticIds.RegexPatternDiagnosticId,
                        diagnosticSeverity:=DiagnosticSeverity.Warning,
                        diagnosticMessage:=String.Format(FeaturesResources.Regex_issue_0, WorkspacesResources.Too_many_close_parens))
        End Function
    End Class
End Namespace
