// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

namespace Microsoft.CodeAnalysis.RulesetToEditorconfig.UnitTests
{
    public class RulesetToEditorconfigConverterTests
    {
        private const string PrimaryRulesetName = "MyRules.ruleset";
        private const string IncludedRulesetName = "IncludedRules.ruleset";

        private static void Verify(string rulesetText, string expectedEditorconfigText, string? includedRulesetText = null)
        {
            var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

            try
            {
                var ruleset = Path.Combine(directory.FullName, PrimaryRulesetName);
                File.WriteAllText(ruleset, rulesetText);

                if (includedRulesetText != null)
                {
                    Assert.Contains("<Include ", rulesetText, StringComparison.OrdinalIgnoreCase);
                    var includedRuleset = Path.Combine(directory.FullName, IncludedRulesetName);
                    File.WriteAllText(includedRuleset, includedRulesetText);
                }

                var editorconfigPath = Path.Combine(directory.FullName, ".editorconfig");
                Converter.GenerateEditorconfig(ruleset, editorconfigPath);

                var actualEditorConfigText = File.ReadAllText(editorconfigPath).Trim();
                expectedEditorconfigText = expectedEditorconfigText.Trim();
                if (!Equals(expectedEditorconfigText, actualEditorConfigText))
                {
                    // Dump the entire expected and actual lines for easy update to baseline.
                    Assert.Fail($"Expected:\r\n{expectedEditorconfigText}\r\n\r\nActual:\r\n{actualEditorConfigText}");
                }
            }
            finally
            {
                Directory.Delete(directory.FullName, recursive: true);
            }
        }

        [Fact]
        public void RuleSeveritiesPreserved()
            => Verify("""
                <?xml version="1.0" encoding="utf-8"?>
                <RuleSet Name="ConfigurationFileName" Description="Configuration file description" ToolsVersion="14.0">
                  <Rules AnalyzerId="MyAnalyzers" RuleNamespace="MyAnalyzers">
                    <Rule Id="CA1000" Action="Error" />
                    <Rule Id="CA1001" Action="Warning" />
                    <Rule Id="CA1002" Action="Info" />
                    <Rule Id="CA1003" Action="Hidden" />
                    <Rule Id="CA1004" Action="None" />
                  </Rules>
                </RuleSet>
                """, """
                # NOTE: Requires **VS2019 16.3** or later

                # ConfigurationFileName
                # Description: Configuration file description

                # Code files
                [*.{cs,vb}]


                dotnet_diagnostic.CA1000.severity = error

                dotnet_diagnostic.CA1001.severity = warning

                dotnet_diagnostic.CA1002.severity = suggestion

                dotnet_diagnostic.CA1003.severity = silent

                dotnet_diagnostic.CA1004.severity = none
                """);

        [Fact]
        public void RuleSeveritiesAcrossRulesGroupsPreserved()
            => Verify("""
                <?xml version="1.0" encoding="utf-8"?>
                <RuleSet Name="ConfigurationFileName" Description="Configuration file description" ToolsVersion="14.0">
                  <Rules AnalyzerId="MyAnalyzers1" RuleNamespace="MyAnalyzers1">
                    <Rule Id="CA1000" Action="Error" />
                  </Rules>
                  <Rules AnalyzerId="MyAnalyzers2" RuleNamespace="MyAnalyzers2">
                    <Rule Id="CA1001" Action="Warning" />
                  </Rules>
                </RuleSet>
                """, """
                # NOTE: Requires **VS2019 16.3** or later

                # ConfigurationFileName
                # Description: Configuration file description

                # Code files
                [*.{cs,vb}]


                dotnet_diagnostic.CA1000.severity = error

                dotnet_diagnostic.CA1001.severity = warning
                """);

        [Fact]
        public void RuleSeverityOverrideAfterIncludePreserved()
            => Verify($"""
                <?xml version="1.0" encoding="utf-8"?>
                <RuleSet Name="ConfigurationFileName" Description="Configuration file description" ToolsVersion="14.0">
                  <Include Path=".\{IncludedRulesetName}" Action="Default" />

                  <Rules AnalyzerId="MyAnalyzers" RuleNamespace="MyAnalyzers">
                    <Rule Id="CA1000" Action="Warning" />
                  </Rules>
                </RuleSet>
                """, """
                # NOTE: Requires **VS2019 16.3** or later

                # ConfigurationFileName
                # Description: Configuration file description

                # Code files
                [*.{cs,vb}]


                dotnet_diagnostic.CA1000.severity = warning
                """, """
                <?xml version="1.0" encoding="utf-8"?>
                <RuleSet Name="IncludedFileName" Description="Included configuration file description" ToolsVersion="14.0">
                  <Rules AnalyzerId="MyAnalyzers" RuleNamespace="MyAnalyzers">
                    <Rule Id="CA1000" Action="Error" />
                  </Rules>
                </RuleSet>
                """);

