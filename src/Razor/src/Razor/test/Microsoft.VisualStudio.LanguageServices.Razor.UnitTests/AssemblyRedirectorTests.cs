// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.VisualStudio.Razor;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Test;

public class AssemblyRedirectorTests
{
    [Fact]
    public void AssemblyRedirector_RedirectedAssemblies()
    {
        // This test ensures that the expected set of razor assemblies are redirected in Roslyn.
        // This will fail if you change assembly names or move types around. You will need to
        // update this test to ensure that the new set of assemblies are redirected as needed.
        var expectedAssemblies = new[]
        {
            "Microsoft.CodeAnalysis.Razor.Compiler.dll",
            "Microsoft.AspNetCore.Razor.Utilities.Shared.dll",
            "Microsoft.Extensions.ObjectPool.dll",
            "System.Collections.Immutable.dll"
        };

        var redirector = new RazorAnalyzerAssemblyRedirector();
        foreach (var assembly in expectedAssemblies)
        {
            var actualPath = redirector.RedirectPath(assembly);
            Assert.NotNull(actualPath);
            Assert.EndsWith(assembly, actualPath, StringComparison.OrdinalIgnoreCase);
        }

        // Something not in the list doesn't get redirected
        Assert.Null(redirector.RedirectPath("goo.dll"));
    }

    [Fact]
    public void AssemblyRedirector_RedirectOlderAssembly()
    {
        // test that we correctly redirect the old generator assembly to the new named one
        var redirector = new RazorAnalyzerAssemblyRedirector();
        var actualPath = redirector.RedirectPath("Microsoft.NET.Sdk.Razor.SourceGenerators.dll");
        Assert.NotNull(actualPath);
        Assert.EndsWith("Microsoft.CodeAnalysis.Razor.Compiler.dll", actualPath, StringComparison.OrdinalIgnoreCase);
    }
}
