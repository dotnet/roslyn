﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.DisambiguateSameVariable;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.DisambiguateSameVariable;

using VerifyCS = CSharpCodeFixVerifier<
    EmptyDiagnosticAnalyzer,
    CSharpDisambiguateSameVariableCodeFixProvider>;

public sealed class DisambiguateSameVariableTests
{
    [Fact]
    public async Task TestParamToParamWithNoMatch()
    {
        var code = """
            class C
            {
                void M(int a)
                {
                    {|CS1717:a = a|};
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task TestLocalToLocalWithNoMatch()
    {
        var code = """
            class C
            {
                void M()
                {
                    var a = 0;
                    {|CS1717:a = a|};
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact]
    public async Task TestFieldToFieldWithNoMatch()
    {
        var code = """
            class C
            {
                int a;
                void M()
                {
                    {|CS1717:a = a|};
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28290")]
    public Task TestParamToParamWithSameNamedField()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                int a;
                void M(int a)
                {
                    {|CS1717:a = a|};
                }
            }
            """, """
            class C
            {
                int a;
                void M(int a)
                {
                    this.a = a;
                }
            }
            """);

    [Fact]
    public async Task TestFieldToFieldWithNonMatchingField()
    {
        var code = """
            class C
            {
                int x;
                void M()
                {
                    {|CS0103:a|} = {|CS0103:a|};
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28290")]
    public Task TestParamToParamWithUnderscoreNamedField()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                int _a;
                void M(int a)
                {
                    {|CS1717:a = a|};
                }
            }
            """, """
            class C
            {
                int _a;
                void M(int a)
                {
                    _a = a;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28290")]
    public Task TestParamToParamWithCapitalizedField()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                int A;
                void M(int a)
                {
                    {|CS1717:a = a|};
                }
            }
            """, """
            class C
            {
                int A;
                void M(int a)
                {
                    A = a;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28290")]
    public Task TestParamToParamWithProperty()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                int A { get; set; }
                void M(int a)
                {
                    {|CS1717:a = a|};
                }
            }
            """, """
            class C
            {
                int A { get; set; }
                void M(int a)
                {
                    A = a;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28290")]
    public Task TestParamToParamWithReadOnlyFieldInConstructor()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                readonly int a;
                public C(int a)
                {
                    {|CS1717:a = a|};
                }
            }
            """, """
            class C
            {
                readonly int a;
                public C(int a)
                {
                    this.a = a;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28290")]
    public Task TestParamToParamWithReadOnlyFieldOutsideOfConstructor()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                readonly int a;
                void M(int a)
                {
                    {|CS1717:a = a|};
                }
            }
            """, """
            class C
            {
                readonly int a;
                void M(int a)
                {
                    {|CS0191:this.a|} = a;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28290")]
    public Task TestParamToParamWithAccessibleFieldInBaseType()
        => VerifyCS.VerifyCodeFixAsync("""
            class Base
            {
                protected int a;
            }

            class C : Base
            {
                public C(int a)
                {
                    {|CS1717:a = a|};
                }
            }
            """, """
            class Base
            {
                protected int a;
            }

            class C : Base
            {
                public C(int a)
                {
                    this.a = a;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28290")]
    public async Task TestParamToParamNotWithInaccessibleFieldInBaseType()
    {
        var code = """
            class Base
            {
                private int a;
            }

            class C : Base
            {
                public C(int a)
                {
                    {|CS1717:a = a|};
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28290")]
    public async Task TestParamToParamNotWithStaticField()
    {
        var code = """
            class C
            {
                static int a;
                public C(int a)
                {
                    {|CS1717:a = a|};
                }
            }
            """;

        await VerifyCS.VerifyCodeFixAsync(code, code);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28290")]
    public Task TestParamToParamCompareWithSameNamedField()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                int a;
                void M(int a)
                {
                    if ({|CS1718:a == a|})
                    {
                    }
                }
            }
            """, """
            class C
            {
                int a;
                void M(int a)
                {
                    if (this.a == a)
                    {
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28290")]
    public Task TestFixAll1()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                int a;
                void M(int a)
                {
                    {|CS1717:a = a|};
                    {|CS1717:a = a|};
                }
            }
            """, """
            class C
            {
                int a;
                void M(int a)
                {
                    this.a = a;
                    this.a = a;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28290")]
    public Task TestFieldToFieldWithPropAvailableOffOfThis()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                int a;
                int A { get; set; }
                void M()
                {
                    {|CS1717:this.a = this.a|};
                }
            }
            """, """
            class C
            {
                int a;
                int A { get; set; }
                void M()
                {
                    this.A = this.a;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/28290")]
    public Task TestFieldToFieldWithPropAvailableOffOfOtherInstance()
        => VerifyCS.VerifyCodeFixAsync("""
            class C
            {
                int a;
                int A { get; set; }
                void M(C c)
                {
                    {|CS1717:c.a = c.a|};
                }
            }
            """, """
            class C
            {
                int a;
                int A { get; set; }
                void M(C c)
                {
                    c.A = c.a;
                }
            }
            """);
}
