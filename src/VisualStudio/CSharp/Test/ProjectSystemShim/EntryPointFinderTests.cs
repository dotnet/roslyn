// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim;

[WorkItem("https://github.com/dotnet/roslyn/issues/35376")]
public sealed class EntryPointFinderTests
{
    [Theory, CombinatorialData]
    public void PositiveTests(
        [CombinatorialValues("public", "private", "")] string accessibility,
        [CombinatorialValues("void", "int", "System.Int32", "Int32", "ValueTask", "Task", "ValueTask<int>", "Task<int>")] string returnType,
        [CombinatorialValues("string[] args", "string[] args1", "")] string parameters)
    {
        var compilation = CSharpCompilation.Create("Test", references: [TestBase.MscorlibRef]).AddSyntaxTrees(CSharpSyntaxTree.ParseText($$"""
            using System;
            using System.Threading.Tasks;

            class C
            {
                static {{accessibility}} {{returnType}} Main({{parameters}})
                {
                }
            }
            """));

        var entryPoints = CSharpEntryPointFinder.FindEntryPoints(compilation);
        Assert.Single(entryPoints);
        Assert.Equal("C", entryPoints.Single().Name);
    }

    [Theory, CombinatorialData]
    public void TestWrongName(
        [CombinatorialValues("public", "private", "")] string accessibility,
        [CombinatorialValues("void", "int", "System.Int32", "Int32", "ValueTask", "Task", "ValueTask<int>", "Task<int>")] string returnType,
        [CombinatorialValues("string[] args", "string[] args1", "")] string parameters)
    {
        var compilation = CSharpCompilation.Create("Test").AddSyntaxTrees(CSharpSyntaxTree.ParseText($$"""
            using System;
            using System.Threading.Tasks;

            class C
            {
                static {{accessibility}} {{returnType}} main({{parameters}})
                {
                }
            }
            """));

        var entryPoints = CSharpEntryPointFinder.FindEntryPoints(compilation);
        Assert.Empty(entryPoints);
    }

    [Theory, CombinatorialData]
    public void TestNotStatic(
        [CombinatorialValues("public", "private", "")] string accessibility,
        [CombinatorialValues("void", "int", "System.Int32", "Int32", "ValueTask", "Task", "ValueTask<int>", "Task<int>")] string returnType,
        [CombinatorialValues("string[] args", "string[] args1", "")] string parameters)
    {
        var compilation = CSharpCompilation.Create("Test").AddSyntaxTrees(CSharpSyntaxTree.ParseText($$"""
            using System;
            using System.Threading.Tasks;

            class C
            {
                {{accessibility}} {{returnType}} main({{parameters}})
                {
                }
            }
            """));

        var entryPoints = CSharpEntryPointFinder.FindEntryPoints(compilation);
        Assert.Empty(entryPoints);
    }

    [Theory, CombinatorialData]
    public void TestInvalidReturnType(
        [CombinatorialValues("public", "private", "")] string accessibility,
        [CombinatorialValues("string", "Task<string>", "ValueTask<string>")] string returnType,
        [CombinatorialValues("string[] args", "string[] args1", "")] string parameters)
    {
        var compilation = CSharpCompilation.Create("Test").AddSyntaxTrees(CSharpSyntaxTree.ParseText($$"""
            using System;
            using System.Threading.Tasks;

            class C
            {
                static {{accessibility}} {{returnType}} Main({{parameters}})
                {
                }
            }
            """));

        var entryPoints = CSharpEntryPointFinder.FindEntryPoints(compilation);
        Assert.Empty(entryPoints);
    }

    [Theory, CombinatorialData]
    public void TestInvalidArgumentType(
        [CombinatorialValues("public", "private", "")] string accessibility,
        [CombinatorialValues("void", "int", "System.Int32", "Int32", "ValueTask", "Task", "ValueTask<int>", "Task<int>")] string returnType,
        [CombinatorialValues("string args", "string* args", "int[] args")] string parameters)
    {
        var compilation = CSharpCompilation.Create("Test").AddSyntaxTrees(CSharpSyntaxTree.ParseText($$"""
            using System;
            using System.Threading.Tasks;

            class C
            {
                static {{accessibility}} {{returnType}} Main({{parameters}})
                {
                }
            }
            """));

        var entryPoints = CSharpEntryPointFinder.FindEntryPoints(compilation);
        Assert.Empty(entryPoints);
    }
}
