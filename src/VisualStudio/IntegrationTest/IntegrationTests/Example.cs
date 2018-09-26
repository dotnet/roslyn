// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests
{
    [Collection(nameof(SharedIntegrationHostFixture))]
    public class Example : AbstractIntegrationTest
    {
        public Example(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
        }

        [WpfFact]
        public void Test1()
        {
            VisualStudio.SolutionExplorer.CreateSolution(SolutionName);
            VisualStudio.SolutionExplorer.AddProject(
                new ProjectUtils.Project(ProjectName),
                WellKnownProjectTemplates.CSharpNetCoreClassLibrary,
                LanguageNames.CSharp);
        }

        [WpfFact]
        public void Test2()
        {
            VisualStudio.SolutionExplorer.CreateSolution(SolutionName);
            VisualStudio.SolutionExplorer.AddProject(
                new ProjectUtils.Project(ProjectName),
                WellKnownProjectTemplates.CSharpNetCoreClassLibrary,
                LanguageNames.CSharp);
        }
    }
}
