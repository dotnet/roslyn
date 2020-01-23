// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class ExportIncrementalAnalyzerProviderAttribute : ExportAttribute
    {
        public bool HighPriorityForActiveFile { get; }
        public string Name { get; }
        public string[] WorkspaceKinds { get; }

        public ExportIncrementalAnalyzerProviderAttribute(string name, string[] workspaceKinds)
            : base(typeof(IIncrementalAnalyzerProvider))
        {
            this.WorkspaceKinds = workspaceKinds;
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.HighPriorityForActiveFile = false;
        }

        public ExportIncrementalAnalyzerProviderAttribute(bool highPriorityForActiveFile, string name, string[] workspaceKinds)
            : this(name, workspaceKinds)
        {
            this.HighPriorityForActiveFile = highPriorityForActiveFile;
        }
    }
}
