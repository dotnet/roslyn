// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    [Obsolete]
    internal readonly struct UnitTestingIncrementalAnalyzerProviderMetadataWrapper
    {
        public UnitTestingIncrementalAnalyzerProviderMetadataWrapper(
            string name,
            bool highPriorityForActiveFile,
            params string[] workspaceKinds)
            => UnderlyingObject = new IncrementalAnalyzerProviderMetadata(name, highPriorityForActiveFile, workspaceKinds);

        internal UnitTestingIncrementalAnalyzerProviderMetadataWrapper(IncrementalAnalyzerProviderMetadata underlyingObject)
            => UnderlyingObject = underlyingObject ?? throw new ArgumentNullException(nameof(underlyingObject));

        internal IncrementalAnalyzerProviderMetadata UnderlyingObject { get; }
    }
}
