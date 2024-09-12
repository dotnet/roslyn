// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class ExportIncrementalAnalyzerProviderAttribute(string name, string[] workspaceKinds) : ExportAttribute(typeof(IIncrementalAnalyzerProvider))
    {
        public bool HighPriorityForActiveFile { get; } = false;
        public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
        public string[] WorkspaceKinds { get; } = workspaceKinds;

        public ExportIncrementalAnalyzerProviderAttribute(bool highPriorityForActiveFile, string name, string[] workspaceKinds)
            : this(name, workspaceKinds)
        {
            this.HighPriorityForActiveFile = highPriorityForActiveFile;
        }
    }
}
