﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public sealed class CompilerResolverTests : IDisposable
{
    public TempRoot TempRoot { get; }
    public int DefaultLoadContextCount { get; }
    public AssemblyLoadContext CompilerContext { get; }
    public AssemblyLoadContext ScratchContext { get; }
    public Assembly AssemblyInCompilerContext { get; }
    internal AnalyzerAssemblyLoader Loader { get; }

    public CompilerResolverTests()
    {
        TempRoot = new TempRoot();
        DefaultLoadContextCount = AssemblyLoadContext.Default.Assemblies.Count();
        CompilerContext = new AssemblyLoadContext(nameof(CompilerResolverTests), isCollectible: true);
        AssemblyInCompilerContext = CompilerContext.LoadFromAssemblyPath(typeof(AnalyzerAssemblyLoader).Assembly.Location);
        ScratchContext = new AssemblyLoadContext("Scratch", isCollectible: true);
        Loader = new AnalyzerAssemblyLoader([], [AnalyzerAssemblyLoader.DiskAnalyzerAssemblyResolver], CompilerContext);
    }

    public void Dispose()
    {
        // This test should not pollute the default load context and hence interfere with other tests.
        Assert.Equal(DefaultLoadContextCount, AssemblyLoadContext.Default.Assemblies.Count());
        CompilerContext.Unload();
        ScratchContext.Unload();
        TempRoot.Dispose();
    }

    [Fact]
    public void ResolveReturnsNullForNonHostAssembly()
    {
        var name = new AssemblyName("NotARealAssembly");
        var assembly = Loader.CompilerAnalyzerAssemblyResolver.Resolve(Loader, name, ScratchContext, TempRoot.CreateDirectory().Path);
        Assert.Null(assembly);
    }

    [Fact]
    public void ResolveReturnsForHostAssembly()
    {
        var assembly = Loader.CompilerAnalyzerAssemblyResolver.Resolve(Loader, AssemblyInCompilerContext.GetName(), ScratchContext, TempRoot.CreateDirectory().Path);
        Assert.Same(AssemblyInCompilerContext, assembly);
    }
}
#endif
