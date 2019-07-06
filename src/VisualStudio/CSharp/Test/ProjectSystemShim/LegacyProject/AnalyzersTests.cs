// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim.LegacyProject
{
    [UseExportProvider]
    public class AnalyzersTests : TestBase
    {
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void RuleSet_GeneralOption()
        {
            var ruleSetFile = Temp.CreateFile().WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Error"" />
</RuleSet>
");
            using (var environment = new TestEnvironment())
            {
                var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

                var options = (CSharpCompilationOptions)environment.GetUpdatedCompilationOptionOfSingleProject();

                Assert.Equal(expected: ReportDiagnostic.Default, actual: options.GeneralDiagnosticOption);

                ((IAnalyzerHost)project).SetRuleSetFile(ruleSetFile.Path);

                options = (CSharpCompilationOptions)environment.GetUpdatedCompilationOptionOfSingleProject();

                Assert.Equal(expected: ReportDiagnostic.Error, actual: options.GeneralDiagnosticOption);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void RuleSet_CanBeFetchedFromWorkspace()
        {
            var ruleSetFile = Temp.CreateFile();

            using (var environment = new TestEnvironment())
            {
                var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

                ((IAnalyzerHost)project).SetRuleSetFile(ruleSetFile.Path);

                var projectId = environment.Workspace.CurrentSolution.ProjectIds.Single();
                Assert.Equal(ruleSetFile.Path, environment.Workspace.TryGetRuleSetPathForProject(projectId));
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void RuleSet_ProjectSettingOverridesGeneralOption()
        {
            var ruleSetFile = Temp.CreateFile().WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Warning"" />
</RuleSet>
");

            using (var environment = new TestEnvironment())
            {
                var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

                ((IAnalyzerHost)project).SetRuleSetFile(ruleSetFile.Path);

                var workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
                var options = (CSharpCompilationOptions)workspaceProject.CompilationOptions;

                Assert.Equal(expected: ReportDiagnostic.Warn, actual: options.GeneralDiagnosticOption);

                project.SetOption(CompilerOptions.OPTID_WARNINGSAREERRORS, true);

                workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
                options = (CSharpCompilationOptions)workspaceProject.CompilationOptions;

                Assert.Equal(expected: ReportDiagnostic.Error, actual: options.GeneralDiagnosticOption);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void RuleSet_SpecificOptions()
        {
            var ruleSetFile = Temp.CreateFile().WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Error"" />
  </Rules>
</RuleSet>
");

            using (var environment = new TestEnvironment())
            {
                var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

                ((IAnalyzerHost)project).SetRuleSetFile(ruleSetFile.Path);

                var options = (CSharpCompilationOptions)environment.GetUpdatedCompilationOptionOfSingleProject();

                var ca1012DiagnosticOption = options.SpecificDiagnosticOptions["CA1012"];
                Assert.Equal(expected: ReportDiagnostic.Error, actual: ca1012DiagnosticOption);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void RuleSet_ProjectSettingsOverrideSpecificOptions()
        {
            var ruleSetFile = Temp.CreateFile().WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CS1014"" Action=""None"" />
  </Rules>
</RuleSet>
");
            using (var environment = new TestEnvironment())
            {
                var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

                ((IAnalyzerHost)project).SetRuleSetFile(ruleSetFile.Path);
                project.SetOption(CompilerOptions.OPTID_WARNASERRORLIST, "1014");

                var options = (CSharpCompilationOptions)environment.GetUpdatedCompilationOptionOfSingleProject();

                var ca1014DiagnosticOption = options.SpecificDiagnosticOptions["CS1014"];
                Assert.Equal(expected: ReportDiagnostic.Error, actual: ca1014DiagnosticOption);
            }
        }

        [WpfFact, WorkItem(1087250, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087250")]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void SetRuleSetFile_RemoveExtraBackslashes()
        {
            var ruleSetFile = Temp.CreateFile();
            using (var environment = new TestEnvironment())
            {
                var project = CSharpHelpers.CreateCSharpProject(environment, "Test");
                var pathWithExtraBackslashes = ruleSetFile.Path.Replace(@"\", @"\\");

                ((IAnalyzerHost)project).SetRuleSetFile(pathWithExtraBackslashes);

                var projectRuleSetFile = project.VisualStudioProjectOptionsProcessor.ExplicitRuleSetFilePath;

                Assert.Equal(expected: ruleSetFile.Path, actual: projectRuleSetFile);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        [WorkItem(1092636, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1092636")]
        [WorkItem(1040247, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1040247")]
        [WorkItem(1048368, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1048368")]
        [WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")]
        public void RuleSet_ProjectSettingsOverrideSpecificOptionsAndRestore()
        {
            var ruleSetFile = Temp.CreateFile().WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CS1014"" Action=""None"" />
  </Rules>
</RuleSet>
");

            using (var environment = new TestEnvironment())
            {
                var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

                ((IAnalyzerHost)project).SetRuleSetFile(ruleSetFile.Path);

                project.SetOption(CompilerOptions.OPTID_WARNASERRORLIST, "1014");
                var options = environment.GetUpdatedCompilationOptionOfSingleProject();
                Assert.Equal(expected: ReportDiagnostic.Error, actual: options.SpecificDiagnosticOptions["CS1014"]);

                project.SetOption(CompilerOptions.OPTID_WARNNOTASERRORLIST, "1014");
                options = environment.GetUpdatedCompilationOptionOfSingleProject();
                Assert.Equal(expected: ReportDiagnostic.Suppress, actual: options.SpecificDiagnosticOptions["CS1014"]);

                project.SetOption(CompilerOptions.OPTID_WARNNOTASERRORLIST, null);
                options = environment.GetUpdatedCompilationOptionOfSingleProject();
                Assert.Equal(expected: ReportDiagnostic.Error, actual: options.SpecificDiagnosticOptions["CS1014"]);

                project.SetOption(CompilerOptions.OPTID_WARNASERRORLIST, null);
                options = environment.GetUpdatedCompilationOptionOfSingleProject();
                Assert.Equal(expected: ReportDiagnostic.Suppress, actual: options.SpecificDiagnosticOptions["CS1014"]);
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        [WorkItem(468, "https://github.com/dotnet/roslyn/issues/468")]
        public void RuleSet_ProjectNoWarnOverridesOtherSettings()
        {
            var ruleSetFile = Temp.CreateFile().WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CS1014"" Action=""Info"" />
  </Rules>
</RuleSet>
");

            using (var environment = new TestEnvironment())
            {
                var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

                ((IAnalyzerHost)project).SetRuleSetFile(ruleSetFile.Path);
                project.SetOption(CompilerOptions.OPTID_NOWARNLIST, "1014");
                project.SetOption(CompilerOptions.OPTID_WARNASERRORLIST, "1014");

                var workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
                var options = (CSharpCompilationOptions)workspaceProject.CompilationOptions;

                var ca1014DiagnosticOption = options.SpecificDiagnosticOptions["CS1014"];
                Assert.Equal(expected: ReportDiagnostic.Suppress, actual: ca1014DiagnosticOption);
            }
        }

        [WpfTheory]
        [CombinatorialData]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        [WorkItem(33505, "https://github.com/dotnet/roslyn/pull/33505")]
        public void RuleSet_FileChangingOnDiskRefreshes(bool useCpsProject)
        {
            var ruleSetSource =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Error"" />
</RuleSet>
";

            var ruleSetFile = Temp.CreateFile().WriteAllText(ruleSetSource);
            using (var environment = new TestEnvironment())
            {
                if (useCpsProject)
                {
                    CSharpHelpers.CreateCSharpCPSProject(environment, "Test", binOutputPath: null, $"/ruleset:\"{ruleSetFile.Path}\"");
                }
                else
                {
                    // Test legacy project handling
                    var project = CSharpHelpers.CreateCSharpProject(environment, "Test");
                    ((IAnalyzerHost)project).SetRuleSetFile(ruleSetFile.Path);
                }

                var options = (CSharpCompilationOptions)environment.GetUpdatedCompilationOptionOfSingleProject();

                // Assert the value exists now
                Assert.Equal(expected: ReportDiagnostic.Error, actual: options.GeneralDiagnosticOption);

                // Modify the file and raise a mock file change
                File.WriteAllText(ruleSetFile.Path, ruleSetSource.Replace("Error", "Warning"));
                environment.RaiseFileChange(ruleSetFile.Path);

                var listenerProvider = environment.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
                var waiter = listenerProvider.GetWaiter(FeatureAttribute.RuleSetEditor);
                waiter.CreateExpeditedWaitTask().JoinUsingDispatcher(CancellationToken.None);

                options = (CSharpCompilationOptions)environment.GetUpdatedCompilationOptionOfSingleProject();
                Assert.Equal(expected: ReportDiagnostic.Warn, actual: options.GeneralDiagnosticOption);
            }
        }
    }
}
