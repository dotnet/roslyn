// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseNullPropagation;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseNullPropagation
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpUseNullPropagationDiagnosticAnalyzer,
        CSharpUseNullPropagationCodeFixProvider>;

    [Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)]
    public partial class UseNullPropagationTests
    {
        private static async Task TestInRegularAndScript1Async(string testCode, string fixedCode, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
        {
            await new VerifyCS.Test
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                // code action is currently generating invalid trees.  Specifically, it transforms `x.Y()` into `x.?Y()`
                // by just rewriting `x.Y` into `x?.Y`.  That is not correct.  the RHS of the `?` should `.Y()` not
                // `.Y`.
                CodeActionValidationMode = CodeActionValidationMode.None,
                LanguageVersion = LanguageVersion.CSharp9,
                TestState =
                {
                    OutputKind = outputKind,
                },
            }.RunAsync();
        }

        private static async Task TestMissingInRegularAndScriptAsync(string testCode, LanguageVersion languageVersion = LanguageVersion.CSharp9)
        {
            await new VerifyCS.Test
            {
                TestCode = testCode,
                FixedCode = testCode,
                LanguageVersion = languageVersion,
            }.RunAsync();
        }

        [Fact]
        public async Task TestLeft_Equals()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestLeft_Equals_IfStatement()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestIfStatement_WithBlock()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestIfStatement_NotWithElse()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestIfStatement_NotWithMultipleStatements()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestLeft_Equals_IfStatement_TopLevel()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestLeft_IsNull()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestLeft_IsNotNull()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestLeft_IsNotNull_IfStatement()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestMissingOnCSharp5()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMissingOnCSharp5_IfStatement()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestRight_Equals()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestRight_Equals_IfStatement()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestLeft_NotEquals()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestWithNullableType()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestWithNullableType_IfStatement()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestWithNullableTypeAndObjectCast()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestWithNullableTypeAndObjectCast_IfStatement()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestRight_NotEquals()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestIndexer()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestIndexer_IfStatement()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestConditionalAccess()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestConditionalAccess_IfStatement()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestMemberAccess()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestMemberAccess_IfStatement()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestMissingOnSimpleMatch()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMissingOnSimpleMatch_IfStatement()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestParenthesizedCondition()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestFixAll1()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestFixAll2()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15505")]
        public async Task TestOtherValueIsNotNull1()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/15505")]
        public async Task TestOtherValueIsNotNull2()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16287")]
        public async Task TestMethodGroup()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17623")]
        public async Task TestInExpressionTree()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17623")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/33992")]
        public async Task TestInExpressionTree2()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17623")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/33992")]
        public async Task TestInExpressionTree3()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17623")]
        [WorkItem("https://github.com/dotnet/roslyn/issues/33992")]
        public async Task TestInExpressionTree4()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19774")]
        public async Task TestNullableMemberAccess()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19774")]
        public async Task TestNullableMemberAccess_IfStatement()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19774")]
        public async Task TestNullableElementAccess()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndIsNull()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndIsNotNull()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndIsNotNull_IfStatement()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndIsType()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndIsType_IfStatement1()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndIsType_IfStatement2()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndIsType_IfStatement3()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestIsOtherConstant()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                class C
                {
                    void M(string s)
                    {
                        int? x = s is "" ? null : (int?)s.Length;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndReferenceEquals1()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndReferenceEquals1_IfStatement()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndReferenceEquals2()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndReferenceEquals2_IfStatement()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndReferenceEqualsOtherValue1()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndReferenceEqualsOtherValue1_IfStatement1()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndReferenceEqualsOtherValue1_IfStatement2()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndReferenceEqualsOtherValue2()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndReferenceEqualsWithObject1()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndReferenceEqualsWithObject1_IfStatement()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndReferenceEqualsWithObject2()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndReferenceEqualsWithObject2_IfStatement()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndReferenceEqualsOtherValueWithObject1()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndReferenceEqualsOtherValueWithObject2()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndNotIsNull()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndNotIsNotNull()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndNotIsType()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndNotIsOtherConstant()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                class C
                {
                    void M(string s)
                    {
                        int? x = !(s is "") ? (int?)s.Length : null;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndLogicalNotReferenceEquals1()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndLogicalNotReferenceEquals2()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndLogicalNotReferenceEqualsOtherValue1()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndLogicalNotReferenceEqualsOtherValue2()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndLogicalNotReferenceEqualsWithObject1()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndLogicalNotReferenceEqualsWithObject2()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndLogicalNotReferenceEqualsOtherValueWithObject1()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestWithNullableTypeAndLogicalNotReferenceEqualsOtherValueWithObject2()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestEqualsWithLogicalNot()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestEqualsWithLogicalNot_IfStatement()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestNotEqualsWithLogicalNot()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestEqualsOtherValueWithLogicalNot()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")]
        public async Task TestNotEqualsOtherValueWithLogicalNot()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49517")]
        public async Task TestParenthesizedExpression()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49517")]
        public async Task TestReversedParenthesizedExpression()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49517")]
        public async Task TestParenthesizedNull()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/49517")]
        public async Task TestReversedParenthesizedNull()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestIfStatement_Trivia1()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestIfStatement_Trivia2()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestIfStatement_Trivia3()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestIfStatement_Trivia4()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestIfStatement_Trivia5()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact]
        public async Task TestNotOnPointer_IfStatement()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63557")]
        public async Task TestNotWithColorColorStaticCase()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63557")]
        public async Task TestWithColorColorInstanceCase()
        {
            await TestInRegularAndScript1Async(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/53860")]
        public async Task TestWithMethodGroupReference()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                using System;
                using System.Collections.Generic;

                class C
                {
                    Action<int> M(List<int> p) => p is null ? null : p.Add;
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66036")]
        public async Task TestElseIfStatement1()
        {
            await TestInRegularAndScript1Async("""
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66036")]
        public async Task TestElseIfStatement2()
        {
            await TestInRegularAndScript1Async("""
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66036")]
        public async Task TestElseIfStatement_Trivia()
        {
            await TestInRegularAndScript1Async("""
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66036")]
        public async Task TestElseIfStatement_KeepBracePlacementStyle()
        {
            await TestInRegularAndScript1Async("""
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
        }
    }
}
