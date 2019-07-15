// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal class IncrementalAnalyzerProviderMetadata : WorkspaceKindMetadata
    {
        public bool HighPriorityForActiveFile { get; }
        public string Name { get; }

        public IncrementalAnalyzerProviderMetadata(IDictionary<string, object> data) :
            base(data)
        {
            this.HighPriorityForActiveFile = (bool)data.GetValueOrDefault("HighPriorityForActiveFile");
            this.Name = (string)data.GetValueOrDefault("Name");
        }

        public IncrementalAnalyzerProviderMetadata(string name, bool highPriorityForActiveFile, params string[] workspaceKinds) :
            base(workspaceKinds)
        {
            this.HighPriorityForActiveFile = highPriorityForActiveFile;
            this.Name = name;
        }
    }
}
