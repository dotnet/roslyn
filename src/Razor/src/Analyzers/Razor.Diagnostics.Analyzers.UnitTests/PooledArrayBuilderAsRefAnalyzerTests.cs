// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace Razor.Diagnostics.Analyzers.Test;

using VerifyCS = CSharpAnalyzerVerifier<PooledArrayBuilderAsRefAnalyzer>;

public class PooledArrayBuilderAsRefAnalyzerTests
{
    private const string PooledArrayBuilderSource = """
        namespace Microsoft.AspNetCore.Razor.PooledObjects
        {
            internal struct PooledArrayBuilder<T> : System.IDisposable
            {
                public void Dispose() { }
            }

            internal static class PooledArrayBuilderExtensions
            {
                public static ref PooledArrayBuilder<T> AsRef<T>(this in PooledArrayBuilder<T> array) => throw null;
            }
        }
        """;

    [Fact]
    public async Task TestUsingVariable_CSharpAsync()
    {
        var code = $$"""
            using Microsoft.AspNetCore.Razor.PooledObjects;

            class C
            {
                void Method()
                {
                    using (var array = new PooledArrayBuilder<int>())
                    {
                        ref var arrayRef1 = ref array.AsRef();
                        ref var arrayRef2 = ref PooledArrayBuilderExtensions.AsRef(in array);
                    }
                }
            }

            {{PooledArrayBuilderSource}}
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
        }.RunAsync();
    }

    [Fact]
    public async Task TestUsingDeclarationVariable_CSharpAsync()
    {
        var code = $$"""
            using Microsoft.AspNetCore.Razor.PooledObjects;

            class C
            {
                void Method()
                {
                    using var array = new PooledArrayBuilder<int>();
                    ref var arrayRef1 = ref array.AsRef();
                    ref var arrayRef2 = ref PooledArrayBuilderExtensions.AsRef(in array);
                }
            }

            {{PooledArrayBuilderSource}}
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNonUsingVariable_CSharpAsync()
    {
        var code = $$"""
            using Microsoft.AspNetCore.Razor.PooledObjects;

            class C
            {
                void Method()
                {
                    var array = new PooledArrayBuilder<int>();
                    ref var arrayRef1 = ref [|array.AsRef()|];
                    ref var arrayRef2 = ref [|PooledArrayBuilderExtensions.AsRef(in array)|];
                }
            }

            {{PooledArrayBuilderSource}}
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
        }.RunAsync();
    }
}
