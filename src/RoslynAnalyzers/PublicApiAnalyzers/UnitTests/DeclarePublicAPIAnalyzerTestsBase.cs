﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.VisualBasic.Testing;
using Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.PublicApiAnalyzers.UnitTests
{
    public abstract class DeclarePublicApiAnalyzerTestsBase
    {
        protected abstract bool IsInternalTest { get; }
        protected abstract string EnabledModifierCSharp { get; }
        protected abstract string DisabledModifierCSharp { get; }
        protected abstract string EnabledModifierVB { get; }
        protected abstract string DisabledModifierVB { get; }
        protected abstract string ShippedFileName { get; }
        protected abstract string UnshippedFileName { get; }
        protected abstract string UnshippedFileNamePrefix { get; }
        protected abstract string AddNewApiId { get; }
        protected abstract string RemoveApiId { get; }
        protected abstract string DuplicatedSymbolInApiFileId { get; }
        protected abstract string ShouldAnnotateApiFilesId { get; }
        protected abstract string ObliviousApiId { get; }

        protected abstract DiagnosticDescriptor DeclareNewApiRule { get; }
        protected abstract DiagnosticDescriptor RemoveDeletedApiRule { get; }
        protected abstract DiagnosticDescriptor DuplicateSymbolInApiFiles { get; }
        protected abstract DiagnosticDescriptor AvoidMultipleOverloadsWithOptionalParameters { get; }
        protected abstract DiagnosticDescriptor OverloadWithOptionalParametersShouldHaveMostParameters { get; }
        protected abstract DiagnosticDescriptor AnnotateApiRule { get; }
        protected abstract DiagnosticDescriptor ObliviousApiRule { get; }
        protected abstract DiagnosticDescriptor ApiFilesInvalid { get; }
        protected abstract DiagnosticDescriptor ApiFileMissing { get; }
        protected abstract IEnumerable<string> DisabledDiagnostics { get; }

        #region Helpers
        private static DiagnosticResult GetAdditionalFileResultAt(int line, int column, string path, DiagnosticDescriptor descriptor, params object[] arguments)
        {
#pragma warning disable RS0030 // Do not use banned APIs
            return new DiagnosticResult(descriptor)
                .WithLocation(path, line, column)
#pragma warning restore RS0030 // Do not use banned APIs
                .WithArguments(arguments);
        }

        private static DiagnosticResult GetCSharpResultAt(int line, int column, DiagnosticDescriptor descriptor, params object[] arguments)
        {
#pragma warning disable RS0030 // Do not use banned APIs
            return new DiagnosticResult(descriptor)
                .WithLocation(line, column)
#pragma warning restore RS0030 // Do not use banned APIs
                .WithArguments(arguments);
        }

        private static DiagnosticResult GetBasicResultAt(int line, int column, DiagnosticDescriptor descriptor, params object[] arguments)
        {
#pragma warning disable RS0030 // Do not use banned APIs
            return new DiagnosticResult(descriptor)
                .WithLocation(line, column)
#pragma warning restore RS0030 // Do not use banned APIs
                .WithArguments(arguments);
        }

        private async Task VerifyBasicAsync(string source, string shippedApiText, string unshippedApiText, params DiagnosticResult[] expected)
        {
            var test = new VisualBasicCodeFixTest<DeclarePublicApiAnalyzer, DeclarePublicApiFix, DefaultVerifier>
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

        private async Task VerifyCSharpAsync(string source, string? shippedApiText, string? unshippedApiText, params DiagnosticResult[] expected)
        {
            var test = new CSharpCodeFixTest<DeclarePublicApiAnalyzer, DeclarePublicApiFix, DefaultVerifier>
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

        private async Task VerifyCSharpAsync(string source, string? shippedApiText, string? unshippedApiText, string editorConfigText, params DiagnosticResult[] expected)
        {
            var test = new CSharpCodeFixVerifier<DeclarePublicApiAnalyzer, DeclarePublicApiFix>.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { },
                    AnalyzerConfigFiles = { ("/.editorconfig", editorConfigText) },
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

        private async Task VerifyCSharpAsync(Action<SourceFileList> addSourcesAction, string? shippedApiText, string? unshippedApiText, string editorConfigText, params DiagnosticResult[] expected)
        {
            var test = new CSharpCodeFixVerifier<DeclarePublicApiAnalyzer, DeclarePublicApiFix>.Test
            {
                TestState =
                {
                    AdditionalFiles = { },
                    AnalyzerConfigFiles = { ("/.editorconfig", editorConfigText) },
                },
            };

            addSourcesAction(test.TestState.Sources);

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

        private async Task VerifyCSharpAsync(string source, string shippedApiText, string unshippedApiText, string shippedApiFilePath, string unshippedApiFilePath, params DiagnosticResult[] expected)
        {
            var test = new CSharpCodeFixTest<DeclarePublicApiAnalyzer, DeclarePublicApiFix, DefaultVerifier>
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles = { },
                }
            };

            if (shippedApiText != null)
            {
                test.TestState.AdditionalFiles.Add((shippedApiFilePath, shippedApiText));
            }

            if (unshippedApiText != null)
            {
                test.TestState.AdditionalFiles.Add((unshippedApiFilePath, unshippedApiText));
            }

            test.ExpectedDiagnostics.AddRange(expected);
            test.DisabledDiagnostics.AddRange(DisabledDiagnostics);
            await test.RunAsync();
        }

        private async Task VerifyCSharpAdditionalFileFixAsync(string source, string? shippedApiText, string? oldUnshippedApiText, string newUnshippedApiText)
        {
            await VerifyAdditionalFileFixAsync(LanguageNames.CSharp, source, shippedApiText, oldUnshippedApiText, newUnshippedApiText);
        }

        private async Task VerifyNet50CSharpAdditionalFileFixAsync(string source, string? shippedApiText, string? oldUnshippedApiText, string newUnshippedApiText)
        {
            await VerifyAdditionalFileFixAsync(LanguageNames.CSharp, source, shippedApiText, oldUnshippedApiText, newUnshippedApiText, ReferenceAssemblies.Net.Net50);
        }

        private async Task VerifyNet80CSharpAdditionalFileFixAsync(string source, string? shippedApiText, string? oldUnshippedApiText, string newUnshippedApiText)
        {
            await VerifyAdditionalFileFixAsync(LanguageNames.CSharp, source, shippedApiText, oldUnshippedApiText, newUnshippedApiText, ReferenceAssemblies.Net.Net80);
        }

        private async Task VerifyAdditionalFileFixAsync(string language, string source, string? shippedApiText, string? oldUnshippedApiText, string newUnshippedApiText,
            ReferenceAssemblies? referenceAssemblies = null)
        {
            var test = language == LanguageNames.CSharp
                ? new CSharpCodeFixTest<DeclarePublicApiAnalyzer, DeclarePublicApiFix, DefaultVerifier>()
                : (CodeFixTest<DefaultVerifier>)new VisualBasicCodeFixTest<DeclarePublicApiAnalyzer, DeclarePublicApiFix, DefaultVerifier>();

            if (referenceAssemblies is not null)
            {
                test.ReferenceAssemblies = referenceAssemblies;
            }

            test.TestState.Sources.Add(source);
            if (shippedApiText != null)
                test.TestState.AdditionalFiles.Add((ShippedFileName, shippedApiText));
            if (oldUnshippedApiText != null)
                test.TestState.AdditionalFiles.Add((UnshippedFileName, oldUnshippedApiText));

            test.FixedState.AdditionalFiles.Add((ShippedFileName, shippedApiText ?? string.Empty));
            test.FixedState.AdditionalFiles.Add((UnshippedFileName, newUnshippedApiText));
            test.DisabledDiagnostics.AddRange(DisabledDiagnostics);

            await test.RunAsync();
        }
        #endregion

        #region Diagnostic tests

        [Fact]
        [WorkItem(2622, "https://github.com/dotnet/roslyn-analyzers/issues/2622")]
        public async Task AnalyzerFileMissing_ShippedAsync()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                    private C() { }
                }
                """;

            string? shippedText = null;
            string? unshippedText = @"";

            var expected = new DiagnosticResult(ApiFileMissing)
                .WithArguments(ShippedFileName);
            await VerifyCSharpAsync(source, shippedText, unshippedText, expected);
        }

        [Fact]
        [WorkItem(2622, "https://github.com/dotnet/roslyn-analyzers/issues/2622")]
        public async Task AnalyzerFileMissing_UnshippedAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} class C
                {
                    private C() { }
                }
                """;

            string? shippedText = @"";
            string? unshippedText = null;

            var expected = new DiagnosticResult(ApiFileMissing)
                .WithArguments(UnshippedFileName);
            await VerifyCSharpAsync(source, shippedText, unshippedText, expected);
        }

        [Theory]
        [InlineData("")]
        [InlineData("dotnet_public_api_analyzer.require_api_files = false")]
        [InlineData("dotnet_public_api_analyzer.require_api_files = true")]
        [WorkItem(2622, "https://github.com/dotnet/roslyn-analyzers/issues/2622")]
        public async Task AnalyzerFileMissing_BothAsync(string editorconfigText)
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                    private C() { }
                }
                """;

            string? shippedText = null;
            string? unshippedText = null;

            var expectedDiagnostics = Array.Empty<DiagnosticResult>();
            if (!editorconfigText.EndsWith("true", StringComparison.OrdinalIgnoreCase))
            {
                expectedDiagnostics = new[] { GetCSharpResultAt(2, 8 + EnabledModifierCSharp.Length, DeclareNewApiRule, "C") };
            }

            await VerifyCSharpAsync(source, shippedText, unshippedText, $"[*]\r\n{editorconfigText}", expectedDiagnostics);
        }

        [Fact]
        public async Task AnalyzerFilePresent_MissingNonEnabledText()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                    private C() { }
                }
                """;

            string? shippedText = "";
            string? unshippedText = "";

            var expectedDiagnostics = new[] { GetCSharpResultAt(2, 8 + EnabledModifierCSharp.Length, DeclareNewApiRule, "C") };

            await VerifyCSharpAsync(source, shippedText, unshippedText, $"[*]\r\ndotnet_public_api_analyzer.require_api_files = true", expectedDiagnostics);
        }

        [Fact]
        public async Task EmptyPublicAPIFilesAsync()
        {
            var source = @"";

            var shippedText = @"";
            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact]
        public async Task SimpleMissingTypeAsync()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                    private C() { }
                }
                """;

            var shippedText = @"";
            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText, GetCSharpResultAt(2, 8 + EnabledModifierCSharp.Length, DeclareNewApiRule, "C"));
        }

        [Fact, WorkItem(2690, "https://github.com/dotnet/wpf/issues/2690")]
        public async Task XamlGeneratedNamespaceWorkaroundAsync()
        {
            var source = $$"""

                namespace XamlGeneratedNamespace {
                    {{EnabledModifierCSharp}} sealed class GeneratedInternalTypeHelper
                    {
                    }
                }
                """;

            var shippedText = @"";
            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact]
        public async Task SimpleMissingMember_CSharpAsync()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} int Field;
                    {{EnabledModifierCSharp}} int Property { get; set; }
                    {{EnabledModifierCSharp}} void Method() { } 
                    {{EnabledModifierCSharp}} int ArrowExpressionProperty => 0;
                }
                """;

            var shippedText = @"";
            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText,
                // Test0.cs(2,14): error RS0016: Symbol 'C' is not part of the declared API.
                GetCSharpResultAt(2, 8 + EnabledModifierCSharp.Length, DeclareNewApiRule, "C"),
                // Test0.cs(2,14): warning RS0016: Symbol 'C.C() -> void' is not part of the declared API.
                GetCSharpResultAt(2, 8 + EnabledModifierCSharp.Length, DeclareNewApiRule, "C.C() -> void"),
                // Test0.cs(4,16): error RS0016: Symbol 'C.Field -> int' is not part of the declared API.
                GetCSharpResultAt(4, 10 + EnabledModifierCSharp.Length, DeclareNewApiRule, "C.Field -> int"),
                // Test0.cs(5,27): error RS0016: Symbol 'C.Property.get -> int' is not part of the declared API.
                GetCSharpResultAt(5, 21 + EnabledModifierCSharp.Length, DeclareNewApiRule, "C.Property.get -> int"),
                // Test0.cs(5,32): error RS0016: Symbol 'C.Property.set -> void' is not part of the declared API.
                GetCSharpResultAt(5, 26 + EnabledModifierCSharp.Length, DeclareNewApiRule, "C.Property.set -> void"),
                // Test0.cs(6,17): error RS0016: Symbol 'C.Method() -> void' is not part of the declared API.
                GetCSharpResultAt(6, 11 + EnabledModifierCSharp.Length, DeclareNewApiRule, "C.Method() -> void"),
                // Test0.cs(7,43): error RS0016: Symbol 'C.ArrowExpressionProperty.get -> int' is not part of the declared API.
                GetCSharpResultAt(7, 37 + EnabledModifierCSharp.Length, DeclareNewApiRule, "C.ArrowExpressionProperty.get -> int"));
        }

        [Theory]
        [InlineData("string ", "string!")]
        [InlineData("string?", "string?")]
        [InlineData("int    ", "int")]
        [InlineData("int?   ", "int?")]
        public async Task SimpleMissingMember_CSharp_NullableTypes(string csharp, string message)
        {
            var source = $$"""
                #nullable enable
                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} {{csharp}} Field;
                    {{EnabledModifierCSharp}} {{csharp}} Property { get; set; }
                    {{EnabledModifierCSharp}} void Method({{csharp}} p) { } 
                    {{EnabledModifierCSharp}} {{csharp}} ArrowExpressionProperty => default;
                }
                """;

            var shippedText = "#nullable enable";
            var unshippedText = "#nullable enable";

            await VerifyCSharpAsync(source, shippedText, unshippedText,
                // Test0.cs(2,14): error RS0016: Symbol 'C' is not part of the declared API.
                GetCSharpResultAt(2, 8 + EnabledModifierCSharp.Length, DeclareNewApiRule, "C"),
                // Test0.cs(2,14): warning RS0016: Symbol 'C.C() -> void' is not part of the declared API.
                GetCSharpResultAt(2, 8 + EnabledModifierCSharp.Length, DeclareNewApiRule, "C.C() -> void"),
                // Test0.cs(4,16): error RS0016: Symbol 'C.Field -> int' is not part of the declared API.
                GetCSharpResultAt(4, 14 + EnabledModifierCSharp.Length, DeclareNewApiRule, $"C.Field -> {message}"),
                // Test0.cs(5,27): error RS0016: Symbol 'C.Property.get -> int' is not part of the declared API.
                GetCSharpResultAt(5, 25 + EnabledModifierCSharp.Length, DeclareNewApiRule, $"C.Property.get -> {message}"),
                // Test0.cs(5,32): error RS0016: Symbol 'C.Property.set -> void' is not part of the declared API.
                GetCSharpResultAt(5, 30 + EnabledModifierCSharp.Length, DeclareNewApiRule, "C.Property.set -> void"),
                // Test0.cs(6,17): error RS0016: Symbol 'C.Method() -> void' is not part of the declared API.
                GetCSharpResultAt(6, 11 + EnabledModifierCSharp.Length, DeclareNewApiRule, $"C.Method({message} p) -> void"),
                // Test0.cs(7,43): error RS0016: Symbol 'C.ArrowExpressionProperty.get -> int' is not part of the declared API.
                GetCSharpResultAt(7, 41 + EnabledModifierCSharp.Length, DeclareNewApiRule, $"C.ArrowExpressionProperty.get -> {message}"));
        }

        [Fact, WorkItem(821, "https://github.com/dotnet/roslyn-analyzers/issues/821")]
        public async Task SimpleMissingMember_BasicAsync()
        {
            var source = $"""

                Imports System

                {EnabledModifierVB} Class C
                    {EnabledModifierVB} Field As Integer
                    
                    {EnabledModifierVB} Property [Property]() As Integer
                        Get
                            Return m_Property
                        End Get
                        Set
                            m_Property = Value
                        End Set
                    End Property
                    Private m_Property As Integer

                    {EnabledModifierVB} Sub Method()
                    End Sub

                    {EnabledModifierVB} ReadOnly Property ReadOnlyAutoProperty As Integer = 0
                    {EnabledModifierVB} Property NormalAutoProperty As Integer = 0
                End Class
                """;

            var shippedText = @"";
            var unshippedText = @"";

            await VerifyBasicAsync(source, shippedText, unshippedText,
                // Test0.vb(4,14): warning RS0016: Symbol 'C' is not part of the declared API.
                GetBasicResultAt(4, 14, DeclareNewApiRule, "C"),
                // Test0.cs(2,14): warning RS0016: Symbol 'C.New() -> Void' is not part of the declared API.
                GetBasicResultAt(4, 14, DeclareNewApiRule, "C.New() -> Void"),
                // Test0.vb(5,12): warning RS0016: Symbol 'C.Field -> Integer' is not part of the declared API.
                GetBasicResultAt(5, 12, DeclareNewApiRule, "C.Field -> Integer"),
                // Test0.vb(8,9): warning RS0016: Symbol 'C.Property() -> Integer' is not part of the declared API.
                GetBasicResultAt(8, 9, DeclareNewApiRule, "C.Property() -> Integer"),
                // Test0.vb(11,9): warning RS0016: Symbol 'C.Property(Value As Integer) -> Void' is not part of the declared API.
                GetBasicResultAt(11, 9, DeclareNewApiRule, "C.Property(Value As Integer) -> Void"),
                // Test0.vb(17,16): warning RS0016: Symbol 'C.Method() -> Void' is not part of the declared API.
                GetBasicResultAt(17, 16, DeclareNewApiRule, "C.Method() -> Void"),
                // Test0.vb(20,30): warning RS0016: Symbol 'C.ReadOnlyAutoProperty() -> Integer' is not part of the declared API.
                GetBasicResultAt(20, 30, DeclareNewApiRule, "C.ReadOnlyAutoProperty() -> Integer"),
                // Test0.vb(21,21): warning RS0016: Symbol 'C.NormalAutoProperty() -> Integer' is not part of the declared API.
                GetBasicResultAt(21, 21, DeclareNewApiRule, "C.NormalAutoProperty() -> Integer"),
                // Test0.vb(21,21): warning RS0016: Symbol 'C.NormalAutoProperty(AutoPropertyValue As Integer) -> Void' is not part of the declared API.
                GetBasicResultAt(21, 21, DeclareNewApiRule, "C.NormalAutoProperty(AutoPropertyValue As Integer) -> Void"));
        }

        [Fact(), WorkItem(821, "https://github.com/dotnet/roslyn-analyzers/issues/821")]
        public async Task SimpleMissingMember_Basic1Async()
        {
            var source = $"""

                Imports System
                {EnabledModifierVB} Class C
                    Private m_Property As Integer
                    {EnabledModifierVB} Property [Property]() As Integer
                    '   Get
                    '      Return m_Property
                    '   End Get
                    '   Set
                    '       m_Property = Value
                    '  End Set
                    ' End Property
                    {EnabledModifierVB} ReadOnly Property ReadOnlyProperty0() As Integer
                        Get
                            Return m_Property
                        End Get
                    End Property
                    {EnabledModifierVB} WriteOnly Property WriteOnlyProperty0() As Integer
                        Set
                           m_Property = Value
                        End Set
                    End Property
                    {EnabledModifierVB} ReadOnly Property ReadOnlyProperty1 As Integer = 0
                    {EnabledModifierVB} ReadOnly Property ReadOnlyProperty2 As Integer
                    {EnabledModifierVB} Property Property1 As Integer
                End Class

                """;

            var shippedText = @"
C
C.New() -> Void
C.Property() -> Integer
C.Property(AutoPropertyValue As Integer) -> Void
C.Property1() -> Integer
C.Property1(AutoPropertyValue As Integer) -> Void
C.ReadOnlyProperty0() -> Integer
C.ReadOnlyProperty1() -> Integer
C.ReadOnlyProperty2() -> Integer
C.WriteOnlyProperty0(Value As Integer) -> Void
";
            var unshippedText = @"";
            await VerifyBasicAsync(source, shippedText, unshippedText);
        }

        [Fact, WorkItem(806, "https://github.com/dotnet/roslyn-analyzers/issues/806")]
        public async Task ShippedTextWithImplicitConstructorAsync()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                    private C() { }
                }
                """;

            var shippedText = @"
C
C -> void()";
            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText,
                // PublicAPI.Shipped.txt(3,1): warning RS0017: Symbol 'C -> void()' is part of the declared API, but is either not public or could not be found
                GetAdditionalFileResultAt(3, 1, ShippedFileName, RemoveDeletedApiRule, "C -> void()"));
        }

        [Fact, WorkItem(806, "https://github.com/dotnet/roslyn-analyzers/issues/806")]
        public async Task ShippedTextForImplicitConstructorAsync()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                }
                """;

            var shippedText = @"
