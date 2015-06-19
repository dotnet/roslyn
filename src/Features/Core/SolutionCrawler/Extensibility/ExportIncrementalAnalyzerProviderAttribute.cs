// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class)]
    internal class ExportIncrementalAnalyzerProviderAttribute : ExportAttribute
    {
        public bool HighPriorityForActiveFile { get; }
        public string[] WorkspaceKinds { get; }

        public ExportIncrementalAnalyzerProviderAttribute(params string[] workspaceKinds)
            : base(typeof(IIncrementalAnalyzerProvider))
        {
            if (workspaceKinds == null)
            {
                throw new ArgumentNullException(nameof(workspaceKinds));
            }

            this.WorkspaceKinds = workspaceKinds;
            this.HighPriorityForActiveFile = false;
        }

        public ExportIncrementalAnalyzerProviderAttribute(bool highPriorityForActiveFile, params string[] workspaceKinds)
            : this(workspaceKinds)
        {
            this.HighPriorityForActiveFile = highPriorityForActiveFile;
        }
    }
}
