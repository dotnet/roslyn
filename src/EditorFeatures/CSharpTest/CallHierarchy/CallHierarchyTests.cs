// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Editor.UnitTests.CallHierarchy;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CallHierarchy
{
    public class CallHierarchyTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void InvokeOnMethod()
        {
            var text = @"
namespace N
{
    class C
    {
        void F$$oo()
        {
        }
    }
}";
            var testState = new CallHierarchyTestState(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "N.C.Foo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void InvokeOnProperty()
        {
            var text = @"
namespace N
{
    class C
    {
        public int F$$oo { get; set;}
    }
}";
            var testState = new CallHierarchyTestState(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "N.C.Foo");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void InvokeOnEvent()
        {
            var text = @"
using System;
namespace N
{
    class C
    {
        public event EventHandler Fo$$o;
    }
}";
            var testState = new CallHierarchyTestState(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "N.C.Foo");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void Method_FindCalls()
        {
            var text = @"
namespace N
{
    class C
    {
        void F$$oo()
        {
        }
    }

    class G
    {
        void Main()
        {
            var c = new C();
            c.Foo();
        }

        void Main2()
        {
            var c = new C();
            c.Foo();
        }
    }
}";
            var testState = new CallHierarchyTestState(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "N.C.Foo()", new[] { "Calls To 'Foo'" });
            testState.VerifyResult(root, "Calls To 'Foo'", new[] { "N.G.Main()", "N.G.Main2()" });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void Method_InterfaceImplementation()
        {
            var text = @"
namespace N
{
    interface I
    {
        void Foo();
    }

    class C : I
    {
        public void F$$oo()
        {
        }
    }

    class G
    {
        void Main()
        {
            I c = new C();
            c.Foo();
        }

        void Main2()
        {
            var c = new C();
            c.Foo();
        }
    }
}";
            var testState = new CallHierarchyTestState(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "N.C.Foo()", new[] { "Calls To 'Foo'", "Calls To Interface Implementation 'N.I.Foo()'" });
            testState.VerifyResult(root, "Calls To 'Foo'", new[] { "N.G.Main2()" });
            testState.VerifyResult(root, "Calls To Interface Implementation 'N.I.Foo()'", new[] { "N.G.Main()" });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void Method_CallToOverride()
        {
            var text = @"
namespace N
{
    class C
    {
        protected virtual void F$$oo() { }
    }

    class D : C
    {
        protected override void Foo() { }

        void Bar()
        {
            C c; 
            c.Foo()
        }

        void Baz()
        {
            D d;
            d.Foo();
        }
    }
}";
            var testState = new CallHierarchyTestState(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "N.C.Foo()", new[] { "Calls To 'Foo'", "Calls To Overrides" });
            testState.VerifyResult(root, "Calls To 'Foo'", new[] { "N.D.Bar()" });
            testState.VerifyResult(root, "Calls To Overrides", new[] { "N.D.Baz()" });
        }

        [Fact, WorkItem(829705), Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void Method_CallToBase()
        {
            var text = @"
namespace N
{
    class C
    {
        protected virtual void Foo() { }
    }

    class D : C
    {
        protected override void Foo() { }

        void Bar()
        {
            C c; 
            c.Foo()
        }

        void Baz()
        {
            D d;
            d.Fo$$o();
        }
    }
}";
            var testState = new CallHierarchyTestState(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "N.D.Foo()", new[] { "Calls To 'Foo'", "Calls To Base Member 'N.C.Foo()'" });
            testState.VerifyResult(root, "Calls To 'Foo'", new[] { "N.D.Baz()" });
            testState.VerifyResult(root, "Calls To Base Member 'N.C.Foo()'", new[] { "N.D.Bar()" });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void FieldInitializers()
        {
            var text = @"
namespace N
{
    class C
    {
        public int foo = Foo();

        protected int Foo$$() { return 0; }
    }
}";
            var testState = new CallHierarchyTestState(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "N.C.Foo()", new[] { "Calls To 'Foo'" });
            testState.VerifyResultName(root, "Calls To 'Foo'", new[] { "Initializers" });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void FieldReferences()
        {
            var text = @"
namespace N
{
    class C
    {
        public int f$$oo = Foo();

        protected int Foo() { foo = 3; }
    }
}";
            var testState = new CallHierarchyTestState(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "N.C.foo", new[] { "References To Field 'foo'" });
            testState.VerifyResult(root, "References To Field 'foo'", new[] { "N.C.Foo()" });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void PropertyGet()
        {
            var text = @"
namespace N
{
    class C
    {
        public int val
        {
            g$$et
            {
                return 0;
            }
        }

        public int foo()
        {
            var x = this.val;
        }
    }
}";
            var testState = new CallHierarchyTestState(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "N.C.val.get", new[] { "Calls To 'get_val'" });
            testState.VerifyResult(root, "Calls To 'get_val'", new[] { "N.C.foo()" });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void Generic()
        {
            var text = @"
namespace N
{
    class C
    {
        public int gen$$eric<T>(this string generic, ref T stuff)
        {
            return 0;
        }

        public int foo()
        {
            int i;
            generic("", ref i);
        }
    }
}";
            var testState = new CallHierarchyTestState(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "N.C.generic<T>(this string, ref T)", new[] { "Calls To 'generic'" });
            testState.VerifyResult(root, "Calls To 'generic'", new[] { "N.C.foo()" });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void ExtensionMethods()
        {
            var text = @"
namespace ConsoleApplication10
{
    class Program
    {
        static void Main(string[] args)
        {
            var x = ""string"";
            x.BarStr$$ing();
        }
    }
    
    public static class Extensions
    {
        public static string BarString(this string s)
        {
            return s;
        }
    }
}";
            var testState = new CallHierarchyTestState(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "ConsoleApplication10.Extensions.BarString(this string)", new[] { "Calls To 'BarString'" });
            testState.VerifyResult(root, "Calls To 'BarString'", new[] { "ConsoleApplication10.Program.Main(string[])" });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void GenericExtensionMethods()
        {
            var text = @"
using System.Collections.Generic;
using System.Linq;
namespace N
{
    class Program
    {
        static void Main(string[] args)
        {
            List<int> x = new List<int>();
            var z = x.Si$$ngle();
        }
    }
}";
            var testState = new CallHierarchyTestState(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "System.Linq.Enumerable.Single<TSource>(this System.Collections.Generic.IEnumerable<TSource>)", new[] { "Calls To 'Single'" });
            testState.VerifyResult(root, "Calls To 'Single'", new[] { "N.Program.Main(string[])" });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void InterfaceImplementors()
        {
            var text = @"
namespace N
{
    interface I
    {
        void Fo$$o();
    }

    class C : I
    {
        public void Foo()
        {
        }
    }

    class G
    {
        void Main()
        {
            I c = new C();
            c.Foo();
        }

        void Main2()
        {
            var c = new C();
            c.Foo();
        }
    }
}";
            var testState = new CallHierarchyTestState(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "N.I.Foo()", new[] { "Calls To 'Foo'", "Implements 'Foo'" });
            testState.VerifyResult(root, "Calls To 'Foo'", new[] { "N.G.Main()" });
            testState.VerifyResult(root, "Implements 'Foo'", new[] { "N.C.Foo()" });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void NoFindOverridesOnSealedMethod()
        {
            var text = @"
namespace N
{
    class C
    {
        void F$$oo()
        {
        }
    }
}";
            var testState = new CallHierarchyTestState(text);
            var root = testState.GetRoot();
            Assert.DoesNotContain("Overrides", root.SupportedSearchCategories.Select(s => s.DisplayName));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void FindOverrides()
        {
            var text = @"
namespace N
{
    class C
    {
        public virtual void F$$oo()
        {
        }
    }

    class G : C
    {
        public override void Foo()
        {
        }
    }
}";
            var testState = new CallHierarchyTestState(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "N.C.Foo()", new[] { "Calls To 'Foo'", "Overrides" });
            testState.VerifyResult(root, "Overrides", new[] { "N.G.Foo()" });
        }

        [WorkItem(844613)]
        [Fact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void AbstractMethodInclusionToOverrides()
        {
            var text = @"
using System;

abstract class Base
{
    public abstract void $$M();
}
 
class Derived : Base
{
    public override void M()
    {
        throw new NotImplementedException();
    }
}";
            var testState = new CallHierarchyTestState(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "Base.M()", new[] { "Calls To 'M'", "Overrides", "Calls To Overrides" });
            testState.VerifyResult(root, "Overrides", new[] { "Derived.M()" });
        }
    }
}