C
C.C() -> void";
            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact, WorkItem(806, "https://github.com/dotnet/roslyn-analyzers/issues/806")]
        public async Task UnshippedTextForImplicitConstructorAsync()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                }
                """;

            var shippedText = @"
C";
            var unshippedText = @"
C.C() -> void";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact, WorkItem(806, "https://github.com/dotnet/roslyn-analyzers/issues/806")]
        public async Task ShippedTextWithMissingImplicitConstructorAsync()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                }
                """;

            var shippedText = @"
C";
            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText,
                // Test0.cs(2,14): warning RS0016: Symbol 'C.C() -> void' is not part of the declared API.
                GetCSharpResultAt(2, 8 + EnabledModifierCSharp.Length, DeclareNewApiRule, "C.C() -> void"));
        }

        [Fact, WorkItem(806, "https://github.com/dotnet/roslyn-analyzers/issues/806")]
        public async Task ShippedTextWithImplicitConstructorAndBreakingCodeChangeAsync()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                    private C() { }
                }
                """;

            var shippedText = @"
C
C.C() -> void";
            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText,
                // PublicAPI.Shipped.txt(3,1): warning RS0017: Symbol 'C.C() -> void' is part of the declared API, but is either not public or could not be found
                GetAdditionalFileResultAt(3, 1, ShippedFileName, RemoveDeletedApiRule, "C.C() -> void"));
        }

        [Fact]
        public async Task SimpleMemberAsync()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} int Field;
                    {{EnabledModifierCSharp}} int Property { get; set; }
                    {{EnabledModifierCSharp}} void Method() { } 
                }

                """;

            var shippedText = @"
C
C.C() -> void
C.Field -> int
C.Property.get -> int
C.Property.set -> void
C.Method() -> void
";
            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact]
        public async Task SplitBetweenShippedUnshippedAsync()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} int Field;
                    {{EnabledModifierCSharp}} int Property { get; set; }
                    {{EnabledModifierCSharp}} void Method() { } 
                }
                """;

            var shippedText = @"
C
C.C() -> void
C.Field -> int
C.Property.get -> int
C.Property.set -> void
";
            var unshippedText = @"
C.Method() -> void
";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact]
        public async Task EnumSplitBetweenFilesAsync()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} enum E 
                {
                    V1 = 1,
                    V2 = 2,
                    V3 = 3,
                }

                """;

            var shippedText = @"
E
E.V1 = 1 -> E
E.V2 = 2 -> E
";

            var unshippedText = @"
E.V3 = 3 -> E
";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact]
        public async Task SimpleRemovedMemberAsync()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                    public int Field;
                    public int Property { get; set; }
                }
                """;

            var shippedText = @"
C
C.C() -> void
C.Field -> int
C.Property.get -> int
C.Property.set -> void
C.Method() -> void
";

            string unshippedText = $@"
{DeclarePublicApiAnalyzer.RemovedApiPrefix}C.Method() -> void
";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem(3329, "https://github.com/dotnet/roslyn-analyzers/issues/3329")]
        public async Task RemovedPrefixForNonRemovedApiAsync(bool includeInShipped)
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} int Field;
                    {{EnabledModifierCSharp}} int Property { get; set; }
                    {{EnabledModifierCSharp}} void Method() { }
                }
                """;

            var shippedText = @"
C
C.C() -> void
C.Field -> int
C.Property.get -> int
C.Property.set -> void
";

            if (includeInShipped)
            {
                shippedText += @"C.Method() -> void
";
            }

            string unshippedText = $@"
{DeclarePublicApiAnalyzer.RemovedApiPrefix}C.Method() -> void
";

            var diagnostics = new[] {
                // PublicAPI.Unshipped.txt(2,1): warning RS0050: Symbol 'C.Method() -> void' is marked as removed but it isn't deleted in source code
                GetAdditionalFileResultAt(2, 1, UnshippedFileName, DeclarePublicApiAnalyzer.RemovedApiIsNotActuallyRemovedRule, "C.Method() -> void")
            };
            if (includeInShipped)
            {
                await VerifyCSharpAsync(source, shippedText, unshippedText, diagnostics);
            }
            else
            {
                // /0/Test0.cs(6,17): warning RS0016: Symbol 'C.Method() -> void' is not part of the declared API
                var secondDiagnostic = new[] { GetCSharpResultAt(6, 11 + EnabledModifierCSharp.Length, DeclareNewApiRule, "C.Method() -> void") };

                await VerifyCSharpAsync(source, shippedText, unshippedText, diagnostics.Concat(secondDiagnostic).ToArray());
            }
        }

        [Fact]
        public async Task ApiFileShippedWithRemovedAsync()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} int Field;
                    {{EnabledModifierCSharp}} int Property { get; set; }
                }
                """;

            string shippedText = $@"
C
C.Field -> int
C.Property.get -> int
C.Property.set -> void
{DeclarePublicApiAnalyzer.RemovedApiPrefix}C.Method() -> void
";

            string unshippedText = $@"";

            var expected = new DiagnosticResult(ApiFilesInvalid)
                .WithArguments(DeclarePublicApiAnalyzer.InvalidReasonShippedCantHaveRemoved);
            await VerifyCSharpAsync(source, shippedText, unshippedText, expected);
        }

        [Fact]
        [WorkItem(312, "https://github.com/dotnet/roslyn-analyzers/issues/312")]
        public async Task DuplicateSymbolInSameAPIFileAsync()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} int Field;
                    {{EnabledModifierCSharp}} int Property { get; set; }
                }
                """;

            var shippedText = @"
C
C.Field -> int
C.Property.get -> int
C.Property.set -> void
C.Property.get -> int
";

            var unshippedText = @"";

#pragma warning disable RS0030 // Do not use banned APIs
#pragma warning disable RS0030 // Do not use banned APIs
            var expected = new DiagnosticResult(DuplicateSymbolInApiFiles)
                .WithLocation(ShippedFileName, 6, 1)
#pragma warning restore RS0030 // Do not use banned APIs
                .WithLocation(ShippedFileName, 4, 1)
