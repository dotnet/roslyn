// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.MakePropertyRequired;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.MakePropertyRequired
{
    using VerifyCS = CSharpCodeFixVerifier<
        EmptyDiagnosticAnalyzer,
        CSharpMakePropertyRequiredCodeFixProvider>;

    [Trait(Traits.Feature, Traits.Features.CodeActionsMakePropertyRequired)]
    public sealed class MakePropertyRequiredTests
    {
        [Fact]
        public async Task SimpleSetProperty()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    #nullable enable

                    class MyClass
                    {
                        public string {|CS8618:MyProperty|} { get; set; }
                    }
                    """,
                FixedCode = """
                    #nullable enable

                    class MyClass
                    {
                        public required string MyProperty { get; set; }
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp11,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net70
            }.RunAsync();
        }

        [Fact]
        public async Task SimpleInitProperty()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    #nullable enable

                    class MyClass
                    {
                        public string {|CS8618:MyProperty|} { get; init; }
                    }
                    """,
                FixedCode = """
                    #nullable enable

                    class MyClass
                    {
                        public required string MyProperty { get; init; }
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp11,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net70
            }.RunAsync();
        }

        [Fact]
        public async Task NotOnGetOnlyProperty()
        {
            var code = """
                #nullable enable
                
                class MyClass
                {
                    public string {|CS8618:MyProperty|} { get; }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp11,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net70
            }.RunAsync();
        }

        [Theory]
        [InlineData("public", "public", "public", true)]
        [InlineData("public", "public", "private", false)]
        [InlineData("public", "public", "protected", false)]
        [InlineData("public", "public", "internal", false)]
        [InlineData("public", "public", "private protected", false)]
        [InlineData("public", "public", "protected internal", false)]
        [InlineData("public", "private", "public", true)]
        [InlineData("public", "private", "private", false)]
        [InlineData("public", "private", "protected", false)]
        [InlineData("public", "private", "internal", true)]
        [InlineData("public", "private", "private protected", false)]
        [InlineData("public", "private", "protected internal", true)]
        [InlineData("public", "protected", "public", true)]
        [InlineData("public", "protected", "private", false)]
        [InlineData("public", "protected", "protected", false)]
        [InlineData("public", "protected", "internal", false)]
        [InlineData("public", "protected", "private protected", false)]
        [InlineData("public", "protected", "protected internal", false)]
        [InlineData("public", "internal", "public", true)]
        [InlineData("public", "internal", "private", false)]
        [InlineData("public", "internal", "protected", false)]
        [InlineData("public", "internal", "internal", true)]
        [InlineData("public", "internal", "private protected", false)]
        [InlineData("public", "internal", "protected internal", true)]
        [InlineData("public", "private protected", "public", true)]
        [InlineData("public", "private protected", "private", false)]
        [InlineData("public", "private protected", "protected", false)]
        [InlineData("public", "private protected", "internal", true)]
        [InlineData("public", "private protected", "private protected", false)]
        [InlineData("public", "private protected", "protected internal", true)]
        [InlineData("public", "protected internal", "public", true)]
        [InlineData("public", "protected internal", "private", false)]
        [InlineData("public", "protected internal", "protected", false)]
        [InlineData("public", "protected internal", "internal", false)]
        [InlineData("public", "protected internal", "private protected", false)]
        [InlineData("public", "protected internal", "protected internal", false)]
        [InlineData("internal", "public", "public", true)]
        [InlineData("internal", "public", "private", false)]
        [InlineData("internal", "public", "protected", false)]
        [InlineData("internal", "public", "internal", true)]
        [InlineData("internal", "public", "private protected", false)]
        [InlineData("internal", "public", "protected internal", true)]
        [InlineData("internal", "private", "public", true)]
        [InlineData("internal", "private", "private", false)]
        [InlineData("internal", "private", "protected", false)]
        [InlineData("internal", "private", "internal", true)]
        [InlineData("internal", "private", "private protected", false)]
        [InlineData("internal", "private", "protected internal", true)]
        [InlineData("internal", "protected", "public", true)]
        [InlineData("internal", "protected", "private", false)]
        [InlineData("internal", "protected", "protected", false)]
        [InlineData("internal", "protected", "internal", true)]
        [InlineData("internal", "protected", "private protected", false)]
        [InlineData("internal", "protected", "protected internal", true)]
        [InlineData("internal", "internal", "public", true)]
        [InlineData("internal", "internal", "private", false)]
        [InlineData("internal", "internal", "protected", false)]
        [InlineData("internal", "internal", "internal", true)]
        [InlineData("internal", "internal", "private protected", false)]
        [InlineData("internal", "internal", "protected internal", true)]
        [InlineData("internal", "private protected", "public", true)]
        [InlineData("internal", "private protected", "private", false)]
        [InlineData("internal", "private protected", "protected", false)]
        [InlineData("internal", "private protected", "internal", true)]
        [InlineData("internal", "private protected", "private protected", false)]
        [InlineData("internal", "private protected", "protected internal", true)]
        [InlineData("internal", "protected internal", "public", true)]
        [InlineData("internal", "protected internal", "private", false)]
        [InlineData("internal", "protected internal", "protected", false)]
        [InlineData("internal", "protected internal", "internal", true)]
        [InlineData("internal", "protected internal", "private protected", false)]
        [InlineData("internal", "protected internal", "protected internal", true)]
        public async Task TestEffectivePropertyAccessibility(string outerClassAccessibility, string containingTypeAccessibility, string propertyAccessibility, bool shouldOfferFix)
        {
            var code = $$"""
                #nullable enable
                
                {{outerClassAccessibility}} class C
                {
                    {{containingTypeAccessibility}} class MyClass
                    {
                        {{propertyAccessibility}} string {|CS8618:MyProperty|} { get; set; }
                    }
                }
                """;

            var fixedCode = $$"""
                #nullable enable
                
                {{outerClassAccessibility}} class C
                {
                    {{containingTypeAccessibility}} class MyClass
                    {
                        {{propertyAccessibility}} required string MyProperty { get; set; }
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = shouldOfferFix ? fixedCode : code,
                LanguageVersion = LanguageVersion.CSharp11,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net70
            }.RunAsync();
        }

        [Theory]
        [InlineData("public", "", true)]
        [InlineData("public", "internal", false)]
        [InlineData("public", "protected", false)]
        [InlineData("public", "private", false)]
        [InlineData("public", "private protected", false)]
        [InlineData("public", "protected internal", false)]
        [InlineData("internal", "", true)]
        [InlineData("internal", "internal", true)]
        [InlineData("internal", "protected", false)]
        [InlineData("internal", "private", false)]
        [InlineData("internal", "private protected", false)]
        [InlineData("internal", "protected internal", true)]
        public async Task TestSetAccessorAccessibility(string containingTypeAccessibility, string setAccessorAccessibility, bool shouldOfferFix)
        {
            var code = $$"""
                #nullable enable
                
                {{containingTypeAccessibility}} class MyClass
                {
                    public string {|CS8618:MyProperty|} { get; {{setAccessorAccessibility}} set; }
                }
                """;

            var fixedCode = $$"""
                #nullable enable
                
                {{containingTypeAccessibility}} class MyClass
                {
                    public required string MyProperty { get; {{setAccessorAccessibility}} set; }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = shouldOfferFix ? fixedCode : code,
                LanguageVersion = LanguageVersion.CSharp11,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net70
            }.RunAsync();
        }

        [Theory]
        [InlineData("public", "", true)]
        [InlineData("public", "internal", false)]
        [InlineData("public", "protected", false)]
        [InlineData("public", "private", false)]
        [InlineData("public", "private protected", false)]
        [InlineData("public", "protected internal", false)]
        [InlineData("internal", "", true)]
        [InlineData("internal", "internal", true)]
        [InlineData("internal", "protected", false)]
        [InlineData("internal", "private", false)]
        [InlineData("internal", "private protected", false)]
        [InlineData("internal", "protected internal", true)]
        public async Task TestInitAccessorAccessibility(string containingTypeAccessibility, string setAccessorAccessibility, bool shouldOfferFix)
        {
            var code = $$"""
                #nullable enable
                
                {{containingTypeAccessibility}} class MyClass
                {
                    public string {|CS8618:MyProperty|} { get; {{setAccessorAccessibility}} init; }
                }
                """;

            var fixedCode = $$"""
                #nullable enable
                
                {{containingTypeAccessibility}} class MyClass
                {
                    public required string MyProperty { get; {{setAccessorAccessibility}} init; }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = shouldOfferFix ? fixedCode : code,
                LanguageVersion = LanguageVersion.CSharp11,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net70
            }.RunAsync();
        }

        [Fact]
        public async Task NotForLowerVersionOfCSharp()
        {
            var code = """
                #nullable enable
                
                class MyClass
                {
                    public string {|CS8618:MyProperty|} { get; set; }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp10,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net70
            }.RunAsync();
        }

        [Fact]
        public async Task NotIfNoRequiredAttributeInMetadata()
        {
            var code = """
                #nullable enable
                
                class MyClass
                {
                    public string {|CS8618:MyProperty|} { get; set; }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp11,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net60
            }.RunAsync();
        }

        [Fact]
        public async Task NotOnConstructorDeclaration()
        {
            var code = """
                #nullable enable
                
                class MyClass
                {
                    public string MyProperty { get; set; }

                    public {|CS8618:MyClass|}() { }
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp11,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net70
            }.RunAsync();
        }

        [Fact]
        public async Task NotOnFieldDeclaration()
        {
            var code = """
                #nullable enable
                
                class MyClass
                {
                    public string {|CS8618:_myField|};
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp11,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net70
            }.RunAsync();
        }

        [Fact]
        public async Task NotOnEventDeclaration()
        {
            var code = """
                #nullable enable
                
                class MyClass
                {
                    public event System.EventHandler {|CS8618:MyEvent|};
                }
                """;

            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp11,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net70
            }.RunAsync();
        }

        [Fact]
        public async Task FixAll()
        {
            await new VerifyCS.Test
            {
                TestCode = """
                    #nullable enable

                    class MyClass
                    {
                        public string {|CS8618:MyProperty|} { get; set; }
                        public string {|CS8618:MyProperty1|} { get; set; }
                    }
                    """,
                FixedCode = """
                    #nullable enable

                    class MyClass
                    {
                        public required string MyProperty { get; set; }
                        public required string MyProperty1 { get; set; }
                    }
                    """,
                LanguageVersion = LanguageVersion.CSharp11,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net70
            }.RunAsync();
        }
    }
}
