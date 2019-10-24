// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;
using Microsoft.VisualStudio.LanguageServices.CSharp.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Interop;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim.LegacyProject
{
    [UseExportProvider]
    public class CSharpCompilerOptionsTests
    {
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        [WorkItem(530980, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530980")]
        public void DocumentationModeSetToDiagnoseIfProducingDocFile()
        {
            using var environment = new TestEnvironment();
            var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

            project.SetOption(CompilerOptions.OPTID_XML_DOCFILE, "DocFile.xml");

            var workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
            var options = (CSharpParseOptions)workspaceProject.ParseOptions;

            Assert.Equal(DocumentationMode.Diagnose, options.DocumentationMode);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        [WorkItem(530980, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530980")]
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
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
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

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        [WorkItem(1092636, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1092636")]
        [WorkItem(1040247, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1040247")]
        [WorkItem(1048368, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1048368")]
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

        [WpfFact]
        [WorkItem(33401, "https://github.com/dotnet/roslyn/pull/33401")]
        public void ProjectOutputPathAndOutputExeNameChange()
        {
            using var environment = new TestEnvironment();
            var initialPath = @"C:\test.dll";
            var project = CSharpHelpers.CreateCSharpProject(environment, "Test");
            project.SetOutputFileName(initialPath);
            Assert.Equal(initialPath, project.GetOutputFileName());

            var outputs = (CompilationOutputFilesWithImplicitPdbPath)environment.Workspace.GetCompilationOutputs(project.Test_VisualStudioProject.Id);
            Assert.Equal(initialPath, outputs.AssemblyFilePath);

            // Change output folder from command line arguments - verify that objOutputPath changes.
            var newPath = @"C:\NewFolder\test.dll";
            project.SetOutputFileName(newPath);
            Assert.Equal(newPath, project.GetOutputFileName());

            outputs = (CompilationOutputFilesWithImplicitPdbPath)environment.Workspace.GetCompilationOutputs(project.Test_VisualStudioProject.Id);
            Assert.Equal(newPath, outputs.AssemblyFilePath);

            // Change output file name - verify that outputPath changes.
            newPath = @"C:\NewFolder\test2.dll";
            project.SetOutputFileName(newPath);
            Assert.Equal(newPath, project.GetOutputFileName());

            outputs = (CompilationOutputFilesWithImplicitPdbPath)environment.Workspace.GetCompilationOutputs(project.Test_VisualStudioProject.Id);
            Assert.Equal(newPath, outputs.AssemblyFilePath);

            // Change output file name and folder - verify that outputPath changes.
            newPath = @"C:\NewFolder3\test3.dll";
            project.SetOutputFileName(newPath);
            Assert.Equal(newPath, project.GetOutputFileName());

            outputs = (CompilationOutputFilesWithImplicitPdbPath)environment.Workspace.GetCompilationOutputs(project.Test_VisualStudioProject.Id);
            Assert.Equal(newPath, outputs.AssemblyFilePath);
        }

        [WpfFact]
        public void ProjectCompilationOutputsChange()
        {
            using var environment = new TestEnvironment();
            var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

            var outputs = (CompilationOutputFilesWithImplicitPdbPath)environment.Workspace.GetCompilationOutputs(project.Test_VisualStudioProject.Id);
            Assert.Null(outputs.AssemblyFilePath);

            Assert.Equal(0, ((ICompilerOptionsHostObject)project).SetCompilerOptions(@"/pdb:C:\a\1.pdb /debug+", out _));

            // Compilation doesn't have output file, so we don't expect any build outputs either.
            outputs = (CompilationOutputFilesWithImplicitPdbPath)environment.Workspace.GetCompilationOutputs(project.Test_VisualStudioProject.Id);
            Assert.Null(outputs.AssemblyFilePath);

            Assert.Equal(0, ((ICompilerOptionsHostObject)project).SetCompilerOptions(@"/out:C:\a\2.dll /debug+", out _));

            outputs = (CompilationOutputFilesWithImplicitPdbPath)environment.Workspace.GetCompilationOutputs(project.Test_VisualStudioProject.Id);
            Assert.Equal(@"C:\a\2.dll", outputs.AssemblyFilePath);

            project.SetOutputFileName(@"C:\a\3.dll");

            outputs = (CompilationOutputFilesWithImplicitPdbPath)environment.Workspace.GetCompilationOutputs(project.Test_VisualStudioProject.Id);
            Assert.Equal(@"C:\a\3.dll", outputs.AssemblyFilePath);

            Assert.Equal(0, ((ICompilerOptionsHostObject)project).SetCompilerOptions(@"/pdb:C:\a\4.pdb /debug+", out _));

            outputs = (CompilationOutputFilesWithImplicitPdbPath)environment.Workspace.GetCompilationOutputs(project.Test_VisualStudioProject.Id);
            Assert.Equal(@"C:\a\3.dll", outputs.AssemblyFilePath);
        }

        [WpfTheory]
        [InlineData(LanguageVersion.CSharp7_3)]
        [InlineData(LanguageVersion.CSharp8)]
        [InlineData(LanguageVersion.Latest)]
        [InlineData(LanguageVersion.LatestMajor)]
        [InlineData(LanguageVersion.Preview)]
        [InlineData(null)]
        public void SetProperty_MaxSupportedLangVersion(LanguageVersion? maxSupportedLangVersion)
        {
            const LanguageVersion attemptedVersion = LanguageVersion.CSharp8;

            var catalog = TestEnvironment.s_exportCatalog.Value
                .WithParts(
                    typeof(CSharpParseOptionsChangingService));

            var factory = ExportProviderCache.GetOrCreateExportProviderFactory(catalog);
            using var environment = new TestEnvironment(exportProviderFactory: factory);

            var hierarchy = environment.CreateHierarchy("CSharpProject", "Bin", projectRefPath: null, projectCapabilities: "CSharp");

            if (!(hierarchy is IVsBuildPropertyStorage storage))
            {
                Assert.True(false);
                return;
            }

            if (maxSupportedLangVersion.HasValue)
            {
                Assert.True(ErrorHandler.Succeeded(
                    storage.SetPropertyValue(
                        "MaxSupportedLangVersion", null, (uint)_PersistStorageType.PST_PROJECT_FILE, maxSupportedLangVersion.Value.ToDisplayString())));
            }

            _ = CSharpHelpers.CreateCSharpProject(environment, "Test", hierarchy);

            var project = environment.Workspace.CurrentSolution.Projects.Single();

            var oldParseOptions = (CSharpParseOptions)project.ParseOptions;

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
    }
}
