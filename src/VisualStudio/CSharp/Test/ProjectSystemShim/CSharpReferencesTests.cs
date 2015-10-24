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
    using static CSharpHelpers;

    public class CSharpReferenceTests
    {
        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void AddingReferenceToProjectMetadataPromotesToProjectReference()
        {
            using (var environment = new TestEnvironment())
            {
                var project1 = CreateCSharpProject(environment, "project1");
                environment.ProjectTracker.UpdateProjectBinPath(project1, null, @"c:\project1.dll");

                var project2 = CreateCSharpProject(environment, "project2");
                environment.ProjectTracker.UpdateProjectBinPath(project2, null, @"c:\project2.dll");

                // since this is known to be the output path of project1, the metadata reference is converted to a project reference
                project2.OnImportAdded(@"c:\project1.dll", "project1");

                Assert.Equal(true, project2.GetCurrentProjectReferences().Any(pr => pr.ProjectId == project1.Id));

                project2.Disconnect();
                project1.Disconnect();
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void AddCyclicProjectMetadataReferences()
        {
            using (var environment = new TestEnvironment())
            {
                var project1 = CreateCSharpProject(environment, "project1");
                environment.ProjectTracker.UpdateProjectBinPath(project1, null, @"c:\project1.dll");

                var project2 = CreateCSharpProject(environment, "project2");
                environment.ProjectTracker.UpdateProjectBinPath(project2, null, @"c:\project2.dll");

                project1.AddProjectReference(project2);

                // normally this metadata reference would be elevated to a project reference, but fails because of cyclicness
                project2.OnImportAdded(@"c:\project1.dll", "project1");

                Assert.Equal(true, project1.GetCurrentProjectReferences().Any(pr => pr.ProjectId == project2.Id));
                Assert.Equal(false, project2.GetCurrentProjectReferences().Any(pr => pr.ProjectId == project1.Id));

                project2.Disconnect();
                project1.Disconnect();
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void AddCyclicProjectReferences()
        {
            using (var environment = new TestEnvironment())
            {
                var project1 = CreateCSharpProject(environment, "project1");
                var project2 = CreateCSharpProject(environment, "project2");

                project1.AddProjectReference(project2);
                project2.AddProjectReference(project1);

                Assert.Equal(true, project1.GetCurrentProjectReferences().Any(pr => pr.ProjectId == project2.Id));
                Assert.Equal(false, project2.GetCurrentProjectReferences().Any(pr => pr.ProjectId == project1.Id));

                project2.Disconnect();
                project1.Disconnect();
            }
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public void AddCyclicProjectReferencesDeep()
        {
            using (var environment = new TestEnvironment())
            {
                var project1 = CreateCSharpProject(environment, "project1");
                var project2 = CreateCSharpProject(environment, "project2");
                var project3 = CreateCSharpProject(environment, "project3");
                var project4 = CreateCSharpProject(environment, "project4");

                project1.AddProjectReference(project2);
                project2.AddProjectReference(project3);
                project3.AddProjectReference(project4);
                project4.AddProjectReference(project1);

                Assert.Equal(true, project1.GetCurrentProjectReferences().Any(pr => pr.ProjectId == project2.Id));
                Assert.Equal(true, project2.GetCurrentProjectReferences().Any(pr => pr.ProjectId == project3.Id));
                Assert.Equal(true, project3.GetCurrentProjectReferences().Any(pr => pr.ProjectId == project4.Id));
                Assert.Equal(false, project4.GetCurrentProjectReferences().Any(pr => pr.ProjectId == project1.Id));

                project4.Disconnect();
                project3.Disconnect();
                project2.Disconnect();
                project1.Disconnect();
            }
        }
    }
}