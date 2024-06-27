// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.UseNameofInAttribute;
using Microsoft.CodeAnalysis.CSharp.UseNameofInAttribute;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseNameofInAttribute;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseNameofInAttributeDiagnosticAnalyzer,
    CSharpUseNameofInAttributeCodeFixProvider>;

public class UseNameofInAttributeTests
{
    [Fact]
    public async Task TestOnMethod()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Diagnostics.CodeAnalysis;
                #nullable enable
                class C
                {
                    [return: NotNullIfNotNull([|"input"|])]
                    string? M(string? input) => input;
                }
                """,
            FixedCode = """
                using System.Diagnostics.CodeAnalysis;
                #nullable enable
                class C
                {
                    [return: NotNullIfNotNull(nameof(input))]
                    string? M(string? input) => input;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithInterpolation()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Diagnostics.CodeAnalysis;
                #nullable enable
                class C
                {
                    [return: NotNullIfNotNull([|$"input"|])]
                    string? M(string? input) => input;
                }
                """,
            FixedCode = """
                using System.Diagnostics.CodeAnalysis;
                #nullable enable
                class C
                {
                    [return: NotNullIfNotNull(nameof(input))]
                    string? M(string? input) => input;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task TestTrivia()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Diagnostics.CodeAnalysis;
                #nullable enable
                class C
                {
                    [return: NotNullIfNotNull(/*before*/[|"input"|]/*after*/)]
                    string? M(string? input) => input;
                }
                """,
            FixedCode = """
                using System.Diagnostics.CodeAnalysis;
                #nullable enable
                class C
                {
                    [return: NotNullIfNotNull(/*before*/nameof(input)/*after*/)]
                    string? M(string? input) => input;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task TestFullAttributeName()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Diagnostics.CodeAnalysis;
                #nullable enable
                class C
                {
                    [return: NotNullIfNotNullAttribute([|"input"|])]
                    string? M(string? input) => input;
                }
                """,
            FixedCode = """
                using System.Diagnostics.CodeAnalysis;
                #nullable enable
                class C
                {
                    [return: NotNullIfNotNullAttribute(nameof(input))]
                    string? M(string? input) => input;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNamedArg()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Diagnostics.CodeAnalysis;
                #nullable enable
                class C
                {
                    [return: NotNullIfNotNullAttribute(parameterName: [|"input"|])]
                    string? M(string? input) => input;
                }
                """,
            FixedCode = """
                using System.Diagnostics.CodeAnalysis;
                #nullable enable
                class C
                {
                    [return: NotNullIfNotNullAttribute(parameterName: nameof(input))]
                    string? M(string? input) => input;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task NotBeforeCSharp11()
    {
        var code = """
            using System.Diagnostics.CodeAnalysis;
            #nullable enable
            class C
            {
                [return: NotNullIfNotNull("input")]
                string? M(string? input) => input;
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            LanguageVersion = LanguageVersion.CSharp10,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task NotOnIncorrectAttributeName()
    {
        var code = """
            using System.Diagnostics.CodeAnalysis;
            #nullable enable
            class C
            {
                [return: {|CS0246:{|CS0246:NotNullIfNotNull1|}|}("input")]
                string? M(string? input) => input;
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWhenMissingArguments()
    {
        var code = """
            using System.Diagnostics.CodeAnalysis;
            #nullable enable
            class C
            {
                [return: {|CS7036:NotNullIfNotNull|}]
                string? M(string? input) => input;
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task NotOnIncorrectReferencedName()
    {
        var code = """
            using System.Diagnostics.CodeAnalysis;
            #nullable enable
            class C
            {
                [return: NotNullIfNotNull("input1")]
                string? M(string? input) => input;
            }
            """;

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task TestOnParameter()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Diagnostics.CodeAnalysis;
                #nullable enable
                class C
                {
                    void M([NotNullIfNotNull([|"input"|])] string? input) { }
                }
                """,
            FixedCode = """
                using System.Diagnostics.CodeAnalysis;
                #nullable enable
                class C
                {
                    void M([NotNullIfNotNull(nameof(input))] string? input) { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task TestForProperty()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Diagnostics.CodeAnalysis;
                #nullable enable
                class C
                {
                    string? Prop { get; set; }

                    [MemberNotNullWhen(true, [|"Prop"|])]
                    bool IsInitialized
                    {
                        get
                        {
                            Prop = "";
                            return true;
                        }
                    }
                }
                """,
            FixedCode = """
                using System.Diagnostics.CodeAnalysis;
                #nullable enable
                class C
                {
                    string? Prop { get; set; }
                
                    [MemberNotNullWhen(true, nameof(Prop))]
                    bool IsInitialized
                    {
                        get
                        {
                            Prop = "";
                            return true;
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultipleArguments()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Diagnostics.CodeAnalysis;
                #nullable enable
                class C
                {
                    string? Prop1 { get; set; }
                    string? Prop2 { get; set; }

                    [MemberNotNull([|"Prop1"|], [|"Prop2"|])]
                    bool IsInitialized
                    {
                        get
                        {
                            Prop1 = "";
                            Prop2 = "";
                            return true;
                        }
                    }
                }
                """,
            FixedCode = """
                using System.Diagnostics.CodeAnalysis;
                #nullable enable
                class C
                {
                    string? Prop1 { get; set; }
                    string? Prop2 { get; set; }
                
                    [MemberNotNull(nameof(Prop1), nameof(Prop2))]
                    bool IsInitialized
                    {
                        get
                        {
                            Prop1 = "";
                            Prop2 = "";
                            return true;
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task TestCallerArgumentExpression1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Runtime.CompilerServices;
                #nullable enable
                class C
                {
                    void M(string s1, [CallerArgumentExpression([|"s1"|])] string? s2 = null) { }
                }
                """,
            FixedCode = """
                using System.Runtime.CompilerServices;
                #nullable enable
                class C
                {
                    void M(string s1, [CallerArgumentExpression(nameof(s1))] string? s2 = null) { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }

    [Fact]
    public async Task TestCallerArgumentExpression2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Runtime.CompilerServices;
                #nullable enable
                class C
                {
                    void M(string s1, [CallerArgumentExpressionAttribute([|"s1"|])] string? s2 = null) { }
                }
                """,
            FixedCode = """
                using System.Runtime.CompilerServices;
                #nullable enable
                class C
                {
                    void M(string s1, [CallerArgumentExpressionAttribute(nameof(s1))] string? s2 = null) { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();
    }
}
