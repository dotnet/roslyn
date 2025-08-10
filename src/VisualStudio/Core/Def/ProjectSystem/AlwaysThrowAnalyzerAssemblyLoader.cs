// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

internal sealed class AlwaysThrowAnalyzerAssemblyLoader
        : IAnalyzerAssemblyLoader
{
    public static readonly AlwaysThrowAnalyzerAssemblyLoader Instance = new();

    private AlwaysThrowAnalyzerAssemblyLoader()
    {
    }

    public void AddDependencyLocation(string fullPath)
    {
    }

    public Assembly LoadFromPath(string fullPath)
    {
        try
        {
            throw new InvalidOperationException(
                $"Analyzers should not be loaded within a {nameof(VisualStudioWorkspace)}.  They should only be loaded in an external process.");
        }
        catch (Exception e) when (FatalError.ReportAndPropagate(e))
        {
            // Report whatever stack attempted to do this so we can find it and fix it.
            throw ExceptionUtilities.Unreachable();
        }
    }
}
