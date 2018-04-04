// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Editor.UnitTests.CallHierarchy;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CallHierarchy
{
    [UseExportProvider]
    public class CSharpCallHierarchyTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void InvokeOnMethod()
        {
            var text = @"
namespace N
{
    class C
    {
        void G$$oo()
        {
        }
    }
}";
            var testState = CallHierarchyTestState.Create(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "N.C.Goo()");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void InvokeOnProperty()
        {
            var text = @"
namespace N
{
    class C
    {
        public int G$$oo { get; set;}
    }
}";
            var testState = CallHierarchyTestState.Create(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "N.C.Goo");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void InvokeOnEvent()
        {
            var text = @"
using System;
namespace N
{
    class C
    {
        public event EventHandler Go$$o;
    }
}";
            var testState = CallHierarchyTestState.Create(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "N.C.Goo");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void Method_FindCalls()
        {
            var text = @"
namespace N
{
    class C
    {
        void G$$oo()
        {
        }
    }

    class G
    {
        void Main()
        {
            var c = new C();
            c.Goo();
        }

        void Main2()
        {
            var c = new C();
            c.Goo();
        }
    }
}";
            var testState = CallHierarchyTestState.Create(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "N.C.Goo()", new[] { string.Format(EditorFeaturesResources.Calls_To_0, "Goo") });
            testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_0, "Goo"), new[] { "N.G.Main()", "N.G.Main2()" });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void Method_InterfaceImplementation()
        {
            var text = @"
namespace N
{
    interface I
    {
        void Goo();
    }

    class C : I
    {
        public void G$$oo()
        {
        }
    }

    class G
    {
        void Main()
        {
            I c = new C();
            c.Goo();
        }

        void Main2()
        {
            var c = new C();
            c.Goo();
        }
    }
}";
            var testState = CallHierarchyTestState.Create(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "N.C.Goo()", new[] { string.Format(EditorFeaturesResources.Calls_To_0, "Goo"), string.Format(EditorFeaturesResources.Calls_To_Interface_Implementation_0, "N.I.Goo()") });
            testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_0, "Goo"), new[] { "N.G.Main2()" });
            testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_Interface_Implementation_0, "N.I.Goo()"), new[] { "N.G.Main()" });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void Method_CallToOverride()
        {
            var text = @"
namespace N
{
    class C
    {
        protected virtual void G$$oo() { }
    }

    class D : C
    {
        protected override void Goo() { }

        void Bar()
        {
            C c; 
            c.Goo()
        }

        void Baz()
        {
            D d;
            d.Goo();
        }
    }
}";
            var testState = CallHierarchyTestState.Create(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "N.C.Goo()", new[] { string.Format(EditorFeaturesResources.Calls_To_0, "Goo"), EditorFeaturesResources.Calls_To_Overrides });
            testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_0, "Goo"), new[] { "N.D.Bar()" });
            testState.VerifyResult(root, EditorFeaturesResources.Calls_To_Overrides, new[] { "N.D.Baz()" });
        }

        [WpfFact, WorkItem(829705, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/829705"), Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void Method_CallToBase()
        {
            var text = @"
namespace N
{
    class C
    {
        protected virtual void Goo() { }
    }

    class D : C
    {
        protected override void Goo() { }

        void Bar()
        {
            C c; 
            c.Goo()
        }

        void Baz()
        {
            D d;
            d.Go$$o();
        }
    }
}";
            var testState = CallHierarchyTestState.Create(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "N.D.Goo()", new[] { string.Format(EditorFeaturesResources.Calls_To_0, "Goo"), string.Format(EditorFeaturesResources.Calls_To_Base_Member_0, "N.C.Goo()") });
            testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_0, "Goo"), new[] { "N.D.Baz()" });
            testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_Base_Member_0, "N.C.Goo()"), new[] { "N.D.Bar()" });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void FieldInitializers()
        {
            var text = @"
namespace N
{
    class C
    {
        public int goo = Goo();

        protected int Goo$$() { return 0; }
    }
}";
            var testState = CallHierarchyTestState.Create(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "N.C.Goo()", new[] { string.Format(EditorFeaturesResources.Calls_To_0, "Goo") });
            testState.VerifyResultName(root, string.Format(EditorFeaturesResources.Calls_To_0, "Goo"), new[] { EditorFeaturesResources.Initializers });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void FieldReferences()
        {
            var text = @"
namespace N
{
    class C
    {
        public int g$$oo = Goo();

        protected int Goo() { goo = 3; }
    }
}";
            var testState = CallHierarchyTestState.Create(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "N.C.goo", new[] { string.Format(EditorFeaturesResources.References_To_Field_0, "goo") });
            testState.VerifyResult(root, string.Format(EditorFeaturesResources.References_To_Field_0, "goo"), new[] { "N.C.Goo()" });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
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

        public int goo()
        {
            var x = this.val;
        }
    }
}";
            var testState = CallHierarchyTestState.Create(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "N.C.val.get", new[] { string.Format(EditorFeaturesResources.Calls_To_0, "get_val") });
            testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_0, "get_val"), new[] { "N.C.goo()" });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
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

        public int goo()
        {
            int i;
            generic("", ref i);
        }
    }
}";
            var testState = CallHierarchyTestState.Create(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "N.C.generic<T>(this string, ref T)", new[] { string.Format(EditorFeaturesResources.Calls_To_0, "generic") });
            testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_0, "generic"), new[] { "N.C.goo()" });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
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
            var testState = CallHierarchyTestState.Create(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "ConsoleApplication10.Extensions.BarString(this string)", new[] { string.Format(EditorFeaturesResources.Calls_To_0, "BarString") });
            testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_0, "BarString"), new[] { "ConsoleApplication10.Program.Main(string[])" });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
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
            var testState = CallHierarchyTestState.Create(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "System.Linq.Enumerable.Single<TSource>(this System.Collections.Generic.IEnumerable<TSource>)", new[] { string.Format(EditorFeaturesResources.Calls_To_0, "Single") });
            testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_0, "Single"), new[] { "N.Program.Main(string[])" });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void InterfaceImplementors()
        {
            var text = @"
namespace N
{
    interface I
    {
        void Go$$o();
    }

    class C : I
    {
        public async Task Goo()
        {
        }
    }

    class G
    {
        void Main()
        {
            I c = new C();
            c.Goo();
        }

        void Main2()
        {
            var c = new C();
            c.Goo();
        }
    }
}";
            var testState = CallHierarchyTestState.Create(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "N.I.Goo()", new[] { string.Format(EditorFeaturesResources.Calls_To_0, "Goo"), string.Format(EditorFeaturesResources.Implements_0, "Goo") });
            testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_0, "Goo"), new[] { "N.G.Main()" });
            testState.VerifyResult(root, string.Format(EditorFeaturesResources.Implements_0, "Goo"), new[] { "N.C.Goo()" });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void NoFindOverridesOnSealedMethod()
        {
            var text = @"
namespace N
{
    class C
    {
        void G$$oo()
        {
        }
    }
}";
            var testState = CallHierarchyTestState.Create(text);
            var root = testState.GetRoot();
            Assert.DoesNotContain("Overrides", root.SupportedSearchCategories.Select(s => s.DisplayName));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void FindOverrides()
        {
            var text = @"
namespace N
{
    class C
    {
        public virtual void G$$oo()
        {
        }
    }

    class G : C
    {
        public override void Goo()
        {
        }
    }
}";
            var testState = CallHierarchyTestState.Create(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "N.C.Goo()", new[] { string.Format(EditorFeaturesResources.Calls_To_0, "Goo"), EditorFeaturesResources.Overrides_ });
            testState.VerifyResult(root, EditorFeaturesResources.Overrides_, new[] { "N.G.Goo()" });
        }

        [WorkItem(844613, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/844613")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
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
            var testState = CallHierarchyTestState.Create(text);
            var root = testState.GetRoot();
            testState.VerifyRoot(root, "Base.M()", new[] { string.Format(EditorFeaturesResources.Calls_To_0, "M"), EditorFeaturesResources.Overrides_, EditorFeaturesResources.Calls_To_Overrides });
            testState.VerifyResult(root, EditorFeaturesResources.Overrides_, new[] { "Derived.M()" });
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.CallHierarchy)]
        public void SearchAfterEditWorks()
        {
            var text = @"
namespace N
{
    class C
    {
        void G$$oo()
        {
        }
    }
}";
            var testState = CallHierarchyTestState.Create(text);
            var root = testState.GetRoot();

            testState.Workspace.Documents.Single().GetTextBuffer().Insert(0, "/* hello */");

            testState.VerifyRoot(root, "N.C.Goo()", new[] { string.Format(EditorFeaturesResources.Calls_To_0, "Goo"), });
            testState.VerifyResult(root, string.Format(EditorFeaturesResources.Calls_To_0, "Goo"), expectedCallers: new[] { "N.C.Goo()" });
        }
    }
}