#pragma warning restore RS0030 // Do not use banned APIs
                .WithArguments("C.Property.get -> int");
            await VerifyCSharpAsync(source, shippedText, unshippedText, expected);
        }

        [Fact]
        [WorkItem(312, "https://github.com/dotnet/roslyn-analyzers/issues/312")]
        public async Task DuplicateSymbolInDifferentAPIFilesAsync()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} int Field;
                    {{EnabledModifierCSharp}} int Property { get; set; }
                }

                """;

            var shippedText = @"
C
C.C() -> void
C.Field -> int
C.Property.get -> int
C.Property.set -> void
";

            var unshippedText = @"
C.Property.get -> int";

#pragma warning disable RS0030 // Do not use banned APIs
#pragma warning disable RS0030 // Do not use banned APIs
            var expected = new DiagnosticResult(DuplicateSymbolInApiFiles)
                .WithLocation(UnshippedFileName, 2, 1)
#pragma warning restore RS0030 // Do not use banned APIs
                .WithLocation(ShippedFileName, 5, 1)
#pragma warning restore RS0030 // Do not use banned APIs
                .WithArguments("C.Property.get -> int");
            await VerifyCSharpAsync(source, shippedText, unshippedText, expected);
        }

        [Fact]
        [WorkItem(4584, "https://github.com/dotnet/roslyn-analyzers/issues/4584")]
        public async Task DuplicateObliviousSymbolsInSameApiFileAsync()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} int Field;
                    {{EnabledModifierCSharp}} int Property { get; set; }
                }
                """;

            var shippedText = $$"""
                #nullable enable
                C
                C.C() -> void
                C.Field -> int
                C.Property.set -> void
                ~C.Property.get -> int
                {|{{DuplicatedSymbolInApiFileId}}:~C.Property.get -> int|}
                """;

            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact]
        [WorkItem(4584, "https://github.com/dotnet/roslyn-analyzers/issues/4584")]
        public async Task DuplicateSymbolUsingObliviousInSameApiFilesAsync()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} int Field;
                    {{EnabledModifierCSharp}} int Property { get; set; }
                }
                """;

            var shippedText = $$"""
                #nullable enable
                C
                C.C() -> void
                C.Field -> int
                C.Property.get -> int
                C.Property.set -> void
                {|{{DuplicatedSymbolInApiFileId}}:~C.Property.get -> int|}

                """;

            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact]
        [WorkItem(4584, "https://github.com/dotnet/roslyn-analyzers/issues/4584")]
        public async Task DuplicateSymbolUsingObliviousInDifferentApiFilesAsync()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} int Field;
                    {{EnabledModifierCSharp}} int Property { get; set; }
                }
                """;

            var shippedText = @"#nullable enable
C
C.C() -> void
C.Field -> int
~C.Property.get -> int
C.Property.set -> void
";

            var unshippedText = $$"""
                #nullable enable
                {|{{DuplicatedSymbolInApiFileId}}:C.Property.get -> int|}
                """;

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact]
        [WorkItem(4584, "https://github.com/dotnet/roslyn-analyzers/issues/4584")]
        public async Task MultipleDuplicateSymbolsUsingObliviousInDifferentApiFilesAsync()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} int Field;
                    {{EnabledModifierCSharp}} int Property { get; set; }
                }

                """;

            var shippedText = @"#nullable enable
C
C.C() -> void
C.Field -> int
C.Property.get -> int
C.Property.set -> void
";

            var unshippedText = $$"""
                #nullable enable
                {|{{DuplicatedSymbolInApiFileId}}:~C.Property.get -> int|}
                {|{{DuplicatedSymbolInApiFileId}}:C.Property.get -> int|}
                {|{{DuplicatedSymbolInApiFileId}}:~C.Property.set -> void|}
                """;

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact, WorkItem(773, "https://github.com/dotnet/roslyn-analyzers/issues/773")]
        public async Task ApiFileShippedWithNonExistentMembersAsync()
        {
            // Type C has no public member "Method", but the shipped API has an entry for it.
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} int Field;
                    {{EnabledModifierCSharp}} int Property { get; set; }
                    private void Method() { }
                }

                """;

            string shippedText = $@"
