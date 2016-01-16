// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.F1Help
{
    public class F1HelpTests
    {
        private async Task TestAsync(string markup, string expectedText)
        {
            using (var workspace = await TestWorkspaceFactory.CreateCSharpWorkspaceFromFileAsync(markup))
            {
                var caret = workspace.Documents.First().CursorPosition;

                var service = new CSharpHelpContextService();
                var actualText = await service.GetHelpTermAsync(workspace.CurrentSolution.Projects.First().Documents.First(), workspace.Documents.First().SelectedSpans.First(), CancellationToken.None);
                Assert.Equal(expectedText, actualText);
            }
        }

        private async Task Test_KeywordAsync(string markup, string expectedText)
        {
            await TestAsync(markup, expectedText + "_CSharpKeyword");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestVoid()
        {
            await Test_KeywordAsync(@"
class C
{
    vo[||]id foo() { }
}", "void");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestReturn()
        {
            await Test_KeywordAsync(@"
class C
{
    void foo() 
    { 
        ret[||]urn; 
    }
}", "return");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestPartialType()
        {
            await Test_KeywordAsync(@"
part[||]ial class C
{
    partial void foo();
}", "partialtype");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestPartialMethod()
        {
            await Test_KeywordAsync(@"
partial class C
{
    par[||]tial void foo();
}", "partialmethod");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestWhereClause()
        {
            await Test_KeywordAsync(@"
using System.Linq;
class Program<T> where T : class {
    void foo(string[] args)
    {
        var x = from a in args whe[||]re a.Length > 0 select a;
    }
}", "whereclause");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestWhereConstraint()
        {
            await Test_KeywordAsync(@"
using System.Linq;
class Program<T> wh[||]ere T : class {
    void foo(string[] args)
    {
        var x = from a in args where a.Length > 0 select a;
    }
}", "whereconstraint");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestPreprocessor()
        {
            await TestAsync(@"
#regi[||]on
#endregion", "#region");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestConstructor()
        {
            await TestAsync(@"
namespace N
{
class C
{
    void foo()
    {
        var x = new [|C|]();
    }
}
}", "N.C.#ctor");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestGenericClass()
        {
            await TestAsync(@"
namespace N
{
class C<T>
{
    void foo()
    {
        [|C|]<int> c;
    }
}
}", "N.C`1");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestGenericMethod()
        {
            await TestAsync(@"
namespace N
{
class C<T>
{
    void foo<T, U, V>(T t, U u, V v)
    {
        C<int> c;
        c.f[|oo|](1, 1, 1);
    }
}
}", "N.C`1.foo``3");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestOperator()
        {
            await TestAsync(@"
namespace N
{
class C
{
    void foo()
    {
        var two = 1 [|+|] 1;
    }
}", "+_CSharpKeyword");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestVar()
        {
            await TestAsync(@"using System;
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
            await TestAsync(@"using System;
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
            await TestAsync(@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        var x = from n i[||]n { 1} select n
    }
}", "from_CSharpKeyword");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestProperty()
        {
            await TestAsync(@"using System;
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
            await TestAsync(@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        foreach (var x in[||] { 1} )
        {

        }
    }
}", "in_CSharpKeyword");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestRegionDescription()
        {
            await TestAsync(@"
class Program
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
            await TestAsync(@"class Program
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
            await TestAsync(@"using System;
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

        [WorkItem(864266)]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestConstantField()
        {
            await TestAsync(@"class Program
{
    static void Main(string[] args)
    {
        var i = int.Ma[||]xValue;
    }
}", "System.Int32.MaxValue");
        }

        [WorkItem(862420)]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestParameter()
        {
            await TestAsync(@"class Class2
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
", "System.Int32");
        }

        [WorkItem(862420)]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestArgumentType()
        {
            await TestAsync(@"class Class2
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
", "System.Int32");
        }

        [WorkItem(862396)]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestNoToken()
        {
            await TestAsync(@"class Program
{
    static void Main(string[] args)
    {
        [||]
    }
}", "");
        }

        [WorkItem(862328)]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestLiteral()
        {
            await TestAsync(@"class Program
{
    static void Main(string[] args)
    {
        Main(new string[] { ""fo[||]o"" });
    }
    }", "System.String");
        }

        [WorkItem(862478)]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestColonColon()
        {
            await TestAsync(@"using System;
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

        [WorkItem(864658)]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestNullable()
        {
            await TestAsync(@"using System;
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

        [WorkItem(863517)]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestAfterLastToken()
        {
            await TestAsync(@"using System;
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
            await TestAsync(@"class Program
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
            await TestAsync(@"class C
{
    void M()
    {
        var a = 0;
        int v[||]ar = 1;
    }
}", "System.Int32");
        }

        [WorkItem(867574)]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestFatArrow()
        {
            await TestAsync(@"class C
{
    void M()
    {
        var a = new System.Action(() =[||]> { });
    }
}", "=>_CSharpKeyword");
        }

        [WorkItem(867572)]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestSubscription()
        {
            await TestAsync(@"class CCC
{
    event System.Action e;
    void M()
    {
        e +[||]= () => { };
    }
}", "CCC.e.add");
        }

        [WorkItem(867554)]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestComment()
        {
            await TestAsync(@"// some comm[||]ents here", "comments");
        }

        [WorkItem(867529)]
        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public async Task TestDynamic()
        {
            await TestAsync(@"class C
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
            await TestAsync(@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        var zzz = from y in args select [||]y;
    }
}", "System.String");
        }
    }
}