// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class ExportIncrementalAnalyzerProviderAttribute(string name, string[] workspaceKinds, bool highPriorityForActiveFile = false)
        : ExportAttribute(typeof(IIncrementalAnalyzerProvider))
    {
        public bool HighPriorityForActiveFile { get; } = highPriorityForActiveFile;
        public string Name { get; } = name;
        public IReadOnlyList<string> WorkspaceKinds { get; } = workspaceKinds;
    }
}