C
C.C() -> void
C.Field -> int
C.Property.get -> int
C.Property.set -> void
C.Method() -> void
";
            string unshippedText = $@"";

            await VerifyCSharpAsync(source, shippedText, unshippedText,
                // PublicAPI.Shipped.txt(7,1): warning RS0017: Symbol 'C.Method() -> void' is part of the declared API, but is either not public or could not be found
                GetAdditionalFileResultAt(7, 1, ShippedFileName, RemoveDeletedApiRule, "C.Method() -> void"));
        }

        [Fact, WorkItem(773, "https://github.com/dotnet/roslyn-analyzers/issues/773")]
        public async Task ApiFileShippedWithNonExistentMembers_TestFullPathAsync()
        {
            // Type C has no public member "Method", but the shipped API has an entry for it.
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} int Field;
                    {{EnabledModifierCSharp}} int Property { get; set; }
                    private void Method() { }
                }
                """;

            var tempPath = Path.GetTempPath();
            string shippedText = $@"
C
C.C() -> void
C.Field -> int
C.Property.get -> int
C.Property.set -> void
C.Method() -> void
";
            var shippedFilePath = Path.Combine(tempPath, ShippedFileName);

            string unshippedText = $@"";
            var unshippedFilePath = Path.Combine(tempPath, UnshippedFileName);

            await VerifyCSharpAsync(source, shippedText, unshippedText, shippedFilePath, unshippedFilePath,
                // <%TEMP_PATH%>\PublicAPI.Shipped.txt(7,1): warning RS0017: Symbol 'C.Method() -> void' is part of the declared API, but is either not public or could not be found
                GetAdditionalFileResultAt(7, 1, shippedFilePath, RemoveDeletedApiRule, "C.Method() -> void"));
        }

        [Fact]
        public async Task TypeForwardsAreProcessed1Async()
        {
            if (IsInternalTest)
            {
                return;
            }

            var source = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.StringComparison))]
";

#if NETCOREAPP
            var containingAssembly = "System.Runtime";
#else
            var containingAssembly = "mscorlib";
#endif
            string shippedText = $@"
System.StringComparison (forwarded, contained in {containingAssembly})
System.StringComparison.CurrentCulture = 0 -> System.StringComparison (forwarded, contained in {containingAssembly})
System.StringComparison.CurrentCultureIgnoreCase = 1 -> System.StringComparison (forwarded, contained in {containingAssembly})
System.StringComparison.InvariantCulture = 2 -> System.StringComparison (forwarded, contained in {containingAssembly})
System.StringComparison.InvariantCultureIgnoreCase = 3 -> System.StringComparison (forwarded, contained in {containingAssembly})
System.StringComparison.Ordinal = 4 -> System.StringComparison (forwarded, contained in {containingAssembly})
System.StringComparison.OrdinalIgnoreCase = 5 -> System.StringComparison (forwarded, contained in {containingAssembly})
";
            string unshippedText = $@"";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact]
        public async Task TypeForwardsAreProcessed2Async()
        {
            if (IsInternalTest)
            {
                return;
            }

            var source = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.StringComparer))]
";

#if NETCOREAPP
            var containingAssembly = "System.Runtime.Extensions";
            const string NonNullSuffix = "!";
            const string NullableSuffix = "?";
#else
            var containingAssembly = "mscorlib";
            const string NonNullSuffix = "";
            const string NullableSuffix = "";
#endif
            string shippedText = $@"
System.StringComparer (forwarded, contained in {containingAssembly})
static System.StringComparer.InvariantCulture.get -> System.StringComparer{NonNullSuffix} (forwarded, contained in {containingAssembly})
static System.StringComparer.InvariantCultureIgnoreCase.get -> System.StringComparer{NonNullSuffix} (forwarded, contained in {containingAssembly})
static System.StringComparer.CurrentCulture.get -> System.StringComparer{NonNullSuffix} (forwarded, contained in {containingAssembly})
static System.StringComparer.CurrentCultureIgnoreCase.get -> System.StringComparer{NonNullSuffix} (forwarded, contained in {containingAssembly})
static System.StringComparer.Ordinal.get -> System.StringComparer{NonNullSuffix} (forwarded, contained in {containingAssembly})
static System.StringComparer.OrdinalIgnoreCase.get -> System.StringComparer{NonNullSuffix} (forwarded, contained in {containingAssembly})
static System.StringComparer.Create(System.Globalization.CultureInfo{NonNullSuffix} culture, bool ignoreCase) -> System.StringComparer{NonNullSuffix} (forwarded, contained in {containingAssembly})
System.StringComparer.Compare(object{NullableSuffix} x, object{NullableSuffix} y) -> int (forwarded, contained in {containingAssembly})
System.StringComparer.Equals(object{NullableSuffix} x, object{NullableSuffix} y) -> bool (forwarded, contained in {containingAssembly})
System.StringComparer.GetHashCode(object{NonNullSuffix} obj) -> int (forwarded, contained in {containingAssembly})
abstract System.StringComparer.Compare(string{NullableSuffix} x, string{NullableSuffix} y) -> int (forwarded, contained in {containingAssembly})
abstract System.StringComparer.Equals(string{NullableSuffix} x, string{NullableSuffix} y) -> bool (forwarded, contained in {containingAssembly})
abstract System.StringComparer.GetHashCode(string{NonNullSuffix} obj) -> int (forwarded, contained in {containingAssembly})
System.StringComparer.StringComparer() -> void (forwarded, contained in {containingAssembly})
";

#if NETCOREAPP
            shippedText = $@"
#nullable enable
{shippedText}
static System.StringComparer.Create(System.Globalization.CultureInfo{NonNullSuffix} culture, System.Globalization.CompareOptions options) -> System.StringComparer{NonNullSuffix} (forwarded, contained in {containingAssembly})
static System.StringComparer.FromComparison(System.StringComparison comparisonType) -> System.StringComparer{NonNullSuffix} (forwarded, contained in {containingAssembly})
";
#endif

            string unshippedText = $@"";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact, WorkItem(1192, "https://github.com/dotnet/roslyn-analyzers/issues/1192")]
        public async Task OpenGenericTypeForwardsAreProcessedAsync()
        {
            if (IsInternalTest)
            {
                return;
            }

            var source = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Collections.Generic.IEnumerable<>))]
";

            string shippedText = "";
            string unshippedText = "";

#if NETCOREAPP
            var containingAssembly = "System.Runtime";
#else
            var containingAssembly = "mscorlib";
#endif

            await VerifyCSharpAsync(source, shippedText, unshippedText,
                // /0/Test0.cs(2,12): warning RS0016: Symbol 'System.Collections.Generic.IEnumerable<T> (forwarded, contained in System.Runtime)' is not part of the declared API
                GetCSharpResultAt(2, 12, DeclareNewApiRule, $"System.Collections.Generic.IEnumerable<T> (forwarded, contained in {containingAssembly})"),
                // /0/Test0.cs(2,12): warning RS0016: Symbol 'System.Collections.Generic.IEnumerable<T>.GetEnumerator() -> System.Collections.Generic.IEnumerator<T> (forwarded, contained in System.Runtime)' is not part of the declared API
                GetCSharpResultAt(2, 12, DeclareNewApiRule, $"System.Collections.Generic.IEnumerable<T>.GetEnumerator() -> System.Collections.Generic.IEnumerator<T> (forwarded, contained in {containingAssembly})")
#if NETCOREAPP
                // /0/Test0.cs(2,12): warning RS0037: PublicAPI.txt is missing '#nullable enable', so the nullability annotations of API isn't recorded. It is recommended to enable this tracking.
                , GetCSharpResultAt(2, 12, DeclarePublicApiAnalyzer.ShouldAnnotatePublicApiFilesRule)
#endif
                );
        }

        [Fact, WorkItem(1192, "https://github.com/dotnet/roslyn-analyzers/issues/1192")]
        public async Task GenericTypeForwardsAreProcessedAsync()
        {
            if (IsInternalTest)
            {
                return;
            }

            var source = @"
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Collections.Generic.IEnumerable<string>))]
";

            string shippedText = "";
            string unshippedText = "";

#if NETCOREAPP
            var containingAssembly = "System.Runtime";
#else
            var containingAssembly = "mscorlib";
#endif

            await VerifyCSharpAsync(source, shippedText, unshippedText,
                // /0/Test0.cs(2,12): warning RS0016: Symbol 'System.Collections.Generic.IEnumerable<string> (forwarded, contained in System.Runtime)' is not part of the declared API
                GetCSharpResultAt(2, 12, DeclareNewApiRule, $"System.Collections.Generic.IEnumerable<string> (forwarded, contained in {containingAssembly})"),
                // /0/Test0.cs(2,12): warning RS0016: Symbol 'System.Collections.Generic.IEnumerable<string>.GetEnumerator() -> System.Collections.Generic.IEnumerator<string> (forwarded, contained in System.Runtime)' is not part of the declared API
                GetCSharpResultAt(2, 12, DeclareNewApiRule, $"System.Collections.Generic.IEnumerable<string>.GetEnumerator() -> System.Collections.Generic.IEnumerator<string> (forwarded, contained in {containingAssembly})")
#if NETCOREAPP
                // /0/Test0.cs(2,12): warning RS0037: PublicAPI.txt is missing '#nullable enable', so the nullability annotations of API isn't recorded. It is recommended to enable this tracking.
                , GetCSharpResultAt(2, 12, DeclarePublicApiAnalyzer.ShouldAnnotatePublicApiFilesRule)
#endif
                );
        }

        [Fact, WorkItem(851, "https://github.com/dotnet/roslyn-analyzers/issues/851")]
        public async Task TestAvoidMultipleOverloadsWithOptionalParametersAsync()
        {
            var source = $$"""

                public class C
                {
                    // ok - single overload with optional params, 2 overloads have no public API entries.
                    {{EnabledModifierCSharp}} void Method1(int p1, int p2, int p3 = 0) { }
                    {{EnabledModifierCSharp}} void Method1() { }
                    {{EnabledModifierCSharp}} void Method1(int p1, int p2) { }
                    {{EnabledModifierCSharp}} void Method1(char p1, params int[] p2) { }

                    // ok - multiple overloads with optional params, but only one is public.
                    {{EnabledModifierCSharp}} void Method2(int p1 = 0) { }
                    {{DisabledModifierCSharp}} void Method2(char p1 = '0') { }
                    private void Method2(string p1 = null) { }

                    // ok - multiple overloads with optional params, but all are shipped.
                    {{EnabledModifierCSharp}} void Method3(int p1 = 0) { }
                    {{EnabledModifierCSharp}} void Method3(string p1 = null) { }

                    // fire on unshipped (1) - multiple overloads with optional params, all but first are shipped.
                    {{EnabledModifierCSharp}} void Method4(int p1 = 0) { }
                    {{EnabledModifierCSharp}} void Method4(char p1 = 'a') { }
                    {{EnabledModifierCSharp}} void Method4(string p1 = null) { }

                    // fire on all unshipped (3) - multiple overloads with optional params, all are unshipped, 2 have unshipped entries.
                    {{EnabledModifierCSharp}} void Method5(int p1 = 0) { }
                    {{EnabledModifierCSharp}} void Method5(char p1 = 'a') { }
                    {{EnabledModifierCSharp}} void Method5(string p1 = null) { }

                    // ok - multiple overloads with optional params, but all have same params (differ only by generic vs non-generic).
                    {{EnabledModifierCSharp}} object Method6(int p1 = 0) { return Method6<object>(p1); }
                    {{EnabledModifierCSharp}} T Method6<T>(int p1 = 0) { return default(T); }
                }
                """;

            string shippedText = """
                C.Method3(int p1 = 0) -> void
                C.Method3(string p1 = null) -> void
                C.Method4(char p1 = 'a') -> void
                C.Method4(string p1 = null) -> void
                """;
            string unshippedText =
                (IsInternalTest ? "" :
                """
                C
                C.C() -> void

                """) +
                """
                C.Method1() -> void
                C.Method1(int p1, int p2) -> void
                C.Method2(int p1 = 0) -> void
                C.Method4(int p1 = 0) -> void
                C.Method5(char p1 = 'a') -> void
                C.Method5(string p1 = null) -> void
                C.Method6(int p1 = 0) -> object
                C.Method6<T>(int p1 = 0) -> T
                """;

            // The error on Method2 is the difference between internal and public results
            var result = IsInternalTest
                ? new DiagnosticResult[]
                {
                    // Test0.cs(5,17): warning RS0016: Symbol 'C.Method1(int p1, int p2, int p3 = 0) -> void' is not part of the declared API.
                    GetCSharpResultAt(5, 19, DeclareNewApiRule, "C.Method1(int p1, int p2, int p3 = 0) -> void"),
                    // Test0.cs(8,17): warning RS0016: Symbol 'C.Method1(char p1, params int[] p2) -> void' is not part of the declared API.
                    GetCSharpResultAt(8, 19, DeclareNewApiRule, "C.Method1(char p1, params int[] p2) -> void"),
                    // /0/Test0.cs(11,19): warning RS0059: Symbol 'Method2' violates the backcompat requirement: 'Do not add multiple overloads with optional parameters'. See 'https://github.com/dotnet/roslyn/blob/main/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                    GetCSharpResultAt(11, 19, AvoidMultipleOverloadsWithOptionalParameters, "Method2", AvoidMultipleOverloadsWithOptionalParameters.HelpLinkUri),
                    // Test0.cs(20,17): warning RS0026: Symbol 'Method4' violates the backcompat requirement: 'Do not add multiple overloads with optional parameters'. See 'https://github.com/dotnet/roslyn/blob/main/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                    GetCSharpResultAt(20, 19, AvoidMultipleOverloadsWithOptionalParameters, "Method4", AvoidMultipleOverloadsWithOptionalParameters.HelpLinkUri),
                    // Test0.cs(25,17): warning RS0016: Symbol 'C.Method5(int p1 = 0) -> void' is not part of the declared API.
                    GetCSharpResultAt(25, 19, DeclareNewApiRule, "C.Method5(int p1 = 0) -> void"),
                    // Test0.cs(25,17): warning RS0026: Symbol 'Method5' violates the backcompat requirement: 'Do not add multiple overloads with optional parameters'. See 'https://github.com/dotnet/roslyn/blob/main/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                    GetCSharpResultAt(25, 19, AvoidMultipleOverloadsWithOptionalParameters, "Method5", AvoidMultipleOverloadsWithOptionalParameters.HelpLinkUri),
                    // Test0.cs(26,17): warning RS0026: Symbol 'Method5' violates the backcompat requirement: 'Do not add multiple overloads with optional parameters'. See 'https://github.com/dotnet/roslyn/blob/main/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                    GetCSharpResultAt(26, 19, AvoidMultipleOverloadsWithOptionalParameters, "Method5", AvoidMultipleOverloadsWithOptionalParameters.HelpLinkUri),
                    // Test0.cs(27,17): warning RS0026: Symbol 'Method5' violates the backcompat requirement: 'Do not add multiple overloads with optional parameters'. See 'https://github.com/dotnet/roslyn/blob/main/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                    GetCSharpResultAt(27, 19, AvoidMultipleOverloadsWithOptionalParameters, "Method5", AvoidMultipleOverloadsWithOptionalParameters.HelpLinkUri)
                }
                : new[] {
                    // Test0.cs(5,17): warning RS0016: Symbol 'C.Method1(int p1, int p2, int p3 = 0) -> void' is not part of the declared API.
                    GetCSharpResultAt(5, 17, DeclareNewApiRule, "C.Method1(int p1, int p2, int p3 = 0) -> void"),
                    // Test0.cs(8,17): warning RS0016: Symbol 'C.Method1(char p1, params int[] p2) -> void' is not part of the declared API.
                    GetCSharpResultAt(8, 17, DeclareNewApiRule, "C.Method1(char p1, params int[] p2) -> void"),
                    // Test0.cs(20,17): warning RS0026: Symbol 'Method4' violates the backcompat requirement: 'Do not add multiple overloads with optional parameters'. See 'https://github.com/dotnet/roslyn/blob/main/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                    GetCSharpResultAt(20, 17, AvoidMultipleOverloadsWithOptionalParameters, "Method4", AvoidMultipleOverloadsWithOptionalParameters.HelpLinkUri),
                    // Test0.cs(25,17): warning RS0016: Symbol 'C.Method5(int p1 = 0) -> void' is not part of the declared API.
                    GetCSharpResultAt(25, 17, DeclareNewApiRule, "C.Method5(int p1 = 0) -> void"),
                    // Test0.cs(25,17): warning RS0026: Symbol 'Method5' violates the backcompat requirement: 'Do not add multiple overloads with optional parameters'. See 'https://github.com/dotnet/roslyn/blob/main/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                    GetCSharpResultAt(25, 17, AvoidMultipleOverloadsWithOptionalParameters, "Method5", AvoidMultipleOverloadsWithOptionalParameters.HelpLinkUri),
                    // Test0.cs(26,17): warning RS0026: Symbol 'Method5' violates the backcompat requirement: 'Do not add multiple overloads with optional parameters'. See 'https://github.com/dotnet/roslyn/blob/main/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                    GetCSharpResultAt(26, 17, AvoidMultipleOverloadsWithOptionalParameters, "Method5", AvoidMultipleOverloadsWithOptionalParameters.HelpLinkUri),
                    // Test0.cs(27,17): warning RS0026: Symbol 'Method5' violates the backcompat requirement: 'Do not add multiple overloads with optional parameters'. See 'https://github.com/dotnet/roslyn/blob/main/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                    GetCSharpResultAt(27, 17, AvoidMultipleOverloadsWithOptionalParameters, "Method5", AvoidMultipleOverloadsWithOptionalParameters.HelpLinkUri)
                };

            await VerifyCSharpAsync(source, shippedText, unshippedText, result);
        }

        [Fact, WorkItem(851, "https://github.com/dotnet/roslyn-analyzers/issues/851")]
        public async Task TestOverloadWithOptionalParametersShouldHaveMostParametersAsync()
        {
            var source = $$"""

                public class C
                {
                    // ok - single overload with optional params has most parameters.
                    {{EnabledModifierCSharp}} void Method1(int p1, int p2, int p3 = 0) { }
                    {{EnabledModifierCSharp}} void Method1() { }
                    {{EnabledModifierCSharp}} void Method1(int p1, int p2) { }
                    {{EnabledModifierCSharp}} void Method1(char p1, params int[] p2) { }

                    // ok - multiple overloads with optional params violating most params requirement, but only one is public.
                    {{EnabledModifierCSharp}} void Method2(int p1 = 0) { }
                    {{DisabledModifierCSharp}} void Method2(int p1, char p2 = '0') { }
                    private void Method2(string p1 = null) { }

                    // ok - multiple overloads with optional params violating most params requirement, but all are shipped.
                    {{EnabledModifierCSharp}} void Method3(int p1 = 0) { }
                    {{EnabledModifierCSharp}} void Method3(string p1 = null) { }
                    {{EnabledModifierCSharp}} void Method3(int p1, int p2) { }

                    // fire on unshipped (1) - single overload with optional params and violating most params requirement.
                    {{EnabledModifierCSharp}} void Method4(int p1 = 0) { }     // unshipped
                    {{EnabledModifierCSharp}} void Method4(char p1, int p2) { }        // unshipped
                    {{EnabledModifierCSharp}} void Method4(string p1, int p2) { }      // unshipped

                    // fire on shipped (1) - single shipped overload with optional params and violating most params requirement due to a new unshipped API.
                    {{EnabledModifierCSharp}} void Method5(int p1 = 0) { }     // shipped
                    {{EnabledModifierCSharp}} void Method5(char p1) { }        // shipped
                    {{EnabledModifierCSharp}} void Method5(string p1) { }      // unshipped

                    // fire on multiple shipped (2) - multiple shipped overloads with optional params and violating most params requirement due to a new unshipped API
                    {{EnabledModifierCSharp}} void Method6(int p1 = 0) { }     // shipped
                    {{EnabledModifierCSharp}} void Method6(char p1 = 'a') { }  // shipped
                    {{EnabledModifierCSharp}} void Method6(string p1) { }      // unshipped
                }
                """;

            string shippedText = """
                C.Method3(int p1 = 0) -> void
                C.Method3(int p1, int p2) -> void
                C.Method3(string p1 = null) -> void
                C.Method5(char p1) -> void
                C.Method5(int p1 = 0) -> void
                C.Method6(char p1 = 'a') -> void
                C.Method6(int p1 = 0) -> void
                """;
            string unshippedText = (IsInternalTest ? "" :
                """
                C
                C.C() -> void

                """) + """
                C.Method1() -> void
                C.Method1(char p1, params int[] p2) -> void
                C.Method1(int p1, int p2) -> void
                C.Method1(int p1, int p2, int p3 = 0) -> void
                C.Method2(int p1 = 0) -> void
                C.Method4(char p1, int p2) -> void
                C.Method4(int p1 = 0) -> void
                C.Method4(string p1, int p2) -> void
                C.Method5(string p1) -> void
                C.Method6(string p1) -> void
                """;

            var diagnostics = IsInternalTest ? new DiagnosticResult[] {
                // /0/Test0.cs(11,19): warning RS0059: Symbol 'Method2' violates the backcompat requirement: 'Do not add multiple overloads with optional parameters'. See 'https://github.com/dotnet/roslyn/blob/main/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                GetCSharpResultAt(11, 19, AvoidMultipleOverloadsWithOptionalParameters, "Method2", AvoidMultipleOverloadsWithOptionalParameters.HelpLinkUri),
             } : Array.Empty<DiagnosticResult>();

            diagnostics = diagnostics.Concat(new[] {
                // Test0.cs(21,17): warning RS0027: Symbol 'Method4' violates the backcompat requirement: 'Public API with optional parameter(s) should have the most parameters amongst its public overloads'. See 'https://github.com/dotnet/roslyn/blob/main/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                GetCSharpResultAt(21, 11 + EnabledModifierCSharp.Length, OverloadWithOptionalParametersShouldHaveMostParameters, "Method4", OverloadWithOptionalParametersShouldHaveMostParameters.HelpLinkUri),
                // Test0.cs(26,17): warning RS0027: Symbol 'Method5' violates the backcompat requirement: 'Public API with optional parameter(s) should have the most parameters amongst its public overloads'. See 'https://github.com/dotnet/roslyn/blob/main/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                GetCSharpResultAt(26, 11 + EnabledModifierCSharp.Length, OverloadWithOptionalParametersShouldHaveMostParameters, "Method5", OverloadWithOptionalParametersShouldHaveMostParameters.HelpLinkUri),
                // Test0.cs(31,17): warning RS0027: Symbol 'Method6' violates the backcompat requirement: 'Public API with optional parameter(s) should have the most parameters amongst its public overloads'. See 'https://github.com/dotnet/roslyn/blob/main/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                GetCSharpResultAt(31, 11 + EnabledModifierCSharp.Length, OverloadWithOptionalParametersShouldHaveMostParameters, "Method6", OverloadWithOptionalParametersShouldHaveMostParameters.HelpLinkUri),
                // Test0.cs(32,17): warning RS0027: Symbol 'Method6' violates the backcompat requirement: 'Public API with optional parameter(s) should have the most parameters amongst its public overloads'. See 'https://github.com/dotnet/roslyn/blob/main/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md' for details.
                GetCSharpResultAt(32, 11 + EnabledModifierCSharp.Length, OverloadWithOptionalParametersShouldHaveMostParameters, "Method6", OverloadWithOptionalParametersShouldHaveMostParameters.HelpLinkUri)
            }).ToArray();

            await VerifyCSharpAsync(source, shippedText, unshippedText, diagnostics);
        }

        [Fact, WorkItem(4766, "https://github.com/dotnet/roslyn-analyzers/issues/4766")]
        public async Task TestObsoleteOverloadWithOptionalParameters_NoDiagnosticAsync()
        {
            var source = $$"""

                using System;

                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} void M(int p1 = 0) { }

                    [Obsolete]
                    {{EnabledModifierCSharp}} void M(char p1, int p2) { }
                }
                """;

            string shippedText = string.Empty;
            string unshippedText = @"
C
C.C() -> void

C.M(char p1, int p2) -> void
C.M(int p1 = 0) -> void
";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact, WorkItem(4766, "https://github.com/dotnet/roslyn-analyzers/issues/4766")]
        public async Task TestMultipleOverloadsWithOptionalParameter_OneIsObsoleteAsync()
        {
            var source = $$"""

                using System;

                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} void M(int p1 = 0) { }

                    [Obsolete]
                    {{EnabledModifierCSharp}} void M(char p1 = '0') { }
                }
                """;

            string shippedText = @"C
C.C() -> void
C.M(char p1 = '0') -> void";
            string unshippedText = "C.M(int p1 = 0) -> void";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact]
        public async Task ObliviousMember_SimpleAsync()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} string Field;
                    {{EnabledModifierCSharp}} string Property { get; set; }
                    {{EnabledModifierCSharp}} string Method(string x) => throw null!;
                    {{EnabledModifierCSharp}} string ArrowExpressionProperty => throw null!;
                }
                """;

            var shippedText = @"#nullable enable
