' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.SolutionExplorer
    Public Class DiagnosticItemSourceTests
        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub EffectiveSeverity_DiagnosticDefault1()
            Dim specificOptions = New Dictionary(Of String, ReportDiagnostic)
            Dim generalOption = ReportDiagnostic.Default
            Dim diagnosticDefault = DiagnosticSeverity.Warning
            Dim enabledByDefault = True

            Dim effectiveSeverity = DiagnosticItemSource.GetEffectiveSeverity(
                "Test0001",
                specificOptions,
                generalOption,
                diagnosticDefault,
                enabledByDefault)

            Assert.Equal(expected:=ReportDiagnostic.Warn, actual:=effectiveSeverity)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub EffectiveSeverity_DiagnosticDefault2()
            Dim specificOptions = New Dictionary(Of String, ReportDiagnostic) From
                {
                    {"Test0001", ReportDiagnostic.Default}
                }
            Dim generalOption = ReportDiagnostic.Error
            Dim diagnosticDefault = DiagnosticSeverity.Warning
            Dim enabledByDefault = True

            Dim effectiveSeverity = DiagnosticItemSource.GetEffectiveSeverity(
                "Test0001",
                specificOptions,
                generalOption,
                diagnosticDefault,
                enabledByDefault)

            Assert.Equal(expected:=ReportDiagnostic.Warn, actual:=effectiveSeverity)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub EffectiveSeverity_GeneralOption()
            Dim specificOptions = New Dictionary(Of String, ReportDiagnostic)
            Dim generalOption = ReportDiagnostic.Error
            Dim diagnosticDefault = DiagnosticSeverity.Warning
            Dim enabledByDefault = True

            Dim effectiveSeverity = DiagnosticItemSource.GetEffectiveSeverity(
                "Test0001",
                specificOptions,
                generalOption,
                diagnosticDefault,
                enabledByDefault)

            Assert.Equal(expected:=ReportDiagnostic.Error, actual:=effectiveSeverity)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub EffectiveSeverity_SpecificOption()
            Dim specificOptions = New Dictionary(Of String, ReportDiagnostic) From
                {
                    {"Test0001", ReportDiagnostic.Suppress}
                }
            Dim generalOption = ReportDiagnostic.Error
            Dim diagnosticDefault = DiagnosticSeverity.Warning
            Dim enabledByDefault = True

            Dim effectiveSeverity = DiagnosticItemSource.GetEffectiveSeverity(
                "Test0001",
                specificOptions,
                generalOption,
                diagnosticDefault,
                enabledByDefault)

            Assert.Equal(expected:=ReportDiagnostic.Suppress, actual:=effectiveSeverity)
        End Sub

        <WorkItem(1107500, "DevDiv")>
        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub EffectiveSeverity_GeneralOptionDoesNotEnableDisabledDiagnostic()
            Dim specificOptions = New Dictionary(Of String, ReportDiagnostic)
            Dim generalOption = ReportDiagnostic.Error
            Dim diagnosticDefault = DiagnosticSeverity.Warning
            Dim enabledByDefault = False

            Dim effectiveSeverity = DiagnosticItemSource.GetEffectiveSeverity(
                "Test0001",
                specificOptions,
                generalOption,
                diagnosticDefault,
                enabledByDefault)

            Assert.Equal(expected:=ReportDiagnostic.Suppress, actual:=effectiveSeverity)
        End Sub

        <WorkItem(1107500, "DevDiv")>
        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
        Public Sub EffectiveSeverity_SpecificOptionEnablesDisabledDiagnostic()
            Dim specificOptions = New Dictionary(Of String, ReportDiagnostic) From
                {
                    {"Test0001", ReportDiagnostic.Warn}
                }
            Dim generalOption = ReportDiagnostic.Error
            Dim diagnosticDefault = DiagnosticSeverity.Warning
            Dim enabledByDefault = False

            Dim effectiveSeverity = DiagnosticItemSource.GetEffectiveSeverity(
                "Test0001",
                specificOptions,
                generalOption,
                diagnosticDefault,
                enabledByDefault)

            Assert.Equal(expected:=ReportDiagnostic.Warn, actual:=effectiveSeverity)
        End Sub
    End Class
End Namespace

