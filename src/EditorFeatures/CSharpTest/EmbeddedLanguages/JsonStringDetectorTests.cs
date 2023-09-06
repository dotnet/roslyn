// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EmbeddedLanguages
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpJsonDetectionAnalyzer,
        CSharpJsonDetectionCodeFixProvider>;

    [Trait(Traits.Feature, Traits.Features.CodeActionsDetectJsonString)]
    public class JsonStringDetectorTests
    {
        [Fact]
        public async Task TestStrict()
        {
            await new VerifyCS.Test
            {
                TestCode =
                """
                class C
                {
                    void Goo()
                    {
                        var j = [|"{ \"a\": 0 }"|];
                    }
                }
                """,
                FixedCode =
                """
                class C
                {
                    void Goo()
                    {
                        var j = /*lang=json,strict*/ "{ \"a\": 0 }";
                    }
                }
                """,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNonStrict()
        {
            await new VerifyCS.Test
            {
                TestCode =
                """
                class C
                {
                    void Goo()
                    {
                        var j = [|"{ 'a': 00 }"|];
                    }
                }
                """,
                FixedCode =
                """
                class C
                {
                    void Goo()
                    {
                        var j = /*lang=json*/ "{ 'a': 00 }";
                    }
                }
                """,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNonStrictRawString()
        {
            await new VerifyCS.Test
            {
                TestCode =
                """"
                class C
                {
                    void Goo()
                    {
                        var j = [|"""{ 'a': 00 }"""|];
                    }
                }
                """",
                FixedCode =
                """"
                class C
                {
                    void Goo()
                    {
                        var j = /*lang=json*/ """{ 'a': 00 }""";
                    }
                }
                """",
                LanguageVersion = LanguageVersion.Preview,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotWithExistingComment()
        {
            var code = """
                class C
                {
                    void Goo()
                    {
                        var j = /*lang=json,strict*/ "{ \"a\": 0 }";
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }

        [Fact]
        public async Task TestNotOnUnlikelyJson()
        {
            var code = """
                class C
                {
                    void Goo()
                    {
                        var j = "[1, 2, 3]";
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
            }.RunAsync();
        }
    }
}
