// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