C
C.ArrowExpressionProperty.get -> string
C.C() -> void
C.Field -> string
C.Method(string x) -> string
C.Property.get -> string
C.Property.set -> void";

            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText,
                GetCSharpResultAt(4, 13 + EnabledModifierCSharp.Length, AnnotateApiRule, "C.Field -> string"),
                GetCSharpResultAt(4, 13 + EnabledModifierCSharp.Length, ObliviousApiRule, "Field"),
                GetCSharpResultAt(5, 24 + EnabledModifierCSharp.Length, AnnotateApiRule, "C.Property.get -> string"),
                GetCSharpResultAt(5, 24 + EnabledModifierCSharp.Length, ObliviousApiRule, "Property.get"),
                GetCSharpResultAt(5, 29 + EnabledModifierCSharp.Length, AnnotateApiRule, "C.Property.set -> void"),
                GetCSharpResultAt(5, 29 + EnabledModifierCSharp.Length, ObliviousApiRule, "Property.set"),
                GetCSharpResultAt(6, 13 + EnabledModifierCSharp.Length, AnnotateApiRule, "C.Method(string x) -> string"),
                GetCSharpResultAt(6, 13 + EnabledModifierCSharp.Length, ObliviousApiRule, "Method"),
                GetCSharpResultAt(7, 40 + EnabledModifierCSharp.Length, AnnotateApiRule, "C.ArrowExpressionProperty.get -> string"),
                GetCSharpResultAt(7, 40 + EnabledModifierCSharp.Length, ObliviousApiRule, "ArrowExpressionProperty.get")
                );
        }

        [Theory]
        [InlineData("string ", "string", "string!")]
        [InlineData("string?", "string", "string?")]
        public async Task ObliviousMember_Simple_NullableTypes(string csharp, string unannotated, string annotated)
        {
            var source = $$"""
                #nullable enable
                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} {{csharp}} Field;
                    {{EnabledModifierCSharp}} {{csharp}} Property { get; set; }
                    {{EnabledModifierCSharp}} {{csharp}} Method({{csharp}} x) => throw null!;
                    {{EnabledModifierCSharp}} {{csharp}} ArrowExpressionProperty => throw null!;
                }
                """;

            var shippedText = $"""
                #nullable enable
                C
                C.ArrowExpressionProperty.get -> {unannotated}
                C.C() -> void
                C.Field -> {unannotated}
                C.Method({unannotated} x) -> {unannotated}
                C.Property.get -> {unannotated}
                C.Property.set -> void
                """;

            var unshippedText = "";

            await VerifyCSharpAsync(source, shippedText, unshippedText,
                GetCSharpResultAt(4, 14 + EnabledModifierCSharp.Length, AnnotateApiRule, $"C.Field -> {annotated}"),
                GetCSharpResultAt(5, 25 + EnabledModifierCSharp.Length, AnnotateApiRule, $"C.Property.get -> {annotated}"),
                GetCSharpResultAt(6, 14 + EnabledModifierCSharp.Length, AnnotateApiRule, $"C.Method({annotated} x) -> {annotated}"),
                GetCSharpResultAt(7, 41 + EnabledModifierCSharp.Length, AnnotateApiRule, $"C.ArrowExpressionProperty.get -> {annotated}"));
        }

        [Fact]
        public async Task ObliviousMember_AlreadyMarkedAsObliviousAsync()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} string Field;
                    {{EnabledModifierCSharp}} D<string
                #nullable enable
                        > Field2;
                #nullable disable
                    {{EnabledModifierCSharp}} string Property { get; set; }
                    {{EnabledModifierCSharp}} void Method(string x) => throw null!;
                    {{EnabledModifierCSharp}} string Method2() => throw null!;
                    {{EnabledModifierCSharp}} string ArrowExpressionProperty => throw null!;
                #nullable enable
                    {{EnabledModifierCSharp}} D<string>.E<
                #nullable disable
                        string
                #nullable enable
                            > Method3() => throw null!;
                #nullable disable
                    {{EnabledModifierCSharp}} string this[string x] { get => throw null!; set => throw null!; }
                }
                {{EnabledModifierCSharp}} class D<T> { public class E<T> { } }

                """;

            var shippedText = @"#nullable enable
C
~C.ArrowExpressionProperty.get -> string
C.C() -> void
~C.Field -> string
~C.Field2 -> D<string>!
~C.Method(string x) -> void
~C.Method2() -> string
~C.Property.get -> string
~C.Property.set -> void
~C.Method3() -> D<string!>.E<string>!
~C.this[string x].set -> void
~C.this[string x].get -> string
D<T>
D<T>.D() -> void
D<T>.E<T>
D<T>.E<T>.E() -> void";

            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText,
                GetCSharpResultAt(4, 13 + EnabledModifierCSharp.Length, ObliviousApiRule, "Field"),
                GetCSharpResultAt(7, 11, ObliviousApiRule, "Field2"),
                GetCSharpResultAt(9, 24 + EnabledModifierCSharp.Length, ObliviousApiRule, "Property.get"),
                GetCSharpResultAt(9, 29 + EnabledModifierCSharp.Length, ObliviousApiRule, "Property.set"),
                GetCSharpResultAt(10, 11 + EnabledModifierCSharp.Length, ObliviousApiRule, "Method"),
                GetCSharpResultAt(11, 13 + EnabledModifierCSharp.Length, ObliviousApiRule, "Method2"),
                GetCSharpResultAt(12, 40 + EnabledModifierCSharp.Length, ObliviousApiRule, "ArrowExpressionProperty.get"),
                GetCSharpResultAt(18, 15, ObliviousApiRule, "Method3"),
                GetCSharpResultAt(20, 30 + EnabledModifierCSharp.Length, ObliviousApiRule, "this.get"),
                GetCSharpResultAt(20, 50 + EnabledModifierCSharp.Length, ObliviousApiRule, "this.set")
                );
        }

        [Fact]
        public async Task ObliviousMember_AlreadyMarkedAsOblivious_TypeParametersWithClassConstraintAsync()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} void M<T>(T t) where T : class { }

                #nullable enable
                    {{EnabledModifierCSharp}} void M2<T>(T t) where T : class { }
                    {{EnabledModifierCSharp}} void M3<T>(T t) where T : class? { }
                #nullable disable
                }
                {{EnabledModifierCSharp}} class D<T> where T : class { }
                {{EnabledModifierCSharp}} class E { public class F<T> where T : class { } }
                """;

            var shippedText = @"#nullable enable
C
C.C() -> void
~C.M<T>(T t) -> void
C.M2<T>(T! t) -> void
C.M3<T>(T t) -> void
~D<T>
D<T>.D() -> void
E
E.E() -> void
~E.F<T>
E.F<T>.F() -> void
";

            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText,
                GetCSharpResultAt(4, 11 + EnabledModifierCSharp.Length, ObliviousApiRule, "M<T>"),
                GetCSharpResultAt(11, 8 + EnabledModifierCSharp.Length, ObliviousApiRule, "D<T>"),
                GetCSharpResultAt(12, 25 + EnabledModifierCSharp.Length, ObliviousApiRule, "F<T>")
                );
        }

        [Fact]
        public async Task ObliviousMember_AlreadyMarkedAsOblivious_TypeParametersWithNotNullConstraintAsync()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} void M<T>(T t) where T : notnull { }

                #nullable enable
                    {{EnabledModifierCSharp}} void M2<T>(T t) where T : notnull { }
                #nullable disable
                }
                """;

            var shippedText = @"#nullable enable
C
C.C() -> void
C.M<T>(T t) -> void
C.M2<T>(T t) -> void
";

            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact]
        public async Task ObliviousMember_AlreadyMarkedAsOblivious_TypeParametersWithMiscConstraintsAsync()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} interface I { }
                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} void M1<T>() where T : I { }
                    {{EnabledModifierCSharp}} void M2<T>() where T : C { }
                    {{EnabledModifierCSharp}} void M3<T, U>() where T : U where U : class { }

                #nullable enable
                    {{EnabledModifierCSharp}} void M1b<T>() where T : I { }
                    {{EnabledModifierCSharp}} void M2b<T>() where T : C? { }
                    {{EnabledModifierCSharp}} void M3b<T, U>() where T : U where U : class { }
                #nullable disable
                }
                """;

            var shippedText = @"#nullable enable
I
C
C.C() -> void
~C.M1<T>() -> void
~C.M2<T>() -> void
~C.M3<T, U>() -> void
C.M1b<T>() -> void
C.M2b<T>() -> void
C.M3b<T, U>() -> void
";

            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText,
                GetCSharpResultAt(5, 11 + EnabledModifierCSharp.Length, ObliviousApiRule, "M1<T>"),
                GetCSharpResultAt(6, 11 + EnabledModifierCSharp.Length, ObliviousApiRule, "M2<T>"),
                GetCSharpResultAt(7, 11 + EnabledModifierCSharp.Length, ObliviousApiRule, "M3<T, U>")
                );
        }

        [Fact]
        public async Task ObliviousMember_AlreadyMarkedAsOblivious_TypeParametersWithMiscConstraints2Async()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} interface I<T> { }
                {{EnabledModifierCSharp}} class C
                {
                #nullable enable
                    {{EnabledModifierCSharp}} void M1<T>() where T : I<
                #nullable disable
                        string
                #nullable enable
                            > { }
                #nullable disable
                }

                """;

            var shippedText = @"#nullable enable
I<T>
C
C.C() -> void
~C.M1<T>() -> void
";

            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText,
                GetCSharpResultAt(6, 11 + EnabledModifierCSharp.Length, ObliviousApiRule, "M1<T>")
                );
        }

        [Fact]
        public async Task ObliviousMember_NestedEnumIsNotObliviousAsync()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} enum E
                    {
                        None,
                        Some
                    }
                }
                """;

            var shippedText = @"#nullable enable
