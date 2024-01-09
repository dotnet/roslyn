// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Test.Utilities;

public abstract class TestHostSolution<TDocument>
    where TDocument : TestHostDocument
{
    public readonly SolutionId Id;
    public readonly VersionStamp Version;
    public readonly string FilePath = null;
    public readonly IEnumerable<TestHostProject<TDocument>> Projects;

    protected TestHostSolution(params TestHostProject<TDocument>[] projects)
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
