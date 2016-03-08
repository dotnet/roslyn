// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim
{
    public class AnalyzersTests
    {
        private sealed class DisposableFile : IDisposable
        {
            private readonly string _filePath;

            public DisposableFile()
            {
                _filePath = System.IO.Path.GetTempFileName();
            }

            public void Dispose()
            {
                File.Delete(_filePath);
            }

            public string Path
            {
                get { return _filePath; }
            }
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void RuleSet_GeneralOption()
        {
            string ruleSetSource = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Error"" />
</RuleSet>
";
            using (var ruleSetFile = new DisposableFile())
            using (var environment = new TestEnvironment())
            {
                File.WriteAllText(ruleSetFile.Path, ruleSetSource);

                var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

                var workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
                var options = (CSharpCompilationOptions)workspaceProject.CompilationOptions;

                Assert.Equal(expected: ReportDiagnostic.Default, actual: options.GeneralDiagnosticOption);

                project.SetRuleSetFile(ruleSetFile.Path);

                workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
                options = (CSharpCompilationOptions)workspaceProject.CompilationOptions;

                Assert.Equal(expected: ReportDiagnostic.Error, actual: options.GeneralDiagnosticOption);
            }
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void RuleSet_ProjectSettingOverridesGeneralOption()
        {
            string ruleSetSource = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Warning"" />
</RuleSet>
";

            using (var ruleSetFile = new DisposableFile())
            using (var environment = new TestEnvironment())
            {
                File.WriteAllText(ruleSetFile.Path, ruleSetSource);

                var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

                project.SetRuleSetFile(ruleSetFile.Path);

                var workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
                var options = (CSharpCompilationOptions)workspaceProject.CompilationOptions;

                Assert.Equal(expected: ReportDiagnostic.Warn, actual: options.GeneralDiagnosticOption);

                project.SetOptionWithMarshaledValue(CompilerOptions.OPTID_WARNINGSAREERRORS, true);

                workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
                options = (CSharpCompilationOptions)workspaceProject.CompilationOptions;

                Assert.Equal(expected: ReportDiagnostic.Error, actual: options.GeneralDiagnosticOption);
            }
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void RuleSet_SpecificOptions()
        {
            string ruleSetSource = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Error"" />
  </Rules>
</RuleSet>
";

            using (var ruleSetFile = new DisposableFile())
            using (var environment = new TestEnvironment())
            {
                File.WriteAllText(ruleSetFile.Path, ruleSetSource);

                var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

                project.SetRuleSetFile(ruleSetFile.Path);

                var workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
                var options = (CSharpCompilationOptions)workspaceProject.CompilationOptions;

                var ca1012DiagnosticOption = options.SpecificDiagnosticOptions["CA1012"];
                Assert.Equal(expected: ReportDiagnostic.Error, actual: ca1012DiagnosticOption);
            }
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void RuleSet_ProjectSettingsOverrideSpecificOptions()
        {
            string ruleSetSource = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CS1014"" Action=""None"" />
  </Rules>
</RuleSet>
";

            using (var ruleSetFile = new DisposableFile())
            using (var environment = new TestEnvironment())
            {
                File.WriteAllText(ruleSetFile.Path, ruleSetSource);

                var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

                project.SetRuleSetFile(ruleSetFile.Path);
                project.SetOptionWithMarshaledValue(CompilerOptions.OPTID_WARNASERRORLIST, "1014");

                var workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
                var options = (CSharpCompilationOptions)workspaceProject.CompilationOptions;

                var ca1014DiagnosticOption = options.SpecificDiagnosticOptions["CS1014"];
                Assert.Equal(expected: ReportDiagnostic.Error, actual: ca1014DiagnosticOption);
            }
        }

        [Fact, WorkItem(1087250, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087250")]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void SetRuleSetFile_RemoveExtraBackslashes()
        {
            using (var ruleSetFile = new DisposableFile())
            using (var environment = new TestEnvironment())
            {
                var project = CSharpHelpers.CreateCSharpProject(environment, "Test");
                var pathWithExtraBackslashes = ruleSetFile.Path.Replace(@"\", @"\\");

                project.SetRuleSetFile(pathWithExtraBackslashes);

                var projectRuleSetFile = project.RuleSetFile;

                Assert.Equal(expected: ruleSetFile.Path, actual: projectRuleSetFile.FilePath);
            }
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        [WorkItem(1092636, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1092636")]
        [WorkItem(1040247, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1040247")]
        [WorkItem(1048368, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1048368")]
        [WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")]
        public void RuleSet_ProjectSettingsOverrideSpecificOptionsAndRestore()
        {
            string ruleSetSource = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CS1014"" Action=""None"" />
  </Rules>
</RuleSet>
";

            using (var ruleSetFile = new DisposableFile())
            using (var environment = new TestEnvironment())
            {
                File.WriteAllText(ruleSetFile.Path, ruleSetSource);

                var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

                project.SetRuleSetFile(ruleSetFile.Path);

                project.SetOptionWithMarshaledValue(CompilerOptions.OPTID_WARNASERRORLIST, "1014");
                var options = environment.GetUpdatedCompilationOptionOfSingleProject();
                Assert.Equal(expected: ReportDiagnostic.Error, actual: options.SpecificDiagnosticOptions["CS1014"]);

                project.SetOptionWithMarshaledValue(CompilerOptions.OPTID_WARNNOTASERRORLIST, "1014");
                options = environment.GetUpdatedCompilationOptionOfSingleProject();
                Assert.Equal(expected: ReportDiagnostic.Suppress, actual: options.SpecificDiagnosticOptions["CS1014"]);

                project.SetOptionWithMarshaledValue(CompilerOptions.OPTID_WARNNOTASERRORLIST, null);
                options = environment.GetUpdatedCompilationOptionOfSingleProject();
                Assert.Equal(expected: ReportDiagnostic.Error, actual: options.SpecificDiagnosticOptions["CS1014"]);

                project.SetOptionWithMarshaledValue(CompilerOptions.OPTID_WARNASERRORLIST, null);
                options = environment.GetUpdatedCompilationOptionOfSingleProject();
                Assert.Equal(expected: ReportDiagnostic.Suppress, actual: options.SpecificDiagnosticOptions["CS1014"]);
            }
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        [WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")]
        public void RuleSet_ProjectNoWarnOverridesOtherSettings()
        {
            string ruleSetSource = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CS1014"" Action=""Info"" />
  </Rules>
</RuleSet>
";

            using (var ruleSetFile = new DisposableFile())
            using (var environment = new TestEnvironment())
            {
                File.WriteAllText(ruleSetFile.Path, ruleSetSource);

                var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

                project.SetRuleSetFile(ruleSetFile.Path);
                project.SetOptionWithMarshaledValue(CompilerOptions.OPTID_NOWARNLIST, "1014");
                project.SetOptionWithMarshaledValue(CompilerOptions.OPTID_WARNASERRORLIST, "1014");

                var workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
                var options = (CSharpCompilationOptions)workspaceProject.CompilationOptions;

                var ca1014DiagnosticOption = options.SpecificDiagnosticOptions["CS1014"];
                Assert.Equal(expected: ReportDiagnostic.Suppress, actual: ca1014DiagnosticOption);
            }
        }
    }
}
