using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.InvokeDelegateWithConditionalAccess;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.InvokeDelegateWithConditionalAccess
{
    public class InvokeDelegateWithConditionalAccessTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override Tuple<DiagnosticAnalyzer, CodeFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
        {
            return new Tuple<DiagnosticAnalyzer, CodeFixProvider>(
                new InvokeDelegateWithConditionalAccessAnalyzer(),
                new InvokeDelegateWithConditionalAccessCodeFixProvider());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public void Test1()
        {
            Test(
@"class C
{
    System.Action a;
    void Foo()
    {
        [||]var v = a;
        if (v != null)
        {
            v();
        }
    }
}",
@"
class C
{
    System.Action a;
    void Foo()
    {
        a?.Invoke();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public void TestInvertedIf()
        {
            Test(
@"class C
{
    System.Action a;
    void Foo()
    {
        [||]var v = a;
        if (null != v)
        {
            v();
        }
    }
}",
@"
class C
{
    System.Action a;
    void Foo()
    {
        a?.Invoke();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public void TestIfWithNoBraces()
        {
            Test(
@"class C
{
    System.Action a;
    void Foo()
    {
        [||]var v = a;
        if (null != v)
            v();
    }
}",
@"
class C
{
    System.Action a;
    void Foo()
    {
        a?.Invoke();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public void TestWithComplexExpression()
        {
            Test(
@"class C
{
    System.Action a;
    void Foo()
    {
        bool b = true;
        [||]var v = b ? a : null;
        if (v != null)
        {
            v();
        }
    }
}",
@"
class C
{
    System.Action a;
    void Foo()
    {
        bool b = true;
        (b ? a : null)?.Invoke();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public void TestMissingWithElseClause()
        {
            TestMissing(
@"class C
{
    System.Action a;
    void Foo()
    {
        [||]var v = a;
        if (v != null)
        {
            v();
        }
        else {}
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public void TestMissingWithMultipleVariables()
        {
            TestMissing(
@"class C
{
    System.Action a;
    void Foo()
    {
        [||]var v = a, x = a;
        if (v != null)
        {
            v();
        }
        else {}
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public void TestMissingIfUsedOutside()
        {
            TestMissing(
@"class C
{
    System.Action a;
    void Foo()
    {
        [||]var v = a;
        if (v != null)
        {
            v();
        }

        v = null;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public void TestSimpleForm1()
        {
            Test(
@"
using System;

class C
{
    public event EventHandler E;
    void M()
    {
        [||]if (this.E != null)
        {
            this.E(this, EventArgs.Empty);
        }
    }
}",
@"
using System;

class C
{
    public event EventHandler E;
    void M()
    {
        this.E?.Invoke(this, EventArgs.Empty);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public void TestInElseClause1()
        {
            Test(
@"
using System;

class C
{
    public event EventHandler E;
    void M()
    {
        if (true != true)
        {
        }
        else [||]if (this.E != null)
        {
            this.E(this, EventArgs.Empty);
        }
    }
}",
@"
using System;

class C
{
    public event EventHandler E;
    void M()
    {
        if (true != true)
        {
        }
        else
        {
            this.E?.Invoke(this, EventArgs.Empty);
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public void TestInElseClause2()
        {
            Test(
@"
using System;

class C
{
    public event EventHandler E;
    void M()
    {
        if (true != true)
        {
        }
        else [||]if (this.E != null)
            this.E(this, EventArgs.Empty);
    }
}",
@"
using System;

class C
{
    public event EventHandler E;
    void M()
    {
        if (true != true)
        {
        }
        else this.E?.Invoke(this, EventArgs.Empty);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public void TestTrivia1()
        {
            Test(
@"class C
{
    System.Action a;
    void Foo()
    {
        // Comment
        [||]var v = a;
        if (v != null)
        {
            v();
        }
    }
}",
@"class C
{
    System.Action a;
    void Foo()
    {
        // Comment
        a?.Invoke();
    }
}", compareTokens: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInvokeDelegateWithConditionalAccess)]
        public void TestTrivia2()
        {
            Test(
@"class C
{
    System.Action a;
    void Foo()
    {
        // Comment
        [||]if (a != null)
        {
            a();
        }
    }
}",
@"class C
{
    System.Action a;
    void Foo()
    {
        // Comment
        a?.Invoke();
    }
}", compareTokens: false);
        }
    }
}
