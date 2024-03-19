// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
    public class AnalyzersTests : TestBase
    {
        [WpfFact]
        public void RuleSet_GeneralOption()
        {
            var ruleSetFile = Temp.CreateFile().WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Error"" />
</RuleSet>
");
            using var environment = new TestEnvironment();
            var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

            var options = (CSharpCompilationOptions)environment.GetUpdatedCompilationOptionOfSingleProject();

            Assert.Equal(expected: ReportDiagnostic.Default, actual: options.GeneralDiagnosticOption);

            ((IAnalyzerHost)project).SetRuleSetFile(ruleSetFile.Path);

            options = (CSharpCompilationOptions)environment.GetUpdatedCompilationOptionOfSingleProject();

            Assert.Equal(expected: ReportDiagnostic.Error, actual: options.GeneralDiagnosticOption);
        }

        [WpfFact]
        public void RuleSet_CanBeFetchedFromWorkspace()
        {
            var ruleSetFile = Temp.CreateFile();

            using var environment = new TestEnvironment();
            var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

            ((IAnalyzerHost)project).SetRuleSetFile(ruleSetFile.Path);

            var projectId = environment.Workspace.CurrentSolution.ProjectIds.Single();
            Assert.Equal(ruleSetFile.Path, environment.Workspace.TryGetRuleSetPathForProject(projectId));
        }

        [WpfFact]
        public void RuleSet_ProjectSettingOverridesGeneralOption()
        {
            var ruleSetFile = Temp.CreateFile().WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Warning"" />
</RuleSet>
");

            using var environment = new TestEnvironment();
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

        [WpfFact]
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

            using var environment = new TestEnvironment();
            var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

            ((IAnalyzerHost)project).SetRuleSetFile(ruleSetFile.Path);

            var options = (CSharpCompilationOptions)environment.GetUpdatedCompilationOptionOfSingleProject();

            var ca1012DiagnosticOption = options.SpecificDiagnosticOptions["CA1012"];
            Assert.Equal(expected: ReportDiagnostic.Error, actual: ca1012DiagnosticOption);
        }

        [WpfFact]
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
            using var environment = new TestEnvironment();
            var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

            ((IAnalyzerHost)project).SetRuleSetFile(ruleSetFile.Path);
            project.SetOption(CompilerOptions.OPTID_WARNASERRORLIST, "1014");

            var options = (CSharpCompilationOptions)environment.GetUpdatedCompilationOptionOfSingleProject();

            var ca1014DiagnosticOption = options.SpecificDiagnosticOptions["CS1014"];
            Assert.Equal(expected: ReportDiagnostic.Error, actual: ca1014DiagnosticOption);
        }

        [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087250")]
        public void SetRuleSetFile_RemoveExtraBackslashes()
        {
            var ruleSetFile = Temp.CreateFile();
            using var environment = new TestEnvironment();
            var project = CSharpHelpers.CreateCSharpProject(environment, "Test");
            var pathWithExtraBackslashes = ruleSetFile.Path.Replace(@"\", @"\\");

            ((IAnalyzerHost)project).SetRuleSetFile(pathWithExtraBackslashes);

            var projectRuleSetFile = project.ProjectSystemProjectOptionsProcessor.ExplicitRuleSetFilePath;

            Assert.Equal(expected: ruleSetFile.Path, actual: projectRuleSetFile);
        }

        [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1092636")]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1040247")]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1048368")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/468")]
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

            using var environment = new TestEnvironment();
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

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/468")]
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

            using var environment = new TestEnvironment();
            var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

            ((IAnalyzerHost)project).SetRuleSetFile(ruleSetFile.Path);
            project.SetOption(CompilerOptions.OPTID_NOWARNLIST, "1014");
            project.SetOption(CompilerOptions.OPTID_WARNASERRORLIST, "1014");

            var workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
            var options = (CSharpCompilationOptions)workspaceProject.CompilationOptions;

            var ca1014DiagnosticOption = options.SpecificDiagnosticOptions["CS1014"];
            Assert.Equal(expected: ReportDiagnostic.Suppress, actual: ca1014DiagnosticOption);
        }

        [WpfTheory]
        [CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/pull/33505")]
        public async Task RuleSet_FileChangingOnDiskRefreshes(bool useCpsProject)
        {
            var ruleSetSource =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Error"" />
</RuleSet>
";

            var ruleSetFile = Temp.CreateFile().WriteAllText(ruleSetSource);
            using var environment = new TestEnvironment();
            if (useCpsProject)
            {
                await CSharpHelpers.CreateCSharpCPSProjectAsync(environment, "Test", binOutputPath: @"C:\test.dll", $"/ruleset:\"{ruleSetFile.Path}\"");
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
            await environment.RaiseFileChangeAsync(ruleSetFile.Path);

            var listenerProvider = environment.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
            var waiter = listenerProvider.GetWaiter(FeatureAttribute.RuleSetEditor);
            waiter.ExpeditedWaitAsync().JoinUsingDispatcher(CancellationToken.None);

            options = (CSharpCompilationOptions)environment.GetUpdatedCompilationOptionOfSingleProject();
            Assert.Equal(expected: ReportDiagnostic.Warn, actual: options.GeneralDiagnosticOption);
        }
    }
}
