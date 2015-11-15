// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class CommonCommandLineParserTests : TestBase
    {
        private void VerifyCommandLineSplitter(string commandLine, string[] expected, bool removeHashComments = false)
        {
            var actual = CommandLineParser.SplitCommandLineIntoArguments(commandLine, removeHashComments).ToArray();

            Assert.Equal(expected.Length, actual.Length);
            for (int i = 0; i < actual.Length; ++i)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }

        private RuleSet ParseRuleSet(string source, params string[] otherSources)
        {
            var dir = Temp.CreateDirectory();
            var file = dir.CreateFile("a.ruleset");
            file.WriteAllText(source);

            for (int i = 1; i <= otherSources.Length; i++)
            {
                var newFile = dir.CreateFile("file" + i + ".ruleset");
                newFile.WriteAllText(otherSources[i - 1]);
            }

            if (otherSources.Length != 0)
            {
                return RuleSet.LoadEffectiveRuleSetFromFile(file.Path);
            }

            return RuleSetProcessor.LoadFromFile(file.Path);
        }

        private void VerifyRuleSetError(string source, Func<string> messageFormatter, bool locSpecific = true, params string[] otherSources)
        {
            CultureInfo saveUICulture = Thread.CurrentThread.CurrentUICulture;

            if (locSpecific)
            {
                var preferred = EnsureEnglishUICulture.PreferredOrNull;
                if (preferred == null)
                {
                    locSpecific = false;
                }
                else
                {
                    Thread.CurrentThread.CurrentUICulture = preferred;
                }
            }

            try
            {
                ParseRuleSet(source, otherSources);
            }
            catch (Exception e)
            {
                Assert.Equal(messageFormatter(), e.Message);
                return;
            }
            finally
            {
                if (locSpecific)
                {
                    Thread.CurrentThread.CurrentUICulture = saveUICulture;
                }
            }

            Assert.True(false, "Didn't return an error");
        }

        [Fact]
        public void TestCommandLineSplitter()
        {
            VerifyCommandLineSplitter("", new string[0]);
            VerifyCommandLineSplitter("   \t   ", new string[0]);
            VerifyCommandLineSplitter("   abc\tdef baz    quuz   ", new[] { "abc", "def", "baz", "quuz" });
            VerifyCommandLineSplitter(@"  ""abc def""  fi""ddle dee de""e  ""hi there ""dude  he""llo there""  ",
                                        new string[] { @"abc def", @"fi""ddle dee de""e", @"""hi there ""dude", @"he""llo there""" });
            VerifyCommandLineSplitter(@"  ""abc def \"" baz quuz"" ""\""straw berry"" fi\""zz \""buzz fizzbuzz",
                                        new string[] { @"abc def \"" baz quuz", @"\""straw berry", @"fi\""zz", @"\""buzz", @"fizzbuzz" });
            VerifyCommandLineSplitter(@"  \\""abc def""  \\\""abc def"" ",
                                        new string[] { @"\\""abc def""", @"\\\""abc", @"def"" " });
            VerifyCommandLineSplitter(@"  \\\\""abc def""  \\\\\""abc def"" ",
                                        new string[] { @"\\\\""abc def""", @"\\\\\""abc", @"def"" " });
            VerifyCommandLineSplitter(@"  \\\\""abc def""  \\\\\""abc def"" q a r ",
                                        new string[] { @"\\\\""abc def""", @"\\\\\""abc", @"def"" q a r " });
            VerifyCommandLineSplitter(@"abc #Comment ignored",
                                        new string[] { @"abc" }, removeHashComments: true);
            VerifyCommandLineSplitter(@"""foo bar"";""baz"" ""tree""",
                                        new string[] { @"""foo bar"";""baz""", "tree" });
            VerifyCommandLineSplitter(@"/reference:""a, b"" ""test""",
                                        new string[] { @"/reference:""a, b""", "test" });
            VerifyCommandLineSplitter(@"fo""o ba""r",
                                        new string[] { @"fo""o ba""r" });
        }

        [Fact]
        public void TestRuleSetParsingDuplicateRule()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Error"" />
    <Rule Id=""CA1012"" Action=""Warning"" />
    <Rule Id=""CA1013"" Action=""Warning"" />
    <Rule Id=""CA1014"" Action=""None"" />
  </Rules>
</RuleSet>";

            VerifyRuleSetError(source, () => string.Format(CodeAnalysisResources.RuleSetHasDuplicateRules, "CA1012", "Error", "Warn"));
        }

        [Fact]
        public void TestRuleSetParsingDuplicateRule2()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Error"" />
    <Rule Id=""CA1014"" Action=""None"" />
  </Rules>
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Warning"" />
    <Rule Id=""CA1013"" Action=""None"" />
  </Rules>
</RuleSet>";

            VerifyRuleSetError(source, () => string.Format(CodeAnalysisResources.RuleSetHasDuplicateRules, "CA1012", "Error", "Warn"), locSpecific: false);
        }

        [Fact]
        public void TestRuleSetParsingDuplicateRule3()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Error"" />
    <Rule Id=""CA1014"" Action=""None"" />
  </Rules>
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Error"" />
    <Rule Id=""CA1013"" Action=""None"" />
  </Rules>
</RuleSet>";

            var ruleSet = ParseRuleSet(source);
            Assert.Equal(expected: ReportDiagnostic.Error, actual: ruleSet.SpecificDiagnosticOptions["CA1012"]);
        }

        [Fact]
        public void TestRuleSetParsingDuplicateRuleSet()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"">
  <IncludeAll Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Error"" />
  </Rules>
</RuleSet>
<RuleSet Name=""Ruleset2"" Description=""Test"">
  <IncludeAll Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Error"" />
  </Rules>
</RuleSet>
";
            VerifyRuleSetError(source, () => "There are multiple root elements. Line 8, position 2.");
        }

        [Fact]
        public void TestRuleSetParsingIncludeAll1()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Error"" />
  </Rules>
</RuleSet>
";
            var ruleSet = ParseRuleSet(source);
            Assert.Equal(ReportDiagnostic.Warn, ruleSet.GeneralDiagnosticOption);
        }

        [Fact]
        public void TestRuleSetParsingIncludeAll2()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Error"" />
  </Rules>
</RuleSet>
";
            var ruleSet = ParseRuleSet(source);
            Assert.Equal(ReportDiagnostic.Default, ruleSet.GeneralDiagnosticOption);
        }

        [Fact]
        public void TestRuleSetParsingWithIncludeOfSameFile()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <Include Path=""a.ruleset"" Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Error"" />
  </Rules>
</RuleSet>
";
            var ruleSet = ParseRuleSet(source, new string[] { "" });
            Assert.Equal(ReportDiagnostic.Default, ruleSet.GeneralDiagnosticOption);
            Assert.Equal(1, RuleSet.GetEffectiveIncludesFromFile(ruleSet.FilePath).Count());
        }

        [Fact]
        public void TestRuleSetParsingWithMutualIncludes()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <Include Path=""file1.ruleset"" Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Error"" />
  </Rules>
</RuleSet>
";

            string source1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <Include Path=""a.ruleset"" Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Error"" />
  </Rules>
</RuleSet>
";
            var ruleSet = ParseRuleSet(source, source1);
            Assert.Equal(ReportDiagnostic.Default, ruleSet.GeneralDiagnosticOption);
            Assert.Equal(2, RuleSet.GetEffectiveIncludesFromFile(ruleSet.FilePath).Count());
        }

        [Fact]
        public void TestRuleSetParsingWithSiblingIncludes()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <Include Path=""file1.ruleset"" Action=""Warning"" />
  <Include Path=""file2.ruleset"" Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Error"" />
  </Rules>
</RuleSet>
";

            string source1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <Include Path=""file2.ruleset"" Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Error"" />
  </Rules>
</RuleSet>
";

            string source2 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <Include Path=""file1.ruleset"" Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Error"" />
  </Rules>
</RuleSet>
";
            var ruleSet = ParseRuleSet(source, source1, source2);
            Assert.Equal(ReportDiagnostic.Default, ruleSet.GeneralDiagnosticOption);
            Assert.Equal(3, RuleSet.GetEffectiveIncludesFromFile(ruleSet.FilePath).Count());
        }

        [Fact]
        public void TestRuleSetParsingIncludeAll3()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Default"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Error"" />
  </Rules>
