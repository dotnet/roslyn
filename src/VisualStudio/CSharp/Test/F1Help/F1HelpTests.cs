// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService;
using Microsoft.VisualStudio.LanguageServices.Implementation.F1Help;
using Microsoft.VisualStudio.LanguageServices.UnitTests;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.F1Help
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.F1Help)]
    public class F1HelpTests
    {
        private static async Task TestAsync(string markup, string expectedText)
        {
            using var workspace = TestWorkspace.CreateCSharp(markup, composition: VisualStudioTestCompositions.LanguageServices);
            var caret = workspace.Documents.First().CursorPosition;

            var service = Assert.IsType<CSharpHelpContextService>(workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetService<IHelpContextService>());
            var actualText = await service.GetHelpTermAsync(workspace.CurrentSolution.Projects.First().Documents.First(), workspace.Documents.First().SelectedSpans.First(), CancellationToken.None);
            Assert.Equal(expectedText, actualText);
        }

        private static async Task Test_KeywordAsync(string markup, string expectedText)
        {
            await TestAsync(markup, expectedText + "_CSharpKeyword");
        }

        [Fact]
        public async Task TestInternal()
        {
            await Test_KeywordAsync(
@"intern[||]al class C
{
}", "internal");
        }

        [Fact]
        public async Task TestProtected()
        {
            await Test_KeywordAsync(
@"public class C
{
    protec[||]ted void goo();
}", "protected");
        }

        [Fact]
        public async Task TestProtectedInternal1()
        {
            await Test_KeywordAsync(
@"public class C
{
    internal protec[||]ted void goo();
}", "protectedinternal");
        }

        [Fact]
        public async Task TestProtectedInternal2()
        {
            await Test_KeywordAsync(
@"public class C
{
    protec[||]ted internal void goo();
}", "protectedinternal");
        }

        [Fact]
        public async Task TestPrivateProtected1()
        {
            await Test_KeywordAsync(
@"public class C
{
    private protec[||]ted void goo();
}", "privateprotected");
        }

        [Fact]
        public async Task TestPrivateProtected2()
        {
            await Test_KeywordAsync(
@"public class C
{
    priv[||]ate protected void goo();
}", "privateprotected");
        }

        [Fact]
        public async Task TestPrivateProtected3()
        {
            await Test_KeywordAsync(
@"public class C
{
    protected priv[||]ate void goo();
}", "privateprotected");
        }

        [Fact]
        public async Task TestPrivateProtected4()
        {
            await Test_KeywordAsync(
@"public class C
{
    prot[||]ected private void goo();
}", "privateprotected");
        }

        [Fact]
        public async Task TestModifierSoup()
        {
            await Test_KeywordAsync(
    @"public class C
{
    private new prot[||]ected static unsafe void foo()
    {
    }
}", "privateprotected");
        }

        [Fact]
        public async Task TestModifierSoupField()
        {
            await Test_KeywordAsync(
    @"public class C
{
    new prot[||]ected static unsafe private goo;
}", "privateprotected");
        }

        [Fact]
        public async Task TestVoid()
        {
            await Test_KeywordAsync(
@"class C
{
    vo[||]id goo()
    {
    }
}", "void");
        }

        [Fact]
        public async Task TestReturn()
        {
            await Test_KeywordAsync(
@"class C
{
    void goo()
    {
        ret[||]urn;
    }
}", "return");
        }

        [Fact]
        public async Task TestClassPartialType()
        {
            await Test_KeywordAsync(
@"part[||]ial class C
{
    partial void goo();
}", "partialtype");
        }

        [Fact]
        public async Task TestRecordPartialType()
        {
            await Test_KeywordAsync(
@"part[||]ial record C
{
    partial void goo();
}", "partialtype");
        }

        [Fact]
        public async Task TestRecordWithPrimaryConstructorPartialType()
        {
            await Test_KeywordAsync(
@"part[||]ial record C(string S)
{
    partial void goo();
}", "partialtype");
        }

        [Fact]
        public async Task TestPartialMethodInClass()
        {
            await Test_KeywordAsync(
@"partial class C
{
    par[||]tial void goo();
}", "partialmethod");
        }

        [Fact]
        public async Task TestPartialMethodInRecord()
        {
            await Test_KeywordAsync(
@"partial record C
{
    par[||]tial void goo();
}", "partialmethod");
        }

        [Fact]
        public async Task TestExtendedPartialMethod()
        {
            await Test_KeywordAsync(
@"partial class C
{
    public par[||]tial void goo();
}", "partialmethod");
        }

        [Fact]
        public async Task TestWhereClause()
        {
            await Test_KeywordAsync(
@"using System.Linq;

class Program<T> where T : class
{
    void goo(string[] args)
    {
        var x = from a in args
                whe[||]re a.Length > 0
                select a;
    }
}", "whereclause");
        }

        [Fact]
        public async Task TestWhereConstraint()
        {
            await Test_KeywordAsync(
@"using System.Linq;

class Program<T> wh[||]ere T : class
{
    void goo(string[] args)
    {
        var x = from a in args
                where a.Length > 0
                select a;
    }
}", "whereconstraint");
        }

        [Fact]
        public async Task TestPreprocessor()
        {
            await TestAsync(
@"#regi[||]on
#endregion", "#region");
        }

        [Fact]
        public async Task TestPreprocessor2()
        {
            await TestAsync(
@"#region[||]
#endregion", "#region");
        }

        [Fact]
        public async Task TestConstructor()
        {
            await TestAsync(
@"namespace N
{
    class C
    {
        void goo()
        {
            var x = new [|C|]();
        }
    }
}", "N.C.#ctor");
        }

        [Fact]
        public async Task TestGenericClass()
        {
            await TestAsync(
@"namespace N
{
    class C<T>
    {
        void goo()
        {
            [|C|]<int> c;
        }
    }
}", "N.C`1");
        }

        [Fact]
        public async Task TestGenericMethod()
        {
            await TestAsync(
@"namespace N
{
    class C<T>
    {
        void goo<T, U, V>(T t, U u, V v)
        {
            C<int> c;
            c.g[|oo|](1, 1, 1);
        }
    }
}", "N.C`1.goo``3");
        }

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
        public async Task TestBinaryOperator(string operatorText)
        {
            await TestAsync(
$@"namespace N
{{
    class C
    {{
        void goo()
        {{
            var two = 1 [|{operatorText}|] 1;
        }}
    }}
}}", $"{operatorText}_CSharpKeyword");
        }

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
        public async Task TestCompoundOperator(string operatorText)
        {
            await TestAsync(
$@"namespace N
{{
    class C
    {{
        void goo(int x)
        {{
            x [|{operatorText}|] x;
        }}
    }}
}}", $"{operatorText}_CSharpKeyword");
        }

        [Theory]
        [InlineData("++")]
        [InlineData("--")]
        [InlineData("!")]
        [InlineData("~")]
        public async Task TestPrefixOperator(string operatorText)
        {
            await TestAsync(
$@"namespace N
{{
    class C
    {{
        void goo(int x)
        {{
            x = [|{operatorText}|]x;
        }}
    }}
}}", $"{operatorText}_CSharpKeyword");
        }

        [Theory]
        [InlineData("++")]
        [InlineData("--")]
        public async Task TestPostfixOperator(string operatorText)
        {
            await TestAsync(
$@"namespace N
{{
    class C
    {{
        void goo(int x)
        {{
            x = x[|{operatorText}|];
        }}
    }}
}}", $"{operatorText}_CSharpKeyword");
        }

        [Fact]
        public async Task TestRelationalPattern()
        {
            await TestAsync(
@"namespace N
{
    class C
    {
        void goo(string x)
        {
            if (x is { Length: [||]> 5 }) { }
        }
    }
}", ">_CSharpKeyword");
        }

        [Fact]
        public async Task TestGreaterThanInFunctionPointer()
        {
            await TestAsync(@"
unsafe class C
{
    delegate*[||]<int> f;
}
", "functionPointer_CSharpKeyword");
        }

        [Fact]
        public async Task TestLessThanInFunctionPointer()
        {
            await TestAsync(@"
unsafe class C
{
    delegate*[||]<int> f;
}
", "functionPointer_CSharpKeyword");
        }

        [Fact]
        public async Task TestEqualsOperatorInParameter()
        {
            await TestAsync(
@"namespace N
{
    class C
    {
        void goo(int x [|=|] 0)
        {
        }
    }
}", "optionalParameter_CSharpKeyword");
        }

        [Fact]
        public async Task TestEqualsOperatorInPropertyInitializer()
        {
            await TestAsync(
@"namespace N
{
    class C
    {
        int P { get; } [|=|] 5;
    }
}", "propertyInitializer_CSharpKeyword");
        }

        [Fact]
        public async Task TestVar()
        {
            await TestAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        var[||] x = 3;
    }
}", "var_CSharpKeyword");
        }

        [Fact]
        public async Task TestEquals()
        {
            await TestAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        var x =[||] 3;
    }
}", "=_CSharpKeyword");
        }

        [Fact]
        public async Task TestEqualsInEnum()
        {
            await TestAsync(
@"
enum E
{
    A [||]= 1
}", "enum_CSharpKeyword");
        }

        [Fact]
        public async Task TestEqualsInAttribute()
        {
            await TestAsync(
@"
using System;

[AttributeUsage(AttributeTargets.Class, Inherited [|=|] true)]
class MyAttribute : Attribute
{
}
", "attributeNamedArgument_CSharpKeyword");
        }

        [Fact]
        public async Task TestEqualsInUsingAlias()
        {
            await TestAsync(
@"
using SC [||]= System.Console;
", "using_CSharpKeyword");
        }

        [Fact]
        public async Task TestEqualsInAnonymousObjectMemberDeclarator()
        {
            await TestAsync(
@"
class C
{
    void M()
    {
        var x = new { X [||]= 0 };
    }
}
", "anonymousObject_CSharpKeyword");
        }

        [Fact]
        public async Task TestEqualsInDocumentationComment()
        {
            await TestAsync(
@"
class C
{
    /// <summary>
    /// <a b[||]=""c"" />
    /// </summary>
    void M()
    {
        var x = new { X [||]= 0 };
    }
}
", "see");
        }

        [Fact]
        public async Task TestEqualsInLet()
        {
            await TestAsync(
@"
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

", "let_CSharpKeyword");
        }

        [Fact]
        public async Task TestLetKeyword()
        {
            await TestAsync(
@"
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

", "let_CSharpKeyword");
        }

        [Fact]
        public async Task TestFromIn()
        {
            await TestAsync(
@"using System;
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
}", "from_CSharpKeyword");
        }

        [Fact]
        public async Task TestProperty()
        {
            await TestAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        new UriBuilder().Fragm[||]ent;
    }
}", "System.UriBuilder.Fragment");
        }

        [Fact]
        public async Task TestForeachIn()
        {
            await TestAsync(
@"using System;
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
}", "in_CSharpKeyword");
        }

        [Fact]
        public async Task TestRegionDescription()
        {
            await TestAsync(
@"class Program
{
    static void Main(string[] args)
    {
        #region Begin MyR[||]egion for testing
        #endregion End
    }
}", "#region");
        }

        [Fact]
        public async Task TestGenericAngle_LessThanToken_TypeArgument()
        {
            await TestAsync(
@"class Program
{
    static void generic<T>(T t)
    {
        generic[||]<int>(0);
    }
}", "generics_CSharpKeyword");
        }

        [Fact]
        public async Task TestGenericAngle_GreaterThanToken_TypeArgument()
        {
            await TestAsync(
@"class Program
{
    static void generic<T>(T t)
    {
        generic<int[|>|](0);
    }
}", "generics_CSharpKeyword");
        }

        [Fact]
        public async Task TestGenericAngle_LessThanToken_TypeParameter()
        {
            await TestAsync(
@"class Program
{
    static void generic[|<|]T>(T t)
    {
        generic<int>(0);
    }
}", "generics_CSharpKeyword");
        }

        [Fact]
        public async Task TestGenericAngle_GreaterThanToken_TypeParameter()
        {
            await TestAsync(
@"class Program
{
    static void generic<T[|>|](T t)
    {
        generic<int>(0);
    }
}", "generics_CSharpKeyword");
        }

        [Fact]
        public async Task TestLocalReferenceIsType()
        {
            await TestAsync(
@"using System;
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
}", "System.Int32");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/864266")]
        public async Task TestConstantField()
        {
            await TestAsync(
@"class Program
{
    static void Main(string[] args)
    {
        var i = int.Ma[||]xValue;
    }
}", "System.Int32.MaxValue");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/862420")]
        public async Task TestParameter()
        {
            await TestAsync(
@"class Class2
{
    void M1(int par[||]ameter)  // 1
    {
    }

    void M2()
    {
        int argument = 1;
        M1(parameter: argument);   // 2
    }
}", "System.Int32");
        }

        [Fact]
        public async Task TestRefReadonlyParameter_Ref()
        {
            await TestAsync(
                """
                class C
                {
                    void M(r[||]ef readonly int x)
                    {
                    }
                }
                """, "ref_CSharpKeyword");
        }

        [Fact]
        public async Task TestRefReadonlyParameter_ReadOnly()
        {
            await TestAsync(
                """
                class C
                {
                    void M(ref read[||]only int x)
                    {
                    }
                }
                """, "readonly_CSharpKeyword");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/862420")]
        public async Task TestArgumentType()
        {
            await TestAsync(
@"class Class2
{
    void M1(int pa[||]rameter)  // 1
    {
    }

    void M2()
    {
        int argument = 1;
        M1(parameter: argument);   // 2
    }
}", "System.Int32");
        }

        [Fact]
        public async Task TestYieldReturn_OnYield()
        {
            await TestAsync(@"
using System.Collections.Generic;

public class C
{
    public IEnumerable<int> M()
    {
        [|yield|] return 0;
    }
}
", "yield_CSharpKeyword");
        }

        [Fact]
        public async Task TestYieldReturn_OnReturn()
        {
            await TestAsync(@"
using System.Collections.Generic;

public class C
{
    public IEnumerable<int> M()
    {
        yield [|return|] 0;
    }
}
", "yield_CSharpKeyword");
        }

        [Fact]
        public async Task TestYieldBreak_OnYield()
        {
            await TestAsync(@"
using System.Collections.Generic;

public class C
{
    public IEnumerable<int> M()
    {
        [|yield|] break;
    }
}
", "yield_CSharpKeyword");
        }

        [Fact]
        public async Task TestYieldBreak_OnBreak()
        {
            await TestAsync(@"
using System.Collections.Generic;

public class C
{
    public IEnumerable<int> M()
    {
        yield [|break|] 0;
    }
}
", "yield_CSharpKeyword");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/862396")]
        public async Task TestNoToken()
        {
            await TestAsync(
@"class Program
{
    static void Main(string[] args)
    {
    }
}[||]", "vs.texteditor");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/862328")]
        public async Task TestLiteral()
        {
            await TestAsync(
@"class Program
{
    static void Main(string[] args)
    {
        Main(new string[] { ""fo[||]o"" });
    }
}", "System.String");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/862478")]
        public async Task TestColonColon()
        {
            await TestAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        global:[||]:System.Console.Write("");
    }
}", "::_CSharpKeyword");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46986")]
        public async Task TestStringInterpolation()
        {
            await TestAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine($[||]""Hello, {args[0]}"");
    }
}", "$_CSharpKeyword");
        }

        [Fact]
        public async Task TestUtf8String()
        {
            await TestAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        var x = ""Hel[||]lo""u8;
    }
}", "Utf8StringLiteral_CSharpKeyword");
        }

        [Fact]
        public async Task TestRawString()
        {
            await TestAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        var x = """"""Hel[||]lo"""""";
    }
}", "RawStringLiteral_CSharpKeyword");
        }

        [Fact]
        public async Task TestUtf8RawString()
        {
            await TestAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        var x = """"""Hel[||]lo""""""u8;
    }
}", "Utf8StringLiteral_CSharpKeyword");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46986")]
        public async Task TestVerbatimString()
        {
            await TestAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(@[||]""Hello\"");
    }
}", "@_CSharpKeyword");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46986")]
        public async Task TestVerbatimInterpolatedString1()
        {
            await TestAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(@[||]$""Hello\ {args[0]}"");
    }
}", "@$_CSharpKeyword");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46986")]
        public async Task TestVerbatimInterpolatedString2()
        {
            await TestAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine($[||]@""Hello\ {args[0]}"");
    }
}", "@$_CSharpKeyword");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/864658")]
        public async Task TestNullable()
        {
            await TestAsync(
@"using System;
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
}", "System.Nullable`1");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/863517")]
        public async Task TestAfterLastToken()
        {
            await TestAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        foreach (char var in ""!!!"")$$[||]
        {
        }
    }
}", "vs.texteditor");
        }

        [Fact]
        public async Task TestConditional()
        {
            await TestAsync(
@"class Program
{
    static void Main(string[] args)
    {
        var x = true [|?|] true : false;
    }
}", "?_CSharpKeyword");
        }

        [Fact]
        public async Task TestLocalVar()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        var a = 0;
        int v[||]ar = 1;
    }
}", "System.Int32");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/867574")]
        public async Task TestFatArrow()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        var a = new System.Action(() =[||]> {
        });
    }
}", "=>_CSharpKeyword");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/867572")]
        public async Task TestSubscription()
        {
            await TestAsync(
@"class CCC
{
    event System.Action e;

    void M()
    {
        e +[||]= () => {
        };
    }
}", "+=_CSharpKeyword");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/867554")]
        public async Task TestComment()
        {
            await TestAsync(@"// some comm[||]ents here", "comments");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/867529")]
        public async Task TestDynamic()
        {
            await TestAsync(
@"class C
{
    void M()
    {
        dyna[||]mic d = 0;
    }
}", "dynamic_CSharpKeyword");
        }

        [Fact]
        public async Task TestRangeVariable()
        {
            await TestAsync(
@"using System;
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
}", "System.String");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36001")]
        public async Task TestNameof()
        {
            await Test_KeywordAsync(
@"class C
{
    void goo()
    {
        var v = [||]nameof(goo);
    }
}", "nameof");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46988")]
        public async Task TestNullForgiving()
        {
            await Test_KeywordAsync(
@"#nullable enable
class C
{
    int goo(string? x)
    {
        return x[||]!.GetHashCode();
    }
}", "nullForgiving");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46988")]
        public async Task TestLogicalNot()
        {
            await Test_KeywordAsync(
@"class C
{
    bool goo(bool x)
    {
        return [||]!x;
    }
}", "!");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestDefaultSwitchCase()
        {
            await Test_KeywordAsync(
@"class C
{
    void M1(int parameter)
    {
        switch(parameter) {
            defa[||]ult:
                parameter = default;
                break;
        }
    }
}", "defaultcase");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestDefaultLiteralExpressionInsideSwitch()
        {
            await Test_KeywordAsync(
@"class C
{
    void M1(int parameter)
    {
        switch(parameter) {
            default:
                parameter = defa[||]ult;
                break;
        }
    }
}", "default");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestDefaultExpressionInsideSwitch()
        {
            await Test_KeywordAsync(
@"class C
{
    void M1(int parameter)
    {
        switch(parameter) {
            default:
                parameter = defa[||]ult(int);
                break;
        }
    }
}", "default");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestDefaultLiteralExpression()
        {
            await Test_KeywordAsync(
@"class C
{
    int field = defa[||]ult;
}", "default");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestDefaultExpression()
        {
            await Test_KeywordAsync(
@"class C
{
    int field = defa[||]ult(int);
}", "default");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestDefaultLiteralExpressionInOptionalParameter()
        {
            await Test_KeywordAsync(
@"class C
{
    void M1(int parameter = defa[||]ult) {
    }
}", "default");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestDefaultExpressionInOptionalParameter()
        {
            await Test_KeywordAsync(
@"class C
{
    void M1(int parameter = defa[||]ult(int)) {
    }
}", "default");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestDefaultLiteralExpressionInMethodCall()
        {
            await Test_KeywordAsync(
@"class C
{
    void M1() {
        M2(defa[||]ult);
    }
}", "default");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestDefaultExpressionInMethodCall()
        {
            await Test_KeywordAsync(
@"class C
{
    void M1() {
        M2(defa[||]ult(int));
    }
}", "default");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestOuterClassDeclaration()
        {
            await Test_KeywordAsync(
@"cla[||]ss OuterClass<T> where T : class
{ 
    class InnerClass<T> where T : class { }
}", "class");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestInnerClassDeclaration()
        {
            await Test_KeywordAsync(
@"class OuterClass<T> where T : class
{ 
    cla[||]ss InnerClass<T> where T : class { }
}", "class");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestClassConstraintInOuterClass()
        {
            await Test_KeywordAsync(
@"class OuterClass<T> where T : cla[||]ss
{ 
    class InnerClass<T> where T : class { }
}", "classconstraint");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestClassConstraintInInnerClass()
        {
            await Test_KeywordAsync(
@"class OuterClass<T> where T : class
{ 
    class InnerClass<T> where T : cla[||]ss { }
}", "classconstraint");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestClassConstraintInGenericMethod()
        {
            await Test_KeywordAsync(
@"class C
{ 
    void M1<T>() where T : cla[||]ss { }
}", "classconstraint");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestClassConstraintInGenericDelegate()
        {
            await Test_KeywordAsync(
@"class C
{ 
    delegate T MyDelegate<T>() where T : cla[||]ss;
}", "classconstraint");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestOuterStructDeclaration()
        {
            await Test_KeywordAsync(
@"str[||]uct OuterStruct<T> where T : struct
{ 
    struct InnerStruct<T> where T : struct { }
}", "struct");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestInnerStructDeclaration()
        {
            await Test_KeywordAsync(
@"struct OuterStruct<T> where T : struct
{ 
    str[||]uct InnerStruct<T> where T : struct { }
}", "struct");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestStructConstraintInOuterStruct()
        {
            await Test_KeywordAsync(
@"struct OuterStruct<T> where T : str[||]uct
{ 
    struct InnerStruct<T> where T : struct { }
}", "structconstraint");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestStructConstraintInInnerStruct()
        {
            await Test_KeywordAsync(
@"struct OuterStruct<T> where T : struct
{ 
    struct InnerStruct<T> where T : str[||]uct { }
}", "structconstraint");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestStructConstraintInGenericMethod()
        {
            await Test_KeywordAsync(
@"struct C
{ 
    void M1<T>() where T : str[||]uct { }
}", "structconstraint");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestStructConstraintInGenericDelegate()
        {
            await Test_KeywordAsync(
@"struct C
{ 
    delegate T MyDelegate<T>() where T : str[||]uct;
}", "structconstraint");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestAllowsRefStructAntiConstraint()
        {
            await Test_KeywordAsync(
@"class C
{ 
    void M<T>()
        where T : all[||]ows ref struct
    {
    }
}", "allows");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestUsingStaticOnUsingKeyword()
        {
            await Test_KeywordAsync(
@"us[||]ing static namespace.Class;

static class C
{ 
    static int Field;

    static void Method() {}
}", "using-static");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestNormalUsingDirective()
        {
            await Test_KeywordAsync(
@"us[||]ing namespace.Class;

static class C
{ 
    static int Field;

    static void Method() {}
}", "using");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestUsingStatement()
        {
            await Test_KeywordAsync(
@"using namespace.Class;

class C
{ 
    void Method(String someString) {
        us[||]ing (var reader = new StringReader(someString))
        {
        }
    }
}", "using-statement");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestUsingDeclaration()
        {
            await Test_KeywordAsync(
@"using namespace.Class;

class C
{ 
    void Method(String someString) {
        us[||]ing var reader = new StringReader(someString);
    }
}", "using-statement");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestUsingStaticOnStaticKeyword()
        {
            await Test_KeywordAsync(
@"using sta[||]tic namespace.Class;

static class C
{ 
    static int Field;

    static void Method() {}
}", "using-static");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestStaticClass()
        {
            await Test_KeywordAsync(
@"using static namespace.Class;

sta[||]tic class C
{ 
    static int Field;

    static void Method() {}
}", "static");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestStaticField()
        {
            await Test_KeywordAsync(
@"using static namespace.Class;

static class C
{ 
    sta[||]tic int Field;

    static void Method() {}
}", "static");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/48392")]
        public async Task TestStaticMethod()
        {
            await Test_KeywordAsync(
@"using static namespace.Class;

static class C
{ 
    static int Field;

    sta[||]tic void Method() {}
}", "static");
        }

        [Fact]
        public async Task TestWithKeyword()
        {
            await Test_KeywordAsync(
@"
public record Point(int X, int Y);

public static class Program
{ 
    public static void Main()
    {
        var p1 = new Point(0, 0);
        var p2 = p1 w[||]ith { X = 5 };
    }
}", "with");
        }

        [Fact]
        public async Task TestDiscard()
        {
            await Test_KeywordAsync(
@"
class C
{
    void M()
    {
        [||]_ = Goo();
    }

    object Goo() => null;
}", "discard");
        }

        [Fact]
        public async Task TestNotFound()
        {
            await TestAsync(
@"
#if ANY[||]THING
#endif
", "vs.texteditor");
        }

        [Fact]
        public async Task TestChecked_01()
        {
            await Test_KeywordAsync(
@"public class C
{
    void goo()
    {
        chec[||]ked
        {
        }
    }
}", "checked");
        }

        [Fact]
        public async Task TestChecked_02()
        {
            await Test_KeywordAsync(
@"public class C
{
    int goo()
    {
        return chec[||]ked(0);
    }
}", "checked");
        }

        [Fact]
        public async Task TestChecked_03()
        {
            await Test_KeywordAsync(
@"public class C
{
    C operator chec[||]ked -(C x) {}
}", "checked");
        }

        [Fact]
        public async Task TestChecked_04()
        {
            await Test_KeywordAsync(
@"public class C
{
    C operator chec[||]ked +(C x, C y) {}
}", "checked");
        }

        [Fact]
        public async Task TestChecked_05()
        {
            await Test_KeywordAsync(
@"public class C
{
    explicit operator chec[||]ked string(C x) {}
}", "checked");
        }

        [Fact]
        public async Task TestChecked_06()
        {
            await Test_KeywordAsync(
@"public class C
{
    C I1.operator chec[||]ked -(C x) {}
}", "checked");
        }

        [Fact]
        public async Task TestChecked_07()
        {
            await Test_KeywordAsync(
@"public class C
{
    C I1.operator chec[||]ked +(C x, C y) {}
}", "checked");
        }

        [Fact]
        public async Task TestChecked_08()
        {
            await Test_KeywordAsync(
@"public class C
{
    explicit I1.operator chec[||]ked string(C x) {}
}", "checked");
        }

        [Fact]
        public async Task TestChecked_09()
        {
            await Test_KeywordAsync(
@"public class C
{
    /// <summary>
    /// <see cref=""operator chec[||]ked +(C, C)""/>
    /// </summary>
    void goo()
    {
    }
}", "checked");
        }

        [Fact]
        public async Task TestChecked_10()
        {
            await Test_KeywordAsync(
@"public class C
{
    /// <summary>
    /// <see cref=""operator chec[||]ked -(C)""/>
    /// </summary>
    void goo()
    {
    }
}", "checked");
        }

        [Fact]
        public async Task TestChecked_11()
        {
            await Test_KeywordAsync(
@"public class C
{
    /// <summary>
    /// <see cref=""explicit operator chec[||]ked string(C)""/>
    /// </summary>
    void goo()
    {
    }
}", "checked");
        }

        [Fact]
        public async Task TestRequired()
        {
            await Test_KeywordAsync("""
                public class C
                {
                    re[||]quired int Field;
                }
                """, "required");
        }

        [Fact]
        public async Task TestDefaultConstraint()
        {
            await Test_KeywordAsync("""
                public class Base
                {
                    virtual void M<T>(T? t) { }
                }
                public class C
                {
                    override void M<T>() where T : def[||]ault { }
                }
                """, expectedText: "defaultconstraint");
        }

        [Fact]
        public async Task TestDefaultCase()
        {
            await Test_KeywordAsync("""
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
        }

        [Fact]
        public async Task TestGotoDefault()
        {
            await Test_KeywordAsync("""
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
        }

        [Fact]
        public async Task TestLineDefault()
        {
            await Test_KeywordAsync("""
                #line def[||]ault
                """, expectedText: "defaultline");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65311")]
        public async Task TestNotnull_OnType()
        {
            await Test_KeywordAsync("""
                public class C<T> where T : not[||]null
                {
                }
                """, expectedText: "notnull");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65311")]
        public async Task TestNotnull_OnMethod()
        {
            await Test_KeywordAsync("""
                public class C
                {
                    void M<T>() where T : not[||]null
                    {
                    }
                }
                """, expectedText: "notnull");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65311")]
        public async Task TestNotnull_FieldName()
        {
            await TestAsync("""
                public class C
                {
                    int not[||]null = 0;
                }
                """, expectedText: "C.notnull");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65311")]
        public async Task TestUnmanaged_OnType()
        {
            await Test_KeywordAsync("""
                public class C<T> where T : un[||]managed
                {
                }
                """, expectedText: "unmanaged");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65311")]
        public async Task TestUnmanaged_OnMethod()
        {
            await Test_KeywordAsync("""
                public class C
                {
                    void M<T>() where T : un[||]managed
                    {
                    }
                }
                """, expectedText: "unmanaged");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65311")]
        public async Task TestUnmanaged_LocalName()
        {
            await TestAsync("""
                int un[||]managed = 0;
                """, expectedText: "System.Int32");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65312")]
        public async Task TestSwitchStatement()
        {
            await Test_KeywordAsync("""
                swit[||]ch (1) { default: break; }
                """, expectedText: "switch");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/65312")]
        public async Task TestSwitchExpression()
        {
            await Test_KeywordAsync("""
                _ = 1 swit[||]ch { _ => 0 };
                """, expectedText: "switch-expression");
        }

        [Fact]
        public async Task TestFile()
        {
            await Test_KeywordAsync("""
                fi[||]le class C { }
                """, expectedText: "file");
        }

        [Fact]
        public async Task TestRightShift()
        {
            await Test_KeywordAsync("""
                _ = 1 >[||]> 2;
                """, expectedText: ">>");
        }

        [Fact]
        public async Task TestUnsignedRightShift()
        {
            await Test_KeywordAsync("""
                _ = 1 >>[||]> 2;
                """, expectedText: ">>>");
        }

        [Fact]
        public async Task TestUnsignedRightShiftAssignment()
        {
            await Test_KeywordAsync("""
                1 >>[||]>= 2;
                """, expectedText: ">>>=");
        }

        [Fact]
        public async Task TestPreprocessorIf()
        {
            await TestAsync(
@"
#i[||]f ANY
#endif
", "#if");
        }

        [Fact]
        public async Task TestPreprocessorIf2()
        {
            await TestAsync(
@"
#if ANY[||]
#endif
", "#if");
        }

        [Fact]
        public async Task TestPreprocessorEndIf()
        {
            await TestAsync(
@"
#if ANY
#en[||]dif
", "#endif");
        }

        [Fact]
        public async Task TestPreprocessorEndIf2()
        {
            await TestAsync(
@"
#if ANY
#endif[||]
", "#endif");
        }

        [Fact]
        public async Task TestPreprocessorElse()
        {
            await TestAsync(
@"
#if ANY
#el[||]se
#endif
", "#else");
        }

        [Fact]
        public async Task TestPreprocessorElse2()
        {
            await TestAsync(
@"
#if ANY
#else[||]
#endif
", "#else");
        }

        [Fact]
        public async Task TestPreprocessorElIf()
        {
            await TestAsync(
@"
#if ANY
#el[||]if SOME
#endif
", "#elif");
        }
    }
}
