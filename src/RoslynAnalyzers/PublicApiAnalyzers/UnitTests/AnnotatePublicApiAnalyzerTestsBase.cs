// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable CA1305

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.PublicApiAnalyzers.UnitTests
{
    public abstract class AnnotatePublicApiAnalyzerTestsBase
    {
        protected abstract bool IsInternalTest { get; }
        protected abstract string EnabledModifier { get; }
        protected abstract string ShippedFileName { get; }
        protected abstract string UnshippedFileName { get; }
        protected abstract string UnshippedFileNamePrefix { get; }
        protected abstract string AnnotateApiId { get; }
        protected abstract string ShouldAnnotateApiFilesId { get; }
        protected abstract string ObliviousApiId { get; }

        protected abstract IEnumerable<string> DisabledDiagnostics { get; }

        #region Utilities
        private async Task VerifyCSharpAsync(string source, string shippedApiText, string unshippedApiText, params DiagnosticResult[] expected)
        {
            var test = new CSharpCodeFixTest<DeclarePublicApiAnalyzer, AnnotatePublicApiFix, DefaultVerifier>
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { },
                },
            };

            if (shippedApiText != null)
            {
                test.TestState.AdditionalFiles.Add((ShippedFileName, shippedApiText));
            }

            if (unshippedApiText != null)
            {
                test.TestState.AdditionalFiles.Add((UnshippedFileName, unshippedApiText));
            }

            test.ExpectedDiagnostics.AddRange(expected);
            test.DisabledDiagnostics.AddRange(DisabledDiagnostics);
            await test.RunAsync();
        }

        private Task VerifyCSharpAdditionalFileFixAsync(string source, string oldShippedApiText, string oldUnshippedApiText, string newShippedApiText, string newUnshippedApiText)
            => VerifyAdditionalFileFixAsync(source, oldShippedApiText, oldUnshippedApiText, newShippedApiText, newUnshippedApiText);

        private async Task VerifyAdditionalFileFixAsync(string source, string oldShippedApiText, string oldUnshippedApiText, string newShippedApiText, string newUnshippedApiText)
        {
            var test = new CSharpCodeFixTest<DeclarePublicApiAnalyzer, AnnotatePublicApiFix, DefaultVerifier>();

            test.TestState.Sources.Add(source);
            test.TestState.AdditionalFiles.Add((ShippedFileName, oldShippedApiText));
            test.TestState.AdditionalFiles.Add((UnshippedFileName, oldUnshippedApiText));

            test.FixedState.AdditionalFiles.Add((ShippedFileName, newShippedApiText));
            test.FixedState.AdditionalFiles.Add((UnshippedFileName, newUnshippedApiText));

            await test.RunAsync();
        }
        #endregion

        #region Fix tests

        [Fact, WorkItem(4040, "https://github.com/dotnet/roslyn-analyzers/issues/4040")]
        public Task NoObliviousWhenUnannotatedClassConstraintAsync()
            => VerifyCSharpAsync($$"""
                #nullable enable
                {{EnabledModifier}} class C<T> where T : class
                {
                }
                """, @"", """
                #nullable enable
                C<T>.C() -> void
                C<T>
                """);

        [Fact, WorkItem(4040, "https://github.com/dotnet/roslyn-analyzers/issues/4040")]
        public Task NoObliviousWhenAnnotatedClassConstraintAsync()
            => VerifyCSharpAsync($$"""
                #nullable enable
                {{EnabledModifier}} class C<T> where T : class?
                {
                }
                """, @"", """
                #nullable enable
                C<T>.C() -> void
                C<T>
                """);

        [Fact]
        public async Task NoObliviousWhenAnnotatedClassConstraintMultipleFiles()
        {
            var source = $$"""
                #nullable enable
                {{EnabledModifier}} class C<T> where T : class?
                {
                }
                """;

            var shippedText = @"#nullable enable";
            var unshippedText1 = """
                #nullable enable
                C<T>.C() -> void
                """;
            var unshippedText2 = """
                #nullable enable
                C<T>
                """;

            var test = new CSharpCodeFixTest<DeclarePublicApiAnalyzer, AnnotatePublicApiFix, DefaultVerifier>
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles =
                    {
                        (ShippedFileName, shippedText),
                        (UnshippedFileName, unshippedText1),
                        (UnshippedFileNamePrefix + "test" + DeclarePublicApiAnalyzer.Extension, unshippedText2),
                    },
                },
            };

            await test.RunAsync();
        }

        [Fact, WorkItem(4040, "https://github.com/dotnet/roslyn-analyzers/issues/4040")]
        public Task ObliviousWhenObliviousClassConstraintAsync()
            => VerifyCSharpAsync($$"""
                #nullable enable
                {{EnabledModifier}} class {|{{ObliviousApiId}}:C|}<T> // oblivious
                #nullable disable
                    where T : class
                {
                }
                """, @"", """
                #nullable enable
                C<T>.C() -> void
                ~C<T>
                """);

        [Fact, WorkItem(4040, "https://github.com/dotnet/roslyn-analyzers/issues/4040")]
        public Task NoObliviousWhenUnannotatedReferenceTypeConstraintAsync()
            => VerifyCSharpAsync($$"""
                #nullable enable
                {{EnabledModifier}} class D { }
                {{EnabledModifier}} class C<T> where T : D
                {
                }
                """, @"", """
                #nullable enable
                C<T>.C() -> void
                C<T>
                D
                D.D() -> void
                """);

        [Fact, WorkItem(4040, "https://github.com/dotnet/roslyn-analyzers/issues/4040")]
        public Task NoObliviousWhenAnnotatedReferenceTypeConstraintAsync()
            => VerifyCSharpAsync($$"""
                #nullable enable
                {{EnabledModifier}} class D { }
                {{EnabledModifier}} class C<T> where T : D?
                {
                }
                """, @"", """
                #nullable enable
                C<T>.C() -> void
                C<T>
                D
                D.D() -> void
                """);

        [Fact, WorkItem(4040, "https://github.com/dotnet/roslyn-analyzers/issues/4040")]
        public Task ObliviousWhenObliviousReferenceTypeConstraintAsync()
            => VerifyCSharpAsync($$"""
                #nullable enable
                {{EnabledModifier}} class D { }

                {{EnabledModifier}} class {|{{ObliviousApiId}}:C|}<T> // oblivious
                #nullable disable
                    where T : D
                {
                }
                """, @"", """
                #nullable enable
                C<T>.C() -> void
                ~C<T>
                D
                D.D() -> void
                """);

        [Fact]
        public Task DoNotAnnotateMemberInUnannotatedUnshippedAPI_NullableAsync()
            => VerifyCSharpAsync($$"""
                #nullable enable
                {{EnabledModifier}} class C
                {
                    {{EnabledModifier}} string? {|{{ShouldAnnotateApiFilesId}}:Field|};
                }
                """, @"", """
                C
                C.C() -> void
                C.Field -> string
                """);

        [Fact]
        public Task DoNotAnnotateMemberInUnannotatedUnshippedAPI_NonNullableAsync()
            => VerifyCSharpAsync($$"""
                #nullable enable
                {{EnabledModifier}} class C
                {
                    {{EnabledModifier}} string {|{{ShouldAnnotateApiFilesId}}:Field2|};
                }
                """, @"", """
                C
                C.C() -> void
                C.Field2 -> string
                """);

        [Fact]
        public Task DoNotAnnotateMemberInUnannotatedShippedAPIAsync()
            => VerifyCSharpAsync($$"""
                #nullable enable
                {{EnabledModifier}} class C
                {
                    {{EnabledModifier}} string? {|{{ShouldAnnotateApiFilesId}}:Field|};
                    {{EnabledModifier}} string {|{{ShouldAnnotateApiFilesId}}:Field2|};
                }
                """, """
                C
                C.C() -> void
                C.Field -> string
                C.Field2 -> string
                """, @"");

        [Fact]
        public async Task AnnotatedMemberInAnnotatedShippedAPIAsync()
        {
            var unshippedText = @"";
            await VerifyCSharpAdditionalFileFixAsync($$"""
                #nullable enable
                {{EnabledModifier}} class C
                {
                    {{EnabledModifier}} string? OldField;
                    {{EnabledModifier}} string? {|{{AnnotateApiId}}:Field|};
                    {{EnabledModifier}} string {|{{AnnotateApiId}}:Field2|};
                }
                """, """
                #nullable enable
                C
                C.C() -> void
                C.OldField -> string?
                C.Field -> string
                C.Field2 -> string
                """, unshippedText, """
                #nullable enable
                C
                C.C() -> void
                C.OldField -> string?
                C.Field -> string?
                C.Field2 -> string!
                """, newUnshippedApiText: unshippedText);
        }

        [Fact]
        public async Task AnnotatedMemberInAnnotatedUnshippedAPI_EnabledViaUnshippedAsync()
        {
            var shippedText = @"";
            await VerifyCSharpAdditionalFileFixAsync($$"""
                #nullable enable
                {{EnabledModifier}} class C
                {
                    {{EnabledModifier}} string? OldField;
                    {{EnabledModifier}} string? {|{{AnnotateApiId}}:Field|};
                    {{EnabledModifier}} string {|{{AnnotateApiId}}:Field2|};
                }
                """, shippedText, """
                #nullable enable
                C
                C.C() -> void
                C.OldField -> string?
                C.Field -> string
                C.Field2 -> string
                """, shippedText, """
                #nullable enable
                C
                C.C() -> void
                C.OldField -> string?
                C.Field -> string?
                C.Field2 -> string!
                """);
        }

        [Fact]
        public async Task AnnotatedMemberInAnnotatedUnshippedAPI_EnabledViaMultipleUnshippedAsync()
        {
            var shippedText = @"";
            var test = new CSharpCodeFixTest<DeclarePublicApiAnalyzer, AnnotatePublicApiFix, DefaultVerifier>();

            test.TestState.Sources.Add($$"""
                #nullable enable
                {{EnabledModifier}} class C
                {
                    {{EnabledModifier}} string? OldField;
                    {{EnabledModifier}} string? {|{{AnnotateApiId}}:Field|};
                    {{EnabledModifier}} string {|{{AnnotateApiId}}:Field2|};
                }
                """);

            test.TestState.AdditionalFiles.Add((ShippedFileName, shippedText));
            test.TestState.AdditionalFiles.Add((UnshippedFileName, string.Empty));
            test.TestState.AdditionalFiles.Add((UnshippedFileNamePrefix + "test" + DeclarePublicApiAnalyzer.Extension, """
                #nullable enable
                C
                C.C() -> void
                C.OldField -> string?
                C.Field -> string
                C.Field2 -> string
                """));

            test.FixedState.AdditionalFiles.Add((ShippedFileName, shippedText));
            test.FixedState.AdditionalFiles.Add((UnshippedFileName, string.Empty));
            test.FixedState.AdditionalFiles.Add((UnshippedFileNamePrefix + "test" + DeclarePublicApiAnalyzer.Extension, """
                #nullable enable
                C
                C.C() -> void
                C.OldField -> string?
                C.Field -> string?
                C.Field2 -> string!
                """));

            await test.RunAsync();
        }

        [Fact]
        public async Task AnnotatedMemberInAnnotatedUnshippedAPI_EnabledViaShippedAsync()
        {
            var shippedText = @"#nullable enable";
            await VerifyCSharpAdditionalFileFixAsync($$"""
                #nullable enable
                {{EnabledModifier}} class C
                {
                    {{EnabledModifier}} string? OldField;
                    {{EnabledModifier}} string? {|{{AnnotateApiId}}:Field|};
                    {{EnabledModifier}} string {|{{AnnotateApiId}}:Field2|};
                }
                """, shippedText, """
                C
                C.C() -> void
                C.OldField -> string?
                C.Field -> string
                C.Field2 -> string
                """, newShippedApiText: shippedText, """
                C
                C.C() -> void
                C.OldField -> string?
                C.Field -> string?
                C.Field2 -> string!
                """);
        }

        [Fact]
        public async Task AnnotatedMemberInAnnotatedUnshippedAPI_EnabledViaBothAsync()
        {
            var shippedText = @"#nullable enable";
            await VerifyCSharpAdditionalFileFixAsync($$"""
                #nullable enable
                {{EnabledModifier}} class C
                {
                    {{EnabledModifier}} string? OldField;
                    {{EnabledModifier}} string? {|{{AnnotateApiId}}:Field|};
                    {{EnabledModifier}} string {|{{AnnotateApiId}}:Field2|};
                }
                """, shippedText, """
                #nullable enable
                C
                C.C() -> void
                C.OldField -> string?
                C.Field -> string
                C.Field2 -> string
                """, newShippedApiText: shippedText, """
                #nullable enable
                C
                C.C() -> void
                C.OldField -> string?
                C.Field -> string?
                C.Field2 -> string!
                """);
        }

        [Fact]
        public async Task TestAddAndRemoveMembers_CSharp_Fix_WithAddedNullability_WithoutObliviousAsync()
        {
            var shippedText = $@"#nullable enable";
            await VerifyCSharpAdditionalFileFixAsync($$"""
                #nullable enable
                {{EnabledModifier}} class C
                {
                    {{EnabledModifier}} string? {|{{AnnotateApiId}}:ChangedField|};
                }
                """, shippedText, """
                C
                C.C() -> void
                C.ChangedField -> string
                """, newShippedApiText: shippedText, """
                C
                C.C() -> void
                C.ChangedField -> string?
                """);
        }

        [Fact]
        public async Task LegacyAPIShouldBeAnnotatedWithObliviousMarkerAsync()
        {
            var shippedText = $@"#nullable enable";
            await VerifyCSharpAdditionalFileFixAsync($$"""
                {{EnabledModifier}} class C
                {
                    {{EnabledModifier}} string {|{{AnnotateApiId}}:{|{{ObliviousApiId}}:Field|}|}; // oblivious
                }
                """, shippedText, """
                C
                C.C() -> void
                C.Field -> string
                """, newShippedApiText: shippedText, """
                C
                C.C() -> void
                ~C.Field -> string
                """);
        }

        [Fact]
        public async Task LegacyAPIShouldBeAnnotatedWithObliviousMarker_ShippedFileAsync()
        {
            var unshippedText = @"";
            await VerifyCSharpAdditionalFileFixAsync($$"""
                {{EnabledModifier}} class C
                {
                    {{EnabledModifier}} string {|{{AnnotateApiId}}:{|{{ObliviousApiId}}:Field|}|}; // oblivious
                }
                """, $"""
                #nullable enable
                C
                C.C() -> void
                C.Field -> string
                """, unshippedText, $"""
                #nullable enable
                C
                C.C() -> void
                ~C.Field -> string
                """, newUnshippedApiText: unshippedText);
        }

        [Fact]
        public async Task LegacyAPIWithObliviousMarkerGetsAnnotatedAsNullableAsync()
        {
            var unshippedText = @"";
            await VerifyCSharpAdditionalFileFixAsync($$"""
                #nullable enable
                {{EnabledModifier}} class C
                {
                    {{EnabledModifier}} string? {|{{AnnotateApiId}}:Field|};
                }
                """, $"""
                #nullable enable
                C
                C.C() -> void
                ~C.Field -> string
                """, unshippedText, $"""
                #nullable enable
                C
                C.C() -> void
                C.Field -> string?
                """, newUnshippedApiText: unshippedText);
        }

        [Fact]
        public async Task LegacyAPIWithObliviousMarkerGetsAnnotatedAsNotNullableAsync()
        {
            var unshippedText = @"";
            await VerifyCSharpAdditionalFileFixAsync($$"""
                #nullable enable
                {{EnabledModifier}} class C
                {
                    {{EnabledModifier}} string {|{{AnnotateApiId}}:Field|};
                }
                """, $"""
                #nullable enable
                C
                C.C() -> void
                ~C.Field -> string
                """, unshippedText, $"""
                #nullable enable
                C
                C.C() -> void
                C.Field -> string!
                """, newUnshippedApiText: unshippedText);
        }

        #endregion
    }
}