</RuleSet>
";
            VerifyRuleSetError(source, () => string.Format(CodeAnalysisResources.RuleSetBadAttributeValue, "Action", "Default"));
        }

        [Fact]
        public void TestRuleSetParsingRulesMissingAttribute1()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Action=""Error"" />
  </Rules>
</RuleSet>
";
            VerifyRuleSetError(source, () => string.Format(CodeAnalysisResources.RuleSetMissingAttribute, "Rule", "Id"));
        }

        [Fact]
        public void TestRuleSetParsingRulesMissingAttribute2()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" />
  </Rules>
</RuleSet>
";
            VerifyRuleSetError(source, () => string.Format(CodeAnalysisResources.RuleSetMissingAttribute, "Rule", "Action"));
        }

        [Fact]
        public void TestRuleSetParsingRulesMissingAttribute3()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Warning"" />
  <Rules RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Error"" />
  </Rules>
</RuleSet>
";
            VerifyRuleSetError(source, () => string.Format(CodeAnalysisResources.RuleSetMissingAttribute, "Rules", "AnalyzerId"));
        }

        [Fact]
        public void TestRuleSetParsingRulesMissingAttribute4()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"">
    <Rule Id=""CA1012"" Action=""Error"" />
  </Rules>
</RuleSet>
";
            VerifyRuleSetError(source, () => string.Format(CodeAnalysisResources.RuleSetMissingAttribute, "Rules", "RuleNamespace"));
        }

        [Fact]
        public void TestRuleSetParsingRulesMissingAttribute5()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" >
  <IncludeAll Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Error"" />
  </Rules>
