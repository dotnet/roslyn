// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Microsoft.CodeAnalysis.Workspaces.AnalyzerRedirecting;

internal static class AnalyzerAssemblyRedirectorUtilities
{
    public static string? TryRedirectAnalyzerAssembly(
        string fullPath,
        ImmutableArray<IAnalyzerAssemblyRedirector> analyzerAssemblyRedirectors,
        string? logProjectName)
    {
        string? redirectedPath = null;

        foreach (var redirector in analyzerAssemblyRedirectors)
        {
            try
            {
                if (redirector.RedirectPath(fullPath) is { } currentlyRedirectedPath)
                {
                    if (redirectedPath == null)
                    {
                        redirectedPath = currentlyRedirectedPath;

                        if (logProjectName is not null)
                        {
                            CodeAnalysisEventSource.Log.AnanlyzerReferenceRedirected(
                                redirector.GetType().Name, fullPath, redirectedPath, logProjectName);
                        }
                    }
                    else if (redirectedPath != currentlyRedirectedPath)
                    {
                        throw new InvalidOperationException($"Multiple redirectors disagree on the path to redirect '{fullPath}' to ('{redirectedPath}' vs '{currentlyRedirectedPath}').");
                    }
                }
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.General))
            {
                // Ignore if the external redirector throws.
            }
        }

        return redirectedPath;
    }
}