        [Fact]
        public void IncludeAllPreserved()
            => Verify("""
                <?xml version="1.0" encoding="utf-8"?>
                <RuleSet Name="ConfigurationFileName" Description="Configuration file description" ToolsVersion="14.0">
                  <IncludeAll Action="Warning" />

                  <Rules AnalyzerId="MyAnalyzers" RuleNamespace="MyAnalyzers">
                    <Rule Id="CA1000" Action="Error" />
                  </Rules>
                </RuleSet>
                """, """
                # NOTE: Requires **VS2019 16.3** or later

                # ConfigurationFileName
                # Description: Configuration file description

                # Code files
                [*.{cs,vb}]


                # Default severity for analyzer diagnostics - Requires **VS2019 16.5** or later
                dotnet_analyzer_diagnostic.severity = warning

                dotnet_diagnostic.CA1000.severity = error
                """);

        [Fact]
        public void CommentBeforeRulePreserved()
            => Verify("""
                <?xml version="1.0" encoding="utf-8"?>
                <RuleSet Name="ConfigurationFileName" Description="Configuration file description" ToolsVersion="14.0">
                  <Rules AnalyzerId="MyAnalyzers" RuleNamespace="MyAnalyzers">
                    <!-- Comment before rule -->
                    <Rule Id="CA1000" Action="None" />
                  </Rules>
                </RuleSet>
                """, """
                # NOTE: Requires **VS2019 16.3** or later

                # ConfigurationFileName
                # Description: Configuration file description

                # Code files
                [*.{cs,vb}]


                # Comment before rule
                dotnet_diagnostic.CA1000.severity = none
                """);

        [Fact]
        public void MultilineCommentBeforeRulePreserved()
            => Verify("""
                <?xml version="1.0" encoding="utf-8"?>
                <RuleSet Name="ConfigurationFileName" Description="Configuration file description" ToolsVersion="14.0">
                  <Rules AnalyzerId="MyAnalyzers" RuleNamespace="MyAnalyzers">
                    <!-- Multiline
                         comment
                         before
                         rule -->
                    <Rule Id="CA1000" Action="None" />
                  </Rules>
                </RuleSet>
                """, """
                # NOTE: Requires **VS2019 16.3** or later

                # ConfigurationFileName
                # Description: Configuration file description

                # Code files
                [*.{cs,vb}]


                # Multiline
                # comment
                # before
                # rule
                dotnet_diagnostic.CA1000.severity = none
                """);

        [Fact]
        public void MultipleCommentsBeforeRulePreserved()
            => Verify("""
                <?xml version="1.0" encoding="utf-8"?>
                <RuleSet Name="ConfigurationFileName" Description="Configuration file description" ToolsVersion="14.0">
                  <Rules AnalyzerId="MyAnalyzers" RuleNamespace="MyAnalyzers">
                    <!-- Comment1 before rule -->
                    <!-- Comment2 before rule -->
                    <Rule Id="CA1000" Action="None" />
                  </Rules>
                </RuleSet>
                """, """
                # NOTE: Requires **VS2019 16.3** or later

                # ConfigurationFileName
                # Description: Configuration file description

                # Code files
                [*.{cs,vb}]


                # Comment1 before rule
                # Comment2 before rule
                dotnet_diagnostic.CA1000.severity = none
                """);

        [Fact]
        public void CommentAfterRulePreserved()
            => Verify("""
                <?xml version="1.0" encoding="utf-8"?>
                <RuleSet Name="ConfigurationFileName" Description="Configuration file description" ToolsVersion="14.0">
                  <Rules AnalyzerId="MyAnalyzers" RuleNamespace="MyAnalyzers">
                    <Rule Id="CA1000" Action="None" />   <!-- Comment after rule -->
                  </Rules>
                </RuleSet>
                """, """
                # NOTE: Requires **VS2019 16.3** or later

                # ConfigurationFileName
                # Description: Configuration file description

                # Code files
                [*.{cs,vb}]


                # Comment after rule
                dotnet_diagnostic.CA1000.severity = none
                """);

