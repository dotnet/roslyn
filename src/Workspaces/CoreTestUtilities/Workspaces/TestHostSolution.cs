// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    internal partial class TestHostSolution(params TestHostProject[] projects)
        : TestHostSolution<TestHostDocument>(projects)
    {
        public TestHostSolution(
            HostLanguageServices languageServiceProvider,
            CompilationOptions compilationOptions,
            ParseOptions parseOptions,
            params MetadataReference[] references)
            : this(new TestHostProject(languageServiceProvider, compilationOptions, parseOptions, references))
        {
        }
    }

    internal partial class TestHostSolution<TDocument>
        where TDocument : TestHostDocument
    {
        public readonly SolutionId Id;
        public readonly VersionStamp Version;
        public readonly string FilePath = null;
        public readonly IEnumerable<TestHostProject<TDocument>> Projects;

        public TestHostSolution(params TestHostProject<TDocument>[] projects)
        {
            this.Id = SolutionId.CreateNewId();
            this.Version = VersionStamp.Create();
            this.Projects = projects;

            foreach (var project in projects)
            {
                project.SetSolution();
            }
        }
    }
}
