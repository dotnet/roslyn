// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.RemoveUnnecessarySuppressions;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.RemoveUnnecessarySuppressions;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpRemoveUnnecessaryNullableWarningSuppressionsDiagnosticAnalyzer,
    CSharpRemoveUnnecessaryNullableWarningSuppressionsCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessarySuppressions)]
[WorkItem("https://github.com/dotnet/roslyn/issues/44176")]
public sealed class RemoveUnnecessaryNullableWarningSuppressionsTests
{
    [Fact]
    public Task KeepWhenNeeded1()
        => new VerifyCS.Test
        {
            TestCode = """
                #nullable enable

                class C
                {
                    void M(string? s)
                    {
                        var t = s!.ToString();
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task KeepWhenNeeded2()
        => new VerifyCS.Test
        {
            TestCode = """
                #nullable enable

                class C
                {
                    void M()
                    {
                        object o = null!;
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task RemoveWhenNotNeeded1()
        => new VerifyCS.Test
        {
            TestCode = """
                #nullable enable

                class C
                {
                    void M()
                    {
                        object o = ""[|!|];
                    }
                }
                """,
            FixedCode = """
                #nullable enable

                class C
                {
                    void M()
                    {
                        object o = "";
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task RemoveWhenNotNeeded_FixAll()
        => new VerifyCS.Test
        {
            TestCode = """
                #nullable enable

                class C
                {
                    void M()
                    {
                        object o = ((""[|!|]) + "")[|!|];
                    }
                }
                """,
            FixedCode = """
                #nullable enable

                class C
                {
                    void M()
                    {
                        object o = (("") + "");
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestWithDefaultAndTypeParameter()
        => new VerifyCS.Test
        {
            TestCode = """
                #nullable enable

                class C
                {
                    public TLanguageService GetService<TLanguageService>()
                    {
                        return default!;
                    }
                }
                """,
        }.RunAsync();

    [Fact]
    public Task TestInAttribute()
        => new VerifyCS.Test
        {
            TestCode = """
                #nullable enable

                class XAttribute : System.Attribute
                {
                    public XAttribute(string? s) { }
                }

                [X(null[|!|])]
                class C
                {
                }
                """,
            FixedCode = """
                #nullable enable
            
                class XAttribute : System.Attribute
                {
                    public XAttribute(string? s) { }
                }
            
                [X(null)]
                class C
                {
                }
                """,
        }.RunAsync();

    [Fact]
    public Task KeepWhenNeeded_FieldInitializer()
        => new VerifyCS.Test
        {
            TestCode = """
                #nullable enable

                class C
                {
                    string s = null!;
                }
                """,
        }.RunAsync();

    [Fact]
    public Task KeepWhenNeeded_PropertyInitializer()
        => new VerifyCS.Test
        {
            TestCode = """
                #nullable enable

                class C
                {
                    public string S { get; } = null!;
                }
                """,
        }.RunAsync();
    // [Fact] -- Disabled due to failure in CI (likely due to TargetFramework differences in nullability annotations)
    public Task TestGetMethodReturnsNullable()
        => new VerifyCS.Test
        {
            TestCode = """
                #nullable enable
                using System.Reflection;

                class C
                {
                    void M()
                    {
                        MethodInfo m = typeof(C).GetMethod("M")!;
                    }
                }
                """,
        }.RunAsync();
}
