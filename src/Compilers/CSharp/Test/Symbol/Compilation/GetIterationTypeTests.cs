// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

public class GetIterationTypeTests : CSharpTestBase
{
    private const string Source = """
        #nullable enable
        using System;
        using System.Collections;
        using System.Collections.Generic;

        class C
        {
            void M(
                int[] Array,
                List<string> List,
                List<string?> NullableList,
                string String,
                Span<int> Span,
                IEnumerable NonGeneric,
                dynamic Dynamic,
                Pattern Pattern,
                int NotIterable,
                IAsyncEnumerable<int> Async,
                WithExtension WithExtension)
            {
                /*outside*/
            }
        }

        class Pattern { public PatternEnumerator GetEnumerator() => null!; }
        class PatternEnumerator { public long Current => 0; public bool MoveNext() => false; }
        class WithExtension { }

        namespace N
        {
            static class Extensions
            {
                public static IEnumerator<char> GetEnumerator(this WithExtension w) => null!;
            }

            class D
            {
                void M()
                {
                    /*inside*/
                }
            }
        }
        """;

    private static (SemanticModel model, INamedTypeSymbol c) Compile()
    {
        var comp = CreateCompilation(Source, targetFramework: TargetFramework.NetCoreApp);
        comp.VerifyDiagnostics();
        return (comp.GetSemanticModel(comp.SyntaxTrees.Single()), ((Compilation)comp).GetTypeByMetadataName("C")!);
    }

    private static ITypeSymbol ParameterType(INamedTypeSymbol c, string name)
        => ((IMethodSymbol)c.GetMembers("M").Single()).Parameters.Single(p => p.Name == name).Type;

    [Theory]
    [InlineData("Array", "System.Int32")]
    [InlineData("List", "System.String!")]
    [InlineData("NullableList", "System.String?")]
    [InlineData("String", "System.Char")]
    [InlineData("Span", "System.Int32")]
    [InlineData("NonGeneric", "System.Object")]
    [InlineData("Dynamic", "dynamic")]
    [InlineData("Pattern", "System.Int64")]
    [InlineData("NotIterable", null)]
    [InlineData("Async", null)]
    [InlineData("WithExtension", null)]
    public void GetIterationType(string member, string? expected)
    {
        var (model, c) = Compile();
        var iterationType = model.GetIterationType(Source.IndexOf("/*outside*/"), ParameterType(c, member));

        Assert.Equal(expected, iterationType?.ToTestDisplayString(includeNonNullable: true));
    }

    [Fact]
    public void GetIterationType_ExtensionGetEnumeratorInScope()
    {
        var (model, c) = Compile();
        var iterationType = model.GetIterationType(Source.IndexOf("/*inside*/"), ParameterType(c, "WithExtension"));

        Assert.Equal("System.Char", iterationType?.ToTestDisplayString());
    }

    [Theory]
    [InlineData("Async", "System.Int32")]
    [InlineData("List", null)]
    [InlineData("NotIterable", null)]
    public void GetAsyncIterationType(string member, string? expected)
    {
        var (model, c) = Compile();
        var iterationType = model.GetAsyncIterationType(Source.IndexOf("/*outside*/"), ParameterType(c, member));

        Assert.Equal(expected, iterationType?.ToTestDisplayString());
    }

    [Fact]
    public void GetIterationType_NullType()
    {
        var (model, _) = Compile();

        Assert.Throws<System.ArgumentNullException>(() => model.GetIterationType(0, null!));
        Assert.Throws<System.ArgumentNullException>(() => model.GetAsyncIterationType(0, null!));
    }
}
