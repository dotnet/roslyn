// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService;
using Microsoft.VisualStudio.LanguageServices.Implementation.F1Help;
using Microsoft.VisualStudio.LanguageServices.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.F1Help;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.F1Help)]
public sealed class F1HelpTests
{
    private static async Task TestAsync([StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup, string expectedText)
    {
        using var workspace = TestWorkspace.CreateCSharp(markup, composition: VisualStudioTestCompositions.LanguageServices);
        var caret = workspace.Documents.First().CursorPosition;

        var service = Assert.IsType<CSharpHelpContextService>(workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetService<IHelpContextService>());
        var actualText = await service.GetHelpTermAsync(workspace.CurrentSolution.Projects.First().Documents.First(), workspace.Documents.First().SelectedSpans.First(), CancellationToken.None);
        Assert.Equal(expectedText, actualText);
    }

    private static Task Test_KeywordAsync([StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup, string expectedText)
        => TestAsync(markup, expectedText + "_CSharpKeyword");

    [Fact]
    public Task TestInternal()
        => Test_KeywordAsync(
            """
            intern[||]al class C
            {
            }
            """, "internal");

    [Fact]
    public Task TestProtected()
        => Test_KeywordAsync(
            """
            public class C
            {
                protec[||]ted void goo();
            }
            """, "protected");

    [Fact]
    public Task TestProtectedInternal1()
        => Test_KeywordAsync(
            """
            public class C
            {
                internal protec[||]ted void goo();
            }
            """, "protectedinternal");

    [Fact]
    public Task TestProtectedInternal2()
        => Test_KeywordAsync(
            """
            public class C
            {
                protec[||]ted internal void goo();
            }
            """, "protectedinternal");

    [Fact]
    public Task TestPrivateProtected1()
        => Test_KeywordAsync(
            """
            public class C
            {
                private protec[||]ted void goo();
            }
            """, "privateprotected");

    [Fact]
    public Task TestPrivateProtected2()
        => Test_KeywordAsync(
            """
            public class C
            {
                priv[||]ate protected void goo();
            }
            """, "privateprotected");

    [Fact]
    public Task TestPrivateProtected3()
        => Test_KeywordAsync(
            """
            public class C
            {
                protected priv[||]ate void goo();
            }
            """, "privateprotected");

    [Fact]
    public Task TestPrivateProtected4()
        => Test_KeywordAsync(
            """
            public class C
            {
                prot[||]ected private void goo();
            }
            """, "privateprotected");

    [Fact]
    public Task TestModifierSoup()
        => Test_KeywordAsync(
"""
public class C
{
    private new prot[||]ected static unsafe void foo()
    {
    }
}
""", "privateprotected");

    [Fact]
    public Task TestModifierSoupField()
        => Test_KeywordAsync(
"""
public class C
{
    new prot[||]ected static unsafe private goo;
}
""", "privateprotected");

    [Fact]
    public Task TestVoid()
        => Test_KeywordAsync(
            """
            class C
            {
                vo[||]id goo()
                {
                }
            }
            """, "void");

    [Fact]
    public Task TestReturn()
        => Test_KeywordAsync(
            """
            class C
            {
                void goo()
                {
                    ret[||]urn;
                }
            }
            """, "return");

    [Fact]
    public Task TestClassPartialType()
        => Test_KeywordAsync(
            """
            part[||]ial class C
            {
                partial void goo();
            }
            """, "partialtype");

    [Fact]
    public Task TestRecordPartialType()
        => Test_KeywordAsync(
            """
            part[||]ial record C
            {
                partial void goo();
            }
            """, "partialtype");

    [Fact]
    public Task TestRecordWithPrimaryConstructorPartialType()
        => Test_KeywordAsync(
            """
            part[||]ial record C(string S)
            {
                partial void goo();
            }
            """, "partialtype");

    [Fact]
    public Task TestPartialMethodInClass()
        => Test_KeywordAsync(
            """
            partial class C
            {
                par[||]tial void goo();
            }
            """, "partialmethod");

    [Fact]
    public Task TestPartialMethodInRecord()
        => Test_KeywordAsync(
            """
            partial record C
            {
                par[||]tial void goo();
            }
            """, "partialmethod");

    [Fact]
    public Task TestExtendedPartialMethod()
        => Test_KeywordAsync(
            """
            partial class C
            {
                public par[||]tial void goo();
            }
            """, "partialmethod");

    [Fact]
    public Task TestWhereClause()
        => Test_KeywordAsync(
            """
            using System.Linq;

            class Program<T> where T : class
            {
                void goo(string[] args)
                {
                    var x = from a in args
                            whe[||]re a.Length > 0
                            select a;
                }
            }
            """, "whereclause");

    [Fact]
    public Task TestWhereConstraint()
        => Test_KeywordAsync(
            """
            using System.Linq;

            class Program<T> wh[||]ere T : class
            {
                void goo(string[] args)
                {
                    var x = from a in args
                            where a.Length > 0
                            select a;
                }
            }
            """, "whereconstraint");

    [Fact]
    public Task TestPreprocessor()
        => TestAsync(
            """
            #regi[||]on
            #endregion
            """, "#region");

    [Fact]
    public Task TestPreprocessor2()
        => TestAsync(
            """
            #region[||]
            #endregion
            """, "#region");

    [Fact]
    public Task TestConstructor()
        => TestAsync(
            """
            namespace N
            {
                class C
                {
                    void goo()
                    {
                        var x = new [|C|]();
                    }
                }
            }
            """, "N.C.#ctor");

    [Fact]
    public Task TestGenericClass()
        => TestAsync(
            """
            namespace N
            {
                class C<T>
                {
                    void goo()
                    {
                        [|C|]<int> c;
                    }
                }
            }
            """, "N.C`1");

    [Fact]
    public Task TestGenericMethod()
        => TestAsync(
            """
            namespace N
            {
                class C<T>
                {
                    void goo<T, U, V>(T t, U u, V v)
                    {
                        C<int> c;
                        c.g[|oo|](1, 1, 1);
                    }
                }
            }
            """, "N.C`1.goo``3");

    [Theory]
    [InlineData("+")]
    [InlineData("-")]
    [InlineData("&")]
    [InlineData("|")]
    [InlineData("/")]
    [InlineData("^")]
    [InlineData(">")]
    [InlineData(">=")]
    [InlineData("!=")]
    [InlineData("<")]
    [InlineData("<=")]
    [InlineData("<<")]
    [InlineData(">>")]
    [InlineData(">>>")]
    [InlineData("*")]
    [InlineData("%")]
    [InlineData("&&")]
    [InlineData("||")]
    [InlineData("==")]
    public Task TestBinaryOperator(string operatorText)
        => TestAsync(
            $$"""
            namespace N
            {
                class C
                {
                    void goo()
                    {
                        var two = 1 [|{{operatorText}}|] 1;
                    }
                }
            }
            """, $"{operatorText}_CSharpKeyword");

    [Theory]
    [InlineData("+=")]
    [InlineData("-=")]
    [InlineData("/=")]
    [InlineData("*=")]
    [InlineData("%=")]
    [InlineData("&=")]
    [InlineData("|=")]
    [InlineData("^=")]
    [InlineData("<<=")]
    [InlineData(">>=")]
    [InlineData(">>>=")]
    public Task TestCompoundOperator(string operatorText)
        => TestAsync(
            $$"""
            namespace N
            {
                class C
                {
                    void goo(int x)
                    {
                        x [|{{operatorText}}|] x;
                    }
                }
            }
            """, $"{operatorText}_CSharpKeyword");

    [Theory]
    [InlineData("++")]
    [InlineData("--")]
    [InlineData("!")]
    [InlineData("~")]
    public Task TestPrefixOperator(string operatorText)
        => TestAsync(
            $$"""
            namespace N
            {
                class C
                {
                    void goo(int x)
                    {
                        x = [|{{operatorText}}|]x;
                    }
                }
            }
            """, $"{operatorText}_CSharpKeyword");

    [Theory]
    [InlineData("++")]
    [InlineData("--")]
    public Task TestPostfixOperator(string operatorText)
        => TestAsync(
            $$"""
            namespace N
            {
                class C
                {
                    void goo(int x)
                    {
                        x = x[|{{operatorText}}|];
                    }
                }
            }
            """, $"{operatorText}_CSharpKeyword");

    [Fact]
    public Task TestRelationalPattern()
        => TestAsync(
            """
            namespace N
            {
                class C
                {
                    void goo(string x)
                    {
                        if (x is { Length: [||]> 5 }) { }
                    }
                }
            }
            """, ">_CSharpKeyword");

    [Fact]
    public Task TestGreaterThanInFunctionPointer()
        => TestAsync("""
            unsafe class C
            {
                delegate*[||]<int> f;
            }
            """, "functionPointer_CSharpKeyword");

    [Fact]
    public Task TestLessThanInFunctionPointer()
        => TestAsync("""
            unsafe class C
            {
                delegate*[||]<int> f;
            }
            """, "functionPointer_CSharpKeyword");

    [Fact]
    public Task TestEqualsOperatorInParameter()
        => TestAsync(
            """
            namespace N
            {
                class C
                {
                    void goo(int x [|=|] 0)
                    {
                    }
                }
            }
            """, "optionalParameter_CSharpKeyword");

    [Fact]
    public Task TestEqualsOperatorInPropertyInitializer()
        => TestAsync(
            """
            namespace N
            {
                class C
                {
                    int P { get; } [|=|] 5;
                }
            }
            """, "propertyInitializer_CSharpKeyword");

    [Fact]
    public Task TestVar()
        => TestAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;

            class Program
            {
                static void Main(string[] args)
                {
                    var[||] x = 3;
                }
            }
            """, "var_CSharpKeyword");

    [Fact]
    public Task TestEquals()
        => TestAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;

            class Program
            {
                static void Main(string[] args)
                {
                    var x =[||] 3;
                }
            }
            """, "=_CSharpKeyword");

    [Fact]
    public Task TestEqualsInEnum()
        => TestAsync(
            """
            enum E
            {
                A [||]= 1
            }
            """, "enum_CSharpKeyword");

    [Fact]
    public Task TestEqualsInAttribute()
        => TestAsync(
            """
            using System;

            [AttributeUsage(AttributeTargets.Class, Inherited [|=|] true)]
            class MyAttribute : Attribute
            {
            }
            """, "attributeNamedArgument_CSharpKeyword");

    [Fact]
    public Task TestEqualsInUsingAlias()
        => TestAsync(
            """
            using SC [||]= System.Console;
            """, "using_CSharpKeyword");

    [Fact]
    public Task TestEqualsInAnonymousObjectMemberDeclarator()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    var x = new { X [||]= 0 };
                }
            }
            """, "anonymousObject_CSharpKeyword");

    [Fact]
    public Task TestEqualsInDocumentationComment()
        => TestAsync(
            """
            class C
            {
                /// <summary>
                /// <a b[||]="c" />
                /// </summary>
                void M()
                {
                    var x = new { X [||]= 0 };
                }
            }
            """, "see");

    [Fact]
    public Task TestEqualsInLet()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    var y =
                        from x1 in x2
                        let x3 [||]= x4
                        select x5;
                }
            }
            """, "let_CSharpKeyword");

    [Fact]
    public Task TestLetKeyword()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    var y =
                        from x1 in x2
                        [||]let x3 = x4
                        select x5;
                }
            }
            """, "let_CSharpKeyword");

    [Fact]
    public Task TestFromIn()
        => TestAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;

            class Program
            {
                static void Main(string[] args)
                {
                    var x = from n i[||]n {
                        1}

                    select n
                }
            }
            """, "from_CSharpKeyword");

    [Fact]
    public Task TestProperty()
        => TestAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;

            class Program
            {
                static void Main(string[] args)
                {
                    new UriBuilder().Fragm[||]ent;
                }
            }
            """, "System.UriBuilder.Fragment");

    [Fact]
    public Task TestForeachIn()
        => TestAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;

            class Program
            {
                static void Main(string[] args)
                {
                    foreach (var x in[||] {
                        1} )
                    {
                    }
                }
            }
            """, "in_CSharpKeyword");

    [Fact]
    public Task TestRegionDescription()
        => TestAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    #region Begin MyR[||]egion for testing
                    #endregion End
                }
            }
            """, "#region");

    [Fact]
    public Task TestGenericAngle_LessThanToken_TypeArgument()
        => TestAsync(
            """
            class Program
            {
                static void generic<T>(T t)
                {
                    generic[||]<int>(0);
                }
            }
            """, "generics_CSharpKeyword");

    [Fact]
    public Task TestGenericAngle_GreaterThanToken_TypeArgument()
        => TestAsync(
            """
            class Program
            {
                static void generic<T>(T t)
                {
                    generic<int[|>|](0);
                }
            }
            """, "generics_CSharpKeyword");

    [Fact]
    public Task TestGenericAngle_LessThanToken_TypeParameter()
        => TestAsync(
            """
            class Program
            {
                static void generic[|<|]T>(T t)
                {
                    generic<int>(0);
                }
            }
            """, "generics_CSharpKeyword");

    [Fact]
    public Task TestGenericAngle_GreaterThanToken_TypeParameter()
        => TestAsync(
            """
            class Program
            {
                static void generic<T[|>|](T t)
                {
                    generic<int>(0);
                }
            }
            """, "generics_CSharpKeyword");

    [Fact]
    public Task TestLocalReferenceIsType()
        => TestAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;

            class Program
            {
                static void Main(string[] args)
                {
                    int x;
                    x[||];
                }
            }
            """, "System.Int32");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/864266")]
    public Task TestConstantField()
        => TestAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var i = int.Ma[||]xValue;
                }
            }
            """, "System.Int32.MaxValue");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/862420")]
    public Task TestParameter()
        => TestAsync(
            """
            class Class2
            {
                void M1(int par[||]ameter)  // 1
                {
                }

                void M2()
                {
                    int argument = 1;
                    M1(parameter: argument);   // 2
                }
            }
            """, "System.Int32");

    [Fact]
    public Task TestRefReadonlyParameter_Ref()
        => TestAsync(
            """
            class C
            {
                void M(r[||]ef readonly int x)
                {
                }
            }
            """, "ref_CSharpKeyword");

    [Fact]
    public Task TestRefReadonlyParameter_ReadOnly()
        => TestAsync(
            """
            class C
            {
                void M(ref read[||]only int x)
                {
                }
            }
            """, "readonly_CSharpKeyword");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/862420")]
    public Task TestArgumentType()
        => TestAsync(
            """
            class Class2
            {
                void M1(int pa[||]rameter)  // 1
                {
                }

                void M2()
                {
                    int argument = 1;
                    M1(parameter: argument);   // 2
                }
            }
            """, "System.Int32");

    [Fact]
    public Task TestYieldReturn_OnYield()
        => TestAsync("""
            using System.Collections.Generic;

            public class C
            {
                public IEnumerable<int> M()
                {
                    [|yield|] return 0;
                }
            }
            """, "yield_CSharpKeyword");

    [Fact]
    public Task TestYieldReturn_OnReturn()
        => TestAsync("""
            using System.Collections.Generic;

            public class C
            {
                public IEnumerable<int> M()
                {
                    yield [|return|] 0;
                }
            }
            """, "yield_CSharpKeyword");

    [Fact]
    public Task TestYieldBreak_OnYield()
        => TestAsync("""
            using System.Collections.Generic;

            public class C
            {
                public IEnumerable<int> M()
                {
                    [|yield|] break;
                }
            }
            """, "yield_CSharpKeyword");

    [Fact]
    public Task TestYieldBreak_OnBreak()
        => TestAsync("""
            using System.Collections.Generic;

            public class C
            {
                public IEnumerable<int> M()
                {
                    yield [|break|] 0;
                }
            }
            """, "yield_CSharpKeyword");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/862396")]
    public Task TestNoToken()
        => TestAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                }
            }[||]
            """, "vs.texteditor");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/862328")]
    public Task TestLiteral()
        => TestAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    Main(new string[] { "fo[||]o" });
                }
            }
            """, "System.String");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/862478")]
    public Task TestColonColon()
        => TestAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;

            class Program
            {
                static void Main(string[] args)
                {
                    global:[||]:System.Console.Write(");
                }
            }
            """, "::_CSharpKeyword");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46986")]
    public Task TestStringInterpolation()
        => TestAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine($[||]"Hello, {args[0]}");
                }
            }
            """, "$_CSharpKeyword");

    [Fact]
    public Task TestUtf8String()
        => TestAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    var x = "Hel[||]lo"u8;
                }
            }
            """, "Utf8StringLiteral_CSharpKeyword");

    [Fact]
    public Task TestRawString()
        => TestAsync(
            """"
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    var x = """Hel[||]lo""";
                }
            }
            """", "RawStringLiteral_CSharpKeyword");

    [Fact]
    public Task TestUtf8RawString()
        => TestAsync(
            """"
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    var x = """Hel[||]lo"""u8;
                }
            }
            """", "Utf8StringLiteral_CSharpKeyword");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46986")]
    public Task TestVerbatimString()
        => TestAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine(@[||]"Hello\");
                }
            }
            """, "@_CSharpKeyword");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46986")]
    public Task TestVerbatimInterpolatedString1()
        => TestAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine(@[||]$"Hello\ {args[0]}");
                }
            }
            """, "@$_CSharpKeyword");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46986")]
    public Task TestVerbatimInterpolatedString2()
        => TestAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine($[||]@"Hello\ {args[0]}");
                }
            }
            """, "@$_CSharpKeyword");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/864658")]
    public Task TestNullable()
        => TestAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;

            class Program
            {
                static void Main(string[] args)
                {
                    int?[||] a = int.MaxValue;
                    a.Value.GetHashCode();
                }
            }
            """, "System.Nullable`1");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/863517")]
    public Task TestAfterLastToken()
        => TestAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;

            class Program
            {
                static void Main(string[] args)
                {
                    foreach (char var in "!!!")$$[||]
                    {
                    }
                }
            }
            """, "vs.texteditor");

    [Fact]
    public Task TestConditional()
        => TestAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    var x = true [|?|] true : false;
                }
            }
            """, "?_CSharpKeyword");

    [Fact]
    public Task TestLocalVar()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    var a = 0;
                    int v[||]ar = 1;
                }
            }
            """, "System.Int32");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/867574")]
    public Task TestFatArrow()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    var a = new System.Action(() =[||]> {
                    });
                }
            }
            """, "=>_CSharpKeyword");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/867572")]
    public Task TestSubscription()
        => TestAsync(
            """
            class CCC
            {
                event System.Action e;

                void M()
                {
                    e +[||]= () => {
                    };
                }
            }
            """, "+=_CSharpKeyword");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/867554")]
    public Task TestComment()
        => TestAsync(@"// some comm[||]ents here", "comments");

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/867529")]
    public Task TestDynamic()
        => TestAsync(
            """
            class C
            {
                void M()
                {
                    dyna[||]mic d = 0;
                }
            }
            """, "dynamic_CSharpKeyword");

    [Fact]
    public Task TestRangeVariable()
        => TestAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;

            class Program
            {
                static void Main(string[] args)
                {
                    var zzz = from y in args
                              select [||]y;
                }
            }
            """, "System.String");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36001")]
    public Task TestNameof()
        => Test_KeywordAsync(
            """
            class C
            {
                void goo()
                {
                    var v = [||]nameof(goo);
                }
            }
            """, "nameof");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46988")]
    public Task TestNullForgiving()
        => Test_KeywordAsync(
            """
            #nullable enable
            class C
            {
                int goo(string? x)
                {
                    return x[||]!.GetHashCode();
                }
            }
            """, "nullForgiving");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46988")]
    public Task TestLogicalNot()
        => Test_KeywordAsync(
            """
            class C
            {
                bool goo(bool x)
                {
                    return [||]!x;
                }
            }
            """, "!");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestDefaultSwitchCase()
        => Test_KeywordAsync(
            """
            class C
            {
                void M1(int parameter)
                {
                    switch(parameter) {
                        defa[||]ult:
                            parameter = default;
                            break;
                    }
                }
            }
            """, "defaultcase");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestDefaultLiteralExpressionInsideSwitch()
        => Test_KeywordAsync(
            """
            class C
            {
                void M1(int parameter)
                {
                    switch(parameter) {
                        default:
                            parameter = defa[||]ult;
                            break;
                    }
                }
            }
            """, "default");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestDefaultExpressionInsideSwitch()
        => Test_KeywordAsync(
            """
            class C
            {
                void M1(int parameter)
                {
                    switch(parameter) {
                        default:
                            parameter = defa[||]ult(int);
                            break;
                    }
                }
            }
            """, "default");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestDefaultLiteralExpression()
        => Test_KeywordAsync(
            """
            class C
            {
                int field = defa[||]ult;
            }
            """, "default");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestDefaultExpression()
        => Test_KeywordAsync(
            """
            class C
            {
                int field = defa[||]ult(int);
            }
            """, "default");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestDefaultLiteralExpressionInOptionalParameter()
        => Test_KeywordAsync(
            """
            class C
            {
                void M1(int parameter = defa[||]ult) {
                }
            }
            """, "default");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestDefaultExpressionInOptionalParameter()
        => Test_KeywordAsync(
            """
            class C
            {
                void M1(int parameter = defa[||]ult(int)) {
                }
            }
            """, "default");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestDefaultLiteralExpressionInMethodCall()
        => Test_KeywordAsync(
            """
            class C
            {
                void M1() {
                    M2(defa[||]ult);
                }
            }
            """, "default");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestDefaultExpressionInMethodCall()
        => Test_KeywordAsync(
            """
            class C
            {
                void M1() {
                    M2(defa[||]ult(int));
                }
            }
            """, "default");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestOuterClassDeclaration()
        => Test_KeywordAsync(
            """
            cla[||]ss OuterClass<T> where T : class
            { 
                class InnerClass<T> where T : class { }
            }
            """, "class");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestInnerClassDeclaration()
        => Test_KeywordAsync(
            """
            class OuterClass<T> where T : class
            { 
                cla[||]ss InnerClass<T> where T : class { }
            }
            """, "class");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestClassConstraintInOuterClass()
        => Test_KeywordAsync(
            """
            class OuterClass<T> where T : cla[||]ss
            { 
                class InnerClass<T> where T : class { }
            }
            """, "classconstraint");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestClassConstraintInInnerClass()
        => Test_KeywordAsync(
            """
            class OuterClass<T> where T : class
            { 
                class InnerClass<T> where T : cla[||]ss { }
            }
            """, "classconstraint");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestClassConstraintInGenericMethod()
        => Test_KeywordAsync(
            """
            class C
            { 
                void M1<T>() where T : cla[||]ss { }
            }
            """, "classconstraint");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestClassConstraintInGenericDelegate()
        => Test_KeywordAsync(
            """
            class C
            { 
                delegate T MyDelegate<T>() where T : cla[||]ss;
            }
            """, "classconstraint");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestOuterStructDeclaration()
        => Test_KeywordAsync(
            """
            str[||]uct OuterStruct<T> where T : struct
            { 
                struct InnerStruct<T> where T : struct { }
            }
            """, "struct");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestInnerStructDeclaration()
        => Test_KeywordAsync(
            """
            struct OuterStruct<T> where T : struct
            { 
                str[||]uct InnerStruct<T> where T : struct { }
            }
            """, "struct");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestStructConstraintInOuterStruct()
        => Test_KeywordAsync(
            """
            struct OuterStruct<T> where T : str[||]uct
            { 
                struct InnerStruct<T> where T : struct { }
            }
            """, "structconstraint");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestStructConstraintInInnerStruct()
        => Test_KeywordAsync(
            """
            struct OuterStruct<T> where T : struct
            { 
                struct InnerStruct<T> where T : str[||]uct { }
            }
            """, "structconstraint");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestStructConstraintInGenericMethod()
        => Test_KeywordAsync(
            """
            struct C
            { 
                void M1<T>() where T : str[||]uct { }
            }
            """, "structconstraint");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestStructConstraintInGenericDelegate()
        => Test_KeywordAsync(
            """
            struct C
            { 
                delegate T MyDelegate<T>() where T : str[||]uct;
            }
            """, "structconstraint");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestAllowsRefStructAntiConstraint()
        => Test_KeywordAsync(
            """
            class C
            { 
                void M<T>()
                    where T : all[||]ows ref struct
                {
                }
            }
            """, "allows");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestUsingStaticOnUsingKeyword()
        => Test_KeywordAsync(
            """
            us[||]ing static namespace.Class;

            static class C
            { 
                static int Field;

                static void Method() {}
            }
            """, "using-static");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestNormalUsingDirective()
        => Test_KeywordAsync(
            """
            us[||]ing namespace.Class;

            static class C
            { 
                static int Field;

                static void Method() {}
            }
            """, "using");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestUsingStatement()
        => Test_KeywordAsync(
            """
            using namespace.Class;

            class C
            { 
                void Method(String someString) {
                    us[||]ing (var reader = new StringReader(someString))
                    {
                    }
                }
            }
            """, "using-statement");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestUsingDeclaration()
        => Test_KeywordAsync(
            """
            using namespace.Class;

            class C
            { 
                void Method(String someString) {
                    us[||]ing var reader = new StringReader(someString);
                }
            }
            """, "using-statement");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestUsingStaticOnStaticKeyword()
        => Test_KeywordAsync(
            """
            using sta[||]tic namespace.Class;

            static class C
            { 
                static int Field;

                static void Method() {}
            }
            """, "using-static");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestStaticClass()
        => Test_KeywordAsync(
            """
            using static namespace.Class;

            sta[||]tic class C
            { 
                static int Field;

                static void Method() {}
            }
            """, "static");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestStaticField()
        => Test_KeywordAsync(
            """
            using static namespace.Class;

            static class C
            { 
                sta[||]tic int Field;

                static void Method() {}
            }
            """, "static");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
    public Task TestStaticMethod()
        => Test_KeywordAsync(
            """
            using static namespace.Class;

            static class C
            { 
                static int Field;

                sta[||]tic void Method() {}
            }
            """, "static");

    [Fact]
    public Task TestWithKeyword()
        => Test_KeywordAsync(
            """
            public record Point(int X, int Y);

            public static class Program
            { 
                public static void Main()
                {
                    var p1 = new Point(0, 0);
                    var p2 = p1 w[||]ith { X = 5 };
                }
            }
            """, "with");

    [Fact]
    public Task TestDiscard()
        => Test_KeywordAsync(
            """
            class C
            {
                void M()
                {
                    [||]_ = Goo();
                }

                object Goo() => null;
            }
            """, "discard");

    [Fact]
    public Task TestChecked_01()
        => Test_KeywordAsync(
            """
            public class C
            {
                void goo()
                {
                    chec[||]ked
                    {
                    }
                }
            }
            """, "checked");

    [Fact]
    public Task TestChecked_02()
        => Test_KeywordAsync(
            """
            public class C
            {
                int goo()
                {
                    return chec[||]ked(0);
                }
            }
            """, "checked");

    [Fact]
    public Task TestChecked_03()
        => Test_KeywordAsync(
            """
            public class C
            {
                C operator chec[||]ked -(C x) {}
            }
            """, "checked");

    [Fact]
    public Task TestChecked_04()
        => Test_KeywordAsync(
            """
            public class C
            {
                C operator chec[||]ked +(C x, C y) {}
            }
            """, "checked");

    [Fact]
    public Task TestChecked_05()
        => Test_KeywordAsync(
            """
            public class C
            {
                explicit operator chec[||]ked string(C x) {}
            }
            """, "checked");

    [Fact]
    public Task TestChecked_06()
        => Test_KeywordAsync(
            """
            public class C
            {
                C I1.operator chec[||]ked -(C x) {}
            }
            """, "checked");

    [Fact]
    public Task TestChecked_07()
        => Test_KeywordAsync(
            """
            public class C
            {
                C I1.operator chec[||]ked +(C x, C y) {}
            }
            """, "checked");

    [Fact]
    public Task TestChecked_08()
        => Test_KeywordAsync(
            """
            public class C
            {
                explicit I1.operator chec[||]ked string(C x) {}
            }
            """, "checked");

    [Fact]
    public Task TestChecked_09()
        => Test_KeywordAsync(
            """
            public class C
            {
                /// <summary>
                /// <see cref="operator chec[||]ked +(C, C)"/>
                /// </summary>
                void goo()
                {
                }
            }
            """, "checked");

    [Fact]
    public Task TestChecked_10()
        => Test_KeywordAsync(
            """
            public class C
            {
                /// <summary>
                /// <see cref="operator chec[||]ked -(C)"/>
                /// </summary>
                void goo()
                {
                }
            }
            """, "checked");

    [Fact]
    public Task TestChecked_11()
        => Test_KeywordAsync(
            """
            public class C
            {
                /// <summary>
                /// <see cref="explicit operator chec[||]ked string(C)"/>
                /// </summary>
                void goo()
                {
                }
            }
            """, "checked");

    [Fact]
    public Task TestRequired()
        => Test_KeywordAsync("""
            public class C
            {
                re[||]quired int Field;
            }
            """, "required");

    [Fact]
    public Task TestScoped()
        => Test_KeywordAsync("""
            sc[||]oped var r = new R();
            ref struct R
            {
            }
            """, "scoped");

    [Fact]
    public Task TestDefaultConstraint()
        => Test_KeywordAsync("""
            public class Base
            {
                virtual void M<T>(T? t) { }
            }
            public class C
            {
                override void M<T>() where T : def[||]ault { }
            }
            """, expectedText: "defaultconstraint");

    [Fact]
    public Task TestDefaultCase()
        => Test_KeywordAsync("""
            public class C
            {
                void M(object o)
                {
                    switch (o)
                    {
                        case 1:
                            goto def[||]ault;
                        default:
                            return;
                    }
                }
            }
            """, expectedText: "defaultcase");

    [Fact]
    public Task TestGotoDefault()
        => Test_KeywordAsync("""
            public class C
            {
                void M(object o)
                {
                    switch (o)
                    {
                        case 1:
                            goto default;
                        def[||]ault:
                            return;
                    }
                }
            }
            """, expectedText: "defaultcase");

    [Fact]
    public Task TestLineDefault()
        => Test_KeywordAsync("""
            #line def[||]ault
            """, expectedText: "defaultline");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65311")]
    public Task TestNotnull_OnType()
        => Test_KeywordAsync("""
            public class C<T> where T : not[||]null
            {
            }
            """, expectedText: "notnull");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65311")]
    public Task TestNotnull_OnMethod()
        => Test_KeywordAsync("""
            public class C
            {
                void M<T>() where T : not[||]null
                {
                }
            }
            """, expectedText: "notnull");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65311")]
    public Task TestNotnull_FieldName()
        => TestAsync("""
            public class C
            {
                int not[||]null = 0;
            }
            """, expectedText: "C.notnull");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65311")]
    public Task TestUnmanaged_OnType()
        => Test_KeywordAsync("""
            public class C<T> where T : un[||]managed
            {
            }
            """, expectedText: "unmanaged");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65311")]
    public Task TestUnmanaged_OnMethod()
        => Test_KeywordAsync("""
            public class C
            {
                void M<T>() where T : un[||]managed
                {
                }
            }
            """, expectedText: "unmanaged");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65311")]
    public Task TestUnmanaged_LocalName()
        => TestAsync("""
            int un[||]managed = 0;
            """, expectedText: "System.Int32");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65312")]
    public Task TestSwitchStatement()
        => Test_KeywordAsync("""
            swit[||]ch (1) { default: break; }
            """, expectedText: "switch");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65312")]
    public Task TestSwitchExpression()
        => Test_KeywordAsync("""
            _ = 1 swit[||]ch { _ => 0 };
            """, expectedText: "switch-expression");

    [Fact]
    public Task TestFile()
        => Test_KeywordAsync("""
            fi[||]le class C { }
            """, expectedText: "file");

    [Fact]
    public Task TestRightShift()
        => Test_KeywordAsync("""
            _ = 1 >[||]> 2;
            """, expectedText: ">>");

    [Fact]
    public Task TestUnsignedRightShift()
        => Test_KeywordAsync("""
            _ = 1 >>[||]> 2;
            """, expectedText: ">>>");

    [Fact]
    public Task TestUnsignedRightShiftAssignment()
        => Test_KeywordAsync("""
            1 >>[||]>= 2;
            """, expectedText: ">>>=");

    [Fact]
    public Task TestPreprocessorIf()
        => TestAsync(
            """
            #i[||]f ANY
            #endif
            """, "#if");

    [Fact]
    public Task TestPreprocessorIf2()
        => TestAsync(
            """
            #if ANY[||]
            #endif
            """, "#if");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66009")]
    public Task TestPreprocessorIf3()
        => TestAsync(
            """
            #if ANY[||]THING
            #endif
            """, "#if");

    [Fact]
    public Task TestPreprocessorEndIf()
        => TestAsync(
            """
            #if ANY
            #en[||]dif
            """, "#endif");

    [Fact]
    public Task TestPreprocessorEndIf2()
        => TestAsync(
            """
            #if ANY
            #endif[||]

            """, "#endif");

    [Fact]
    public Task TestPreprocessorElse()
        => TestAsync(
            """
            #if ANY
            #el[||]se
            #endif
            """, "#else");

    [Fact]
    public Task TestPreprocessorElse2()
        => TestAsync(
            """
            #if ANY
            #else[||]
            #endif
            """, "#else");

    [Fact]
    public Task TestPreprocessorElIf()
        => TestAsync(
            """
            #if ANY
            #el[||]if SOME
            #endif
            """, "#elif");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66009")]
    public Task TestPreprocessorElIf2()
        => TestAsync(
            """
            #if ANY
            #elif S[||]OME
            #endif
            """, "#elif");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66009")]
    public Task TestPreprocessorPragmaWarning1()
        => TestAsync(
            """
            #pragma warning disable CS[||]0312
            """, "#pragma");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66009")]
    public Task TestPreprocessorPragmaWarning2()
        => TestAsync(
            """
            #pragm[||]a warning disable CS0312
            """, "#pragma");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66009")]
    public Task TestPreprocessorPragmaWarning3()
        => TestAsync(
            """
            #pragma warni[||]ng disable CS0312
            """, "#warning");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66009")]
    public Task TestPreprocessorPragmaWarning4()
        => TestAsync(
            """
            #pragma warning dis[||]able CS0312
            """, "#disable");

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/68009")]
    public Task TestGlobalUsing1()
        => Test_KeywordAsync(
            """
            [||]global using System;
            """, "global-using");
}
