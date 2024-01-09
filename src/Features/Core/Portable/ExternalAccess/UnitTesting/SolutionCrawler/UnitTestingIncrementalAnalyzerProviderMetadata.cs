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
        public string Name { get; }

        public UnitTestingIncrementalAnalyzerProviderMetadata(IDictionary<string, object> data)
            : base(data)
        {
            this.Name = (string)data.GetValueOrDefault("Name");
        }

        public UnitTestingIncrementalAnalyzerProviderMetadata(
            string name,
            params string[] workspaceKinds)
            : base(workspaceKinds)
        {
            this.Name = name;
        }

        public override bool Equals(object obj)
        {
            return obj is UnitTestingIncrementalAnalyzerProviderMetadata metadata
                && base.Equals(obj)
                && Name == metadata.Name;
        }

        public override int GetHashCode()
        {
            var hashCode = 1997033996;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            return hashCode;
        }
    }
}
