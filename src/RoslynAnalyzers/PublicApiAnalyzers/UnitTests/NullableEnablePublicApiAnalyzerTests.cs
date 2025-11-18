// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable CA1305

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.PublicApiAnalyzers.UnitTests
{
    public class NullableEnablePublicApiAnalyzerTests
    {
        #region Utilities
        private async Task VerifyCSharpAsync(string source, string shippedApiText, string unshippedApiText, params DiagnosticResult[] expected)
        {
            var test = new CSharpCodeFixTest<DeclarePublicApiAnalyzer, NullableEnablePublicApiFix, DefaultVerifier>
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { },
                },
            };

            if (shippedApiText != null)
            {
                test.TestState.AdditionalFiles.Add((DeclarePublicApiAnalyzer.PublicShippedFileName, shippedApiText));
            }

            if (unshippedApiText != null)
            {
                test.TestState.AdditionalFiles.Add((DeclarePublicApiAnalyzer.PublicUnshippedFileName, unshippedApiText));
            }

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        private Task VerifyCSharpAdditionalFileFixAsync(string source, string oldShippedApiText, string oldUnshippedApiText, string newSource, string newShippedApiText, string newUnshippedApiText)
            => VerifyAdditionalFileFixAsync(source, oldShippedApiText, oldUnshippedApiText, newSource, newShippedApiText, newUnshippedApiText);

        private async Task VerifyAdditionalFileFixAsync(string source, string oldShippedApiText, string oldUnshippedApiText, string newSource, string newShippedApiText, string newUnshippedApiText)
        {
            var test = new CSharpCodeFixTest<DeclarePublicApiAnalyzer, NullableEnablePublicApiFix, DefaultVerifier>();

            test.TestState.Sources.Add(source);
            test.TestState.AdditionalFiles.Add((DeclarePublicApiAnalyzer.PublicShippedFileName, oldShippedApiText));
            test.TestState.AdditionalFiles.Add((DeclarePublicApiAnalyzer.PublicUnshippedFileName, oldUnshippedApiText));

            test.FixedState.Sources.Add(newSource);
            test.FixedState.AdditionalFiles.Add((DeclarePublicApiAnalyzer.PublicShippedFileName, newShippedApiText));
            test.FixedState.AdditionalFiles.Add((DeclarePublicApiAnalyzer.PublicUnshippedFileName, newUnshippedApiText));

            await test.RunAsync();
        }
        #endregion

        #region Fix tests

        [Fact]
        public async Task NullableEnableShippedAPI_NullableMemberAsync()
        {
            var unshippedText = """
                C
                C.C() -> void
                C.Field -> string
                """;
            await VerifyCSharpAdditionalFileFixAsync("""
                #nullable enable
                public class C
                {
                    public string? {|RS0037:Field|};
                }
                """, @"", unshippedText, """
                #nullable enable
                public class C
                {
                    public string? {|RS0036:Field|};
                }
                """, """
                #nullable enable

                """, newUnshippedApiText: unshippedText);
        }

        [Fact]
        public async Task NullableEnableShippedAPI_NonNullableMemberAsync()
        {
            var unshippedText = """
                C
                C.C() -> void
                C.Field2 -> string
                """;
            await VerifyCSharpAdditionalFileFixAsync("""
                #nullable enable
                public class C
                {
                    public string {|RS0037:Field2|};
                }
                """, @"", unshippedText, """
                #nullable enable
                public class C
                {
                    public string {|RS0036:Field2|};
                }
                """, """
                #nullable enable

                """, newUnshippedApiText: unshippedText);
        }

        [Fact]
        public async Task NullableEnableShippedAPI_NonEmptyFileAsync()
        {
            var unshippedText = @"";
            await VerifyCSharpAdditionalFileFixAsync("""
                #nullable enable
                public class C
                {
                    public string? {|RS0037:Field|};
                }
                """, """
                C
                C.C() -> void
                C.Field -> string
                """, unshippedText, """
                #nullable enable
                public class C
                {
                    public string? {|RS0036:Field|};
                }
                """, """
                #nullable enable
                C
                C.C() -> void
                C.Field -> string
                """, newUnshippedApiText: unshippedText);
        }

        [Fact]
        public Task DoNotWarnIfAlreadyEnabled_ViaUnshippedFileAsync()
            => VerifyCSharpAsync("""
                #nullable enable
                public class C
                {
                    public string? {|RS0036:Field|};
                    public string {|RS0036:Field2|};
                }
                """, """
                C
                C.C() -> void
                C.Field -> string
                C.Field2 -> string
                """, @"#nullable enable");

        [Fact]
        public Task DoNotWarnIfAlreadyEnabled_ViaShippedFileAsync()
            => VerifyCSharpAsync("""
                #nullable enable
                public class C
                {
                    public string? {|RS0036:Field|};
                    public string {|RS0036:Field2|};
                }
                """, """
                #nullable enable
                C
                C.C() -> void
                C.Field -> string
                C.Field2 -> string
                """, @"");

        #endregion
    }
}
