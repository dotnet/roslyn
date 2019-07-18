// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim.CPS
{
    [UseExportProvider]
    public class AnalyzersTests : TestBase
    {
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void RuleSet_GeneralOption_CPS()
        {
            var ruleSetFile = Temp.CreateFile().WriteAllText(
@"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Error"" />
</RuleSet>
");
            using var environment = new TestEnvironment();
            using var project = CSharpHelpers.CreateCSharpCPSProject(environment, "Test");
            var workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
            var options = (CSharpCompilationOptions)workspaceProject.CompilationOptions;

            Assert.Equal(expected: ReportDiagnostic.Default, actual: options.GeneralDiagnosticOption);

            project.SetOptions($"/ruleset:{ruleSetFile.Path}");

            workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
            options = (CSharpCompilationOptions)workspaceProject.CompilationOptions;

            Assert.Equal(expected: ReportDiagnostic.Error, actual: options.GeneralDiagnosticOption);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void RuleSet_SpecificOptions_CPS()
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
            using var project = CSharpHelpers.CreateCSharpCPSProject(environment, "Test");
            // Verify SetRuleSetFile updates the ruleset.
            project.SetOptions($"/ruleset:{ruleSetFile.Path}");

            // We need to explicitly update the command line arguments so the new ruleset is used to update options.
            project.SetOptions($"/ruleset:{ruleSetFile.Path}");
            var ca1012DiagnosticOption = environment.Workspace.CurrentSolution.Projects.Single().CompilationOptions.SpecificDiagnosticOptions["CA1012"];
            Assert.Equal(expected: ReportDiagnostic.Error, actual: ca1012DiagnosticOption);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void RuleSet_PathCanBeFound()
        {
            var ruleSetFile = Temp.CreateFile();
            using var environment = new TestEnvironment();
            ProjectId projectId;

            using (var project = CSharpHelpers.CreateCSharpCPSProject(environment, "Test"))
            {
                project.SetOptions($"/ruleset:{ruleSetFile.Path}");

                projectId = project.Id;

                Assert.Equal(ruleSetFile.Path, environment.Workspace.TryGetRuleSetPathForProject(projectId));
            }

            // Ensure it's still not available after we disposed the project
            Assert.Null(environment.Workspace.TryGetRuleSetPathForProject(projectId));
        }
    }
}
