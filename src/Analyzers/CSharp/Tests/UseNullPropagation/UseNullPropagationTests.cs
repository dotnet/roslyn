﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.UseNullPropagation;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseNullPropagation;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseNullPropagationDiagnosticAnalyzer,
    CSharpUseNullPropagationCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)]
public sealed partial class UseNullPropagationTests
{
    private static Task TestInRegularAndScript1Async(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string testCode,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string fixedCode,
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
        LanguageVersion languageVersion = LanguageVersion.CSharp9)
        => new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            // code action is currently generating invalid trees.  Specifically, it transforms `x.Y()` into `x.?Y()`
            // by just rewriting `x.Y` into `x?.Y`.  That is not correct.  the RHS of the `?` should `.Y()` not
            // `.Y`.
            CodeActionValidationMode = CodeActionValidationMode.None,
            LanguageVersion = languageVersion,
            TestState =
            {
                OutputKind = outputKind,
            },
        }.RunAsync();

    private static Task TestMissingInRegularAndScriptAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string testCode,
        LanguageVersion languageVersion = LanguageVersion.CSharp9)
        => new VerifyCS.Test
        {
            TestCode = testCode,
            LanguageVersion = languageVersion,
        }.RunAsync();

    [Fact]
    public Task TestLeft_Equals()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = [|o == null ? null : o.ToString()|];
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = o?.ToString();
                }
            }
            """);

    [Fact]
    public Task TestLeft_Equals_IfStatement()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    [|if|] (o != null)
                        o.ToString();
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    o?.ToString();
                }
            }
            """);

    [Fact]
    public Task TestIfStatement_WithBlock()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    [|if|] (o != null)
                    {
                        o.ToString();
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    o?.ToString();
                }
            }
            """);

    [Fact]
    public Task TestIfStatement_NotWithElse()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    if (o != null)
                        o.ToString();
                    else
                    {
                    }
                }
            }
            """);

    [Fact]
    public Task TestIfStatement_NotWithMultipleStatements()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    if (o != null)
                    {
                        o.ToString();
                        o.ToString();
                    }
                }
            }
            """);

    [Fact]
    public Task TestLeft_Equals_IfStatement_TopLevel()
        => TestInRegularAndScript1Async(
            """
            using System;

            object o = null;
            [|if|] (o != null)
                o.ToString();
            """,
            """
            using System;

            object o = null;
            o?.ToString();
            """, OutputKind.ConsoleApplication);

    [Fact]
    public Task TestLeft_IsNull()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = [|o is null ? null : o.ToString()|];
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = o?.ToString();
                }
            }
            """);

    [Fact]
    public Task TestLeft_IsNotNull()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = [|o is not null ? o.ToString() : null|];
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = o?.ToString();
                }
            }
            """);

    [Fact]
    public Task TestLeft_IsNotNull_IfStatement()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    [|if|] (o is not null)
                        o.ToString();
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    o?.ToString();
                }
            }
            """);

    [Fact]
    public Task TestMissingOnCSharp5()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = o == null ? null : o.ToString();
                }
            }
            """, LanguageVersion.CSharp5);

    [Fact]
    public Task TestMissingOnCSharp5_IfStatement()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    if (o != null)
                        o.ToString();
                }
            }
            """, LanguageVersion.CSharp5);

    [Fact]
    public Task TestRight_Equals()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = [|null == o ? null : o.ToString()|];
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = o?.ToString();
                }
            }
            """);

    [Fact]
    public Task TestRight_Equals_IfStatement()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    [|if|] (null != o)
                                o.ToString();
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    o?.ToString();
                }
            }
            """);

    [Fact]
    public Task TestLeft_NotEquals()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = [|o != null ? o.ToString() : null|];
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = o?.ToString();
                }
            }
            """);

    [Fact]
    public Task TestWithNullableType()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = [|c != null ? c.f : null|];
                }
            }
            """,
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = c?.f;
                }
            }
            """);

    [Fact]
    public Task TestWithNullableType_IfStatement()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    [|if|] (c != null)
                        c.f?.ToString();
                }
            }
            """,
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    c?.f?.ToString();
                }
            }
            """);

    [Fact]
    public Task TestWithNullableTypeAndObjectCast()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = [|(object)c != null ? c.f : null|];
                }
            }
            """,
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = c?.f;
                }
            }
            """);

    [Fact]
    public Task TestWithNullableTypeAndObjectCast_IfStatement()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    [|if|] ((object)c != null)
                        c.f?.ToString();
                }
            }
            """,
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    c?.f?.ToString();
                }
            }
            """);

    [Fact]
    public Task TestRight_NotEquals()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = [|null != o ? o.ToString() : null|];
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = o?.ToString();
                }
            }
            """);

    [Fact]
    public Task TestIndexer()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = [|o == null ? null : {|CS0021:o[0]|}|];
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = o?{|CS0021:[0]|};
                }
            }
            """);

    [Fact]
    public Task TestIndexer_IfStatement()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    [|if|] (o != null)
                        {|CS0021:o[0]|}.ToString();
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    o?{|CS0021:[0]|}.ToString();
                }
            }
            """);

    [Fact]
    public Task TestConditionalAccess()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = [|o == null ? null : o.{|CS1061:B|}?.C|];
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = o?{|CS1061:.B|}?.C;
                }
            }
            """);

    [Fact]
    public Task TestConditionalAccess_IfStatement()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    [|if|](o != null)
                        o.{|CS1061:B|}?.C();
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    o?{|CS1061:.B|}?.C();
                }
            }
            """);

    [Fact]
    public Task TestMemberAccess()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = [|o == null ? null : o.{|CS1061:B|}|];
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = o?{|CS1061:.B|};
                }
            }
            """);

    [Fact]
    public Task TestMemberAccess_IfStatement()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    [|if|] (o != null)
                        o.{|CS1061:B|}();
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    o?{|CS1061:.B|}();
                }
            }
            """);

    [Fact]
    public Task TestMissingOnSimpleMatch()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = o == null ? null : o;
                }
            }
            """);

    [Fact]
    public Task TestMissingOnSimpleMatch_IfStatement()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    if (o != null)
                        {|CS0201:o|};
                }
            }
            """);

    [Fact]
    public Task TestParenthesizedCondition()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = [|(o == null) ? null : o.ToString()|];
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = o?.ToString();
                }
            }
            """);

    [Fact]
    public Task TestFixAll1()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v1 = [|o == null ? null : o.ToString()|];
                    var v2 = [|o != null ? o.ToString() : null|];
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v1 = o?.ToString();
                    var v2 = o?.ToString();
                }
            }
            """);

    [Fact]
    public Task TestFixAll2()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o1, object o2)
                {
                    var v1 = [|o1 == null ? null : o1.{|CS1501:ToString|}([|o2 == null ? null : o2.ToString()|])|];
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o1, object o2)
                {
                    var v1 = o1?{|CS1501:.ToString|}(o2?.ToString());
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15505")]
    public Task TestOtherValueIsNotNull1()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = {|CS0173:o == null ? 0 : o.ToString()|};
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15505")]
    public Task TestOtherValueIsNotNull2()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = {|CS0173:o != null ? o.ToString() : 0|};
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16287")]
    public Task TestMethodGroup()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class D
            {
                void Goo()
                {
                    var c = new C();
                    Action<string> a = c != null ? c.M : (Action<string>)null;
                }
            }
            class C { public void M(string s) { } }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17623")]
    public Task TestInExpressionTree()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Linq.Expressions;

            class Program
            {
                void Main(string s)
                {
                    Method<string>(t => s != null ? s.ToString() : null); // works
                }

                public void Method<T>(Expression<Func<T, string>> functor)
                {
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17623")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/33992")]
    public Task TestInExpressionTree2()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Linq;

            class C
            {
                void Main()
                {
                    _ = from item in Enumerable.Empty<(int? x, int? y)?>().AsQueryable()
                        select item == null ? null : item.Value.x;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17623")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/33992")]
    public Task TestInExpressionTree3()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Linq;

            class C
            {
                void Main()
                {
                    _ = from item in Enumerable.Empty<(int? x, int? y)?>().AsQueryable()
                        where (item == null ? null : item.Value.x) > 0
                        select item;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17623")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/33992")]
    public Task TestInExpressionTree4()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Linq;

            class C
            {
                void Main()
                {
                    _ = from item in Enumerable.Empty<(int? x, int? y)?>().AsQueryable()
                        let x = item == null ? null : item.Value.x
                        select x;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19774")]
    public Task TestNullableMemberAccess()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void Main(DateTime? toDate)
                {
                    var v = [|toDate == null ? null : toDate.Value.ToString("yyyy/MM/ dd")|];
                }
            }
            """,

            """
            using System;

            class C
            {
                void Main(DateTime? toDate)
                {
                    var v = toDate?.ToString("yyyy/MM/ dd");
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19774")]
    public Task TestNullableMemberAccess_IfStatement()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void Main(DateTime? toDate)
                {
                    [|if|] (toDate != null)
                        toDate.Value.ToString("yyyy/MM/ dd");
                }
            }
            """,

            """
            using System;

            class C
            {
                void Main(DateTime? toDate)
                {
                    toDate?.ToString("yyyy/MM/ dd");
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19774")]
    public Task TestNullableElementAccess()
        => TestInRegularAndScript1Async(
            """
            using System;

            struct S
            {
                public string this[int i] => "";
            }

            class C
            {
                void Main(S? s)
                {
                    var x = [|s == null ? null : s.Value[0]|];
                }
            }
            """,

            """
            using System;

            struct S
            {
                public string this[int i] => "";
            }

            class C
            {
                void Main(S? s)
                {
                    var x = s?[0];
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndIsNull()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = [|c is null ? null : c.f|];
                }
            }
            """,
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = c?.f;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndIsNotNull()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = [|c is not null ? c.f : null|];
                }
            }
            """,
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = c?.f;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndIsNotNull_IfStatement()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    [|if|] (c is not null)
                        c.f?.ToString();
                }
            }
            """,
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    c?.f?.ToString();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndIsType()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = c is C ? null : c.f;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndIsType_IfStatement1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    if (c is C)
                        c.f?.ToString();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndIsType_IfStatement2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    if (c is C d)
                        c.f?.ToString();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndIsType_IfStatement3()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    if (c is not C)
                        c.f?.ToString();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestIsOtherConstant()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(string s)
                {
                    int? x = s is "" ? null : (int?)s.Length;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndReferenceEquals1()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = [|ReferenceEquals(c, null) ? null : c.f|];
                }
            }
            """,
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = c?.f;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndReferenceEquals1_IfStatement()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    [|if|] (!ReferenceEquals(c, null))
                        c.f?.ToString();
                }
            }
            """,
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    c?.f?.ToString();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndReferenceEquals2()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = [|ReferenceEquals(null, c) ? null : c.f|];
                }
            }
            """,
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = c?.f;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndReferenceEquals2_IfStatement()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    [|if|] (!ReferenceEquals(null, c))
                        c.f?.ToString();
                }
            }
            """,
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    c?.f?.ToString();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndReferenceEqualsOtherValue1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                public int? f;
                void M(C c, C other)
                {
                    int? x = ReferenceEquals(c, other) ? null : c.f;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndReferenceEqualsOtherValue1_IfStatement1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                public int? f;
                void M(C c, C other)
                {
                    if (ReferenceEquals(c, other))
                        c.f?.ToString();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndReferenceEqualsOtherValue1_IfStatement2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                public int? f;
                void M(C c, C other)
                {
                    if (!ReferenceEquals(c, other))
                        c.f?.ToString();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndReferenceEqualsOtherValue2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                public int? f;
                void M(C c, C other)
                {
                    int? x = ReferenceEquals(other, c) ? null : c.f;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndReferenceEqualsWithObject1()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = [|object.ReferenceEquals(c, null) ? null : c.f|];
                }
            }
            """,
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = c?.f;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndReferenceEqualsWithObject1_IfStatement()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    [|if|] (!object.ReferenceEquals(c, null))
                        c.f?.ToString();
                }
            }
            """,
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    c?.f?.ToString();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndReferenceEqualsWithObject2()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = [|object.ReferenceEquals(null, c) ? null : c.f|];
                }
            }
            """,
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = c?.f;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndReferenceEqualsWithObject2_IfStatement()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    [|if|] (!object.ReferenceEquals(null, c))
                        c.f?.ToString();
                }
            }
            """,
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    c?.f?.ToString();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndReferenceEqualsOtherValueWithObject1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                public int? f;
                void M(C c, C other)
                {
                    int? x = object.ReferenceEquals(c, other) ? null : c.f;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndReferenceEqualsOtherValueWithObject2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                public int? f;
                void M(C c, C other)
                {
                    int? x = object.ReferenceEquals(other, c) ? null : c.f;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndNotIsNull()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = [|!(c is null) ? c.f : null|];
                }
            }
            """,
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = c?.f;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndNotIsNotNull()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = [|!(c is not null) ? null : c.f|];
                }
            }
            """,
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = c?.f;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndNotIsType()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = !(c is C) ? c.f : null;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndNotIsOtherConstant()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M(string s)
                {
                    int? x = !(s is "") ? (int?)s.Length : null;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndLogicalNotReferenceEquals1()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = [|!ReferenceEquals(c, null) ? c.f : null|];
                }
            }
            """,
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = c?.f;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndLogicalNotReferenceEquals2()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = [|!ReferenceEquals(null, c) ? c.f : null|];
                }
            }
            """,
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = c?.f;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndLogicalNotReferenceEqualsOtherValue1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                public int? f;
                void M(C c, C other)
                {
                    int? x = !ReferenceEquals(c, other) ? c.f : null;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndLogicalNotReferenceEqualsOtherValue2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                public int? f;
                void M(C c, C other)
                {
                    int? x = !ReferenceEquals(other, c) ? c.f : null;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndLogicalNotReferenceEqualsWithObject1()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = [|!object.ReferenceEquals(c, null) ? c.f : null|];
                }
            }
            """,
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = c?.f;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndLogicalNotReferenceEqualsWithObject2()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = [|!object.ReferenceEquals(null, c) ? c.f : null|];
                }
            }
            """,
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = c?.f;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndLogicalNotReferenceEqualsOtherValueWithObject1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                public int? f;
                void M(C c, C other)
                {
                    int? x = !object.ReferenceEquals(c, other) ? c.f : null;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestWithNullableTypeAndLogicalNotReferenceEqualsOtherValueWithObject2()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                public int? f;
                void M(C c, C other)
                {
                    int? x = !object.ReferenceEquals(other, c) ? c.f : null;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestEqualsWithLogicalNot()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = [|!(c == null) ? c.f : null|];
                }
            }
            """,
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = c?.f;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestEqualsWithLogicalNot_IfStatement()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    [|if|] (!(c == null))
                        c.f?.ToString();
                }
            }
            """,
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    c?.f?.ToString();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestNotEqualsWithLogicalNot()
        => TestInRegularAndScript1Async(
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = [|!(c != null) ? null : c.f|];
                }
            }
            """,
            """
            class C
            {
                public int? f;
                void M(C c)
                {
                    int? x = c?.f;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestEqualsOtherValueWithLogicalNot()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                public int? f;
                void M(C c, C other)
                {
                    int? x = !(c == other) ? c.f : null;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
    public Task TestNotEqualsOtherValueWithLogicalNot()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                public int? f;
                void M(C c, C other)
                {
                    int? x = !(c != other) ? null : c.f;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49517")]
    public Task TestParenthesizedExpression()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = [|(o == null) ? null : (o.ToString())|];
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = (o?.ToString());
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49517")]
    public Task TestReversedParenthesizedExpression()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = [|(o != null) ? (o.ToString()) : null|];
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = (o?.ToString());
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74273")]
    public Task TestParenthesizedPropertyAccess()
        => TestInRegularAndScript1Async("""
            using System;
            
            class C
            {
                int? Length(Array array) => [|array == null ? null : (array.Length)|];
            }
            """, """
            using System;
            
            class C
            {
                int? Length(Array array) => (array?.Length);
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74273")]
    public Task TestReversedParenthesizedPropertyAccess()
        => TestInRegularAndScript1Async("""
            using System;
            
            class C
            {
                int? Length(Array array) => [|array != null ? (array.Length) : null|];
            }
            """, """
            using System;
            
            class C
            {
                int? Length(Array array) => (array?.Length);
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49517")]
    public Task TestParenthesizedNull()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = [|o == null ? (null) : o.ToString()|];
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = o?.ToString();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49517")]
    public Task TestReversedParenthesizedNull()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = [|o != null ? o.ToString() : (null)|];
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    var v = o?.ToString();
                }
            }
            """);

    [Fact]
    public Task TestIfStatement_Trivia1()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    // Before
                    [|if|] (o != null)
                        o.ToString();
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    // Before
                    o?.ToString();
                }
            }
            """);

    [Fact]
    public Task TestIfStatement_Trivia2()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    // Before1
                    [|if|] (o != null)
                        // Before2
                        o.ToString();
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    // Before1
                    // Before2
                    o?.ToString();
                }
            }
            """);

    [Fact]
    public Task TestIfStatement_Trivia3()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    // Before1
                    [|if|] (o != null)
                    {
                        // Before2
                        o.ToString();
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    // Before1
                    // Before2
                    o?.ToString();
                }
            }
            """);

    [Fact]
    public Task TestIfStatement_Trivia4()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    // Before1
                    [|if|] (o != null)
                    {
                        // Before2
                        o.ToString(); // After
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    // Before1
                    // Before2
                    o?.ToString(); // After
                }
            }
            """);

    [Fact]
    public Task TestIfStatement_Trivia5()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(object o)
                {
                    // Before1
                    [|if|] (o != null)
                    {
                        // Before2
                        o.ToString();
                    } // After
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(object o)
                {
                    // Before1
                    // Before2
                    o?.ToString(); // After
                }
            }
            """);

    [Fact]
    public Task TestNotOnPointer_IfStatement()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                unsafe void M(int* i)
                {
                    if (i != null)
                        i->ToString();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63557")]
    public Task TestNotWithColorColorStaticCase()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class D
            {
                public static void StaticMethod(D d) { }
                public void InstanceMethod(D d) { }
            }

            public class C
            {
                D D { get; }

                public void Test()
                {
                    if (D != null)
                    {
                        D.StaticMethod(D);
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63557")]
    public Task TestWithColorColorInstanceCase()
        => TestInRegularAndScript1Async(
            """
            using System;

            class D
            {
                public static void Method(D d) { }
                public void InstanceMethod(D d) { }
            }

            public class C
            {
                D D { get; }

                public void Test()
                {
                    [|if|] (D != null)
                    {
                        D.InstanceMethod(D);
                    }
                }
            }
            """,
            """
            using System;

            class D
            {
                public static void Method(D d) { }
                public void InstanceMethod(D d) { }
            }

            public class C
            {
                D D { get; }

                public void Test()
                {
                    D?.InstanceMethod(D);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53860")]
    public Task TestWithMethodGroupReference()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                Action<int> M(List<int> p) => p is null ? null : p.Add;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66036")]
    public Task TestElseIfStatement1()
        => TestInRegularAndScript1Async("""
            class C
            {
                void M(string s)
                {
                    if (true)
                    {
                    }
                    else [|if|] (s != null)
                    {
                        s.ToString();
                    }
                }
            }
            """, """
            class C
            {
                void M(string s)
                {
                    if (true)
                    {
                    }
                    else
                    {
                        s?.ToString();
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66036")]
    public Task TestElseIfStatement_NullAssignment1()
        => TestInRegularAndScript1Async("""
            class C
            {
                void M(string s)
                {
                    if (true)
                    {
                    }
                    else [|if|] (s != null)
                    {
                        s.ToString();
                        s = null;
                    }
                }
            }
            """, """
            class C
            {
                void M(string s)
                {
                    if (true)
                    {
                    }
                    else
                    {
                        s?.ToString();
                        s = null;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66036")]
    public Task TestElseIfStatement2()
        => TestInRegularAndScript1Async("""
            class C
            {
                void M(string s)
                {
                    if (true)
                    {
                    }
                    else [|if|] (s != null)
                        s.ToString();
                }
            }
            """, """
            class C
            {
                void M(string s)
                {
                    if (true)
                    {
                    }
                    else
                        s?.ToString();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66036")]
    public Task TestElseIfStatement_Trivia()
        => TestInRegularAndScript1Async("""
            class C
            {
                void M(string s)
                {
                    if (true)
                    {
                    }
                    else [|if|] (s != null)
                    {
                        // comment
                        s.ToString();
                    }
                }
            }
            """, """
            class C
            {
                void M(string s)
                {
                    if (true)
                    {
                    }
                    else
                    {
                        // comment
                        s?.ToString();
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66036")]
    public Task TestElseIfStatement_KeepBracePlacementStyle()
        => TestInRegularAndScript1Async("""
            class C
            {
                void M(string s)
                {
                    if (true)
                    {
                    }
                    else [|if|] (s != null) {
                        s.ToString();
                    }
                }
            }
            """, """
            class C
            {
                void M(string s)
                {
                    if (true)
                    {
                    }
                    else {
                        s?.ToString();
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66141")]
    public Task TestOnValueOffOfNullableValueType1()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(byte? o)
                {
                    object v = [|o == null ? null : o.Value|];
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(byte? o)
                {
                    object v = o;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66141")]
    public Task TestOnValueOffOfNullableValueType2()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                void M(byte? o)
                {
                    object v = [|o != null ? o.Value : null|];
                }
            }
            """,
            """
            using System;

            class C
            {
                void M(byte? o)
                {
                    object v = o;
                }
            }
            """);

    [Fact]
    public Task TestNullConditionalAssignment1()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                int x;

                void M(C c)
                {
                    [|if|] (c != null)
                        c.x = 1;
                }
            }
            """,
            """
            using System;

            class C
            {
                int x;

                void M(C c)
                {
                    c?.x = 1;
                }
            }
            """,
            languageVersion: LanguageVersionExtensions.CSharpNext);

    [Fact]
    public Task TestNullConditionalAssignment2()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                int x;

                void M(C c)
                {
                    if (c != null)
                        c.x = 1;
                }
            }
            """);

    [Fact]
    public Task TestNullAssignmentAfterOperation1()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                int x;

                void M(C c)
                {
                    [|if|] (c != null)
                    {
                        c.x = 1;
                        c = null;
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                int x;

                void M(C c)
                {
                    c?.x = 1;
                    c = null;
                }
            }
            """,
            languageVersion: LanguageVersionExtensions.CSharpNext);

    [Fact]
    public Task TestNullAssignmentAfterOperation2()
        => TestInRegularAndScript1Async(
            """
            using System;

            class C
            {
                int x;

                void M(C c)
                {
                    [|if|] (c != null)
                    {
                        c.x = 1;
                        // Leading comment.
                        c = null;
                    }
                }
            }
            """,
            """
            using System;

            class C
            {
                int x;

                void M(C c)
                {
                    c?.x = 1;
                    // Leading comment.
                    c = null;
                }
            }
            """,
            languageVersion: LanguageVersionExtensions.CSharpNext);

    [Fact]
    public Task TestNotNullAssignmentAfterOperation1()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                int x;

                void M(C c)
                {
                    if (c != null)
                    {
                        c.x = 1;
                        return;
                    }
                }
            }
            """,
            languageVersion: LanguageVersionExtensions.CSharpNext);

    [Fact]
    public Task TestNotNullAssignmentAfterOperation2()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                int x;

                void M(C c)
                {
                    if (c != null)
                    {
                        c.x = 1;
                        c = new();
                    }
                }
            }
            """,
            languageVersion: LanguageVersionExtensions.CSharpNext);

    [Fact]
    public Task TestNotNullAssignmentAfterOperation3()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                int x;

                void M(C c, C d)
                {
                    if (c != null)
                    {
                        c.x = 1;
                        d = null;
                    }
                }
            }
            """,
            languageVersion: LanguageVersionExtensions.CSharpNext);

    [Fact]
    public Task TestNotNullAssignmentAfterOperation4()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                C c;

                void M(C c)
                {
                    if (c != null)
                    {
                        c.c = null;
                        c.c = null;
                    }
                }
            }
            """,
            languageVersion: LanguageVersionExtensions.CSharpNext);

    [Fact]
    public Task TestNullAssignmentAfterOperation_TopLevel1()
        => TestInRegularAndScript1Async(
            """
            using System;
            
            C c = null;
            [|if|] (c != null)
            {
                c.x = 1;
                c = null;
            }

            class C
            {
                public int x;
            }
            """,
            """
            using System;
            
            C c = null;

            c?.x = 1;
            c = null;

            class C
            {
                public int x;
            }
            """,
            OutputKind.ConsoleApplication,
            LanguageVersionExtensions.CSharpNext);

    [Fact]
    public Task TestNullAssignmentAfterOperation_TopLevel2()
        => TestInRegularAndScript1Async(
            """
            using System;
            
            C c = null;
            [|if|] (c != null)
            {
                c.x = 1;
                // Comment
                c = null;
            }

            class C
            {
                public int x;
            }
            """,
            """
            using System;
            
            C c = null;

            c?.x = 1;
            // Comment
            c = null;

            class C
            {
                public int x;
            }
            """,
            OutputKind.ConsoleApplication,
            LanguageVersionExtensions.CSharpNext);
}
