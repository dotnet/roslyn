// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler
{
    internal class UnitTestingIncrementalAnalyzerProviderMetadata : WorkspaceKindMetadata
    {
#if false // Not used in unit testing crawling
        public bool HighPriorityForActiveFile { get; }
#endif
        public string Name { get; }

        public UnitTestingIncrementalAnalyzerProviderMetadata(IDictionary<string, object> data)
            : base(data)
        {
#if false // Not used in unit testing crawling
            this.HighPriorityForActiveFile = (bool)data.GetValueOrDefault("HighPriorityForActiveFile");
#endif
            this.Name = (string)data.GetValueOrDefault("Name");
        }

        public UnitTestingIncrementalAnalyzerProviderMetadata(
            string name,
#if false // Not used in unit testing crawling
            bool highPriorityForActiveFile,
#endif
            params string[] workspaceKinds)
            : base(workspaceKinds)
        {
#if false // Not used in unit testing crawling
            this.HighPriorityForActiveFile = highPriorityForActiveFile;
#endif
            this.Name = name;
        }

        public override bool Equals(object obj)
        {
            return obj is UnitTestingIncrementalAnalyzerProviderMetadata metadata
                && base.Equals(obj)
#if false // Not used in unit testing crawling
                && HighPriorityForActiveFile == metadata.HighPriorityForActiveFile
#endif
                && Name == metadata.Name;
        }

        public override int GetHashCode()
        {
            var hashCode = 1997033996;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
#if false // Not used in unit testing crawling
            hashCode = hashCode * -1521134295 + HighPriorityForActiveFile.GetHashCode();
#endif
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            return hashCode;
        }
    }
}
