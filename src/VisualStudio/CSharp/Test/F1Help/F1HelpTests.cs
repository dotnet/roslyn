// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService;
using Microsoft.VisualStudio.LanguageServices.Implementation.F1Help;
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
            // TODO: Using VisualStudioTestComposition.LanguageServices fails with "Failed to clean up listeners in a timely manner. WorkspaceChanged TaskQueue.cs 38"
            // https://github.com/dotnet/roslyn/issues/46250
            using var workspace = TestWorkspace.CreateCSharp(markup, composition: EditorTestCompositions.EditorFeatures.AddParts(typeof(CSharpHelpContextService)));
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
        public async Task TestPartialType()
        {
            await Test_KeywordAsync(
@"part[||]ial class C
{
    partial void goo();
}", "partialtype");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestPartialMethod()
        {
            await Test_KeywordAsync(
@"partial class C
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

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestOperator()
        {
            await TestAsync(
@"namespace N
{
    class C
    {
        void goo()
        {
            var two = 1 [|+|] 1;
        }
    }", "+_CSharpKeyword");
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
        public async Task TestGenericAngle()
        {
            await TestAsync(
@"class Program
{
    static void generic<T>(T t)
    {
        generic[||]<int>(0);
    }
}", "Program.generic``1");
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
}[||]", "");
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
}", "");
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
    }
}
