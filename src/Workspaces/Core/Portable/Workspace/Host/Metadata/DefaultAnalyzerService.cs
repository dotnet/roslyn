// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host;

[ExportWorkspaceService(typeof(IAnalyzerService)), Shared]
internal sealed class DefaultAnalyzerService : IAnalyzerService
{
    private readonly AnalyzerAssemblyLoader _loader = new();

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DefaultAnalyzerService()
    {
    }

    public IAnalyzerAssemblyLoader GetLoader()
    {
        return _loader;
    }
}