</RuleSet>
";

            VerifyRuleSetError(source, () => string.Format(CodeAnalysisResources.RuleSetMissingAttribute, "RuleSet", "ToolsVersion"));
        }

        [Fact]
        public void TestRuleSetParsingRulesMissingAttribute6()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Description=""Test"" ToolsVersion=""12.0"" >
  <IncludeAll Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Error"" />
  </Rules>
</RuleSet>
";
            VerifyRuleSetError(source, () => string.Format(CodeAnalysisResources.RuleSetMissingAttribute, "RuleSet", "Name"));
        }

        [Fact]
        public void TestRuleSetParsingRules()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"" >
  <IncludeAll Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Error"" />
    <Rule Id=""CA1013"" Action=""Warning"" />
    <Rule Id=""CA1014"" Action=""None"" />
    <Rule Id=""CA1015"" Action=""Info"" />
    <Rule Id=""CA1016"" Action=""Hidden"" />
  </Rules>
</RuleSet>
";
            var ruleSet = ParseRuleSet(source);
            Assert.Contains("CA1012", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ruleSet.SpecificDiagnosticOptions["CA1012"], ReportDiagnostic.Error);
            Assert.Contains("CA1013", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ruleSet.SpecificDiagnosticOptions["CA1013"], ReportDiagnostic.Warn);
            Assert.Contains("CA1014", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ruleSet.SpecificDiagnosticOptions["CA1014"], ReportDiagnostic.Suppress);
            Assert.Contains("CA1015", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ruleSet.SpecificDiagnosticOptions["CA1015"], ReportDiagnostic.Info);
            Assert.Contains("CA1016", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ruleSet.SpecificDiagnosticOptions["CA1016"], ReportDiagnostic.Hidden);
        }

        [Fact]
        public void TestRuleSetParsingRules2()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"" >
  <IncludeAll Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Default"" />
    <Rule Id=""CA1013"" Action=""Warning"" />
    <Rule Id=""CA1014"" Action=""None"" />
  </Rules>
</RuleSet>
";

            VerifyRuleSetError(source, () => string.Format(CodeAnalysisResources.RuleSetBadAttributeValue, "Action", "Default"));
        }

        [Fact]
        public void TestRuleSetInclude()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"" >
  <Include Path=""foo.ruleset"" Action=""Default"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1013"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            var ruleSet = ParseRuleSet(source);
            Assert.True(ruleSet.Includes.Count() == 1);
            Assert.Equal(ruleSet.Includes.First().Action, ReportDiagnostic.Default);
            Assert.Equal(ruleSet.Includes.First().IncludePath, "foo.ruleset");
        }

        [WorkItem(1184500, "DevDiv 1184500")]
        [Fact]
        public void TestRuleSetInclude1()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"" >
  <Include Path=""foo.ruleset"" Action=""Default"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1013"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            var dir = Temp.CreateDirectory();
            var file = dir.CreateFile("a.ruleset");
            file.WriteAllText(source);

            var ruleSet = RuleSet.LoadEffectiveRuleSetFromFile(file.Path);

            Assert.Equal(ReportDiagnostic.Default, ruleSet.GeneralDiagnosticOption);
            Assert.Contains("CA1013", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ReportDiagnostic.Warn, ruleSet.SpecificDiagnosticOptions["CA1013"]);
        }

        [Fact]
        public void TestRuleSetInclude2()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"" >
  <Include Path=""file1.ruleset"" Action=""Default"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Warning"" />
  </Rules>
</RuleSet>
";

            string source1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset2"" Description=""Test"" ToolsVersion=""12.0"" >
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1013"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            var ruleSet = ParseRuleSet(source, source1);
            Assert.Equal(ReportDiagnostic.Default, ruleSet.GeneralDiagnosticOption);
            Assert.Contains("CA1012", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ReportDiagnostic.Warn, ruleSet.SpecificDiagnosticOptions["CA1012"]);
            Assert.Contains("CA1013", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ReportDiagnostic.Warn, ruleSet.SpecificDiagnosticOptions["CA1013"]);
        }

        [Fact]
        public void TestRuleSetIncludeGlobalStrict()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"" >
  <Include Path=""file1.ruleset"" Action=""Default"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Warning"" />
  </Rules>
