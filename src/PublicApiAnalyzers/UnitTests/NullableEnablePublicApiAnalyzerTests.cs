// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

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

        private async Task VerifyCSharpAdditionalFileFixAsync(string source, string oldShippedApiText, string oldUnshippedApiText, string newSource, string newShippedApiText, string newUnshippedApiText)
        {
            await VerifyAdditionalFileFixAsync(source, oldShippedApiText, oldUnshippedApiText, newSource, newShippedApiText, newUnshippedApiText);
        }

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
            var source = @"
#nullable enable
public class C
{
    public string? {|RS0037:Field|};
}
";

            var shippedText = @"";
            var unshippedText = @"C
C.C() -> void
C.Field -> string";

            // The source is unchanged, but a new diagnostic appears
            var newSource = @"
#nullable enable
public class C
{
    public string? {|RS0036:Field|};
}
";
            var fixedShippedText = @"#nullable enable
";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, newSource, fixedShippedText, newUnshippedApiText: unshippedText);
        }

        [Fact]
        public async Task NullableEnableShippedAPI_NonNullableMemberAsync()
        {
            var source = @"
#nullable enable
public class C
{
    public string {|RS0037:Field2|};
}
";

            var shippedText = @"";
            var unshippedText = @"C
C.C() -> void
C.Field2 -> string";

            // The source is unchanged, but a new diagnostic appears
            var newSource = @"
#nullable enable
public class C
{
    public string {|RS0036:Field2|};
}
";

            var fixedShippedText = @"#nullable enable
";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, newSource, fixedShippedText, newUnshippedApiText: unshippedText);
        }

        [Fact]
        public async Task NullableEnableShippedAPI_NonEmptyFileAsync()
        {
            var source = @"
#nullable enable
public class C
{
    public string? {|RS0037:Field|};
}
";

            var shippedText = @"C
C.C() -> void
C.Field -> string";
            var unshippedText = @"";

            var newSource = @"
#nullable enable
public class C
{
    public string? {|RS0036:Field|};
}
";

            var fixedShippedText = @"#nullable enable
C
C.C() -> void
C.Field -> string";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, newSource, fixedShippedText, newUnshippedApiText: unshippedText);
        }

        [Fact]
        public async Task DoNotWarnIfAlreadyEnabled_ViaUnshippedFileAsync()
        {
            var source = @"
#nullable enable
public class C
{
    public string? {|RS0036:Field|};
    public string {|RS0036:Field2|};
}
";

            var shippedText = @"C
C.C() -> void
C.Field -> string
C.Field2 -> string";

            var unshippedText = @"#nullable enable";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact]
        public async Task DoNotWarnIfAlreadyEnabled_ViaShippedFileAsync()
        {
            var source = @"
#nullable enable
public class C
{
    public string? {|RS0036:Field|};
    public string {|RS0036:Field2|};
}
";

            var shippedText = @"#nullable enable
C
C.C() -> void
C.Field -> string
C.Field2 -> string";

            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        #endregion
    }
}
