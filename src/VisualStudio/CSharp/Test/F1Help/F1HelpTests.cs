// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestInternal()
        {
            await Test_KeywordAsync(
@"intern[||]al class C
{
}", "internal");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestProtected()
        {
            await Test_KeywordAsync(
@"public class C
{
    protec[||]ted void goo();
}", "protected");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestProtectedInternal1()
        {
            await Test_KeywordAsync(
@"public class C
{
    internal protec[||]ted void goo();
}", "protectedinternal");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestProtectedInternal2()
        {
            await Test_KeywordAsync(
@"public class C
{
    protec[||]ted internal void goo();
}", "protectedinternal");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestPrivateProtected1()
        {
            await Test_KeywordAsync(
@"public class C
{
    private protec[||]ted void goo();
}", "privateprotected");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestPrivateProtected2()
        {
            await Test_KeywordAsync(
@"public class C
{
    priv[||]ate protected void goo();
}", "privateprotected");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestPrivateProtected3()
        {
            await Test_KeywordAsync(
@"public class C
{
    protected priv[||]ate void goo();
}", "privateprotected");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestPrivateProtected4()
        {
            await Test_KeywordAsync(
@"public class C
{
    prot[||]ected private void goo();
}", "privateprotected");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestModifierSoupField()
        {
            await Test_KeywordAsync(
    @"public class C
{
    new prot[||]ected static unsafe private goo;
}", "privateprotected");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestClassPartialType()
        {
            await Test_KeywordAsync(
@"part[||]ial class C
{
    partial void goo();
}", "partialtype");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestRecordPartialType()
        {
            await Test_KeywordAsync(
@"part[||]ial record C
{
    partial void goo();
}", "partialtype");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestRecordWithPrimaryConstructorPartialType()
        {
            await Test_KeywordAsync(
@"part[||]ial record C(string S)
{
    partial void goo();
}", "partialtype");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestPartialMethodInClass()
        {
            await Test_KeywordAsync(
@"partial class C
{
    par[||]tial void goo();
}", "partialmethod");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestPartialMethodInRecord()
        {
            await Test_KeywordAsync(
@"partial record C
{
    par[||]tial void goo();
}", "partialmethod");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestExtendedPartialMethod()
        {
            await Test_KeywordAsync(
@"partial class C
{
    public par[||]tial void goo();
}", "partialmethod");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestPreprocessor()
        {
            await TestAsync(
@"#regi[||]on
#endregion", "#region");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Theory, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Theory, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Theory, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Theory, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestGreaterThanInFunctionPointer()
        {
            await TestAsync(@"
unsafe class C
{
    delegate*[||]<int> f;
}
", "functionPointer_CSharpKeyword");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestLessThanInFunctionPointer()
        {
            await TestAsync(@"
unsafe class C
{
    delegate*[||]<int> f;
}
", "functionPointer_CSharpKeyword");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestEqualsInEnum()
        {
            await TestAsync(
@"
enum E
{
    A [||]= 1
}", "enum_CSharpKeyword");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestEqualsInUsingAlias()
        {
            await TestAsync(
@"
using SC [||]= System.Console;
", "using_CSharpKeyword");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(864266, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/864266")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(862420, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/862420")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(862420, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/862420")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(862396, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/862396")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(862328, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/862328")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(862478, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/862478")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(46986, "https://github.com/dotnet/roslyn/issues/46986")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestUTF8String()
        {
            await TestAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        var x = ""Hel[||]lo""u8;
    }
}", "UTF8StringLiteral_CSharpKeyword");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestUTF8RawString()
        {
            await TestAsync(
@"using System;

class Program
{
    static void Main(string[] args)
    {
        var x = """"""Hel[||]lo""""""u8;
    }
}", "UTF8StringLiteral_CSharpKeyword");
        }

        [WorkItem(46986, "https://github.com/dotnet/roslyn/issues/46986")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(46986, "https://github.com/dotnet/roslyn/issues/46986")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(46986, "https://github.com/dotnet/roslyn/issues/46986")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(864658, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/864658")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(863517, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/863517")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(867574, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/867574")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(867572, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/867572")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(867554, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/867554")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestComment()
        {
            await TestAsync(@"// some comm[||]ents here", "comments");
        }

        [WorkItem(867529, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/867529")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(36001, "https://github.com/dotnet/roslyn/issues/36001")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(46988, "https://github.com/dotnet/roslyn/issues/46988")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(46988, "https://github.com/dotnet/roslyn/issues/46988")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestDefaultLiteralExpression()
        {
            await Test_KeywordAsync(
@"class C
{
    int field = defa[||]ult;
}", "default");
        }

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestDefaultExpression()
        {
            await Test_KeywordAsync(
@"class C
{
    int field = defa[||]ult(int);
}", "default");
        }

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestDefaultLiteralExpressionInOptionalParameter()
        {
            await Test_KeywordAsync(
@"class C
{
    void M1(int parameter = defa[||]ult) {
    }
}", "default");
        }

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestDefaultExpressionInOptionalParameter()
        {
            await Test_KeywordAsync(
@"class C
{
    void M1(int parameter = defa[||]ult(int)) {
    }
}", "default");
        }

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestOuterClassDeclaration()
        {
            await Test_KeywordAsync(
@"cla[||]ss OuterClass<T> where T : class
{ 
    class InnerClass<T> where T : class { }
}", "class");
        }

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestInnerClassDeclaration()
        {
            await Test_KeywordAsync(
@"class OuterClass<T> where T : class
{ 
    cla[||]ss InnerClass<T> where T : class { }
}", "class");
        }

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestClassConstraintInOuterClass()
        {
            await Test_KeywordAsync(
@"class OuterClass<T> where T : cla[||]ss
{ 
    class InnerClass<T> where T : class { }
}", "classconstraint");
        }

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestClassConstraintInInnerClass()
        {
            await Test_KeywordAsync(
@"class OuterClass<T> where T : class
{ 
    class InnerClass<T> where T : cla[||]ss { }
}", "classconstraint");
        }

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestClassConstraintInGenericMethod()
        {
            await Test_KeywordAsync(
@"class C
{ 
    void M1<T>() where T : cla[||]ss { }
}", "classconstraint");
        }

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestClassConstraintInGenericDelegate()
        {
            await Test_KeywordAsync(
@"class C
{ 
    delegate T MyDelegate<T>() where T : cla[||]ss;
}", "classconstraint");
        }

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestOuterStructDeclaration()
        {
            await Test_KeywordAsync(
@"str[||]uct OuterStruct<T> where T : struct
{ 
    struct InnerStruct<T> where T : struct { }
}", "struct");
        }

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestInnerStructDeclaration()
        {
            await Test_KeywordAsync(
@"struct OuterStruct<T> where T : struct
{ 
    str[||]uct InnerStruct<T> where T : struct { }
}", "struct");
        }

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestStructConstraintInOuterStruct()
        {
            await Test_KeywordAsync(
@"struct OuterStruct<T> where T : str[||]uct
{ 
    struct InnerStruct<T> where T : struct { }
}", "structconstraint");
        }

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestStructConstraintInInnerStruct()
        {
            await Test_KeywordAsync(
@"struct OuterStruct<T> where T : struct
{ 
    struct InnerStruct<T> where T : str[||]uct { }
}", "structconstraint");
        }

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestStructConstraintInGenericMethod()
        {
            await Test_KeywordAsync(
@"struct C
{ 
    void M1<T>() where T : str[||]uct { }
}", "structconstraint");
        }

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestStructConstraintInGenericDelegate()
        {
            await Test_KeywordAsync(
@"struct C
{ 
    delegate T MyDelegate<T>() where T : str[||]uct;
}", "structconstraint");
        }

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [WorkItem(48392, "https://github.com/dotnet/roslyn/issues/48392")]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestNotFound()
        {
            await TestAsync(
@"
#if ANY[||]THING
#endif
", "vs.texteditor");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestChecked_03()
        {
            await Test_KeywordAsync(
@"public class C
{
    C operator chec[||]ked -(C x) {}
}", "checked");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestChecked_04()
        {
            await Test_KeywordAsync(
@"public class C
{
    C operator chec[||]ked +(C x, C y) {}
}", "checked");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestChecked_05()
        {
            await Test_KeywordAsync(
@"public class C
{
    explicit operator chec[||]ked string(C x) {}
}", "checked");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestChecked_06()
        {
            await Test_KeywordAsync(
@"public class C
{
    C I1.operator chec[||]ked -(C x) {}
}", "checked");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestChecked_07()
        {
            await Test_KeywordAsync(
@"public class C
{
    C I1.operator chec[||]ked +(C x, C y) {}
}", "checked");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestChecked_08()
        {
            await Test_KeywordAsync(
@"public class C
{
    explicit I1.operator chec[||]ked string(C x) {}
}", "checked");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
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
    }
}
