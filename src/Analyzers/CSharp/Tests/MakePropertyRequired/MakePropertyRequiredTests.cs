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

    [Trait(Traits.Feature, Traits.Features.CodeActiosMakePropertyRequired)]
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

        [Fact]
        public async Task NotIfPrivateProperty()
        {
            var code = """
                #nullable enable
                
                class MyClass
                {
                    private string {|CS8618:MyProperty|} { get; set; }
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
        public async Task NotIfPrivateSetAccessor()
        {
            var code = """
                #nullable enable
                
                class MyClass
                {
                    public string {|CS8618:MyProperty|} { get; private set; }
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
        public async Task NotIfPrivateInitAccessor()
        {
            var code = """
                #nullable enable
                
                class MyClass
                {
                    public string {|CS8618:MyProperty|} { get; private init; }
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
        public async Task NotWhenNoRequiredAttributeInMetadata()
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
