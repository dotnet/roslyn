// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET
using System.Runtime.Loader;
using System.Reflection;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public sealed class CompilerAnalyzerAssemblyResolverTests
{
    [Fact]
    public void ExceptionReturnsNull()
    {
        var context = new AssemblyLoadContext(nameof(ExceptionReturnsNull), isCollectible: true);
        var resolver = new AnalyzerAssemblyLoader.CompilerAnalyzerAssemblyResolver(context);
        var name = new AssemblyName("NotARealAssembly");
        Assert.Null(resolver.ResolveAssembly(name));
        context.Unload();
    }
}
#endif