</RuleSet>
";

            string source1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset2"" Description=""Test"" ToolsVersion=""12.0"" >
  <IncludeAll Action=""Hidden"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1013"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            var ruleSet = ParseRuleSet(source, source1);
            Assert.Equal(ReportDiagnostic.Hidden, ruleSet.GeneralDiagnosticOption);
            Assert.Contains("CA1012", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ReportDiagnostic.Warn, ruleSet.SpecificDiagnosticOptions["CA1012"]);
            Assert.Contains("CA1013", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ReportDiagnostic.Warn, ruleSet.SpecificDiagnosticOptions["CA1013"]);
        }

        [Fact]
        public void TestRuleSetIncludeGlobalStrict1()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"" >
  <IncludeAll Action=""Info"" />
  <Include Path=""file1.ruleset"" Action=""Default"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Warning"" />
  </Rules>
</RuleSet>
";

            string source1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset2"" Description=""Test"" ToolsVersion=""12.0"" >
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1013"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            var ruleSet = ParseRuleSet(source, source1);
            Assert.Equal(ReportDiagnostic.Info, ruleSet.GeneralDiagnosticOption);
            Assert.Contains("CA1012", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ReportDiagnostic.Warn, ruleSet.SpecificDiagnosticOptions["CA1012"]);
            Assert.Contains("CA1013", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ReportDiagnostic.Warn, ruleSet.SpecificDiagnosticOptions["CA1013"]);
        }

        [Fact]
        public void TestRuleSetIncludeGlobalStrict2()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"" >
  <IncludeAll Action=""Warning"" />
  <Include Path=""file1.ruleset"" Action=""Default"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Warning"" />
  </Rules>
</RuleSet>
";

            string source1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset2"" Description=""Test"" ToolsVersion=""12.0"" >
  <IncludeAll Action=""Error"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1013"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            var ruleSet = ParseRuleSet(source, source1);
            Assert.Equal(ReportDiagnostic.Error, ruleSet.GeneralDiagnosticOption);
            Assert.Contains("CA1012", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ReportDiagnostic.Warn, ruleSet.SpecificDiagnosticOptions["CA1012"]);
            Assert.Contains("CA1013", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ReportDiagnostic.Warn, ruleSet.SpecificDiagnosticOptions["CA1013"]);
        }

        [Fact]
        public void TestRuleSetIncludeGlobalStrict3()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"" >
  <IncludeAll Action=""Warning"" />
  <Include Path=""file1.ruleset"" Action=""Error"" />
  <Include Path=""file2.ruleset"" Action=""Default"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Warning"" />
  </Rules>
</RuleSet>
";

            string source1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset2"" Description=""Test"" ToolsVersion=""12.0"" >
  <IncludeAll Action=""Error"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1013"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            string source2 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset2"" Description=""Test"" ToolsVersion=""12.0"" >
  <IncludeAll Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1013"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            var ruleSet = ParseRuleSet(source, source1, source2);
            Assert.Equal(ReportDiagnostic.Error, ruleSet.GeneralDiagnosticOption);
            Assert.Contains("CA1012", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ReportDiagnostic.Warn, ruleSet.SpecificDiagnosticOptions["CA1012"]);
            Assert.Contains("CA1013", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ReportDiagnostic.Error, ruleSet.SpecificDiagnosticOptions["CA1013"]);
        }

        [Fact]
        public void TestRuleSetIncludeRecursiveIncludes()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"" >
  <IncludeAll Action=""Warning"" />
  <Include Path=""file1.ruleset"" Action=""Default"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Warning"" />
  </Rules>
</RuleSet>
";

            string source1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset2"" Description=""Test"" ToolsVersion=""12.0"" >
  <IncludeAll Action=""Error"" />
  <Include Path=""file2.ruleset"" Action=""Default"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1013"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            string source2 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset2"" Description=""Test"" ToolsVersion=""12.0"" >
  <IncludeAll Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1014"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            var ruleSet = ParseRuleSet(source, source1, source2);
            Assert.Equal(ReportDiagnostic.Error, ruleSet.GeneralDiagnosticOption);
            Assert.Contains("CA1012", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ReportDiagnostic.Warn, ruleSet.SpecificDiagnosticOptions["CA1012"]);
            Assert.Contains("CA1013", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ReportDiagnostic.Warn, ruleSet.SpecificDiagnosticOptions["CA1013"]);
            Assert.Contains("CA1014", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ReportDiagnostic.Warn, ruleSet.SpecificDiagnosticOptions["CA1014"]);
        }

        [Fact]
        public void TestRuleSetIncludeSpecificStrict1()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"" >
  <Include Path=""file1.ruleset"" Action=""Default"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Warning"" />
  </Rules>
