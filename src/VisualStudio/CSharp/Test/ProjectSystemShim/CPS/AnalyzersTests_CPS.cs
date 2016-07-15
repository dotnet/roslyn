// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim
{
    public partial class AnalyzersTests
    {
        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void RuleSet_GeneralOption_CPS()
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

                var cpsProject = CSharpHelpers.CreateCSharpCPSProject(environment, "Test");
                var project = (AbstractProject)cpsProject;
                
                var workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
                var options = (CSharpCompilationOptions)workspaceProject.CompilationOptions;

                Assert.Equal(expected: ReportDiagnostic.Default, actual: options.GeneralDiagnosticOption);

                project.SetRuleSetFile(ruleSetFile.Path);
                CSharpHelpers.SetCommandLineArguments(cpsProject, commandLineArguments: $"/ruleset:{ruleSetFile.Path}");

                workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
                options = (CSharpCompilationOptions)workspaceProject.CompilationOptions;

                Assert.Equal(expected: ReportDiagnostic.Error, actual: options.GeneralDiagnosticOption);
            }
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void RuleSet_SpecificOptions_CPS()
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

                var cpsProject = CSharpHelpers.CreateCSharpCPSProject(environment, "Test");
                var project = (AbstractProject)cpsProject;

                project.SetRuleSetFile(ruleSetFile.Path);
                CSharpHelpers.SetCommandLineArguments(cpsProject, commandLineArguments: $"/ruleset:{ruleSetFile.Path}");

                var workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
                var options = (CSharpCompilationOptions)workspaceProject.CompilationOptions;

                var ca1012DiagnosticOption = options.SpecificDiagnosticOptions["CA1012"];
                Assert.Equal(expected: ReportDiagnostic.Error, actual: ca1012DiagnosticOption);
            }
        }
    }
}
