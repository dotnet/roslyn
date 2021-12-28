﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions
Imports Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions
Imports Microsoft.CodeAnalysis.VisualBasic.Features.EmbeddedLanguages

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EmbeddedLanguages
    Public Class ValidateRegexStringTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicRegexDiagnosticAnalyzer(), Nothing)
        End Function

        Private Shared Function OptionOn() As OptionsCollection
            Dim values = New OptionsCollection(LanguageNames.VisualBasic)
            values.Add(RegularExpressionsOptions.ReportInvalidRegexPatterns, True)
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
                        diagnosticMessage:=String.Format(FeaturesResources.Regex_issue_0, FeaturesResources.Too_many_close_parens))
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
                        diagnosticMessage:=String.Format(FeaturesResources.Regex_issue_0, FeaturesResources.Too_many_close_parens))
        End Function
    End Class
End Namespace
