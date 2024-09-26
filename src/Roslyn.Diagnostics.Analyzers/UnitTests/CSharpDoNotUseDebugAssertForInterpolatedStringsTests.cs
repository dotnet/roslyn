// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Roslyn.Diagnostics.CSharp.Analyzers.CSharpDoNotUseDebugAssertForInterpolatedStrings,
    Roslyn.Diagnostics.CSharp.Analyzers.CSharpDoNotUseDebugAssertForInterpolatedStringsFixer>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class CSharpDoNotUseInterpolatedStringsForDebugAssertTests
    {
        private const string RoslynDebug =
            """
            namespace Roslyn.Utilities
            {
                public static class RoslynDebug
                {
                    public static void Assert(bool condition, string message) { }
                }
            }
            """;

        [Theory]
        [InlineData("""
            $"{0}"
            """)]
        [InlineData("""
            $@"{0}"
            """)]
        [InlineData("""
            @$"{0}"
            """)]
        [InlineData(""""
            $"""{0}"""
            """")]
        public async Task InterpolatedString(string @string)
        {
            var source = $$"""
                using System.Diagnostics;

                class C
                {
                    void M()
                    {
                        [|Debug.Assert(false, {{@string}})|];
                    }
                }

                {{RoslynDebug}}
                """;

            var @fixed = $$"""
                using System.Diagnostics;
                using Roslyn.Utilities;

                class C
                {
                    void M()
                    {
                        RoslynDebug.Assert(false, {{@string}});
                    }
                }

                {{RoslynDebug}}
                """;

            await new VerifyCS.Test
            {
                TestCode = source,
                FixedCode = @fixed,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12,
            }.RunAsync();
        }

        [Fact]
        public async Task NoCrashOnUsingStaticedAssert()
        {
            var source = $$"""
                using static System.Diagnostics.Debug;

                class C
                {
                    void M()
                    {
                        [|Assert(false, $"{0}")|];
                    }
                }

                {{RoslynDebug}}
                """;

            await new VerifyCS.Test
            {
                TestCode = source,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12,
            }.RunAsync();
        }

        [Theory]
        [InlineData("""
            $"{"0"}"
            """)]
        [InlineData("""
            $@"{"0"}"
            """)]
        [InlineData("""
            @$"{"0"}"
            """)]
        [InlineData(""""
            $"""{"0"}"""
            """")]
        public async Task NoAssertForConstantString(string @string)
        {
            var source = $$"""
                using System.Diagnostics;

                class C
                {
                    void M()
                    {
                        Debug.Assert(false, {{@string}});
                    }
                }

                {{RoslynDebug}}
                """;

            await new VerifyCS.Test
            {
                TestCode = source,
                LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12,
            }.RunAsync();
        }
    }
}