</RuleSet>
";

            string source1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset2"" Description=""Test"" ToolsVersion=""12.0"" >
  <IncludeAll Action=""Error"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Error"" />
  </Rules>
</RuleSet>
";
            var ruleSet = ParseRuleSet(source, source1);
            // CA1012's value in source wins.
            Assert.Contains("CA1012", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ReportDiagnostic.Warn, ruleSet.SpecificDiagnosticOptions["CA1012"]);
        }

        [Fact]
        public void TestRuleSetIncludeSpecificStrict2()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"" >
  <Include Path=""file1.ruleset"" Action=""Default"" />
  <Include Path=""file2.ruleset"" Action=""Default"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Warning"" />
  </Rules>
</RuleSet>
";

            string source1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset2"" Description=""Test"" ToolsVersion=""12.0"" >
  <IncludeAll Action=""Error"" />
  <Include Path=""file2.ruleset"" Action=""Default"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Error"" />
  </Rules>
</RuleSet>
";
            string source2 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset2"" Description=""Test"" ToolsVersion=""12.0"" >
  <IncludeAll Action=""Error"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            var ruleSet = ParseRuleSet(source, source1, source2);
            // CA1012's value in source still wins.
            Assert.Contains("CA1012", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ReportDiagnostic.Warn, ruleSet.SpecificDiagnosticOptions["CA1012"]);
        }

        [Fact]
        public void TestRuleSetIncludeSpecificStrict3()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"" >
  <Include Path=""file1.ruleset"" Action=""Default"" />
  <Include Path=""file2.ruleset"" Action=""Default"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Warning"" />
  </Rules>
</RuleSet>
";

            string source1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset2"" Description=""Test"" ToolsVersion=""12.0"" >
  <IncludeAll Action=""Error"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1013"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            string source2 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset2"" Description=""Test"" ToolsVersion=""12.0"" >
  <IncludeAll Action=""Error"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1013"" Action=""Error"" />
  </Rules>
</RuleSet>
";
            var ruleSet = ParseRuleSet(source, source1, source2);
            Assert.Contains("CA1012", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ReportDiagnostic.Warn, ruleSet.SpecificDiagnosticOptions["CA1012"]);
            // CA1013's value in source2 wins.
            Assert.Contains("CA1013", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ReportDiagnostic.Error, ruleSet.SpecificDiagnosticOptions["CA1013"]);
        }

        [Fact]
        public void TestRuleSetIncludeEffectiveAction()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"" >
  <Include Path=""file1.ruleset"" Action=""None"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            string source1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset2"" Description=""Test"" ToolsVersion=""12.0"" >
  <IncludeAll Action=""Error"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1013"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            var ruleSet = ParseRuleSet(source, source1);
            Assert.Contains("CA1012", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ReportDiagnostic.Warn, ruleSet.SpecificDiagnosticOptions["CA1012"]);
            Assert.DoesNotContain("CA1013", ruleSet.SpecificDiagnosticOptions.Keys);
        }

        [Fact]
        public void TestRuleSetIncludeEffectiveAction1()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"" >
  <Include Path=""file1.ruleset"" Action=""Error"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            string source1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset2"" Description=""Test"" ToolsVersion=""12.0"" >
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1013"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            var ruleSet = ParseRuleSet(source, source1);
            Assert.Contains("CA1012", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ReportDiagnostic.Warn, ruleSet.SpecificDiagnosticOptions["CA1012"]);
            Assert.Contains("CA1013", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ReportDiagnostic.Error, ruleSet.SpecificDiagnosticOptions["CA1013"]);
            Assert.Equal(ReportDiagnostic.Default, ruleSet.GeneralDiagnosticOption);
        }

        [Fact]
        public void TestRuleSetIncludeEffectiveActionGlobal1()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"" >
  <Include Path=""file1.ruleset"" Action=""Error"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            string source1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset2"" Description=""Test"" ToolsVersion=""12.0"" >
  <IncludeAll Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1013"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            var ruleSet = ParseRuleSet(source, source1);
            Assert.Equal(ReportDiagnostic.Error, ruleSet.GeneralDiagnosticOption);
        }

        [Fact]
        public void TestRuleSetIncludeEffectiveActionGlobal2()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"" >
  <Include Path=""file1.ruleset"" Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            string source1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset2"" Description=""Test"" ToolsVersion=""12.0"" >
  <IncludeAll Action=""Error"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1013"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            var ruleSet = ParseRuleSet(source, source1);
            Assert.Equal(ReportDiagnostic.Warn, ruleSet.GeneralDiagnosticOption);
        }

        [Fact]
        public void TestRuleSetIncludeEffectiveActionSpecific1()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"" >
  <Include Path=""file1.ruleset"" Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            string source1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset2"" Description=""Test"" ToolsVersion=""12.0"" >
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1013"" Action=""None"" />
  </Rules>
