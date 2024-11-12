// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;
using Microsoft.VisualStudio.LanguageServices.CSharp.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim.LegacyProject
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
    public class CSharpCompilerOptionsTests
    {
        [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530980")]
        public void DocumentationModeSetToDiagnoseIfProducingDocFile()
        {
            using var environment = new TestEnvironment();
            var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

            project.SetOption(CompilerOptions.OPTID_XML_DOCFILE, "DocFile.xml");

            var workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
            var options = (CSharpParseOptions)workspaceProject.ParseOptions;

            Assert.Equal(DocumentationMode.Diagnose, options.DocumentationMode);
        }

        [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530980")]
        public void DocumentationModeSetToParseIfNotProducingDocFile()
        {
            using var environment = new TestEnvironment();
            var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

            project.SetOption(CompilerOptions.OPTID_XML_DOCFILE, "");

            var workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
            var options = (CSharpParseOptions)workspaceProject.ParseOptions;

            Assert.Equal(DocumentationMode.Parse, options.DocumentationMode);
        }

        [WpfFact]
        public void UseOPTID_COMPATIBILITY()
        {
            using var environment = new TestEnvironment();
            var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

            project.SetOption(CompilerOptions.OPTID_COMPATIBILITY, "6");

            var workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
            var options = (CSharpParseOptions)workspaceProject.ParseOptions;

            Assert.Equal(LanguageVersion.CSharp6, options.LanguageVersion);
        }

        ////[WpfFact]
        ////[Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        ////public void UseOPTID_COMPATIBILITY_caseinsensitive()
        ////{
        ////    using (var environment = new TestEnvironment())
        ////    {
        ////        var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

        ////        project.SetOptionWithMarshaledValue(CompilerOptions.OPTID_COMPATIBILITY, "Experimental");

        ////        var workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
        ////        var options = (CSharpParseOptions)workspaceProject.ParseOptions;

        ////        Assert.Equal(LanguageVersion.Experimental, options.LanguageVersion);
        ////    }
        ////}

        [WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1092636")]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1040247")]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1048368")]
        public void ProjectSettingsOptionAddAndRemove()
        {
            using var environment = new TestEnvironment();
            var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

            project.SetOption(CompilerOptions.OPTID_WARNASERRORLIST, "1111");
            var options = environment.GetUpdatedCompilationOptionOfSingleProject();
            Assert.Equal(expected: ReportDiagnostic.Error, actual: options.SpecificDiagnosticOptions["CS1111"]);

            project.SetOption(CompilerOptions.OPTID_WARNASERRORLIST, null);
            options = environment.GetUpdatedCompilationOptionOfSingleProject();
            Assert.False(options.SpecificDiagnosticOptions.ContainsKey("CS1111"));
        }

        [WpfFact, WorkItem("https://github.com/dotnet/roslyn/pull/33401")]
        public void ProjectOutputPathAndOutputExeNameChange()
        {
            using var environment = new TestEnvironment();
            var initialPath = @"C:\test.dll";
            var project = CSharpHelpers.CreateCSharpProject(environment, "Test");
            project.SetOutputFileName(initialPath);
            Assert.Equal(initialPath, project.GetOutputFileName());

            string getCurrentCompilationOutputAssemblyPath()
                => environment.Workspace.CurrentSolution.GetRequiredProject(project.Test_ProjectSystemProject.Id).CompilationOutputInfo.AssemblyPath;

            Assert.Equal(initialPath, getCurrentCompilationOutputAssemblyPath());

            // Change output folder from command line arguments - verify that objOutputPath changes.
            var newPath = @"C:\NewFolder\test.dll";
            project.SetOutputFileName(newPath);
            Assert.Equal(newPath, project.GetOutputFileName());

            Assert.Equal(newPath, getCurrentCompilationOutputAssemblyPath());

            // Change output file name - verify that outputPath changes.
            newPath = @"C:\NewFolder\test2.dll";
            project.SetOutputFileName(newPath);
            Assert.Equal(newPath, project.GetOutputFileName());

            Assert.Equal(newPath, getCurrentCompilationOutputAssemblyPath());

            // Change output file name and folder - verify that outputPath changes.
            newPath = @"C:\NewFolder3\test3.dll";
            project.SetOutputFileName(newPath);
            Assert.Equal(newPath, project.GetOutputFileName());

            Assert.Equal(newPath, getCurrentCompilationOutputAssemblyPath());
        }

        [WpfFact]
        public void ProjectCompilationOutputsChange()
        {
            using var environment = new TestEnvironment();
            var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

            string getCurrentCompilationOutputAssemblyPath()
                => environment.Workspace.CurrentSolution.GetRequiredProject(project.Test_ProjectSystemProject.Id).CompilationOutputInfo.AssemblyPath;

            Assert.Null(getCurrentCompilationOutputAssemblyPath());

            Assert.Equal(0, ((ICompilerOptionsHostObject)project).SetCompilerOptions(@"/pdb:C:\a\1.pdb /debug+", out _));

            // Compilation doesn't have output file, so we don't expect any build outputs either.
            Assert.Null(getCurrentCompilationOutputAssemblyPath());

            Assert.Equal(0, ((ICompilerOptionsHostObject)project).SetCompilerOptions(@"/out:C:\a\2.dll /debug+", out _));

            Assert.Equal(@"C:\a\2.dll", getCurrentCompilationOutputAssemblyPath());

            project.SetOutputFileName(@"C:\a\3.dll");

            Assert.Equal(@"C:\a\3.dll", getCurrentCompilationOutputAssemblyPath());

            Assert.Equal(0, ((ICompilerOptionsHostObject)project).SetCompilerOptions(@"/pdb:C:\a\4.pdb /debug+", out _));

            Assert.Equal(@"C:\a\3.dll", getCurrentCompilationOutputAssemblyPath());
        }

        [WpfTheory]
        [InlineData(LanguageVersion.CSharp7_3)]
        [InlineData(LanguageVersion.CSharp8)]
        [InlineData(LanguageVersion.CSharp9)]
        [InlineData(LanguageVersion.Latest)]
        [InlineData(LanguageVersion.LatestMajor)]
        [InlineData(LanguageVersion.Preview)]
        [InlineData(null)]
        public void SetProperty_MaxSupportedLangVersion(LanguageVersion? maxSupportedLangVersion)
        {
            using var environment = new TestEnvironment(typeof(CSharpParseOptionsChangingService));

            var hierarchy = environment.CreateHierarchy("CSharpProject", "Bin", projectRefPath: null, projectCapabilities: "CSharp");
            var storage = Assert.IsAssignableFrom<IVsBuildPropertyStorage>(hierarchy);

            Assert.True(ErrorHandler.Succeeded(
                storage.SetPropertyValue(
                    "MaxSupportedLangVersion", null, (uint)_PersistStorageType.PST_PROJECT_FILE, maxSupportedLangVersion?.ToDisplayString())));

            _ = CSharpHelpers.CreateCSharpProject(environment, "Test", hierarchy);

            var project = environment.Workspace.CurrentSolution.Projects.Single();

            var oldParseOptions = (CSharpParseOptions)project.ParseOptions;

            const LanguageVersion attemptedVersion = LanguageVersion.CSharp8;

            var canApply = environment.Workspace.CanApplyParseOptionChange(
                oldParseOptions,
                oldParseOptions.WithLanguageVersion(attemptedVersion),
                project);

            if (maxSupportedLangVersion.HasValue)
            {
                Assert.Equal(attemptedVersion <= maxSupportedLangVersion.Value, canApply);
            }
            else
            {
                Assert.True(canApply);
            }
        }

        [WpfFact]
        public void SetProperty_MaxSupportedLangVersion_NotSet()
        {
            using var environment = new TestEnvironment(typeof(CSharpParseOptionsChangingService));

            var hierarchy = environment.CreateHierarchy("CSharpProject", "Bin", projectRefPath: null, projectCapabilities: "CSharp");
            var storage = Assert.IsAssignableFrom<IVsBuildPropertyStorage>(hierarchy);

            _ = CSharpHelpers.CreateCSharpProject(environment, "Test", hierarchy);

            var project = environment.Workspace.CurrentSolution.Projects.Single();

            var oldParseOptions = (CSharpParseOptions)project.ParseOptions;

            const LanguageVersion attemptedVersion = LanguageVersion.CSharp8;

            var canApply = environment.Workspace.CanApplyParseOptionChange(
                oldParseOptions,
                oldParseOptions.WithLanguageVersion(attemptedVersion),
                project);

            Assert.True(canApply);
        }
    }
}
