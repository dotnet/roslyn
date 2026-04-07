// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.UseUnboundGenericTypeInNameOf;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseUnboundGenericTypeInNameOf;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseUnboundGenericTypeInNameOfDiagnosticAnalyzer,
    CSharpUseUnboundGenericTypeInNameOfCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseUnboundGenericTypeInNameOf)]
public sealed class UseUnboundGenericTypeInNameOfTests
{
    [Fact]
    public Task TestBaseCase()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    void M(string[] args)
                    {
                        var v = [|nameof|](List<int>);
                    }
                }
                """,
            FixedCode = """
                using System.Collections.Generic;

                class C
                {
                    void M(string[] args)
                    {
                        var v = nameof(List<>);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestNotIfAlreadyOmitted()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    void M(string[] args)
                    {
                        var v = nameof(List<>);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestMissingBeforeCSharp14()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    void M(string[] args)
                    {
                        var v = nameof(List<int>);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
        }.RunAsync();

    [Fact]
    public Task TestMissingWithFeatureOff()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    void M(string[] args)
                    {
                        var v = nameof(List<int>);
                    }
                }
                """,
            Options =
            {
                { CSharpCodeStyleOptions.PreferUnboundGenericTypeInNameOf, false, CodeStyle.NotificationOption2.Silent }
            },
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestMultipleTypeArguments()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    void M(string[] args)
                    {
                        var v = [|nameof|](Dictionary<int, string>);
                    }
                }
                """,
            FixedCode = """
                using System.Collections.Generic;

                class C
                {
                    void M(string[] args)
                    {
                        var v = nameof(Dictionary<,>);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestGlobal()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    void M(string[] args)
                    {
                        var v = [|nameof|](global::System.Collections.Generic.Dictionary<int, string>);
                    }
                }
                """,
            FixedCode = """
                using System.Collections.Generic;

                class C
                {
                    void M(string[] args)
                    {
                        var v = nameof(global::System.Collections.Generic.Dictionary<,>);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestNestedArgs()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    void M(string[] args)
                    {
                        var v = [|nameof|](Dictionary<List<int>, string>);
                    }
                }
                """,
            FixedCode = """
                using System.Collections.Generic;

                class C
                {
                    void M(string[] args)
                    {
                        var v = nameof(Dictionary<,>);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestNestedType1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    void M(string[] args)
                    {
                        var v = [|nameof|](Outer<int>.Inner<string>);
                    }
                }

                class Outer<T> { public class Inner<T> { } }
                """,
            FixedCode = """
                using System.Collections.Generic;

                class C
                {
                    void M(string[] args)
                    {
                        var v = nameof(Outer<>.Inner<>);
                    }
                }
                
                class Outer<T> { public class Inner<T> { } }
                """,
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestNestedType2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    void M(string[] args)
                    {
                        var v = [|nameof|](Outer<int>.Inner<>);
                    }
                }

                class Outer<T> { public class Inner<T> { } }
                """,
            FixedCode = """
                using System.Collections.Generic;

                class C
                {
                    void M(string[] args)
                    {
                        var v = nameof(Outer<>.Inner<>);
                    }
                }
                
                class Outer<T> { public class Inner<T> { } }
                """,
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestNestedType3()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    void M(string[] args)
                    {
                        var v = [|nameof|](Outer<>.Inner<int>);
                    }
                }

                class Outer<T> { public class Inner<T> { } }
                """,
            FixedCode = """
                using System.Collections.Generic;

                class C
                {
                    void M(string[] args)
                    {
                        var v = nameof(Outer<>.Inner<>);
                    }
                }
                
                class Outer<T> { public class Inner<T> { } }
                """,
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();

    [Fact]
    public Task TestNestedType4()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    void M(string[] args)
                    {
                        var v = nameof(Outer<>.Inner<>);
                    }
                }

                class Outer<T> { public class Inner<T> { } }
                """,
            LanguageVersion = LanguageVersion.CSharp14,
        }.RunAsync();
}
