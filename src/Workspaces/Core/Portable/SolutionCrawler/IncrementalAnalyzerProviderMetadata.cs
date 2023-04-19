// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal class IncrementalAnalyzerProviderMetadata : WorkspaceKindMetadata
    {
        public bool HighPriorityForActiveFile { get; }
        public string Name { get; }

        public IncrementalAnalyzerProviderMetadata(IDictionary<string, object> data)
            : base(data)
        {
            this.HighPriorityForActiveFile = (bool)data.GetValueOrDefault("HighPriorityForActiveFile");
            this.Name = (string)data.GetValueOrDefault("Name");
        }

        public IncrementalAnalyzerProviderMetadata(string name, bool highPriorityForActiveFile, params string[] workspaceKinds)
            : base(workspaceKinds)
        {
            this.HighPriorityForActiveFile = highPriorityForActiveFile;
            this.Name = name;
        }

        public override bool Equals(object obj)
        {
            return obj is IncrementalAnalyzerProviderMetadata metadata
                && base.Equals(obj)
                && HighPriorityForActiveFile == metadata.HighPriorityForActiveFile
                && Name == metadata.Name;
        }

        public override int GetHashCode()
        {
            var hashCode = 1997033996;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + HighPriorityForActiveFile.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            return hashCode;
        }
    }
}
