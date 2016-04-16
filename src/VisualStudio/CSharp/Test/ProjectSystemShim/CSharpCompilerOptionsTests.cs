// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim
{
    public class CSharpCompilerOptionsTests
    {
        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        [WorkItem(530980, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530980")]
        public void DocumentationModeSetToDiagnoseIfProducingDocFile()
        {
            using (var environment = new TestEnvironment())
            {
                var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

                project.SetOptionWithMarshaledValue(CompilerOptions.OPTID_XML_DOCFILE, "DocFile.xml");

                var workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
                var options = (CSharpParseOptions)workspaceProject.ParseOptions;

                Assert.Equal(DocumentationMode.Diagnose, options.DocumentationMode);
            }
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        [WorkItem(530980, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530980")]
        public void DocumentationModeSetToParseIfNotProducingDocFile()
        {
            using (var environment = new TestEnvironment())
            {
                var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

                project.SetOptionWithMarshaledValue(CompilerOptions.OPTID_XML_DOCFILE, "");

                var workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
                var options = (CSharpParseOptions)workspaceProject.ParseOptions;

                Assert.Equal(DocumentationMode.Parse, options.DocumentationMode);
            }
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void UseOPTID_COMPATIBILITY()
        {
            using (var environment = new TestEnvironment())
            {
                var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

                project.SetOptionWithMarshaledValue(CompilerOptions.OPTID_COMPATIBILITY, "6");

                var workspaceProject = environment.Workspace.CurrentSolution.Projects.Single();
                var options = (CSharpParseOptions)workspaceProject.ParseOptions;

                Assert.Equal(LanguageVersion.CSharp6, options.LanguageVersion);
            }
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

        [Fact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        [WorkItem(1092636, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1092636")]
        [WorkItem(1040247, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1040247")]
        [WorkItem(1048368, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1048368")]
        public void ProjectSettingsOptionAddAndRemove()
        {
            using (var environment = new TestEnvironment())
            {
                var project = CSharpHelpers.CreateCSharpProject(environment, "Test");

                project.SetOptionWithMarshaledValue(CompilerOptions.OPTID_WARNASERRORLIST, "1111");
                var options = environment.GetUpdatedCompilationOptionOfSingleProject();
                Assert.Equal(expected: ReportDiagnostic.Error, actual: options.SpecificDiagnosticOptions["CS1111"]);

                project.SetOptionWithMarshaledValue(CompilerOptions.OPTID_WARNASERRORLIST, null);
                options = environment.GetUpdatedCompilationOptionOfSingleProject();
                Assert.False(options.SpecificDiagnosticOptions.ContainsKey("CS1111"));
            }
        }
    }
}
