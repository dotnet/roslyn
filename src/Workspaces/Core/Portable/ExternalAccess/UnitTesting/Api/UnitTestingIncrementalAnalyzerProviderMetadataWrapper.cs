// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
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