C
C.C() -> void
C.E
C.E.None = 0 -> C.E
C.E.Some = 1 -> C.E";

            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact]
        public async Task NestedEnumIsNotObliviousAsync()
        {
            var source = $$"""

                #nullable enable
                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} enum E
                    {
                        None,
                        Some
                    }
                }
                """;

            var shippedText = @"#nullable enable
C
C.C() -> void
C.E
C.E.None = 0 -> C.E
C.E.Some = 1 -> C.E";

            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact]
        public async Task ObliviousTypeArgumentInContainingTypeAsync()
        {
            var source = $$"""

                #nullable enable
                {{EnabledModifierCSharp}} class C<T>
                {
                    {{EnabledModifierCSharp}} struct Nested { }

                    {{EnabledModifierCSharp}} C<
                #nullable disable
                        string
                #nullable enable
                            >.Nested field;
                }
                """;

            var shippedText = @"#nullable enable
C<T>
C<T>.C() -> void
C<T>.Nested
C<T>.Nested.Nested() -> void
~C<T>.field -> C<string>.Nested";

            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText,
                GetCSharpResultAt(11, 22, ObliviousApiRule, "field")
                );
        }

        [Fact]
        public async Task ImplicitContainingType_TClassAsync()
        {
            var source = $$"""

                #nullable enable
                {{EnabledModifierCSharp}} class C<T> where T : class
                {
                    {{EnabledModifierCSharp}} struct Nested { }

                    {{EnabledModifierCSharp}} Nested field;
                    {{EnabledModifierCSharp}} C<T>.Nested field2;
                }

                """;

            var shippedText = @"#nullable enable
C<T>
C<T>.C() -> void
C<T>.Nested
C<T>.Nested.Nested() -> void
~C<T>.field -> C<T>.Nested
C<T>.field2 -> C<T!>.Nested";

            var unshippedText = @"";

            // Note: although the code is entirely nullable-enabled, the compiler uses a containing type that is
            // `C<T~>` so there is an oblivious symbol. This only happens when the type parameter is constrained
            // such that it could be annotated in C# 8 (`T?` would have been allowed).
            //
            // One recourse is to use a suppression around such APIs:
            // #pragma warning disable RS0041 // uses oblivious reference types
            //
            // Another recourse is to make the containing type explicit: `C<T>.Nested`
            await VerifyCSharpAsync(source, shippedText, unshippedText,
                // /0/Test0.cs(7,19): warning RS0041: Symbol 'field' uses some oblivious reference types.
                GetCSharpResultAt(7, 13 + EnabledModifierCSharp.Length, ObliviousApiRule, "field")
                );
        }

        [Fact]
        public async Task ImplicitContainingType_TOpenAsync()
        {
            var source = $$"""

                #nullable enable
                {{EnabledModifierCSharp}} class C<T>
                {
                    {{EnabledModifierCSharp}} struct Nested { }

                    {{EnabledModifierCSharp}} Nested field;
                    {{EnabledModifierCSharp}} Nested field2;
                }
                """;

            var shippedText = @"#nullable enable
C<T>
C<T>.C() -> void
C<T>.Nested
C<T>.Nested.Nested() -> void
C<T>.field -> C<T>.Nested
C<T>.field2 -> C<T>.Nested";

            var unshippedText = @"";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact]
        public async Task SkippedNamespace_ExactMatches()
        {
            var source = $$"""
                namespace My.Namespace
                {
                    {{EnabledModifierCSharp}} class C
                    {
                        {{EnabledModifierCSharp}} C() { }
                    }
                }
                namespace Other.Namespace
                {
                    {{EnabledModifierCSharp}} class D
                    {
                        {{EnabledModifierCSharp}} D() { }
                    }
                }
                """;

            string? shippedText = null;
            string? unshippedText = null;

            await VerifyCSharpAsync(source, shippedText, unshippedText, $"[*]\r\ndotnet_public_api_analyzer.skip_namespaces = My.Namespace",
                // /0/Test0.cs(10,18): warning RS0016: Symbol 'Other.Namespace.D' is not part of the declared public API
                GetCSharpResultAt(10, 12 + EnabledModifierCSharp.Length, DeclareNewApiRule, "Other.Namespace.D"),
                // /0/Test0.cs(12,16): warning RS0016: Symbol 'Other.Namespace.D.D() -> void' is not part of the declared public API
                GetCSharpResultAt(12, 10 + EnabledModifierCSharp.Length, DeclareNewApiRule, "Other.Namespace.D.D() -> void"));
        }

        [Fact]
        public async Task SkippedNamespace_ShorterSpecifiedNamespace()
        {
            var source = $$"""
                namespace My.Namespace
                {
                    {{EnabledModifierCSharp}} class C
                    {
                        {{EnabledModifierCSharp}} C() { }
                    }
                }
                """;

            string? shippedText = null;
            string? unshippedText = null;

            await VerifyCSharpAsync(source, shippedText, unshippedText, $"[*]\r\ndotnet_public_api_analyzer.skip_namespaces = My");
        }

        [Fact]
        public async Task SkippedNamespace_MoreDerivedNamespace()
        {
            var source = $$"""
                namespace My.Namespace
                {
                    {{EnabledModifierCSharp}} class C
                    {
                        {{EnabledModifierCSharp}} C() { }
                    }
                }
                """;

            string? shippedText = null;
            string? unshippedText = null;

            await VerifyCSharpAsync(source, shippedText, unshippedText, $"[*]\r\ndotnet_public_api_analyzer.skip_namespaces = My.Namespace.Longer",
                // /0/Test0.cs(3,18): warning RS0016: Symbol 'My.Namespace.C' is not part of the declared public API
                GetCSharpResultAt(line: 3, 12 + EnabledModifierCSharp.Length, DeclareNewApiRule, "My.Namespace.C"),
                // /0/Test0.cs(5,16): warning RS0016: Symbol 'My.Namespace.C.C() -> void' is not part of the declared public API
                GetCSharpResultAt(5, 10 + EnabledModifierCSharp.Length, DeclareNewApiRule, "My.Namespace.C.C() -> void"));
        }

        [Fact]
        public async Task SkippedNamespace_PartialLocations()
        {
            var source = $$"""
                namespace My.Namespace
                {
                    {{EnabledModifierCSharp}} partial class C
                    {
                    }
                }
                """;

            var addSources = (SourceFileList sources) =>
            {
                sources.Add((filename: $"/path1/Test1.cs", source));
                sources.Add((filename: $"/path2/Test2.cs", source));
            };

            string? shippedText = null;
            string? unshippedText = null;

            await VerifyCSharpAsync(addSources, shippedText, unshippedText, $"[path1/**.cs]\r\ndotnet_public_api_analyzer.skip_namespaces = My.Namespace");
        }

        [Fact]
        public async Task ShippedTextWithMissingImplicitRecordMembers()
        {
            var source = $$"""
                #nullable enable
                {{EnabledModifierCSharp}} record C(int X, string Y)
                {
                }

                namespace System.Runtime.CompilerServices
                {
                    internal static class IsExternalInit
                    {
                    }
                }
                """;

            var shippedText = """
                #nullable enable
                C
                C.C(C! original) -> void
                C.C(int X, string! Y) -> void
                virtual C.<Clone>$() -> C!
                C.Deconstruct(out int X, out string! Y) -> void
                override C.Equals(object? obj) -> bool
                override C.GetHashCode() -> int
                override C.ToString() -> string!
                static C.operator !=(C? left, C? right) -> bool
                static C.operator ==(C? left, C? right) -> bool
                virtual C.EqualityContract.get -> System.Type!
                virtual C.Equals(C? other) -> bool
                virtual C.PrintMembers(System.Text.StringBuilder! builder) -> bool
                C.X.get -> int
                C.X.init -> void
                C.Y.get -> string!
                C.Y.init -> void
                """;

            if (EnabledModifierCSharp == "internal")
            {
                shippedText += $"\r\nSystem.Runtime.CompilerServices.IsExternalInit";
            }

            var unshippedText = string.Empty;

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact]
        public async Task ShippedTextWithMissingDelegate()
        {
            var source = $$"""
                #nullable enable
                {{EnabledModifierCSharp}} delegate void D(int X, string Y);
                """;

            var shippedText = """
                #nullable enable
                D
                virtual D.Invoke(int X, string! Y) -> void
                """;

            var unshippedText = string.Empty;

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        #endregion

        #region Fix tests

        [Fact]
        public async Task ShippedTextWithMissingImplicitStructConstructorAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} struct {|{{AddNewApiId}}:C|}
                {
                }
                """;

            var shippedText = @"
C";
            var unshippedText = string.Empty;
            var fixedUnshippedText = "C.C() -> void";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task ShippedTextWithMissingImplicitStructConstructorWithExplicitPrivateCtorWithParametersAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} struct {|{{AddNewApiId}}:C|}
                {
                    private C(string x) {}
                }
                """;

            var shippedText = @"
C";
            var unshippedText = string.Empty;
            var fixedUnshippedText = "C.C() -> void";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task ShippedTextWithMissingImplicitStructConstructorWithOtherOverloadsAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} struct {|{{AddNewApiId}}:C|}
                {
                    {{EnabledModifierCSharp}} C(int value)
                    {
                    }
                }
                """;

            var shippedText = @"
C
C.C(int value) -> void";
            var unshippedText = string.Empty;
            var fixedUnshippedText = "C.C() -> void";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        [WorkItem(2622, "https://github.com/dotnet/roslyn-analyzers/issues/2622")]
        public async Task AnalyzerFileMissing_Both_FixAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} class {|{{AddNewApiId}}:C|}
                {
                    private C() { }
                }
                """;

            string? shippedText = null;
            string? unshippedText = null;
            var fixedUnshippedText = @"C";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestSimpleMissingMember_FixAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} int Field;
                    {{EnabledModifierCSharp}} int Property { get; set; }
                    {{EnabledModifierCSharp}} void Method() { }
                    {{EnabledModifierCSharp}} int ArrowExpressionProperty => 0;

                    {{EnabledModifierCSharp}} int {|{{AddNewApiId}}:NewField|}; // Newly added field, not in current public API.
                }
                """;

            var shippedText = @"";
            var unshippedText = @"C
C.ArrowExpressionProperty.get -> int
C.C() -> void
C.Field -> int
C.Method() -> void
C.Property.get -> int
C.Property.set -> void";
            var fixedUnshippedText = @"C
C.ArrowExpressionProperty.get -> int
C.C() -> void
C.Field -> int
C.Method() -> void
C.NewField -> int
C.Property.get -> int
C.Property.set -> void";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Theory]
        [WorkItem(4749, "https://github.com/dotnet/roslyn-analyzers/issues/4749")]
        [InlineData("\r\n")] // Windows line ending.
        [InlineData("\n")] // Linux line ending.
        public async Task TestUseExistingLineEndingsAsync(string lineEnding)
        {
            var source = $$"""
                {{EnabledModifierCSharp}} class C
                {
                    private C() { }
                    {{EnabledModifierCSharp}} int Field1;
                    {{EnabledModifierCSharp}} int Field2;
                    {{EnabledModifierCSharp}} int {|{{AddNewApiId}}:Field3|}; // Newly added field, not in current public API.
                }
                """;

            var shippedText = @"";
            var unshippedText = $"C{lineEnding}C.Field1 -> int{lineEnding}C.Field2 -> int";
            var fixedUnshippedText = $"C{lineEnding}C.Field1 -> int{lineEnding}C.Field2 -> int{lineEnding}C.Field3 -> int";
            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        [WorkItem(4749, "https://github.com/dotnet/roslyn-analyzers/issues/4749")]
        public async Task TestUseOSLineEndingAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} class C
                {
                    private C() { }
                    {{EnabledModifierCSharp}} int {|{{AddNewApiId}}:Field1|}; // Newly added field, not in current public API.
                }
                """;
            var shippedText = @"";
            var unshippedText = $"C";
            var fixedUnshippedText = $"C{Environment.NewLine}C.Field1 -> int";
            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestSimpleMissingMember_Fix_WithoutNullabilityAsync()
        {
            var source = $$"""
                #nullable enable
                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} string? {|{{ShouldAnnotateApiFilesId}}:{|{{AddNewApiId}}:NewField|}|}; // Newly added field, not in current public API.
                }
                """;

            var shippedText = @"";
            var unshippedText = @"C
C.C() -> void";
            var fixedUnshippedText = @"C
C.C() -> void
C.NewField -> string";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [InlineData(0)]
        [InlineData(1)]
        [Theory]
        public async Task TestSimpleMissingMember_Fix_WithoutNullability_MultipleFilesAsync(int index)
        {
            var source = $$"""
                #nullable enable
                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} string? {|{{ShouldAnnotateApiFilesId}}:{|{{AddNewApiId}}:NewField|}|}; // Newly added field, not in current public API.
                }
                """;

            var shippedText = @"";
            var unshippedText1 = @"C
C.C() -> void";
            var unshippedText2 = @"";
            var fixedUnshippedText1_index0 = @"C
C.C() -> void
C.NewField -> string";
            var fixedUnshippedText1_index1 = "C.NewField -> string";

            var unshippedTextName2 = UnshippedFileNamePrefix + "test" + DeclarePublicApiAnalyzer.Extension;

            var test = new CSharpCodeFixTest<DeclarePublicApiAnalyzer, DeclarePublicApiFix, DefaultVerifier>();

            test.TestState.Sources.Add(source);
            test.TestState.AdditionalFiles.Add((ShippedFileName, shippedText));
            test.TestState.AdditionalFiles.Add((UnshippedFileName, unshippedText1));
            test.TestState.AdditionalFiles.Add((unshippedTextName2, unshippedText2));

            test.CodeActionIndex = index;
            test.FixedState.AdditionalFiles.Add((ShippedFileName, shippedText));

            if (index == 0)
            {
                test.FixedState.AdditionalFiles.Add((UnshippedFileName, fixedUnshippedText1_index0));
                test.FixedState.AdditionalFiles.Add((unshippedTextName2, unshippedText2));
            }
            else if (index == 1)
            {
                test.FixedState.AdditionalFiles.Add((UnshippedFileName, unshippedText1));
                test.FixedState.AdditionalFiles.Add((unshippedTextName2, fixedUnshippedText1_index1));
            }
            else
            {
                throw new NotSupportedException();
            }

            await test.RunAsync();
        }

        [Fact]
        public async Task TestSimpleMissingMember_Fix_WithNullabilityAsync()
        {
            var source = $$"""
                #nullable enable
                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} string? {|{{AddNewApiId}}:NewField|}; // Newly added field, not in current public API.
                }
                """;

            var shippedText = $@"#nullable enable";
            var unshippedText = @"C
C.C() -> void";
            var fixedUnshippedText = @"C
C.C() -> void
C.NewField -> string?";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestSimpleMissingMember_Fix_WithNullability2Async()
        {
            var source = $$"""
                #nullable enable
                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} string? OldField;
                    {{EnabledModifierCSharp}} string? {|{{AddNewApiId}}:NewField|}; // Newly added field, not in current public API.
                }
                """;
            var shippedText = $@"#nullable enable";
            var unshippedText = @"C
C.C() -> void
C.OldField -> string?";
            var fixedUnshippedText = @"C
C.C() -> void
C.NewField -> string?
C.OldField -> string?";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestSimpleMissingMember_Fix_WithNullability3Async()
        {
            var source = $$"""
                #nullable enable
                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} string? OldField;
                    {{EnabledModifierCSharp}} string? NewField;
                }
                """;
            var shippedText = $@"#nullable enable
C
C.C() -> void
C.NewField -> string?
C.OldField -> string?";

            var unshippedText = "";

            await VerifyCSharpAsync(source, shippedText, unshippedText);
        }

        [Fact]
        public async Task TestAddAndRemoveMembers_CSharp_Fix_WithRemovedNullabilityAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} string {|{{ObliviousApiId}}:{|{{AddNewApiId}}:ChangedField|}|}; // oblivious
                }
                """;
            var shippedText = $@"#nullable enable";
            var unshippedText = $$"""
                C
                C.C() -> void
                {|{{RemoveApiId}}:C.ChangedField -> string?|}
                """;
            var fixedUnshippedText = @"C
C.C() -> void
~C.ChangedField -> string";
            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact, WorkItem(3793, "https://github.com/dotnet/roslyn-analyzers/issues/3793")]
        public async Task ObliviousApiDiagnosticInGeneratedFileStillWarnAsync()
        {
            // We complain about oblivious APIs in generated files too (no special treatment)
            var source = $$"""

                // <autogenerated />
                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} string ObliviousField;
                }
                """;
            var shippedText = "#nullable enable";
            var unshippedText = @"C
C.C() -> void
C.ObliviousField -> string";
            await VerifyCSharpAsync(source, shippedText, unshippedText,
                // /0/Test0.cs(5,19): warning RS0036: Symbol 'C.ObliviousField -> string' is missing nullability annotations in the declared API.
                GetCSharpResultAt(5, 13 + EnabledModifierCSharp.Length, AnnotateApiRule, "C.ObliviousField -> string"),
                // /0/Test0.cs(5,19): warning RS0041: Symbol 'ObliviousField' uses some oblivious reference types.
                GetCSharpResultAt(5, 13 + EnabledModifierCSharp.Length, ObliviousApiRule, "ObliviousField")
                );
        }

        [Fact, WorkItem(3672, "https://github.com/dotnet/roslyn-analyzers/issues/3672")]
        public async Task TypeArgumentRefersToTypeParameter_OnMethodAsync()
        {
            var source = $$"""

                #nullable enable
                {{EnabledModifierCSharp}} static class C
                {
                    {{EnabledModifierCSharp}} static void M<T>()
                        where T : System.IComparable<T>
                    {
                    }
                }
                """;
            var shippedText = "#nullable enable";
            var unshippedText = @"";
            await VerifyCSharpAsync(source, shippedText, unshippedText,
                // /0/Test0.cs(3,21): warning RS0016: Symbol 'C' is not part of the declared API.
                GetCSharpResultAt(3, 15 + EnabledModifierCSharp.Length, DeclareNewApiRule, "C"),
                // /0/Test0.cs(5,24): warning RS0016: Symbol 'static C.M<T>() -> void' is not part of the declared API.
                GetCSharpResultAt(5, 18 + EnabledModifierCSharp.Length, DeclareNewApiRule, "static C.M<T>() -> void")
                );
        }

        [Fact, WorkItem(3672, "https://github.com/dotnet/roslyn-analyzers/issues/3672")]
        public async Task TypeArgumentRefersToTypeParameter_OnTypeAsync()
        {
            var source = $$"""

                #nullable enable
                {{EnabledModifierCSharp}} static class C<T>
                    where T : System.IComparable<T>
                {
                }
                """;
            var shippedText = "#nullable enable";
            var unshippedText = @"";
            await VerifyCSharpAsync(source, shippedText, unshippedText,
                // /0/Test0.cs(3,21): warning RS0016: Symbol 'C<T>' is not part of the declared API.
                GetCSharpResultAt(3, 15 + EnabledModifierCSharp.Length, DeclareNewApiRule, "C<T>")
                );
        }

        [Fact, WorkItem(3672, "https://github.com/dotnet/roslyn-analyzers/issues/3672")]
        public async Task TypeArgumentRefersToTypeParameter_OnType_SecondTypeArgumentAsync()
        {
            var source = $$"""

                #nullable enable
                {{EnabledModifierCSharp}} static class C<T1, T2>
                    where T1 : class
                    where T2 : System.IComparable<
                #nullable disable
                        T1
                #nullable enable
                        >
                {
                }
                """;
            var shippedText = "#nullable enable";
            var unshippedText = @"";
            await VerifyCSharpAsync(source, shippedText, unshippedText,
                // /0/Test0.cs(3,21): warning RS0016: Symbol '~C<T1, T2>' is not part of the declared API.
                GetCSharpResultAt(3, 15 + EnabledModifierCSharp.Length, DeclareNewApiRule, "~C<T1, T2>"),
                // /0/Test0.cs(3,21): warning RS0041: Symbol 'C<T1, T2>' uses some oblivious reference types.
                GetCSharpResultAt(3, 15 + EnabledModifierCSharp.Length, ObliviousApiRule, "C<T1, T2>")
                );
        }

        [Fact, WorkItem(3672, "https://github.com/dotnet/roslyn-analyzers/issues/3672")]
        public async Task TypeArgumentRefersToTypeParameter_OnType_ObliviousReferenceAsync()
        {
            var source = $$"""

                #nullable enable
                {{EnabledModifierCSharp}} static class C<T>
                    where T : class, System.IComparable<
                #nullable disable
                        T
                #nullable enable
                >
                {
                }

                """;
            var shippedText = "#nullable enable";
            var unshippedText = @"";
            await VerifyCSharpAsync(source, shippedText, unshippedText,
                // /0/Test0.cs(3,21): warning RS0016: Symbol '~C<T>' is not part of the declared API.
                GetCSharpResultAt(3, 15 + EnabledModifierCSharp.Length, DeclareNewApiRule, "~C<T>"),
                // /0/Test0.cs(3,21): warning RS0041: Symbol 'C<T>' uses some oblivious reference types.
                GetCSharpResultAt(3, 15 + EnabledModifierCSharp.Length, ObliviousApiRule, "C<T>")
                );
        }

        [Fact]
        public async Task ApiFileShippedWithDuplicateNullableEnableAsync()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                }

                """;

            string shippedText = $@"
