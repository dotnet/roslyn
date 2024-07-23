// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.MakeTypePartial;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.CSharp.UnitTests.MakeTypePartial;

using VerifyCS = CSharpCodeFixVerifier<
    EmptyDiagnosticAnalyzer,
    CSharpMakeTypePartialCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsMakeTypePartial)]
public sealed class MakeTypePartialTests
{
    public static IEnumerable<object[]> AllValidDeclarationTypes()
    {
        yield return new[] { "class" };
        yield return new[] { "struct" };
        yield return new[] { "interface" };
        yield return new[] { "record" };
        yield return new[] { "record class" };
        yield return new[] { "record struct" };
    }

    [Theory]
    [MemberData(nameof(AllValidDeclarationTypes))]
    public async Task OutsideNamespace(string declarationType)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                partial {{declarationType}} Declaration
                {
                }
            
                {{declarationType}} {|CS0260:Declaration|}
                {
                }
                """,
            FixedCode = $$"""
                partial {{declarationType}} Declaration
                {
                }
            
                partial {{declarationType}} Declaration
                {
                }
                """,
            LanguageVersion = LanguageVersion.CSharp10
        }.RunAsync();
    }

    [Theory]
    [MemberData(nameof(AllValidDeclarationTypes))]
    public async Task InsideOneFileScopedNamespace(string declarationType)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                namespace TestNamespace;
            
                partial {{declarationType}} Declaration
                {
                }
            
                {{declarationType}} {|CS0260:Declaration|}
                {
                }
                """,
            FixedCode = $$"""
                namespace TestNamespace;
            
                partial {{declarationType}} Declaration
                {
                }
            
                partial {{declarationType}} Declaration
                {
                }
                """,
            LanguageVersion = LanguageVersion.CSharp10
        }.RunAsync();
    }

    [Theory]
    [MemberData(nameof(AllValidDeclarationTypes))]
    public async Task InsideOneBlockScopedNamespace(string declarationType)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                namespace TestNamespace
                {
                    partial {{declarationType}} Declaration
                    {
                    }
            
                    {{declarationType}} {|CS0260:Declaration|}
                    {
                    }
                }
                """,
            FixedCode = $$"""
                namespace TestNamespace
                {
                    partial {{declarationType}} Declaration
                    {
                    }
            
                    partial {{declarationType}} Declaration
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp10
        }.RunAsync();
    }

    [Theory]
    [MemberData(nameof(AllValidDeclarationTypes))]
    public async Task InsideTwoEqualBlockScopedNamespaces(string declarationType)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                namespace TestNamespace
                {
                    partial {{declarationType}} Declaration
                    {
                    }
                }
            
                namespace TestNamespace
                {
                    {{declarationType}} {|CS0260:Declaration|}
                    {
                    }
                }
                """,
            FixedCode = $$"""
                namespace TestNamespace
                {
                    partial {{declarationType}} Declaration
                    {
                    }
                }
            
                namespace TestNamespace
                {
                    partial {{declarationType}} Declaration
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp10
        }.RunAsync();
    }

    [Theory]
    [MemberData(nameof(AllValidDeclarationTypes))]
    public async Task InDifferentDocuments(string declarationType)
    {
        var document1 = $$"""
            partial {{declarationType}} Declaration
            {
            }
            """;

        var document2 = $$"""
            {{declarationType}} {|CS0260:Declaration|}
            {
            }
            """;

        var fixedDocument2 = $$"""
            partial {{declarationType}} Declaration
            {
            }
            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { document1, document2 }
            },
            FixedState =
            {
                Sources = { document1, fixedDocument2 }
            },
            LanguageVersion = LanguageVersion.CSharp10
        }.RunAsync();
    }

    [Theory]
    [MemberData(nameof(AllValidDeclarationTypes))]
    public async Task WithOtherModifiers(string declarationType)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                public partial {{declarationType}} Declaration
                {
                }
            
                public {{declarationType}} {|CS0260:Declaration|}
                {
                }
                """,
            FixedCode = $$"""
                public partial {{declarationType}} Declaration
                {
                }
            
                public partial {{declarationType}} Declaration
                {
                }
                """,
            LanguageVersion = LanguageVersion.CSharp10
        }.RunAsync();
    }

    [Theory]
    [MemberData(nameof(AllValidDeclarationTypes))]
    public async Task NestedType1(string declarationType)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                class Test
                {
                    public partial {{declarationType}} Declaration
                    {
                    }
            
                    public {{declarationType}} {|CS0260:Declaration|}
                    {
                    }
                }
                """,
            FixedCode = $$"""
                class Test
                {
                    public partial {{declarationType}} Declaration
                    {
                    }
            
                    public partial {{declarationType}} Declaration
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp10
        }.RunAsync();
    }

    [Theory]
    [MemberData(nameof(AllValidDeclarationTypes))]
    public async Task NestedType2(string declarationType)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                partial {{declarationType}} Test
                {
                }

                {{declarationType}} {|CS0260:Test|}
                {
                    public partial {{declarationType}} Declaration
                    {
                    }
            
                    public {{declarationType}} {|CS0260:Declaration|}
                    {
                    }
                }
                """,
            FixedCode = $$"""
                partial {{declarationType}} Test
                {
                }

                partial {{declarationType}} Test
                {
                    public partial {{declarationType}} Declaration
                    {
                    }
            
                    public partial {{declarationType}} Declaration
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp10
        }.RunAsync();
    }

    [Theory]
    [MemberData(nameof(AllValidDeclarationTypes))]
    public async Task FixOne(string declarationType)
    {
        var testCode = $$"""
            partial {{declarationType}} Test
            {
            }
            
            {{declarationType}} {|CS0260:Test|}
            {
            }
            
            {{declarationType}} {|CS0260:Test|}
            {
            }
            """;
        var fixedCode = $$"""
            partial {{declarationType}} Test
            {
            }
            
            partial {{declarationType}} Test
            {
            }
            
            partial {{declarationType}} Test
            {
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            LanguageVersion = LanguageVersion.CSharp10,
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
            DiagnosticSelector = d => d[0]
        }.RunAsync();

        await new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            LanguageVersion = LanguageVersion.CSharp10,
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
            DiagnosticSelector = d => d[1]
        }.RunAsync();
    }

    [Theory]
    [MemberData(nameof(AllValidDeclarationTypes))]
    public async Task NotInDifferentNamespaces(string declarationType)
    {
        var markup = $$"""
            namespace TestNamespace1
            {
                partial {{declarationType}} Declaration
                {
                }
            }
            
            namespace TestNamespace2
            {
                {{declarationType}} Declaration
                {
                }
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = markup,
            FixedCode = markup,
            LanguageVersion = LanguageVersion.CSharp10
        }.RunAsync();
    }
}
