' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.VisualBasicHelpers
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim

    Public Class ConvertedVisualBasicProjectOptionsTests
        <WpfFact, WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")>
        Public Sub RuleSet_GeneralCommandLineOptionOverridesGeneralRuleSetOption()
            Dim convertedOptions = GetConvertedOptions(ruleSetGeneralOption:=ReportDiagnostic.Warn, commandLineGeneralOption:=WarningLevel.WARN_AsError)

            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=convertedOptions.CompilationOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=0, actual:=convertedOptions.CompilationOptions.SpecificDiagnosticOptions.Count)
        End Sub

        <WpfFact, WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")>
        Public Sub RuleSet_GeneralWarnAsErrorPromotesWarningFromRuleSet()
            Dim ruleSetSpecificOptions = New Dictionary(Of String, ReportDiagnostic) From
                {
                    {"Test001", ReportDiagnostic.Warn}
                }.ToImmutableDictionary()

            Dim convertedOptions = GetConvertedOptions(commandLineGeneralOption:=WarningLevel.WARN_AsError, ruleSetSpecificOptions:=ruleSetSpecificOptions)

            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=convertedOptions.CompilationOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=1, actual:=convertedOptions.CompilationOptions.SpecificDiagnosticOptions.Count)
            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=convertedOptions.CompilationOptions.SpecificDiagnosticOptions("Test001"))
        End Sub

        <WpfFact, WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")>
        Public Sub RuleSet_GeneralWarnAsErrorDoesNotPromoteInfoFromRuleSet()
            Dim ruleSetSpecificOptions = New Dictionary(Of String, ReportDiagnostic) From
                {
                    {"Test001", ReportDiagnostic.Info}
                }.ToImmutableDictionary()

            Dim convertedOptions = GetConvertedOptions(commandLineGeneralOption:=WarningLevel.WARN_AsError, ruleSetSpecificOptions:=ruleSetSpecificOptions)

            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=convertedOptions.CompilationOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=1, actual:=convertedOptions.CompilationOptions.SpecificDiagnosticOptions.Count)
            Assert.Equal(expected:=ReportDiagnostic.Info, actual:=convertedOptions.CompilationOptions.SpecificDiagnosticOptions("Test001"))
        End Sub

        <WpfFact, WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")>
        Public Sub RuleSet_SpecificWarnAsErrorPromotesInfoFromRuleSet()
            Dim ruleSetSpecificOptions = New Dictionary(Of String, ReportDiagnostic) From
                {
                    {"Test001", ReportDiagnostic.Info}
                }.ToImmutableDictionary()

            Dim convertedOptions = GetConvertedOptions(
                ruleSetSpecificOptions:=ruleSetSpecificOptions,
                commandLineGeneralOption:=WarningLevel.WARN_AsError,
                commandLineWarnAsErrors:="Test001")

            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=convertedOptions.CompilationOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=1, actual:=convertedOptions.CompilationOptions.SpecificDiagnosticOptions.Count)
            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=convertedOptions.CompilationOptions.SpecificDiagnosticOptions("Test001"))
        End Sub

        <WpfFact, WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")>
        Public Sub RuleSet_SpecificWarnAsErrorMinusResetsRules()
            Dim ruleSetSpecificOptions = New Dictionary(Of String, ReportDiagnostic) From
                {
                    {"Test001", ReportDiagnostic.Warn}
                }.ToImmutableDictionary()

            Dim convertedOptions = GetConvertedOptions(
                ruleSetSpecificOptions:=ruleSetSpecificOptions,
                commandLineGeneralOption:=WarningLevel.WARN_AsError,
                commandLineWarnNotAsErrors:="Test001")

            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=convertedOptions.CompilationOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=1, actual:=convertedOptions.CompilationOptions.SpecificDiagnosticOptions.Count)
            Assert.Equal(expected:=ReportDiagnostic.Warn, actual:=convertedOptions.CompilationOptions.SpecificDiagnosticOptions("Test001"))
        End Sub

        <WpfFact, WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")>
        Public Sub RuleSet_SpecificWarnAsErrorMinusDefaultsRuleNotInRuleSet()
            Dim ruleSetSpecificOptions = New Dictionary(Of String, ReportDiagnostic) From
                {
                    {"Test001", ReportDiagnostic.Warn}
                }.ToImmutableDictionary()

            Dim convertedOptions = GetConvertedOptions(
                ruleSetSpecificOptions:=ruleSetSpecificOptions,
                commandLineGeneralOption:=WarningLevel.WARN_AsError,
                commandLineWarnNotAsErrors:="Test001;Test002")

            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=convertedOptions.CompilationOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=2, actual:=convertedOptions.CompilationOptions.SpecificDiagnosticOptions.Count)
            Assert.Equal(expected:=ReportDiagnostic.Warn, actual:=convertedOptions.CompilationOptions.SpecificDiagnosticOptions("Test001"))
            Assert.Equal(expected:=ReportDiagnostic.Default, actual:=convertedOptions.CompilationOptions.SpecificDiagnosticOptions("Test002"))
        End Sub

        <WpfFact, WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")>
        Public Sub RuleSet_GeneralNoWarnTurnsOffAllButErrors()
            Dim ruleSetSpecificOptions = New Dictionary(Of String, ReportDiagnostic) From
               {
                   {"Test001", ReportDiagnostic.Error},
                   {"Test002", ReportDiagnostic.Warn},
                   {"Test003", ReportDiagnostic.Info}
               }.ToImmutableDictionary()

            Dim convertedOptions = GetConvertedOptions(
                ruleSetSpecificOptions:=ruleSetSpecificOptions,
                commandLineGeneralOption:=WarningLevel.WARN_None)

            Assert.Equal(expected:=ReportDiagnostic.Suppress, actual:=convertedOptions.CompilationOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=3, actual:=convertedOptions.CompilationOptions.SpecificDiagnosticOptions.Count)
            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=convertedOptions.CompilationOptions.SpecificDiagnosticOptions("Test001"))
            Assert.Equal(expected:=ReportDiagnostic.Suppress, actual:=convertedOptions.CompilationOptions.SpecificDiagnosticOptions("Test002"))
            Assert.Equal(expected:=ReportDiagnostic.Suppress, actual:=convertedOptions.CompilationOptions.SpecificDiagnosticOptions("Test003"))
        End Sub

        <WpfFact, WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")>
        Public Sub RuleSet_SpecificNoWarnAlwaysWins()
            Dim ruleSetSpecificOptions = New Dictionary(Of String, ReportDiagnostic) From
                {
                    {"Test001", ReportDiagnostic.Warn}
                }.ToImmutableDictionary()

            Dim convertedOptions = GetConvertedOptions(
                ruleSetSpecificOptions:=ruleSetSpecificOptions,
                commandLineWarnAsErrors:="Test001",
                commandLineNoWarns:="Test001")

            Assert.Equal(expected:=ReportDiagnostic.Default, actual:=convertedOptions.CompilationOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=1, actual:=convertedOptions.CompilationOptions.SpecificDiagnosticOptions.Count)
            Assert.Equal(expected:=ReportDiagnostic.Suppress, actual:=convertedOptions.CompilationOptions.SpecificDiagnosticOptions("Test001"))
        End Sub

        Private Shared Function GetConvertedOptions(
            Optional ruleSetGeneralOption As ReportDiagnostic = ReportDiagnostic.Default,
            Optional ruleSetSpecificOptions As ImmutableDictionary(Of String, ReportDiagnostic) = Nothing,
            Optional commandLineGeneralOption As WarningLevel = WarningLevel.WARN_Regular,
            Optional commandLineWarnAsErrors As String = "",
            Optional commandLineWarnNotAsErrors As String = "",
            Optional commandLineNoWarns As String = "") As ConvertedVisualBasicProjectOptions

            ruleSetSpecificOptions = If(ruleSetSpecificOptions Is Nothing, ImmutableDictionary(Of String, ReportDiagnostic).Empty, ruleSetSpecificOptions)

            Dim compilerOptions = New VBCompilerOptions With
                            {
                                .WarningLevel = commandLineGeneralOption,
                                .wszWarningsAsErrors = commandLineWarnAsErrors,
                                .wszWarningsNotAsErrors = commandLineWarnNotAsErrors,
                                .wszDisabledWarnings = commandLineNoWarns
                            }
            Dim compilerHost = New MockCompilerHost("C:\SDK")
            Dim convertedOptions = New ConvertedVisualBasicProjectOptions(
                                    compilerOptions,
                                    compilerHost,
                                    SpecializedCollections.EmptyEnumerable(Of GlobalImport),
                                    ImmutableArray(Of String).Empty,
                                    Nothing,
                                    New MockRuleSetFile(ruleSetGeneralOption, ruleSetSpecificOptions))
            Return convertedOptions
        End Function
    End Class
End Namespace