#nullable enable
#nullable enable
";

            string unshippedText = $@"";

            var expected = new DiagnosticResult(ApiFilesInvalid)
                .WithArguments(DeclarePublicApiAnalyzer.InvalidReasonMisplacedNullableEnable);
            await VerifyCSharpAsync(source, shippedText, unshippedText, expected);
        }

        [Fact]
        public async Task ApiFileUnshippedWithDuplicateNullableEnableAsync()
        {
            var source = $$"""

                {{EnabledModifierCSharp}} class C
                {
                }
                """;

            string shippedText = $@"";

            string unshippedText = $@"
#nullable enable
#nullable enable
";

            var expected = new DiagnosticResult(ApiFilesInvalid)
                .WithArguments(DeclarePublicApiAnalyzer.InvalidReasonMisplacedNullableEnable);
            await VerifyCSharpAsync(source, shippedText, unshippedText, expected);
        }

        [Fact]
        public async Task ApiFileShippedWithoutNullableEnable_AvoidUnnecessaryDiagnosticAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} class C
                {
                }
                """;

            string shippedText = $@"C
C.C() -> void";

            string unshippedText = $@"";

            // Only oblivious APIs, so no need to warn about lack of '#nullable enable'
            await VerifyCSharpAsync(source, shippedText, unshippedText, System.Array.Empty<DiagnosticResult>());
        }

        [Fact]
        public async Task TestAddAndRemoveMembers_CSharp_FixAsync()
        {
            // Unshipped file has a state 'ObsoleteField' entry and a missing 'NewField' entry.
            var source = $$"""
                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} int Field;
                    {{EnabledModifierCSharp}} int Property { get; set; }
                    {{EnabledModifierCSharp}} void Method() { }
                    {{EnabledModifierCSharp}} int ArrowExpressionProperty => 0;

                    {{EnabledModifierCSharp}} int {|{{AddNewApiId}}:NewField|};
                }
                """;
            var shippedText = @"";
            var unshippedText = $$"""
                C
                C.ArrowExpressionProperty.get -> int
                C.C() -> void
                C.Field -> int
                C.Method() -> void
                {|{{RemoveApiId}}:C.ObsoleteField -> int|}
                C.Property.get -> int
                C.Property.set -> void
                """;
            var fixedUnshippedText = @"C
C.ArrowExpressionProperty.get -> int
C.C() -> void
C.Field -> int
C.Method() -> void
C.NewField -> int
C.Property.get -> int
C.Property.set -> void";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestSimpleMissingType_FixAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} class {|{{AddNewApiId}}:C|}
                {
                    private C() { }
                }
                """;

            var shippedText = @"";
            var unshippedText = @"";
            var fixedUnshippedText = @"C";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestMultipleMissingTypeAndMember_FixAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} class {|{{AddNewApiId}}:C|}
                {
                    private C() { }
                    {{EnabledModifierCSharp}} int {|{{AddNewApiId}}:Field|};
                }

                {{EnabledModifierCSharp}} class {|{{AddNewApiId}}:{|{{AddNewApiId}}:C2|}|} { }
                """;

            var shippedText = @"";
            var unshippedText = @"";
            var fixedUnshippedText = @"C
C.Field -> int
C2
C2.C2() -> void";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestMultipleMissingTypeAndMember_CaseSensitiveFixAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} class {|{{AddNewApiId}}:C|}
                {
                    private C() { }
                    {{EnabledModifierCSharp}} int {|{{AddNewApiId}}:Field_A|};
                    {{EnabledModifierCSharp}} int {|{{AddNewApiId}}:Field_b|};
                    {{EnabledModifierCSharp}} int {|{{AddNewApiId}}:Field_C|};
                    {{EnabledModifierCSharp}} int {|{{AddNewApiId}}:Field_d|};
                }

                {{EnabledModifierCSharp}} class {|{{AddNewApiId}}:{|{{AddNewApiId}}:C2|}|} { }
                """;

            var shippedText = @"";
            var unshippedText = @"";
            var fixedUnshippedText = @"C
C.Field_A -> int
C.Field_b -> int
C.Field_C -> int
C.Field_d -> int
C2
C2.C2() -> void";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestChangingMethodSignatureForAnUnshippedMethod_FixAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} class C
                {
                    private C() { }
                    {{EnabledModifierCSharp}} void {|{{AddNewApiId}}:Method|}(int p1){ }
                }
                """;

            var shippedText = @"C";
            // previously method had no params, so the fix should remove the previous overload.
            var unshippedText = $$"""{|{{RemoveApiId}}:C.Method() -> void|}""";
            var fixedUnshippedText = @"C.Method(int p1) -> void";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestChangingMethodSignatureForAnUnshippedMethod_Fix_WithNullabilityAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} class C
                {
                    private C() { }
                    {{EnabledModifierCSharp}} void {|{{AddNewApiId}}:Method|}(object? p1){ }
                }
                """;

            var shippedText = $@"#nullable enable
C";
            // previously method had no params, so the fix should remove the previous overload.
            var unshippedText = $$"""{|{{RemoveApiId}}:C.Method(string p1) -> void|}""";
            var fixedUnshippedText = @"C.Method(object? p1) -> void";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestChangingMethodSignatureForAnUnshippedMethodWithShippedOverloads_FixAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} class C
                {
                    private C() { }
                    {{EnabledModifierCSharp}} void Method(int p1){ }
                    {{EnabledModifierCSharp}} void Method(int p1, int p2){ }
                    {{EnabledModifierCSharp}} void {|{{AddNewApiId}}:Method|}(char p1){ }
                }
                """;

            var shippedText = @"C
C.Method(int p1) -> void
C.Method(int p1, int p2) -> void";
            // previously method had no params, so the fix should remove the previous overload.
            var unshippedText = $$"""{|{{RemoveApiId}}:C.Method() -> void|}""";
            var fixedUnshippedText = @"C.Method(char p1) -> void";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestAddingNewPublicOverload_FixAsync()
        {
            var source = $$"""
                public class C
                {
                    private C() { }
                    {{EnabledModifierCSharp}} void {|{{AddNewApiId}}:Method|}(){ }
                    {{DisabledModifierCSharp}} void Method(int p1){ }
                    {{DisabledModifierCSharp}} void Method(int p1, int p2){ }
                    {{EnabledModifierCSharp}} void Method(char p1){ }
                }
                """;

            var shippedText = @"";
            var unshippedText = IsInternalTest
                ? """
                C.Method(char p1) -> void
                """
                : """
                C
                C.Method(char p1) -> void
                """;
            var fixedUnshippedText = IsInternalTest
                ? """
                C.Method() -> void
                C.Method(char p1) -> void
                """
                : """
                C
                C.Method() -> void
                C.Method(char p1) -> void
                """;

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestMissingTypeAndMemberAndNestedMembers_FixAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} class {|{{AddNewApiId}}:C|}
                {
                    private C() { }
                    {{EnabledModifierCSharp}} int {|{{AddNewApiId}}:Field|};

                    {{EnabledModifierCSharp}} class CC
                    {
                        public int {|{{AddNewApiId}}:Field|};
                    }
                }

                {{EnabledModifierCSharp}} class {|{{AddNewApiId}}:{|{{AddNewApiId}}:C2|}|} { }
                """;

            var shippedText = @"C.CC
C.CC.CC() -> void";
            var unshippedText = @"";
            var fixedUnshippedText = @"C
