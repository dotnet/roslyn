// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Roslyn.Test.Utilities;
using Xunit;
using DisposableFile = Microsoft.CodeAnalysis.Test.Utilities.DisposableFile;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim.CPS
{
    public class AnalyzersTests
    {
        [WpfFact]
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
            using (var project = CSharpHelpers.CreateCSharpCPSProject(environment, "Test"))
            {
                File.WriteAllText(ruleSetFile.Path, ruleSetSource);

                var workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
                var options = (CSharpCompilationOptions)workspaceProject.CompilationOptions;

                Assert.Equal(expected: ReportDiagnostic.Default, actual: options.GeneralDiagnosticOption);

                project.SetRuleSetFile(ruleSetFile.Path);
                project.SetOptions($"/ruleset:{ruleSetFile.Path}");

                workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
                options = (CSharpCompilationOptions)workspaceProject.CompilationOptions;

                Assert.Equal(expected: ReportDiagnostic.Error, actual: options.GeneralDiagnosticOption);
            }
        }

        [WpfFact]
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
            string ruleSetSource2 = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RuleSet Name=""Ruleset1"" Description=""Test""  ToolsVersion=""12.0"">
  <IncludeAll Action=""Warning"" />
  <Rules AnalyzerId=""Microsoft.Analyzers.ManagedCodeAnalysis"" RuleNamespace=""Microsoft.Rules.Managed"">
    <Rule Id=""CA1012"" Action=""Warning"" />
  </Rules>
</RuleSet>
";
            using (var ruleSetFile = new DisposableFile())
            using (var environment = new TestEnvironment())
            using (var project = CSharpHelpers.CreateCSharpCPSProject(environment, "Test"))
            {
                Assert.Null(project.RuleSetFile);
                
                // Verify SetRuleSetFile updates the ruleset.
                File.WriteAllText(ruleSetFile.Path, ruleSetSource);
                project.SetRuleSetFile(ruleSetFile.Path);
                Assert.Equal(ruleSetFile.Path, project.RuleSetFile.FilePath);

                // We need to explicitly update the command line arguments so the new ruleset is used to update options.
                project.SetOptions($"/ruleset:{ruleSetFile.Path}");
                var ca1012DiagnosticOption = project.CurrentCompilationOptions.SpecificDiagnosticOptions["CA1012"];
                Assert.Equal(expected: ReportDiagnostic.Error, actual: ca1012DiagnosticOption);

                // Verify edits to the ruleset file updates options.
                var lastOptions = project.CurrentCompilationOptions;
                File.WriteAllText(ruleSetFile.Path, ruleSetSource2);
                project.OnRuleSetFileUpdateOnDisk(this, EventArgs.Empty);
                ca1012DiagnosticOption = project.CurrentCompilationOptions.SpecificDiagnosticOptions["CA1012"];
                Assert.Equal(expected: ReportDiagnostic.Warn, actual: ca1012DiagnosticOption);
            }
        }
    }
}
