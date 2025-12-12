// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
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
        => Validate($"static {accessibility} {returnType} Main({parameters})", entryPoints =>
        {
            Assert.Single(entryPoints);
            Assert.Equal("C", entryPoints.Single().Name);
        });

    private static void NegativeTest(string signature)
        => Validate(signature, Assert.Empty);

    private static void Validate(string signature, Action<IEnumerable<INamedTypeSymbol>> validate)
    {
        var compilation = CSharpCompilation.Create("Test", references: [TestBase.MscorlibRef]).AddSyntaxTrees(CSharpSyntaxTree.ParseText($$"""
            using System;
            using System.Threading.Tasks;

            class C
            {
                {{signature}}
                {
                }
            }
            """));

        var entryPoints = CSharpEntryPointFinder.FindEntryPoints(compilation);
        validate(entryPoints);
    }

    [Theory, CombinatorialData]
    public void TestWrongName(
        [CombinatorialValues("public", "private", "")] string accessibility,
        [CombinatorialValues("void", "int", "System.Int32", "Int32", "ValueTask", "Task", "ValueTask<int>", "Task<int>")] string returnType,
        [CombinatorialValues("string[] args", "string[] args1", "")] string parameters)
        => NegativeTest($"static {accessibility} {returnType} main({parameters})");

    [Theory, CombinatorialData]
    public void TestNotStatic(
        [CombinatorialValues("public", "private", "")] string accessibility,
        [CombinatorialValues("void", "int", "System.Int32", "Int32", "ValueTask", "Task", "ValueTask<int>", "Task<int>")] string returnType,
        [CombinatorialValues("string[] args", "string[] args1", "")] string parameters)
        => NegativeTest($"{accessibility} {returnType} Main({parameters})");

    [Theory, CombinatorialData]
    public void TestInvalidReturnType(
        [CombinatorialValues("public", "private", "")] string accessibility,
        [CombinatorialValues("string", "Task<string>", "ValueTask<string>")] string returnType,
        [CombinatorialValues("string[] args", "string[] args1", "")] string parameters)
        => NegativeTest($"static {accessibility} {returnType} Main({parameters})");

    [Theory, CombinatorialData]
    public void TestInvalidParameterTypes(
        [CombinatorialValues("public", "private", "")] string accessibility,
        [CombinatorialValues("void", "int", "System.Int32", "Int32", "ValueTask", "Task", "ValueTask<int>", "Task<int>")] string returnType,
        [CombinatorialValues("string args", "string* args", "int[] args", "string[] args1, string[] args2")] string parameters)
        => NegativeTest($"static {accessibility} {returnType} Main({parameters})");
}