C.CC.Field -> int
C.Field -> int
C2
C2.C2() -> void";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestMissingNestedGenericMembersAndStaleMembers_FixAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} class {|{{AddNewApiId}}:C|}
                {
                    private C() { }
                    {{EnabledModifierCSharp}} CC<int> {|{{AddNewApiId}}:Field|};
                    private C3.C4 Field2;
                    private C3.C4 Method(C3.C4 p1) { throw new System.NotImplementedException(); }

                    {{EnabledModifierCSharp}} class CC<T>
                    {
                        {{EnabledModifierCSharp}} int {|{{AddNewApiId}}:Field|};
                        {{EnabledModifierCSharp}} CC<int> {|{{AddNewApiId}}:Field2|};
                    }

                    {{EnabledModifierCSharp}} class C3
                    {
                        {{EnabledModifierCSharp}} class C4 { }
                    }
                }

                {{EnabledModifierCSharp}} class {|{{AddNewApiId}}:{|{{AddNewApiId}}:C2|}|} { }
                """;

            var shippedText = @"";
            var unshippedText = $$"""
                C.C3
                C.C3.C3() -> void
                C.C3.C4
                C.C3.C4.C4() -> void
                C.CC<T>
                C.CC<T>.CC() -> void
                {|{{RemoveApiId}}:C.Field2 -> C.C3.C4|}
                {|{{RemoveApiId}}:C.Method(C.C3.C4 p1) -> C.C3.C4|}
                """;
            var fixedUnshippedText = """
                C
                C.C3
                C.C3.C3() -> void
                C.C3.C4
                C.C3.C4.C4() -> void
                C.CC<T>
                C.CC<T>.CC() -> void
                C.CC<T>.Field -> int
                C.CC<T>.Field2 -> C.CC<int>
                C.Field -> C.CC<int>
                C2
                C2.C2() -> void
                """;

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestWithExistingUnshippedNestedMembers_FixAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} class {|{{AddNewApiId}}:C|}
                {
                    private C() { }
                    {{EnabledModifierCSharp}} int {|{{AddNewApiId}}:Field|};

                    {{EnabledModifierCSharp}} class CC
                    {
                        {{EnabledModifierCSharp}} int Field;
                    }
                }

                {{EnabledModifierCSharp}} class {|{{AddNewApiId}}:{|{{AddNewApiId}}:C2|}|} { }
                """;

            var shippedText = @"";
            var unshippedText = @"C.CC
C.CC.CC() -> void
C.CC.Field -> int";
            var fixedUnshippedText = @"C
C.CC
C.CC.CC() -> void
C.CC.Field -> int
C.Field -> int
C2
C2.C2() -> void";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestWithExistingUnshippedNestedGenericMembers_FixAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} class C
                {
                    private C() { }
                    {{EnabledModifierCSharp}} class {|{{AddNewApiId}}:CC|}
                    {
                        {{EnabledModifierCSharp}} int Field;
                    }

                    {{EnabledModifierCSharp}} class CC<T>
                    {
                        private CC() { }
                        {{EnabledModifierCSharp}} int Field;
                    }
                }
                """;

            var shippedText = @"";
            var unshippedText = @"C
C.CC
C.CC.Field -> int
C.CC<T>
C.CC<T>.Field -> int";
            var fixedUnshippedText = @"C
C.CC
C.CC.CC() -> void
C.CC.Field -> int
C.CC<T>
C.CC<T>.Field -> int";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestWithExistingShippedNestedMembers_FixAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} class {|{{AddNewApiId}}:C|}
                {
                    private C() { }
                    {{EnabledModifierCSharp}} int {|{{AddNewApiId}}:Field|};

                    {{EnabledModifierCSharp}} class CC
                    {
                        {{EnabledModifierCSharp}} int Field;
                    }
                }

                {{EnabledModifierCSharp}} class {|{{AddNewApiId}}:{|{{AddNewApiId}}:C2|}|} { }
                """;

            var shippedText = @"C.CC
C.CC.CC() -> void
C.CC.Field -> int";
            var unshippedText = @"";
            var fixedUnshippedText = @"C
C.Field -> int
C2
C2.C2() -> void";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        public async Task TestOnlyRemoveStaleSiblingEntries_FixAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} class {|{{AddNewApiId}}:C|}
                {
                    private C() { }
                    {{EnabledModifierCSharp}} int {|{{AddNewApiId}}:Field|};

                    {{EnabledModifierCSharp}} class CC
                    {
                        private int Field; // This has a stale public API entry, but this shouldn't be removed unless we attempt to add a public API entry for a sibling.
                    }
                }

                {{EnabledModifierCSharp}} class {|{{AddNewApiId}}:{|{{AddNewApiId}}:C2|}|} { }
                """;

            var shippedText = @"";
            var unshippedText = $$"""

                C.CC
                C.CC.CC() -> void
                {|{{RemoveApiId}}:C.CC.Field -> int|}
                """;
            var fixedUnshippedText = $$"""
                C
                C.CC
                C.CC.CC() -> void
                {|{{RemoveApiId}}:C.CC.Field -> int|}
                C.Field -> int
                C2
                C2.C2() -> void
                """;

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("\r\n", "\r\n")]
        [InlineData("\r\n\r\n", "\r\n")]
        public async Task TestPreserveTrailingNewlineAsync(string originalEndOfFile, string expectedEndOfFile)
        {
            var source = $$"""
                {{EnabledModifierCSharp}} class C
                {
                    {{EnabledModifierCSharp}} int Property { get; }

                    {{EnabledModifierCSharp}} int {|{{AddNewApiId}}:NewField|}; // Newly added field, not in current public API.
                }
                """;

            var shippedText = @"";
            var unshippedText = $@"C
C.C() -> void
C.Property.get -> int{originalEndOfFile}";
            var fixedUnshippedText = $@"C
C.C() -> void
C.NewField -> int
C.Property.get -> int{expectedEndOfFile}";

            await VerifyCSharpAdditionalFileFixAsync(
                source.ReplaceLineEndings("\r\n"),
                shippedText,
                unshippedText.ReplaceLineEndings("\r\n"),
                fixedUnshippedText.ReplaceLineEndings("\r\n"));
        }

        [Fact]
        public async Task MissingType_AAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} class {|{{AddNewApiId}}:{|{{AddNewApiId}}:A|}|} { }
                {{EnabledModifierCSharp}} class B { }
                {{EnabledModifierCSharp}} class D { }
                """;

            var unshippedText = @"B
B.B() -> void
D
D.D() -> void";

            var expectedUnshippedText = @"A
A.A() -> void
B
B.B() -> void
D
D.D() -> void";
            await VerifyCSharpAdditionalFileFixAsync(source, shippedApiText: "", oldUnshippedApiText: unshippedText, newUnshippedApiText: expectedUnshippedText);
        }

        [Fact]
        public async Task MissingType_CAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} class B { }
                {{EnabledModifierCSharp}} class {|{{AddNewApiId}}:{|{{AddNewApiId}}:C|}|} { }
                {{EnabledModifierCSharp}} class D { }
                """;

            var unshippedText = @"B
B.B() -> void
D
D.D() -> void";

            var expectedUnshippedText = @"B
B.B() -> void
C
C.C() -> void
D
D.D() -> void";
            await VerifyCSharpAdditionalFileFixAsync(source, shippedApiText: "", oldUnshippedApiText: unshippedText, newUnshippedApiText: expectedUnshippedText);
        }

        [Fact]
        public async Task MissingType_EAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} class B { }
                {{EnabledModifierCSharp}} class D { }
                {{EnabledModifierCSharp}} class {|{{AddNewApiId}}:{|{{AddNewApiId}}:E|}|} { }
                """;

            var unshippedText = @"B
B.B() -> void
D
D.D() -> void";

            var expectedUnshippedText = @"B
B.B() -> void
D
D.D() -> void
E
E.E() -> void";
            await VerifyCSharpAdditionalFileFixAsync(source, shippedApiText: "", oldUnshippedApiText: unshippedText, newUnshippedApiText: expectedUnshippedText);
        }

        [Fact]
        public async Task MissingType_Unordered_AAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} class {|{{AddNewApiId}}:{|{{AddNewApiId}}:A|}|} { }
                {{EnabledModifierCSharp}} class B { }
                {{EnabledModifierCSharp}} class D { }
                """;

            var unshippedText = @"D
D.D() -> void
B
B.B() -> void";

            var expectedUnshippedText = @"A
A.A() -> void
D
D.D() -> void
B
B.B() -> void";
            await VerifyCSharpAdditionalFileFixAsync(source, shippedApiText: "", oldUnshippedApiText: unshippedText, newUnshippedApiText: expectedUnshippedText);
        }

        [Fact]
        public async Task MissingType_Unordered_CAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} class B { }
                {{EnabledModifierCSharp}} class {|{{AddNewApiId}}:{|{{AddNewApiId}}:C|}|} { }
                {{EnabledModifierCSharp}} class D { }
                """;

            var unshippedText = @"D
D.D() -> void
B
B.B() -> void";

            var expectedUnshippedText = @"C
C.C() -> void
D
D.D() -> void
B
B.B() -> void";
            await VerifyCSharpAdditionalFileFixAsync(source, shippedApiText: "", oldUnshippedApiText: unshippedText, newUnshippedApiText: expectedUnshippedText);
        }

        [Fact]
        public async Task MissingType_Unordered_EAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} class B { }
                {{EnabledModifierCSharp}} class D { }
                {{EnabledModifierCSharp}} class {|{{AddNewApiId}}:{|{{AddNewApiId}}:E|}|} { }
                """;

            var unshippedText = @"D
D.D() -> void
B
B.B() -> void";

            var expectedUnshippedText = @"D
D.D() -> void
B
B.B() -> void
E
E.E() -> void";
            await VerifyCSharpAdditionalFileFixAsync(source, shippedApiText: "", oldUnshippedApiText: unshippedText, newUnshippedApiText: expectedUnshippedText);
        }

        [Fact, WorkItem(2195, "https://github.com/dotnet/roslyn-analyzers/issues/2195")]
        public async Task TestPartialTypeAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} partial class {|{{AddNewApiId}}:{|{{AddNewApiId}}:C|}|}
                {
                }

                {{EnabledModifierCSharp}} partial class {|{{AddNewApiId}}:{|{{AddNewApiId}}:C|}|}
                {
                }
                """;

            var shippedText = @"";
            var unshippedText = @"";
            var fixedUnshippedText = @"C
C.C() -> void";

            await VerifyCSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact, WorkItem(4133, "https://github.com/dotnet/roslyn-analyzers/issues/4133")]
        public async Task Record_ImplicitProperty_FixAsync()
        {
            var source = $$"""
                {{EnabledModifierCSharp}} record R(int {|{{AddNewApiId}}:P|});
                """;

            var shippedText = """
                #nullable enable
                R.R(R! original) -> void
                virtual R.<Clone>$() -> R!
                R.Deconstruct(out int P) -> void
                override R.Equals(object? obj) -> bool
                override R.GetHashCode() -> int
                override R.ToString() -> string!
                static R.operator !=(R? left, R? right) -> bool
                static R.operator ==(R? left, R? right) -> bool
                virtual R.EqualityContract.get -> System.Type!
                virtual R.Equals(R? other) -> bool
                virtual R.PrintMembers(System.Text.StringBuilder! builder) -> bool
                """;
            var unshippedText = """
                #nullable enable
                R
                R.R(int P) -> void
                R.P.get -> int
                """;
            var fixedUnshippedText = """
                #nullable enable
                R
                R.P.init -> void
                R.R(int P) -> void
                R.P.get -> int
                """;

            await VerifyNet50CSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Fact]
        [WorkItem(6759, "https://github.com/dotnet/roslyn-analyzers/issues/6759")]
        public async Task TestExperimentalApiAsync()
        {
            var source = $$"""
                using System.Diagnostics.CodeAnalysis;

                [Experimental("ID1")]
                {{EnabledModifierCSharp}} class {|{{AddNewApiId}}:{|{{AddNewApiId}}:C|}|}
                {
                }
                """;

            var shippedText = @"";
            var unshippedText = @"";
            var fixedUnshippedText = @"[ID1]C
[ID1]C.C() -> void";

            await VerifyNet80CSharpAdditionalFileFixAsync(source, shippedText, unshippedText, fixedUnshippedText);
        }

        [Theory]
        [InlineData("")]
        [InlineData("null")]
        [InlineData("1")]
        [InlineData("1, 2")]
        [WorkItem(6759, "https://github.com/dotnet/roslyn-analyzers/issues/6759")]
        public async Task TestExperimentalApiWithInvalidArgumentAsync(string invalidArgument)
        {
            var source = $$"""
                using System.Diagnostics.CodeAnalysis;

                [Experimental({{invalidArgument}})]
                {{EnabledModifierCSharp}} class {|{{AddNewApiId}}:{|{{AddNewApiId}}:C|}|}
                {
                }
                """;

            var shippedText = @"";
            var unshippedText = @"";
            var fixedUnshippedText = @"[???]C
[???]C.C() -> void";

            var test = new CSharpCodeFixTest<DeclarePublicApiAnalyzer, DeclarePublicApiFix, DefaultVerifier>()
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
                CompilerDiagnostics = CompilerDiagnostics.None,
                TestState =
                {
                    Sources = { source },
                    AdditionalFiles =
                    {
                        (ShippedFileName, shippedText),
                        (UnshippedFileName, unshippedText),
                    },
                },
                FixedState =
                {
                    AdditionalFiles =
                    {
                        (ShippedFileName, shippedText),
                        (UnshippedFileName, fixedUnshippedText),
                    },
                },
            };

            test.DisabledDiagnostics.AddRange(DisabledDiagnostics);

            await test.RunAsync();
        }

        #endregion
    }
}
