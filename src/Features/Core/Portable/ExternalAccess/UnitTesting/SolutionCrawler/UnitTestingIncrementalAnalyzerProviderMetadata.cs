// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler;

internal sealed class UnitTestingIncrementalAnalyzerProviderMetadata(string name, IReadOnlyList<string> workspaceKinds)
{
    public string Name { get; } = name;
    public IReadOnlyList<string> WorkspaceKinds { get; } = workspaceKinds;

    public UnitTestingIncrementalAnalyzerProviderMetadata(IDictionary<string, object> data)
        : this(name: (string)data[nameof(Name)],
               workspaceKinds: (IReadOnlyList<string>)data[nameof(WorkspaceKinds)])
    {
    }
}
