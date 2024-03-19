' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.SolutionExplorer
    <Trait(Traits.Feature, Traits.Features.Diagnostics)>
    Public Class RuleSetDocumentExtensionsTests
        <Fact>
        Public Sub AdjustSingleNonExistentRule()
            Dim startingRuleSet =
                <RuleSet Name="MyRules" Description="A bunch of rules">
                </RuleSet>

            Dim document = New XDocument(startingRuleSet)

            document.SetSeverity("Alpha.Analyzer", "Test001", ReportDiagnostic.Error)

            Dim expectedRuleSet =
                <RuleSet Name="MyRules" Description="A bunch of rules">
                    <Rules AnalyzerId="Alpha.Analyzer" RuleNamespace="Alpha.Analyzer">
                        <Rule Id="Test001" Action="Error"/>
                    </Rules>
                </RuleSet>

            Assert.Equal(expectedRuleSet.Value, document.Element("RuleSet").Value)
        End Sub

        <Fact>
        Public Sub AdjustSingleExistentRule()
            Dim startingRuleSet =
                <RuleSet Name="MyRules" Description="A bunch of rules">
                    <Rules AnalyzerId="Alpha.Analyzer" RuleNamespace="Alpha.Analyzer">
                        <Rule Id="Test001" Action="Error"/>
                    </Rules>
                </RuleSet>

            Dim document = New XDocument(startingRuleSet)

            document.SetSeverity("Alpha.Analyzer", "Test001", ReportDiagnostic.Warn)

            Dim expectedRuleSet =
                <RuleSet Name="MyRules" Description="A bunch of rules">
                    <Rules AnalyzerId="Alpha.Analyzer" RuleNamespace="Alpha.Analyzer">
                        <Rule Id="Test001" Action="Warn"/>
                    </Rules>
                </RuleSet>

            Assert.Equal(expectedRuleSet.Value, document.Element("RuleSet").Value)
        End Sub

        <Fact>
        Public Sub AdjustSingleRuleUnderDifferentAnalyzer()
            Dim startingRuleSet =
                <RuleSet Name="MyRules" Description="A bunch of rules">
                    <Rules AnalyzerId="Alpha.Analyzer" RuleNamespace="Alpha.Analyzer">
                        <Rule Id="Test001" Action="Error"/>
                    </Rules>
                </RuleSet>

            Dim document = New XDocument(startingRuleSet)

            document.SetSeverity("Beta.Analyzer", "Test001", ReportDiagnostic.Warn)

            Dim expectedRuleSet =
                <RuleSet Name="MyRules" Description="A bunch of rules">
                    <Rules AnalyzerId="Alpha.Analyzer" RuleNamespace="Alpha.Analyzer">
                        <Rule Id="Test001" Action="Warn"/>
                    </Rules>
                    <Rules AnalyzerId="Beta.Analyzer" RuleNamespace="Alpha.Analyzer">
                        <Rule Id="Test001" Action="Warn"/>
                    </Rules>
                </RuleSet>

            Assert.Equal(expectedRuleSet.Value, document.Element("RuleSet").Value)
        End Sub

        <Fact>
        Public Sub AdjustMultipleRules()
            Dim startingRuleSet =
                <RuleSet Name="MyRules" Description="A bunch of rules">
                    <Rules AnalyzerId="Alpha.Analyzer" RuleNamespace="Alpha.Analyzer">
                        <Rule Id="Test001" Action="Warn"/>
                    </Rules>
                    <Rules AnalyzerId="Beta.Analyzer" RuleNamespace="Alpha.Analyzer">
                        <Rule Id="Test001" Action="Warn"/>
                    </Rules>
                </RuleSet>

            Dim document = New XDocument(startingRuleSet)

            document.SetSeverity("Alpha.Analyzer", "Test001", ReportDiagnostic.Error)

            Dim expectedRuleSet =
                <RuleSet Name="MyRules" Description="A bunch of rules">
                    <Rules AnalyzerId="Alpha.Analyzer" RuleNamespace="Alpha.Analyzer">
                        <Rule Id="Test001" Action="Error"/>
                    </Rules>
                    <Rules AnalyzerId="Beta.Analyzer" RuleNamespace="Alpha.Analyzer">
                        <Rule Id="Test001" Action="Error"/>
                    </Rules>
                </RuleSet>

            Assert.Equal(expectedRuleSet.Value, document.Element("RuleSet").Value)
        End Sub

        <Fact>
        Public Sub RemoveSingleNonExistentRule()
            Dim startingRuleSet =
                <RuleSet Name="MyRules" Description="A bunch of rules">
                </RuleSet>

            Dim document = New XDocument(startingRuleSet)

            document.SetSeverity("Alpha.Analyzer", "Test001", ReportDiagnostic.Default)

            Dim expectedRuleSet =
                <RuleSet Name="MyRules" Description="A bunch of rules">
                    <Rules AnalyzerId="Alpha.Analyzer" RuleNamespace="Alpha.Analyzer">
                    </Rules>
                </RuleSet>

            Assert.Equal(expectedRuleSet.Value, document.Element("RuleSet").Value)
        End Sub

        <Fact>
        Public Sub RemoveSingleExistentRule()
            Dim startingRuleSet =
                <RuleSet Name="MyRules" Description="A bunch of rules">
                    <Rules AnalyzerId="Alpha.Analyzer" RuleNamespace="Alpha.Analyzer">
                        <Rule Id="Test001" Action="Error"/>
                    </Rules>
                </RuleSet>

            Dim document = New XDocument(startingRuleSet)

            document.SetSeverity("Alpha.Analyzer", "Test001", ReportDiagnostic.Default)

            Dim expectedRuleSet =
                <RuleSet Name="MyRules" Description="A bunch of rules">
                    <Rules AnalyzerId="Alpha.Analyzer" RuleNamespace="Alpha.Analyzer">
                    </Rules>
                </RuleSet>

            Assert.Equal(expectedRuleSet.Value, document.Element("RuleSet").Value)
        End Sub

        <Fact>
        Public Sub RemoveMultipleRules()
            Dim startingRuleSet =
                <RuleSet Name="MyRules" Description="A bunch of rules">
                    <Rules AnalyzerId="Alpha.Analyzer" RuleNamespace="Alpha.Analyzer">
                        <Rule Id="Test001" Action="Warn"/>
                    </Rules>
                    <Rules AnalyzerId="Beta.Analyzer" RuleNamespace="Alpha.Analyzer">
                        <Rule Id="Test001" Action="Warn"/>
                    </Rules>
                </RuleSet>

            Dim document = New XDocument(startingRuleSet)

            document.SetSeverity("Alpha.Analyzer", "Test001", ReportDiagnostic.Default)

            Dim expectedRuleSet =
                <RuleSet Name="MyRules" Description="A bunch of rules">
                    <Rules AnalyzerId="Alpha.Analyzer" RuleNamespace="Alpha.Analyzer">
                    </Rules>
                    <Rules AnalyzerId="Beta.Analyzer" RuleNamespace="Alpha.Analyzer">
                    </Rules>
                </RuleSet>

            Assert.Equal(expectedRuleSet.Value, document.Element("RuleSet").Value)
        End Sub
    End Class
End Namespace