        [Fact]
        public void CommentsBeforeAndAfterRulePreserved()
            => Verify("""
                <?xml version="1.0" encoding="utf-8"?>
                <RuleSet Name="ConfigurationFileName" Description="Configuration file description" ToolsVersion="14.0">
                  <Rules AnalyzerId="MyAnalyzers" RuleNamespace="MyAnalyzers">
                    <!-- Comment before rule -->
                    <Rule Id="CA1000" Action="None" />   <!-- Comment after rule -->
                  </Rules>
                </RuleSet>
                """, """
                # NOTE: Requires **VS2019 16.3** or later

                # ConfigurationFileName
                # Description: Configuration file description

                # Code files
                [*.{cs,vb}]


                # Comment before rule
                # Comment after rule
                dotnet_diagnostic.CA1000.severity = none
                """);

        [Fact]
        public void CommentsFromIncludedRulesetPreserved()
            => Verify($"""
                <?xml version="1.0" encoding="utf-8"?>
                <RuleSet Name="ConfigurationFileName" Description="Configuration file description" ToolsVersion="14.0">
                  <Include Path=".\{IncludedRulesetName}" Action="Default" />
                </RuleSet>
                """, """
                # NOTE: Requires **VS2019 16.3** or later

                # ConfigurationFileName
                # Description: Configuration file description

                # Code files
                [*.{cs,vb}]


                # Comment before rule
                # Comment after rule
                dotnet_diagnostic.CA1000.severity = none
                """, """
                <?xml version="1.0" encoding="utf-8"?>
                <RuleSet Name="IncludedFileName" Description="Included configuration file description" ToolsVersion="14.0">
                  <Rules AnalyzerId="MyAnalyzers" RuleNamespace="MyAnalyzers">
                    <!-- Comment before rule -->
                    <Rule Id="CA1000" Action="None" />   <!-- Comment after rule -->
                  </Rules>
                </RuleSet>
                """);

        [Fact]
        public void CommentsFromPrimaryAndIncludedRulesetPreserved()
            => Verify($"""
                <?xml version="1.0" encoding="utf-8"?>
                <RuleSet Name="ConfigurationFileName" Description="Configuration file description" ToolsVersion="14.0">
                  <Include Path=".\{IncludedRulesetName}" Action="Default" />

                  <Rules AnalyzerId="MyAnalyzers" RuleNamespace="MyAnalyzers">
                    <!-- Before comment from primary ruleset -->
                    <Rule Id="CA1000" Action="Warning" />   <!-- After comment from primary ruleset -->
                  </Rules>
                </RuleSet>
                """, """
                # NOTE: Requires **VS2019 16.3** or later

                # ConfigurationFileName
                # Description: Configuration file description

                # Code files
                [*.{cs,vb}]


                # Before comment from primary ruleset
                # After comment from primary ruleset
                dotnet_diagnostic.CA1000.severity = warning

                # Before comment from included ruleset
                # After comment from included ruleset
                dotnet_diagnostic.CA10001.severity = error
                """, """
                <?xml version="1.0" encoding="utf-8"?>
                <RuleSet Name="IncludedFileName" Description="Included configuration file description" ToolsVersion="14.0">
                  <Rules AnalyzerId="MyAnalyzers" RuleNamespace="MyAnalyzers">
                    <!-- Before comment from included ruleset -->
                    <Rule Id="CA10001" Action="Error" />   <!-- After comment from included ruleset -->
                  </Rules>
                </RuleSet>
                """);

        [Fact]
        public void CommentsFromOverrideAfterIncludedRulesetPreserved()
            => Verify($"""
                <?xml version="1.0" encoding="utf-8"?>
                <RuleSet Name="ConfigurationFileName" Description="Configuration file description" ToolsVersion="14.0">
                  <Include Path=".\{IncludedRulesetName}" Action="Default" />

                  <Rules AnalyzerId="MyAnalyzers" RuleNamespace="MyAnalyzers">
                    <!-- Before comment from primary ruleset -->
                    <Rule Id="CA1000" Action="Warning" />   <!-- After comment from primary ruleset -->
                  </Rules>
                </RuleSet>
                """, """
                # NOTE: Requires **VS2019 16.3** or later

                # ConfigurationFileName
                # Description: Configuration file description

                # Code files
                [*.{cs,vb}]


                # Before comment from primary ruleset
                # After comment from primary ruleset
                dotnet_diagnostic.CA1000.severity = warning
                """, """
                <?xml version="1.0" encoding="utf-8"?>
                <RuleSet Name="IncludedFileName" Description="Included configuration file description" ToolsVersion="14.0">
                  <Rules AnalyzerId="MyAnalyzers" RuleNamespace="MyAnalyzers">
                    <!-- Before comment from included ruleset -->
                    <Rule Id="CA1000" Action="Error" />   <!-- After comment from included ruleset -->
                  </Rules>
                </RuleSet>
                """);
    }
}
