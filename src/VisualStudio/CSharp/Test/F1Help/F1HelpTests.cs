// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.F1Help
{
    public class F1HelpTests
    {
        private void Test(string markup, string expectedText)
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFile(markup))
            {
                var caret = workspace.Documents.First().CursorPosition;

                var service = new CSharpHelpContextService();
                var actualText = service.GetHelpTermAsync(workspace.CurrentSolution.Projects.First().Documents.First(), workspace.Documents.First().SelectedSpans.First(), CancellationToken.None).WaitAndGetResult(CancellationToken.None);
                Assert.Equal(expectedText, actualText);
            }
        }

        private void Test_Keyword(string markup, string expectedText)
        {
            Test(markup, expectedText + "_CSharpKeyword");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestVoid()
        {
            Test_Keyword(@"
class C
{
    vo[||]id foo() { }
}", "void");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestReturn()
        {
            Test_Keyword(@"
class C
{
    void foo() 
    { 
        ret[||]urn; 
    }
}", "return");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestPartialType()
        {
            Test_Keyword(@"
part[||]ial class C
{
    partial void foo();
}", "partialtype");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestPartialMethod()
        {
            Test_Keyword(@"
partial class C
{
    par[||]tial void foo();
}", "partialmethod");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestWhereClause()
        {
            Test_Keyword(@"
using System.Linq;
class Program<T> where T : class {
    void foo(string[] args)
    {
        var x = from a in args whe[||]re a.Length > 0 select a;
    }
}", "whereclause");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestWhereConstraint()
        {
            Test_Keyword(@"
using System.Linq;
class Program<T> wh[||]ere T : class {
    void foo(string[] args)
    {
        var x = from a in args where a.Length > 0 select a;
    }
}", "whereconstraint");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestPreprocessor()
        {
            Test(@"
#regi[||]on
#endregion", "#region");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestConstructor()
        {
            Test(@"
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestGenericClass()
        {
            Test(@"
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestGenericMethod()
        {
            Test(@"
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestOperator()
        {
            Test(@"
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestVar()
        {
            Test(@"using System;
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestEquals()
        {
            Test(@"using System;
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestFromIn()
        {
            Test(@"using System;
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestProperty()
        {
            Test(@"using System;
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestForeachIn()
        {
            Test(@"using System;
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestRegionDescription()
        {
            Test(@"
class Program
{
    static void Main(string[] args)
    {
        #region Begin MyR[||]egion for testing
        #endregion End
    }
}", "#region");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestGenericAngle()
        {
            Test(@"class Program
{
    static void generic<T>(T t)
    {
        generic[||]<int>(0);
    }
}", "Program.generic``1");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestLocalReferenceIsType()
        {
            Test(@"using System;
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestConstantField()
        {
            Test(@"class Program
{
    static void Main(string[] args)
    {
        var i = int.Ma[||]xValue;
    }
}", "System.Int32.MaxValue");
        }

        [WorkItem(862420)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestParameter()
        {
            Test(@"class Class2
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestArgumentType()
        {
            Test(@"class Class2
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestNoToken()
        {
            Test(@"class Program
{
    static void Main(string[] args)
    {
        [||]
    }
}", "");
        }

        [WorkItem(862328)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestLiteral()
        {
            Test(@"class Program
{
    static void Main(string[] args)
    {
        Main(new string[] { ""fo[||]o"" });
    }
    }", "System.String");
        }

        [WorkItem(862478)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestColonColon()
        {
            Test(@"using System;
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestNullable()
        {
            Test(@"using System;
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestAfterLastToken()
        {
            Test(@"using System;
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestConditional()
        {
            Test(@"class Program
{
    static void Main(string[] args)
    {
        var x = true [|?|] true : false;
    }
}", "?_CSharpKeyword");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestLocalVar()
        {
            Test(@"class C
{
    void M()
    {
        var a = 0;
        int v[||]ar = 1;
    }
}", "System.Int32");
        }

        [WorkItem(867574)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestFatArrow()
        {
            Test(@"class C
{
    void M()
    {
        var a = new System.Action(() =[||]> { });
    }
}", "=>_CSharpKeyword");
        }

        [WorkItem(867572)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestSubscription()
        {
            Test(@"class CCC
{
    event System.Action e;
    void M()
    {
        e +[||]= () => { };
    }
}", "CCC.e.add");
        }

        [WorkItem(867554)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestComment()
        {
            Test(@"// some comm[||]ents here", "comments");
        }

        [WorkItem(867529)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestDynamic()
        {
            Test(@"class C
{
    void M()
    {
        dyna[||]mic d = 0;
    }
}", "dynamic_CSharpKeyword");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.F1Help)]
        public void TestRangeVariable()
        {
            Test(@"using System;
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
