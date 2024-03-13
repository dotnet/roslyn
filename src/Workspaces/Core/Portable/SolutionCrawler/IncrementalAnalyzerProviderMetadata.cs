// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal sealed class IncrementalAnalyzerProviderMetadata(string name, bool highPriorityForActiveFile, IReadOnlyList<string> workspaceKinds)
    {
        public bool HighPriorityForActiveFile { get; } = highPriorityForActiveFile;
        public string Name { get; } = name;
        public IReadOnlyList<string> WorkspaceKinds { get; } = workspaceKinds;

        public IncrementalAnalyzerProviderMetadata(IDictionary<string, object> data)
            : this(name: (string)data[nameof(ExportIncrementalAnalyzerProviderAttribute.Name)],
                   highPriorityForActiveFile: (bool)data[nameof(ExportIncrementalAnalyzerProviderAttribute.HighPriorityForActiveFile)],
                   workspaceKinds: (IReadOnlyList<string>)data[nameof(ExportIncrementalAnalyzerProviderAttribute.WorkspaceKinds)])
        {
        }
    }
}
