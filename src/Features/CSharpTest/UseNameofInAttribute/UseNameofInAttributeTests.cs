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

public sealed class UseNameofInAttributeTests
{
    [Fact]
    public Task TestOnMethod()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestWithInterpolation()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestTrivia()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestFullAttributeName()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestNamedArg()
        => new VerifyCS.Test
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

    [Fact]
    public Task NotBeforeCSharp11()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Diagnostics.CodeAnalysis;
            #nullable enable
            class C
            {
                [return: NotNullIfNotNull("input")]
                string? M(string? input) => input;
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();

    [Fact]
    public Task NotOnIncorrectAttributeName()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Diagnostics.CodeAnalysis;
            #nullable enable
            class C
            {
                [return: {|CS0246:{|CS0246:NotNullIfNotNull1|}|}("input")]
                string? M(string? input) => input;
            }
            """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();

    [Fact]
    public Task TestNotWhenMissingArguments()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Diagnostics.CodeAnalysis;
            #nullable enable
            class C
            {
                [return: {|CS7036:NotNullIfNotNull|}]
                string? M(string? input) => input;
            }
            """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();

    [Fact]
    public Task NotOnIncorrectReferencedName()
        => new VerifyCS.Test
        {
            TestCode = """
            using System.Diagnostics.CodeAnalysis;
            #nullable enable
            class C
            {
                [return: NotNullIfNotNull("input1")]
                string? M(string? input) => input;
            }
            """,
            LanguageVersion = LanguageVersion.CSharp11,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net60,
        }.RunAsync();

    [Fact]
    public Task TestOnParameter()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestForProperty()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestMultipleArguments()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestCallerArgumentExpression1()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestCallerArgumentExpression2()
        => new VerifyCS.Test
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