</RuleSet>
";
            var ruleSet = ParseRuleSet(source, source1);
            Assert.Contains("CA1012", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ReportDiagnostic.Warn, ruleSet.SpecificDiagnosticOptions["CA1012"]);
            Assert.Contains("CA1013", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ReportDiagnostic.Suppress, ruleSet.SpecificDiagnosticOptions["CA1013"]);
        }

        [Fact]
        public void TestRuleSetIncludeEffectiveActionSpecific2()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"" >
  <Include Path=""file1.ruleset"" Action=""Error"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            string source1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset2"" Description=""Test"" ToolsVersion=""12.0"" >
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1013"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            var ruleSet = ParseRuleSet(source, source1);
            Assert.Contains("CA1012", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ReportDiagnostic.Warn, ruleSet.SpecificDiagnosticOptions["CA1012"]);
            Assert.Contains("CA1013", ruleSet.SpecificDiagnosticOptions.Keys);
            Assert.Equal(ReportDiagnostic.Error, ruleSet.SpecificDiagnosticOptions["CA1013"]);
        }

        [Fact]
        public void TestAllCombinations()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""New Rule Set1"" Description=""Test"" ToolsVersion=""12.0"">
  <Include Path=""file1.ruleset"" Action=""Error"" />
  <Include Path=""file2.ruleset"" Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1000"" Action=""Warning"" />
    <Rule Id=""CA1001"" Action=""Warning"" />
    <Rule Id=""CA2111"" Action=""None"" />
  </Rules>
</RuleSet>
";

            string source1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""New Rule Set2"" Description=""Test"" ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA2100"" Action=""Warning"" />
    <Rule Id=""CA2111"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            string source2 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""New Rule Set3"" Description=""Test"" ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA2100"" Action=""Warning"" />
    <Rule Id=""CA2111"" Action=""Warning"" />
    <Rule Id=""CA2119"" Action=""None"" />
    <Rule Id=""CA2104"" Action=""Error"" />
    <Rule Id=""CA2105"" Action=""Warning"" />
  </Rules>
</RuleSet>";

            var ruleSet = ParseRuleSet(source, source1, source2);
            Assert.Equal(ReportDiagnostic.Warn, ruleSet.SpecificDiagnosticOptions["CA1000"]);
            Assert.Equal(ReportDiagnostic.Warn, ruleSet.SpecificDiagnosticOptions["CA1001"]);
            Assert.Equal(ReportDiagnostic.Error, ruleSet.SpecificDiagnosticOptions["CA2100"]);
            Assert.Equal(ReportDiagnostic.Warn, ruleSet.SpecificDiagnosticOptions["CA2104"]);
            Assert.Equal(ReportDiagnostic.Warn, ruleSet.SpecificDiagnosticOptions["CA2105"]);
            Assert.Equal(ReportDiagnostic.Suppress, ruleSet.SpecificDiagnosticOptions["CA2111"]);
            Assert.Equal(ReportDiagnostic.Suppress, ruleSet.SpecificDiagnosticOptions["CA2119"]);
        }

        [Fact]
        public void TestRuleSetIncludeError()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"" >
  <Include Path=""file1.ruleset"" Action=""Error"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            string source1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset2"" Description=""Test"" ToolsVersion=""12.0"" >
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1013"" Action=""Default"" />
  </Rules>
