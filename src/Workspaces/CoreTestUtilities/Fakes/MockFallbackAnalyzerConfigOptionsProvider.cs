// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Test.Utilities;

[ExportWorkspaceService(typeof(IFallbackAnalyzerConfigOptionsProvider), ServiceLayer.Test), Shared, PartNotDiscoverable]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class MockFallbackAnalyzerConfigOptionsProvider() : IFallbackAnalyzerConfigOptionsProvider
{
    public StructuredAnalyzerConfigOptions GetOptions(string language)
        => StructuredAnalyzerConfigOptions.Empty;
}
