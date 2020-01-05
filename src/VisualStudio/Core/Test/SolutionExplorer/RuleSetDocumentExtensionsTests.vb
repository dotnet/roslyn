' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.SolutionExplorer
    Public Class RuleSetDocumentExtensionsTests
        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
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

        <Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)>
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