</RuleSet>
";
            var dir = Temp.CreateDirectory();
            var file = dir.CreateFile("a.ruleset");
            file.WriteAllText(source);
            var newFile = dir.CreateFile("file1.ruleset");
            newFile.WriteAllText(source1);

            using (new EnsureEnglishUICulture())
            {
                try
                {
                    RuleSet.LoadEffectiveRuleSetFromFile(file.Path);
                    Assert.True(false, "Didn't throw an exception");
                }
                catch (InvalidRuleSetException e)
                {
                    Assert.Contains(string.Format(CodeAnalysisResources.InvalidRuleSetInclude, newFile.Path, string.Format(CodeAnalysisResources.RuleSetBadAttributeValue, "Action", "Default")), e.Message, StringComparison.Ordinal);
                }
            }
        }

        [Fact]
        public void GetEffectiveIncludes_NoIncludes()
        {
            string source = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test"" ToolsVersion=""12.0"" >
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Warning"" />
  </Rules>
</RuleSet>
";

            var dir = Temp.CreateDirectory();
            var file = dir.CreateFile("a.ruleset");
            file.WriteAllText(source);

            var includePaths = RuleSet.GetEffectiveIncludesFromFile(file.Path);

            Assert.Equal(expected: 1, actual: includePaths.Length);
            Assert.Equal(expected: file.Path, actual: includePaths[0]);
        }

        [Fact]
        public void GetEffectiveIncludes_OneLevel()
        {
            string ruleSetSource = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""New Rule Set1"" Description=""Test"" ToolsVersion=""12.0"">
  <Include Path=""file1.ruleset"" Action=""Error"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1000"" Action=""Warning"" />
    <Rule Id=""CA1001"" Action=""Warning"" />
    <Rule Id=""CA2111"" Action=""None"" />
  </Rules>
</RuleSet>
";

            string includeSource = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""New Rule Set2"" Description=""Test"" ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA2100"" Action=""Warning"" />
    <Rule Id=""CA2111"" Action=""Warning"" />
  </Rules>
</RuleSet>
";

            var dir = Temp.CreateDirectory();

            var file = dir.CreateFile("a.ruleset");
            file.WriteAllText(ruleSetSource);

            var include = dir.CreateFile("file1.ruleset");
            include.WriteAllText(includeSource);

            var includePaths = RuleSet.GetEffectiveIncludesFromFile(file.Path);

            Assert.Equal(expected: 2, actual: includePaths.Length);
            Assert.Equal(expected: file.Path, actual: includePaths[0]);
            Assert.Equal(expected: include.Path, actual: includePaths[1]);
        }

        [Fact]
        public void GetEffectiveIncludes_TwoLevels()
        {
            string ruleSetSource = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""New Rule Set1"" Description=""Test"" ToolsVersion=""12.0"">
  <Include Path=""file1.ruleset"" Action=""Error"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1000"" Action=""Warning"" />
    <Rule Id=""CA1001"" Action=""Warning"" />
    <Rule Id=""CA2111"" Action=""None"" />
  </Rules>
</RuleSet>
";

            string includeSource1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""New Rule Set2"" Description=""Test"" ToolsVersion=""12.0"">
  <Include Path=""file2.ruleset"" Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA2100"" Action=""Warning"" />
    <Rule Id=""CA2111"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            string includeSource2 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""New Rule Set3"" Description=""Test"" ToolsVersion=""12.0"">
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA2100"" Action=""Warning"" />
    <Rule Id=""CA2111"" Action=""Warning"" />
    <Rule Id=""CA2119"" Action=""None"" />
    <Rule Id=""CA2104"" Action=""Error"" />
    <Rule Id=""CA2105"" Action=""Warning"" />
  </Rules>
</RuleSet>";

            var dir = Temp.CreateDirectory();

            var file = dir.CreateFile("a.ruleset");
            file.WriteAllText(ruleSetSource);

            var include1 = dir.CreateFile("file1.ruleset");
            include1.WriteAllText(includeSource1);

            var include2 = dir.CreateFile("file2.ruleset");
            include2.WriteAllText(includeSource2);

            var includePaths = RuleSet.GetEffectiveIncludesFromFile(file.Path);

            Assert.Equal(expected: 3, actual: includePaths.Length);
            Assert.Equal(expected: file.Path, actual: includePaths[0]);
            Assert.Equal(expected: include1.Path, actual: includePaths[1]);
            Assert.Equal(expected: include2.Path, actual: includePaths[2]);
        }

        [Fact]
        public void ParseSeperatedStrings_ExcludeSeparatorChar()
        {
            Assert.Equal(
                CommandLineParser.ParseSeparatedStrings(@"a,b", new[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                new[] { "a", "b" });

            Assert.Equal(
                CommandLineParser.ParseSeparatedStrings(@"a,,b", new[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                new[] { "a", "b" });

            Assert.Equal(
                CommandLineParser.ParseSeparatedStrings(@"a,,b", new[] { ',' }, StringSplitOptions.None),
                new[] { "a", "", "b" });
        }

        /// <summary>
        /// This function considers quotes when splitting out the strings.  Ensure they are properly
        /// preserved in the final string.
        /// </summary>
        [Fact]
        public void ParseSeperatedStrings_IncludeQuotes()
        {
            Assert.Equal(
                CommandLineParser.ParseSeparatedStrings(@"""a"",b", new[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                new[] { @"""a""", "b" });

            Assert.Equal(
                CommandLineParser.ParseSeparatedStrings(@"""a,b""", new[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                new[] { @"""a,b""" });

            Assert.Equal(
                CommandLineParser.ParseSeparatedStrings(@"""a"",""b", new[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                new[] { @"""a""", @"""b" });
        }

        /// <summary>
        /// This function should always preserve the slashes as they exist in the original command
        /// line.  The only serve to decide whether quotes should count as grouping constructors
        /// or not. 
        /// </summary>
        [Fact]
        public void SplitCommandLineIntoArguments_Slashes()
        {
            Assert.Equal(
                new[] { @"\\test" },
                CommandLineParser.SplitCommandLineIntoArguments(@"\\test", removeHashComments: false));

            // Even though there are an even number of slashes here that doesn't factor into the 
            // output.  It just means the quote is a grouping construct.
            Assert.Equal(
                new[] { @"\\""test" },
                CommandLineParser.SplitCommandLineIntoArguments(@"\\""test", removeHashComments: false));

            Assert.Equal(
                new[] { @"\\\""test" },
                CommandLineParser.SplitCommandLineIntoArguments(@"\\\""test", removeHashComments: false));

            Assert.Equal(
                new[] { @"\\\test" },
                CommandLineParser.SplitCommandLineIntoArguments(@"\\\test", removeHashComments: false));

            Assert.Equal(
                new[] { @"\\\\\test" },
                CommandLineParser.SplitCommandLineIntoArguments(@"\\\\\test", removeHashComments: false));
        }

        /// <summary>
        /// Quotes are used as grouping constructs unless they are escaped by an odd number of slashes.
        /// </summary>
        [Fact]
        public void SplitCommandLineIntoArguments_Quotes()
        {
            Assert.Equal(
                new[] { @"a", @"b" },
                CommandLineParser.SplitCommandLineIntoArguments(@"a b", removeHashComments: false));

            Assert.Equal(
                new[] { @"a b" },
                CommandLineParser.SplitCommandLineIntoArguments(@"""a b""", removeHashComments: false));

            Assert.Equal(
                new[] { @"a ", @"b""" },
                CommandLineParser.SplitCommandLineIntoArguments(@"""a "" b""", removeHashComments: false));

            // In this case the inner quote is escaped so it doesn't count as a real quote.  Strings which have
            // outer quotes with no real inner quotes have the outer quotes removed. 
            Assert.Equal(
                new[] { @"a \"" b" },
                CommandLineParser.SplitCommandLineIntoArguments(@"""a \"" b""", removeHashComments: false));


            Assert.Equal(
                new[] { @"\a", @"b" },
                CommandLineParser.SplitCommandLineIntoArguments(@"\a b", removeHashComments: false));

            // Escaped quote is not a grouping construct
            Assert.Equal(
                new[] { @"\""a", @"b\""" },
                CommandLineParser.SplitCommandLineIntoArguments(@"\""a b\""", removeHashComments: false));

            // Unescaped quote is a grouping construct. 
            Assert.Equal(
                new[] { @"\\""a b\\""" },
                CommandLineParser.SplitCommandLineIntoArguments(@"\\""a b\\""", removeHashComments: false));

            Assert.Equal(
                new[] { @"""a""m""b""" },
                CommandLineParser.SplitCommandLineIntoArguments(@"""a""m""b""", removeHashComments: false));
        }

        /// <summary>
        /// Test all of the cases around slashes in the RemoveQuotes function.  
        /// </summary>
        /// <remarks>
        /// It's important to remember this is testing slash behavior on the strings as they 
        /// are passed to RemoveQuotes, not as they are passed to the command line.  Command 
        /// line arguments have already gone through an initial round of processing.  So a
        /// string that appears here as "\\test.cs" actually came through the command line
        /// as \"\\test.cs\". 
        /// </remarks>
        [Fact]
        public void RemoveQuotes()
        {
            Assert.Equal(@"\\test.cs", CommandLineParser.RemoveQuotesAndSlashes(@"\\test.cs"));
            Assert.Equal(@"\\test.cs", CommandLineParser.RemoveQuotesAndSlashes(@"""\\test.cs"""));
            Assert.Equal(@"\\\test.cs", CommandLineParser.RemoveQuotesAndSlashes(@"\\\test.cs"));
            Assert.Equal(@"\\\\test.cs", CommandLineParser.RemoveQuotesAndSlashes(@"\\\\test.cs"));
            Assert.Equal(@"\\test\a\b.cs", CommandLineParser.RemoveQuotesAndSlashes(@"\\test\a\b.cs"));
            Assert.Equal(@"\\\\test\\a\\b.cs", CommandLineParser.RemoveQuotesAndSlashes(@"\\\\test\\a\\b.cs"));
            Assert.Equal(@"a""b.cs", CommandLineParser.RemoveQuotesAndSlashes(@"a\""b.cs"));
            Assert.Equal(@"a"" mid ""b.cs", CommandLineParser.RemoveQuotesAndSlashes(@"a\"" mid \""b.cs"));
            Assert.Equal(@"a mid b.cs", CommandLineParser.RemoveQuotesAndSlashes(@"a"" mid ""b.cs"));
            Assert.Equal(@"a.cs", CommandLineParser.RemoveQuotesAndSlashes(@"""a.cs"""));
        }
    }
}
