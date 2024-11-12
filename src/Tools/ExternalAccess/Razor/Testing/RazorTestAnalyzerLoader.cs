// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor;

[Export(typeof(RazorTestAnalyzerLoader)), Shared]
internal class RazorTestAnalyzerLoader
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RazorTestAnalyzerLoader()
    {
    }

#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable CA1822 // Mark members as static
    public void InitializeDiagnosticsServices(Workspace workspace)
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore IDE0060 // Remove unused parameter
    {
    }

    public static IAnalyzerAssemblyLoader CreateAnalyzerAssemblyLoader()
    {
        return new DefaultAnalyzerAssemblyLoader();
    }
}
