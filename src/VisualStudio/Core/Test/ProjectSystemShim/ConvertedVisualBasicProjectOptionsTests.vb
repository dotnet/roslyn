' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.VisualBasicHelpers
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim

    Public Class ConvertedVisualBasicProjectOptionsTests
        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/468")>
        Public Sub RuleSet_GeneralCommandLineOptionOverridesGeneralRuleSetOption()
            Dim convertedOptions = GetConvertedOptions(ruleSetGeneralOption:=ReportDiagnostic.Warn, commandLineGeneralOption:=WarningLevel.WARN_AsError)

            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=convertedOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=0, actual:=convertedOptions.SpecificDiagnosticOptions.Count)
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/468")>
        Public Sub RuleSet_GeneralWarnAsErrorPromotesWarningFromRuleSet()
            Dim ruleSetSpecificOptions = New Dictionary(Of String, ReportDiagnostic) From
                {
                    {"Test001", ReportDiagnostic.Warn}
                }.ToImmutableDictionary()

            Dim convertedOptions = GetConvertedOptions(commandLineGeneralOption:=WarningLevel.WARN_AsError, ruleSetSpecificOptions:=ruleSetSpecificOptions)

            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=convertedOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=1, actual:=convertedOptions.SpecificDiagnosticOptions.Count)
            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=convertedOptions.SpecificDiagnosticOptions("Test001"))
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/468")>
        Public Sub RuleSet_GeneralWarnAsErrorDoesNotPromoteInfoFromRuleSet()
            Dim ruleSetSpecificOptions = New Dictionary(Of String, ReportDiagnostic) From
                {
                    {"Test001", ReportDiagnostic.Info}
                }.ToImmutableDictionary()

            Dim convertedOptions = GetConvertedOptions(commandLineGeneralOption:=WarningLevel.WARN_AsError, ruleSetSpecificOptions:=ruleSetSpecificOptions)

            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=convertedOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=1, actual:=convertedOptions.SpecificDiagnosticOptions.Count)
            Assert.Equal(expected:=ReportDiagnostic.Info, actual:=convertedOptions.SpecificDiagnosticOptions("Test001"))
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/468")>
        Public Sub RuleSet_SpecificWarnAsErrorPromotesInfoFromRuleSet()
            Dim ruleSetSpecificOptions = New Dictionary(Of String, ReportDiagnostic) From
                {
                    {"Test001", ReportDiagnostic.Info}
                }.ToImmutableDictionary()

            Dim convertedOptions = GetConvertedOptions(
                ruleSetSpecificOptions:=ruleSetSpecificOptions,
                commandLineGeneralOption:=WarningLevel.WARN_AsError,
                commandLineWarnAsErrors:="Test001")

            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=convertedOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=1, actual:=convertedOptions.SpecificDiagnosticOptions.Count)
            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=convertedOptions.SpecificDiagnosticOptions("Test001"))
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/468")>
        Public Sub RuleSet_SpecificWarnAsErrorMinusResetsRules()
            Dim ruleSetSpecificOptions = New Dictionary(Of String, ReportDiagnostic) From
                {
                    {"Test001", ReportDiagnostic.Warn}
                }.ToImmutableDictionary()

            Dim convertedOptions = GetConvertedOptions(
                ruleSetSpecificOptions:=ruleSetSpecificOptions,
                commandLineGeneralOption:=WarningLevel.WARN_AsError,
                commandLineWarnNotAsErrors:="Test001")

            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=convertedOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=1, actual:=convertedOptions.SpecificDiagnosticOptions.Count)
            Assert.Equal(expected:=ReportDiagnostic.Warn, actual:=convertedOptions.SpecificDiagnosticOptions("Test001"))
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/468")>
        Public Sub RuleSet_SpecificWarnAsErrorMinusDefaultsRuleNotInRuleSet()
            Dim ruleSetSpecificOptions = New Dictionary(Of String, ReportDiagnostic) From
                {
                    {"Test001", ReportDiagnostic.Warn}
                }.ToImmutableDictionary()

            Dim convertedOptions = GetConvertedOptions(
                ruleSetSpecificOptions:=ruleSetSpecificOptions,
                commandLineGeneralOption:=WarningLevel.WARN_AsError,
                commandLineWarnNotAsErrors:="Test001;Test002")

            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=convertedOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=2, actual:=convertedOptions.SpecificDiagnosticOptions.Count)
            Assert.Equal(expected:=ReportDiagnostic.Warn, actual:=convertedOptions.SpecificDiagnosticOptions("Test001"))
            Assert.Equal(expected:=ReportDiagnostic.Default, actual:=convertedOptions.SpecificDiagnosticOptions("Test002"))
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/468")>
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

            Assert.Equal(expected:=ReportDiagnostic.Suppress, actual:=convertedOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=3, actual:=convertedOptions.SpecificDiagnosticOptions.Count)
            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=convertedOptions.SpecificDiagnosticOptions("Test001"))
            Assert.Equal(expected:=ReportDiagnostic.Suppress, actual:=convertedOptions.SpecificDiagnosticOptions("Test002"))
            Assert.Equal(expected:=ReportDiagnostic.Suppress, actual:=convertedOptions.SpecificDiagnosticOptions("Test003"))
        End Sub

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/468")>
        Public Sub RuleSet_SpecificNoWarnAlwaysWins()
            Dim ruleSetSpecificOptions = New Dictionary(Of String, ReportDiagnostic) From
                {
                    {"Test001", ReportDiagnostic.Warn}
                }.ToImmutableDictionary()

            Dim convertedOptions = GetConvertedOptions(
                ruleSetSpecificOptions:=ruleSetSpecificOptions,
                commandLineWarnAsErrors:="Test001",
                commandLineNoWarns:="Test001")

            Assert.Equal(expected:=ReportDiagnostic.Default, actual:=convertedOptions.GeneralDiagnosticOption)
            Assert.Equal(expected:=1, actual:=convertedOptions.SpecificDiagnosticOptions.Count)
            Assert.Equal(expected:=ReportDiagnostic.Suppress, actual:=convertedOptions.SpecificDiagnosticOptions("Test001"))
        End Sub

        Private Shared Function GetConvertedOptions(
            Optional ruleSetGeneralOption As ReportDiagnostic = ReportDiagnostic.Default,
            Optional ruleSetSpecificOptions As ImmutableDictionary(Of String, ReportDiagnostic) = Nothing,
            Optional commandLineGeneralOption As WarningLevel = WarningLevel.WARN_Regular,
            Optional commandLineWarnAsErrors As String = "",
            Optional commandLineWarnNotAsErrors As String = "",
            Optional commandLineNoWarns As String = "") As VisualBasicCompilationOptions

            ruleSetSpecificOptions = If(ruleSetSpecificOptions, ImmutableDictionary(Of String, ReportDiagnostic).Empty)

            Dim compilerOptions = New VBCompilerOptions With
                            {
                                .WarningLevel = commandLineGeneralOption,
                                .wszWarningsAsErrors = commandLineWarnAsErrors,
                                .wszWarningsNotAsErrors = commandLineWarnNotAsErrors,
                                .wszDisabledWarnings = commandLineNoWarns
                            }
            Dim compilerHost = New MockCompilerHost("C:\SDK")
            Return VisualBasicProject.OptionsProcessor.ApplyCompilationOptionsFromVBCompilerOptions(
                New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary) _
                    .WithParseOptions(VisualBasicParseOptions.Default), compilerOptions,
                New MockRuleSetFile(ruleSetGeneralOption, ruleSetSpecificOptions))
        End Function
    End Class
End Namespace
