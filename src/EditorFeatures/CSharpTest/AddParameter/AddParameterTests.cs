// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.AddParameter;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddParameter
{
    public class AddParameterTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new CSharpAddParameterCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestMissingWithImplicitConstructor()
        {
            await TestMissingAsync(
@"
class C
{
}

class D
{
    void M()
    {
        new [|C|](1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestOnEmptyConstructor()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C() { }
}

class D
{
    void M()
    {
        new [|C|](1);
    }
}",
@"
class C
{
    public C(int v) { }
}

class D
{
    void M()
    {
        new C(1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestNamedArg()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C() { }
}

class D
{
    void M()
    {
        new C([|p|]: 1);
    }
}",
@"
class C
{
    public C(int p) { }
}

class D
{
    void M()
    {
        new C(p: 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestMissingWithConstructorWithSameNumberOfParams()
        {
            await TestMissingAsync(
@"
class C
{
    public C(bool b) { }
}

class D
{
    void M()
    {
        new [|C|](1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestAddBeforeMatchingArg()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C(int i) { }
}

class D
{
    void M()
    {
        new [|C|](true, 1);
    }
}",
@"
class C
{
    public C(bool v, int i) { }
}

class D
{
    void M()
    {
        new C(true, 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestAddAfterMatchingConstructorParam()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C(int i) { }
}

class D
{
    void M()
    {
        new [|C|](1, true);
    }
}",
@"
class C
{
    public C(int i, bool v) { }
}

class D
{
    void M()
    {
        new C(1, true);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestParams1()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C(params int[] i) { }
}

class D
{
    void M()
    {
        new C([|true|], 1);
    }
}",
@"
class C
{
    public C(bool v, params int[] i) { }
}

class D
{
    void M()
    {
        new C(true, 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestParams2()
        {
            await TestMissingAsync(
@"
class C
{
    public C(params int[] i) { }
}

class D
{
    void M()
    {
        new [|C|](1, true);
    }
}");
        }

        [WorkItem(20708, "https://github.com/dotnet/roslyn/issues/20708")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestMultiLineParameters1()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C(int i,
             /* goo */ int j)
    {

    }

    private void Goo()
    {
        new [|C|](true, 0, 0);
    }
}",
@"
class C
{
    public C(bool v,
             int i,
             /* goo */ int j)
    {

    }

    private void Goo()
    {
        new C(true, 0, 0);
    }
}");
        }

        [WorkItem(20708, "https://github.com/dotnet/roslyn/issues/20708")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestMultiLineParameters2()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C(int i,
             /* goo */ int j)
    {

    }

    private void Goo()
    {
        new [|C|](0, true, 0);
    }
}",
@"
class C
{
    public C(int i,
             bool v,
             /* goo */ int j)
    {

    }

    private void Goo()
    {
        new C(0, true, 0);
    }
}");
        }

        [WorkItem(20708, "https://github.com/dotnet/roslyn/issues/20708")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestMultiLineParameters3()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C(int i,
             /* goo */ int j)
    {

    }

    private void Goo()
    {
        new [|C|](0, 0, true);
    }
}",
@"
class C
{
    public C(int i,
             /* goo */ int j,
             bool v)
    {

    }

    private void Goo()
    {
        new C(0, 0, true);
    }
}");
        }

        [WorkItem(20708, "https://github.com/dotnet/roslyn/issues/20708")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestMultiLineParameters4()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C(
        int i,
        /* goo */ int j)
    {

    }

    private void Goo()
    {
        new [|C|](true, 0, 0);
    }
}",
@"
class C
{
    public C(
        bool v,
        int i,
        /* goo */ int j)
    {

    }

    private void Goo()
    {
        new C(true, 0, 0);
    }
}");
        }

        [WorkItem(20708, "https://github.com/dotnet/roslyn/issues/20708")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestMultiLineParameters5()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C(
        int i,
        /* goo */ int j)
    {

    }

    private void Goo()
    {
        new [|C|](0, true, 0);
    }
}",
@"
class C
{
    public C(
        int i,
        bool v,
        /* goo */ int j)
    {

    }

    private void Goo()
    {
        new C(0, true, 0);
    }
}");
        }

        [WorkItem(20708, "https://github.com/dotnet/roslyn/issues/20708")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestMultiLineParameters6()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C(
        int i,
        /* goo */ int j)
    {

    }

    private void Goo()
    {
        new [|C|](0, 0, true);
    }
}",
@"
class C
{
    public C(
        int i,
        /* goo */ int j,
        bool v)
    {

    }

    private void Goo()
    {
        new C(0, 0, true);
    }
}");
        }

        [WorkItem(20973, "https://github.com/dotnet/roslyn/issues/20973")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestNullArg1()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C(int i) { }
}

class D
{
    void M()
    {
        new [|C|](null, 1);
    }
}",
@"
class C
{
    public C(object p, int i) { }
}

class D
{
    void M()
    {
        new C(null, 1);
    }
}");
        }

        [WorkItem(20973, "https://github.com/dotnet/roslyn/issues/20973")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestNullArg2()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C(string s) { }
}

class D
{
    void M()
    {
        new [|C|](null, 1);
    }
}",
@"
class C
{
    public C(string s, int v) { }
}

class D
{
    void M()
    {
        new C(null, 1);
    }
}");
        }

        [WorkItem(20973, "https://github.com/dotnet/roslyn/issues/20973")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestDefaultArg1()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C(int i) { }
}

class D
{
    void M()
    {
        new [|C|](default, 1);
    }
}",
@"
class C
{
    public C(int i, int v) { }
}

class D
{
    void M()
    {
        new C(default, 1);
    }
}");
        }

        [WorkItem(20973, "https://github.com/dotnet/roslyn/issues/20973")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestDefaultArg2()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    public C(string s) { }
}

class D
{
    void M()
    {
        new [|C|](default, 1);
    }
}",
@"
class C
{
    public C(string s, int v) { }
}

class D
{
    void M()
    {
        new C(default, 1);
    }
}");
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestInvocationInstanceMethod1()
        {
            await TestInRegularAndScriptAsync(
@"
class C
{
    void M1()
    {
    }
    void M2()
    {
        int i=0;
        [|M1|](i);
    }
}
",
@"
class C
{
    void M1(int i)
    {
    }
    void M2()
    {
        int i=0;
        M1(i);
    }
}
");
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestInvocationInheritedMethodGetFixed()
        {
            await TestInRegularAndScriptAsync(
@"
class Base
{
    protected void M1()
    {
    }
}
class C1 : Base
{
    void M2()
    {
        int i = 0;
        [|M1|](i);
    }
}",
@"
class Base
{
    protected void M1(int i)
    {
    }
}
class C1 : Base
{
    void M2()
    {
        int i = 0;
        M1(i);
    }
}");
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestInvocationInheritedMethodInMetadatGetsNotFixed()
        {
            await TestMissingAsync(
    @"
class C1
{
    void M2()
    {
        int i = 0;
        [|GetHashCode|](i);
    }
}");
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestInvocationLocalFunction()
        {
            await TestInRegularAndScriptAsync(
@"
class C1
{
    void M1()
    {
        int Local() => 1;
        [|Local|](2);
    }
}",
@"
class C1
{
    void M1()
    {
        int Local(int v) => 1;
        Local(2);
    }
}");
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        [Trait("TODO", "Fix broken")]
        public async Task TestInvocationLambda1()
        {
            await TestInRegularAndScriptAsync(
@"
using System;
class C1
{
    void M1()
    {
        Action a = () => { };
        [|a|](2);
    }
}",
@"
using System;
class C1
{
    void M1()
    {
        Action a = (int v) => { };
        a(2);
    }
}");
            //Should be Action<int> a = (int v) => { };
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestInvocationStaticMethod()
        {
            await TestInRegularAndScriptAsync(
@"
class C1
{
    static void M1()
    {
    }
    void M2()
    {
        [|M1|](1);
    }
}",
@"
class C1
{
    static void M1(int v)
    {
    }
    void M2()
    {
        M1(1);
    }
}");
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestInvocationExtensionMethod()
        {
            var code =
@"
namespace N {
static class Extensions
{
    public static void ExtensionM1(this object o)
    {
    }
}
class C1
{
    void M1()
    {
        new object().[|ExtensionM1|](1);
    }
}}";
            var fix =
@"
namespace N {
static class Extensions
{
    public static void ExtensionM1(this object o, int v)
    {
    }
}
class C1
{
    void M1()
    {
        new object().ExtensionM1(1);
    }
}}";
            await TestInRegularAndScriptAsync(code, fix);
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestInvocationOverride()
        {
            await TestInRegularAndScriptAsync(
@"
class Base
{
    protected virtual void M1() { }
}
class C1 : Base
{
    protected override void M1() { }
    void M2()
    {
        [|M1|](1);
    }
}",
@"
class Base
{
    protected virtual void M1() { }
}
class C1 : Base
{
    protected override void M1(int v) { }
    void M2()
    {
        M1(1);
    }
}");
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestInvocationExplicitInterface()
        {
            await TestInRegularAndScriptAsync(
@"
interface I1
{
    void M1();
}
class C1 : I1
{
    void I1.M1() { }
    void M2()
    {
        ((I1)this).[|M1|](1);
    }
}",
@"
interface I1
{
    void M1(int v);
}
class C1 : I1
{
    void I1.M1() { }
    void M2()
    {
        ((I1)this).M1(1);
    }
}");
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestInvocationImplicitInterface()
        {
            await TestInRegularAndScriptAsync(
@"
interface I1
{
    void M1();
}
class C1 : I1
{
    public void M1() { }
    void M2()
    {
        [|M1|](1);
    }
}",
@"
interface I1
{
    void M1();
}
class C1 : I1
{
    public void M1(int v) { }
    void M2()
    {
        M1(1);
    }
}");
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        [Trait("TODO", "Fix broken")]
        public async Task TestInvocationGenericMethod()
        {
            await TestInRegularAndScriptAsync(
@"
class C1
{
    void M1<T>(T arg) { }
    void M2()
    {
        [|M1|](1, 2);
    }
}",
@"
class C1
{
    void M1<T>(T arg, int v) { }
    void M2()
    {
        M1(1, 2);
    }
}");
            //Should fix to: void M1<T>(T arg, T v) { }
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestInvocationRecursion()
        {
            await TestInRegularAndScriptAsync(
@"
class C1
{
    void M1()
    {
        [|M1|](1);
    }
}",
@"
class C1
{
    void M1(int v)
    {
        M1(1);
    }
}");
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestInvocationOverloads1()
        {
            var code =
@"
class C1
{
    void M1(string s) { }
    void M1(int i) { }
    void M2()
    {
        [|M1|](1, 2);
    }
}";
            var fix0 =
@"
class C1
{
    void M1(string s) { }
    void M1(int i, int v) { }
    void M2()
    {
        M1(1, 2);
    }
}";
            var fix1 =
@"
class C1
{
    void M1(int v, string s) { }
    void M1(int i) { }
    void M2()
    {
        M1(1, 2);
    }
}";
            await TestInRegularAndScriptAsync(code, fix0, 0);
            await TestInRegularAndScriptAsync(code, fix1, 1);
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestInvocationOverloads2()
        {
            var code =
@"
class C1
{
    void M1(string s1, string s2) { }
    void M1(string s) { }
    void M1(int i) { }
    void M2()
    {
        M1(1, [|2|]);
    }
}";
            var fix0 =
@"
class C1
{
    void M1(string s1, string s2) { }
    void M1(string s) { }
    void M1(int i, int v) { }
    void M2()
    {
        M1(1, 2);
    }
}";
            var fix1 =
@"
class C1
{
    void M1(string s1, string s2) { }
    void M1(int v, string s) { }
    void M1(int i) { }
    void M2()
    {
        M1(1, 2);
    }
}";
            await TestInRegularAndScriptAsync(code, fix0, 0);
            await TestInRegularAndScriptAsync(code, fix1, 1);
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestInvocationTuple1()
        {
            var code =
@"
class C1
{
    void M1((int, int) t1)
    {
    }
    void M2()
    {
        [|M1|]((0, 0), (1, ""1""));
    }
}";
            var fix0 =
    @"
class C1
{
    void M1((int, int) t1, (int, string) p)
    {
    }
    void M2()
    {
        M1((0, 0), (1, ""1""));
    }
}";
            await TestInRegularAndScriptAsync(code, fix0, 0);
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestInvocationTuple2()
        {
            var code =
@"
class C1
{
    void M1((int, int) t1)
    {
    }
    void M2()
    {
        var tup = (1, ""1"");
        [|M1|]((0, 0), tup);
    }
}";
            var fix0 =
    @"
class C1
{
    void M1((int, int) t1, (int, string) tup)
    {
    }
    void M2()
    {
        var tup = (1, ""1"");
        M1((0, 0), tup);
    }
}";
            await TestInRegularAndScriptAsync(code, fix0, 0);
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestInvocationTuple3()
        {
            var code =
@"
class C1
{
    void M1((int, int) t1)
    {
    }
    void M2()
    {
        var tup = (i: 1, s: ""1"");
        [|M1|]((0, 0), tup);
    }
}";
            var fix0 =
    @"
class C1
{
    void M1((int, int) t1, (int i, string s) tup)
    {
    }
    void M2()
    {
        var tup = (i: 1, s: ""1"");
        M1((0, 0), tup);
    }
}";
            await TestInRegularAndScriptAsync(code, fix0, 0);
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestInvocationCS0305()
        {
            var code =
@"
class C1
{
    void M1<T>(T i) { }
    void M2()
    {
        [|M1|]<int, bool>(1, true);
    }
}";
            var fix0 =
@"
class C1
{
    void M1<T, T1>(T i, T1 v) { }
    void M2()
    {
        M1<int, bool>(1, true);
    }
}";
            await TestInRegularAndScriptAsync(code, fix0, 0);
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestInvocationCS0308()
        {
            var code =
@"
class C1
{
    void M1(int i) { }
    void M2()
    {
        [|M1<bool>|](1, true);
    }
}";
            var fix0 =
@"
class C1
{
    void M1<T>(int i, T v) { }
    void M2()
    {
        M1<bool>(1, true);
    }
}";
            await TestInRegularAndScriptAsync(code, fix0, 0);
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        [Trait("TODO", "Fix missing")]
        public async Task TestInvocationCS0539()
        {
            var code =
@"
interface I1
{
    void M1();
}
class C1 : I1
{
        void I1.M1() { }
        void I1.[|M1|](int i) { }
}";
            // Should apply argument to interface method: void M1(int i);
            await TestMissingAsync(code);
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestInvocationCS1503()
        {
            var code =
@"
    class C1
    {
        void M1(int i1, int i2) { }
        void M1(double d) { }
        void M2()
        {
            M1([|1.0|], 1);
        }
    }
";
            var fix0 =
@"
    class C1
    {
        void M1(int i1, int i2) { }
        void M1(double d, int v) { }
        void M2()
        {
            M1(1.0, 1);
        }
    }
";
            await TestInRegularAndScriptAsync(code, fix0);
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestInvocationCS1660()
        {
            var code =
@"
    class C1
    {
        void M1(int i1, int i2) { }
        void M1(System.Action a) { }
        void M2()
        {
            M1([|()=> { }|], 1);
        }
    }
";
            var fix =
@"
    class C1
    {
        void M1(int i1, int i2) { }
        void M1(System.Action a, int v) { }
        void M2()
        {
            M1(()=> { }, 1);
        }
    }
";
            await TestInRegularAndScriptAsync(code, fix);
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestInvocationCS1739()
        {
            var code =
@"
    class C1
    {
        void M1(int i1) { }
        void M2()
        {
            M1([|i2|]: 1);
        }
    }
";
            var fix =
@"
    class C1
    {
        void M1(int i1, int i2) { }
        void M2()
        {
            M1(i2: 1);
        }
    }
";
            await TestInRegularAndScriptAsync(code, fix);
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestInvocationAddTypeParameter_AddTypeParameterIfUserSpecifiesOne_OnlyTypeArgument()
        {
            var code =
@"
    class C1
    {
        void M1() { }
        void M2()
        {
            [|M1|]<bool>();
        }
    }
";
            var fix0 =
@"
    class C1
    {
        void M1<T>() { }
        void M2()
        {
            M1<bool>();
        }
    }
";
            await TestInRegularAndScriptAsync(code, fix0);
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestInvocationAddTypeParameter_AddTypeParameterIfUserSpecifiesOne_TypeArgumentAndParameterArgument()
        {
            var code =
@"
    class C1
    {
        void M1() { }
        void M2()
        {
            [|M1|]<bool>(true);
        }
    }
";
            var fix0 =
@"
    class C1
    {
        void M1<T>(T v) { }
        void M2()
        {
            M1<bool>(true);
        }
    }
";
            await TestInRegularAndScriptAsync(code, fix0);
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestInvocation_ExisitingTypeArgumentIsNotGeneralized()
        {
            var code =
@"
    class C1
    {
        void M1<T>(T v) { }
        void M2()
        {
            [|M1|](true, true);
        }
    }
";
            var fix0 =
@"
    class C1
    {
        void M1<T>(T v, bool v1) { }
        void M2()
        {
            M1(true, true);
        }
    }
";
            await TestInRegularAndScriptAsync(code, fix0);
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestInvocationPartialClasses()
        {
            var code =
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
namespace N1
{
    partial class C1
    {
        partial void PartialM();
    }
}
        </Document>
        <Document>
namespace N1
{
    partial class C1
    {
        partial void PartialM() { }
        void M1()
        {
            [|PartialM|](1);
        }
    }
}
        </Document>
    </Project>
</Workspace>";
            var fix0 =
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
namespace N1
{
    partial class C1
    {
        partial void PartialM(int v);
    }
}
        </Document>
        <Document>
namespace N1
{
    partial class C1
    {
        partial void PartialM(int v) { }
        void M1()
        {
            PartialM(1);
        }
    }
}
        </Document>
    </Project>
</Workspace>";
            await TestInRegularAndScriptAsync(code, fix0);
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        [Trait("TODO", "Code formatting")]
        public async Task TestObjectCreationAddTypeParameter_AddTypeParameterIfUserSpecifiesOne_TypeParameterAndParameterArgument()
        {
            var code =
@"
    class C1
    {
        C1() { }
        void M2()
        {
            var c = new [|C1<bool>|](true);
        }
    }
";
            var fix0 =
@"
    class C1<T>
{
        C1(T v) { }
        void M2()
        {
            var c = new C1<bool>(true);
        }
    }
";
            await TestInRegularAndScriptAsync(code, fix0);
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestObjectCreationAddTypeParameter_GeneralizeGenericArgument1()
        {
            var code =
@"
    class C1<T>
    {
        C1() { }
        void M2()
        {
            var c = new [|C1<bool>|](true);
        }
    }
";
            var fix0 =
@"
    class C1<T>
    {
        C1(T v) { }
        void M2()
        {
            var c = new C1<bool>(true);
        }
    }
";
            await TestInRegularAndScriptAsync(code, fix0);
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestObjectCreationAddTypeParameter_GeneralizeGenericArgument2()
        {
            var code =
@"
    class C1<T>
    {
        C1() { }

        void M1<U>() { }
        void M2()
        {
            var c = new C1<bool>();
            c.[|M1<int, bool>|]();
        }
    }
";
            var fix0 =
@"
    class C1<T>
    {
        C1() { }

        void M1<U, T1>() { }
        void M2()
        {
            var c = new C1<bool>();
            c.M1<int, bool>();
        }
    }
";
            await TestInRegularAndScriptAsync(code, fix0);
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestObjectCreationAddTypeParameter_GeneralizeGenericArgument3()
        {
            var code =
@"
    class C1<T>
    {
        C1() { }

        void M1<U>() { }
        void M2()
        {
            var c = new C1<bool>();
            c.[|M1<int, string>|]();
        }
    }
";
            var fix0 =
@"
    class C1<T>
    {
        C1() { }

        void M1<U, T1>() { }
        void M2()
        {
            var c = new C1<bool>();
            c.M1<int, string>();
        }
    }
";
            await TestInRegularAndScriptAsync(code, fix0);
        }

        [WorkItem(21446, "https://github.com/dotnet/roslyn/issues/21446")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddParameter)]
        public async Task TestObjectCreationAddTypeParameter_GeneralizeGenericArgument4()
        {
            var code =
@"
    class C1<T> where T: struct
    {
        C1() { }

        void M1<U>() where U : struct
        {
        }
        void M2()
        {
            var c = new C1<bool>();
            c.[|M1<int, string>|]();
        }
    }
";
            var fix0 =
@"
    class C1<T> where T: struct
    {
        C1() { }

        void M1<U, T1>() where U : struct
        {
        }
        void M2()
        {
            var c = new C1<bool>();
            c.M1<int, string>();
        }
    }
";
            await TestInRegularAndScriptAsync(code, fix0);
        }
    }
}
