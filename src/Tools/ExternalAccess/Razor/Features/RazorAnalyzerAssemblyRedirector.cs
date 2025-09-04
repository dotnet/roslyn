// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Workspaces.AnalyzerRedirecting;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor;

[Export(typeof(IAnalyzerAssemblyRedirector)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RazorAnalyzerAssemblyRedirector([Import(AllowDefault = true)] Lazy<RazorAnalyzerAssemblyRedirector.IRazorAnalyzerAssemblyRedirector>? razorRedirector) : IAnalyzerAssemblyRedirector
{
    public string? RedirectPath(string fullPath)
    {
        // Simple heuristic so we don't load razor unnecessarily.
        if (fullPath.IndexOf("razor", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return razorRedirector?.Value.RedirectPath(fullPath);
        }
        return null;
    }

    internal interface IRazorAnalyzerAssemblyRedirector
    {
        string? RedirectPath(string fullPath);
    }
}
