// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    internal partial class TestHostSolution
    {
        public readonly SolutionId Id;
        public readonly VersionStamp Version;
        public readonly string FilePath = null;
        public readonly IEnumerable<TestHostProject> Projects;

        public TestHostSolution(
            HostLanguageServices languageServiceProvider,
            CompilationOptions compilationOptions,
            ParseOptions parseOptions,
            params MetadataReference[] references)
            : this(new TestHostProject(languageServiceProvider, compilationOptions, parseOptions, references))
        {
        }

        public TestHostSolution(params TestHostProject[] projects)
        {
            this.Id = SolutionId.CreateNewId();
            this.Version = VersionStamp.Create();
            this.Projects = projects;

            foreach (var project in projects)
            {
                project.SetSolution(this);
            }
        }
    }
}
