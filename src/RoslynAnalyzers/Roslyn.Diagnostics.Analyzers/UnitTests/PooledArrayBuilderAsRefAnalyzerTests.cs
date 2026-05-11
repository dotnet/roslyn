// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using VerifyCS = Test.Utilities.CSharpSecurityCodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.PooledArrayBuilderAsRefAnalyzer,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
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
                LanguageVersion = LanguageVersion.CSharp9,
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
                LanguageVersion = LanguageVersion.CSharp9,
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
                LanguageVersion = LanguageVersion.CSharp9,
                TestCode = code,
            }.RunAsync();
        }
    }
}
