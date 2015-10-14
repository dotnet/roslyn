// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.CSharp.Debugging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Debugging
{
    public class NameResolverTests
    {
        private void Test(string text, string searchText, params string[] expectedNames)
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(text))
            {
                var nameResolver = new BreakpointResolver(workspace.CurrentSolution, searchText);
                var results = nameResolver.DoAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);

                Assert.Equal(expectedNames, results.Select(r => r.LocationNameOpt));
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public void TestCSharpLanguageDebugInfoCreateNameResolver()
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromLines(" "))
            {
                var debugInfo = new CSharpBreakpointResolutionService();
                var results = debugInfo.ResolveBreakpointsAsync(workspace.CurrentSolution, "foo", CancellationToken.None).WaitAndGetResult(CancellationToken.None);
                Assert.Equal(0, results.Count());
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public void TestSimpleNameInClass()
        {
            var text =
@"class C
{
  void Foo()
  {
  }
}";
            Test(text, "Foo", "C.Foo()");
            Test(text, "foo");
            Test(text, "C.Foo", "C.Foo()");
            Test(text, "N.C.Foo");
            Test(text, "Foo<T>");
            Test(text, "C<T>.Foo");
            Test(text, "Foo()", "C.Foo()");
            Test(text, "Foo(int i)");
            Test(text, "Foo(int)");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public void TestSimpleNameInNamespace()
        {
            var text =
@"
namespace N
{
  class C
  {
    void Foo()
    {
    }
  }
}";
            Test(text, "Foo", "N.C.Foo()");
            Test(text, "foo");
            Test(text, "C.Foo", "N.C.Foo()");
            Test(text, "N.C.Foo", "N.C.Foo()");
            Test(text, "Foo<T>");
            Test(text, "C<T>.Foo");
            Test(text, "Foo()", "N.C.Foo()");
            Test(text, "C.Foo()", "N.C.Foo()");
            Test(text, "N.C.Foo()", "N.C.Foo()");
            Test(text, "Foo(int i)");
            Test(text, "Foo(int)");
            Test(text, "Foo(a)");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public void TestSimpleNameInGenericClassNamespace()
        {
            var text =
@"
namespace N
{
  class C<T>
  {
    void Foo()
    {
    }
  }
}";
            Test(text, "Foo", "N.C<T>.Foo()");
            Test(text, "foo");
            Test(text, "C.Foo", "N.C<T>.Foo()");
            Test(text, "N.C.Foo", "N.C<T>.Foo()");
            Test(text, "Foo<T>");
            Test(text, "C<T>.Foo", "N.C<T>.Foo()");
            Test(text, "C<T>.Foo()", "N.C<T>.Foo()");
            Test(text, "Foo()", "N.C<T>.Foo()");
            Test(text, "C.Foo()", "N.C<T>.Foo()");
            Test(text, "N.C.Foo()", "N.C<T>.Foo()");
            Test(text, "Foo(int i)");
            Test(text, "Foo(int)");
            Test(text, "Foo(a)");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public void TestGenericNameInClassNamespace()
        {
            var text =
@"
namespace N
{
  class C
  {
    void Foo<T>()
    {
    }
  }
}";
            Test(text, "Foo", "N.C.Foo<T>()");
            Test(text, "foo");
            Test(text, "C.Foo", "N.C.Foo<T>()");
            Test(text, "N.C.Foo", "N.C.Foo<T>()");
            Test(text, "Foo<T>", "N.C.Foo<T>()");
            Test(text, "Foo<X>", "N.C.Foo<T>()");
            Test(text, "Foo<T,X>");
            Test(text, "C<T>.Foo");
            Test(text, "C<T>.Foo()");
            Test(text, "Foo()", "N.C.Foo<T>()");
            Test(text, "C.Foo()", "N.C.Foo<T>()");
            Test(text, "N.C.Foo()", "N.C.Foo<T>()");
            Test(text, "Foo(int i)");
            Test(text, "Foo(int)");
            Test(text, "Foo(a)");
            Test(text, "Foo<T>(int i)");
            Test(text, "Foo<T>(int)");
            Test(text, "Foo<T>(a)");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public void TestOverloadsInSingleClass()
        {
            var text =
@"class C
{
  void Foo()
  {
  }

  void Foo(int i)
  {
  }
}";
            Test(text, "Foo", "C.Foo()", "C.Foo(int)");
            Test(text, "foo");
            Test(text, "C.Foo", "C.Foo()", "C.Foo(int)");
            Test(text, "N.C.Foo");
            Test(text, "Foo<T>");
            Test(text, "C<T>.Foo");
            Test(text, "Foo()", "C.Foo()");
            Test(text, "Foo(int i)", "C.Foo(int)");
            Test(text, "Foo(int)", "C.Foo(int)");
            Test(text, "Foo(i)", "C.Foo(int)");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public void TestMethodsInMultipleClasses()
        {
            var text =
@"namespace N
{
  class C
  {
    void Foo()
    {
    }
  }
}

namespace N1
{
  class C
  {
    void Foo(int i)
    {
    }
  }
}";
            Test(text, "Foo", "N1.C.Foo(int)", "N.C.Foo()");
            Test(text, "foo");
            Test(text, "C.Foo", "N1.C.Foo(int)", "N.C.Foo()");
            Test(text, "N.C.Foo", "N.C.Foo()");
            Test(text, "N1.C.Foo", "N1.C.Foo(int)");
            Test(text, "Foo<T>");
            Test(text, "C<T>.Foo");
            Test(text, "Foo()", "N.C.Foo()");
            Test(text, "Foo(int i)", "N1.C.Foo(int)");
            Test(text, "Foo(int)", "N1.C.Foo(int)");
            Test(text, "Foo(i)", "N1.C.Foo(int)");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public void TestMethodsWithDifferentArityInMultipleClasses()
        {
            var text =
@"namespace N
{
  class C
  {
    void Foo()
    {
    }
  }
}

namespace N1
{
  class C
  {
    void Foo<T>(int i)
    {
    }
  }
}";
            Test(text, "Foo", "N1.C.Foo<T>(int)", "N.C.Foo()");
            Test(text, "foo");
            Test(text, "C.Foo", "N1.C.Foo<T>(int)", "N.C.Foo()");
            Test(text, "N.C.Foo", "N.C.Foo()");
            Test(text, "N1.C.Foo", "N1.C.Foo<T>(int)");
            Test(text, "Foo<T>", "N1.C.Foo<T>(int)");
            Test(text, "C<T>.Foo");
            Test(text, "Foo()", "N.C.Foo()");
            Test(text, "Foo<T>()");
            Test(text, "Foo(int i)", "N1.C.Foo<T>(int)");
            Test(text, "Foo(int)", "N1.C.Foo<T>(int)");
            Test(text, "Foo(i)", "N1.C.Foo<T>(int)");
            Test(text, "Foo<T>(int i)", "N1.C.Foo<T>(int)");
            Test(text, "Foo<T>(int)", "N1.C.Foo<T>(int)");
            Test(text, "Foo<T>(i)", "N1.C.Foo<T>(int)");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public void TestOverloadsWithMultipleParametersInSingleClass()
        {
            var text =
@"class C
{
  void Foo(int a)
  {
  }

  void Foo(int a, string b = ""bb"")
  {
  }

  void Foo(__arglist)
  {
  }
}";
            Test(text, "Foo", "C.Foo(int)", "C.Foo(int, [string])", "C.Foo(__arglist)");
            Test(text, "foo");
            Test(text, "C.Foo", "C.Foo(int)", "C.Foo(int, [string])", "C.Foo(__arglist)");
            Test(text, "N.C.Foo");
            Test(text, "Foo<T>");
            Test(text, "C<T>.Foo");
            Test(text, "Foo()", "C.Foo(__arglist)");
            Test(text, "Foo(int i)", "C.Foo(int)");
            Test(text, "Foo(int)", "C.Foo(int)");
            Test(text, "Foo(int x = 42)", "C.Foo(int)");
            Test(text, "Foo(i)", "C.Foo(int)");
            Test(text, "Foo(int i, int b)", "C.Foo(int, [string])");
            Test(text, "Foo(int, bool)", "C.Foo(int, [string])");
            Test(text, "Foo(i, s)", "C.Foo(int, [string])");
            Test(text, "Foo(,)", "C.Foo(int, [string])");
            Test(text, "Foo(int x = 42,)", "C.Foo(int, [string])");
            Test(text, "Foo(int x = 42, y = 42)", "C.Foo(int, [string])");
            Test(text, "Foo([attr] x = 42, y = 42)", "C.Foo(int, [string])");
            Test(text, "Foo(int i, int b, char c)");
            Test(text, "Foo(int, bool, char)");
            Test(text, "Foo(i, s, c)");
            Test(text, "Foo(__arglist)", "C.Foo(int)");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public void AccessorTests()
        {
            var text =
@"class C
{
  int Property1 { get { return 42; } }
  int Property2 { set { } }
  int Property3 { get; set;}
}";
            Test(text, "Property1", "C.Property1");
            Test(text, "Property2", "C.Property2");
            Test(text, "Property3", "C.Property3");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public void NegativeTests()
        {
            var text =
@"using System.Runtime.CompilerServices;
abstract class C
{
    public abstract void AbstractMethod(int a);
    int Field;
    delegate void Delegate();
    event Delegate Event;
    [IndexerName(""ABCD"")]
    int this[int i] { get { return i; } }
    void Foo() { }
    void Foo(int x = 1, int y = 2) { }
    ~C() { }
}";
            Test(text, "AbstractMethod");
            Test(text, "Field");
            Test(text, "Delegate");
            Test(text, "Event");
            Test(text, "this");
            Test(text, "C.this[int]");
            Test(text, "C.get_Item");
            Test(text, "C.get_Item(i)");
            Test(text, "C[i]");
            Test(text, "ABCD");
            Test(text, "C.ABCD(int)");
            Test(text, "42");
            Test(text, "Foo", "C.Foo()", "C.Foo([int], [int])"); // just making sure it would normally resolve before trying bad syntax
            Test(text, "Foo Foo");
            Test(text, "Foo()asdf");
            Test(text, "Foo(),");
            Test(text, "Foo(),f");
            Test(text, "Foo().Foo");
            Test(text, "Foo(");
            Test(text, "(Foo");
            Test(text, "Foo)");
            Test(text, "(Foo)");
            Test(text, "Foo(x = 42, y = 42)", "C.Foo([int], [int])"); // just making sure it would normally resolve before trying bad syntax
            Test(text, "int x = 42");
            Test(text, "Foo(int x = 42, y = 42");
            Test(text, "C");
            Test(text, "C.C");
            Test(text, "~");
            Test(text, "~C");
            Test(text, "C.~C()");
            Test(text, "");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public void TestInstanceConstructors()
        {
            var text =
@"class C
{
  public C() { }
}

class G<T>
{
  public G() { }
  ~G() { }
}";
            Test(text, "C", "C.C()");
            Test(text, "C.C", "C.C()");
            Test(text, "C.C()", "C.C()");
            Test(text, "C()", "C.C()");
            Test(text, "C<T>");
            Test(text, "C<T>()");
            Test(text, "C(int i)");
            Test(text, "C(int)");
            Test(text, "C(i)");
            Test(text, "G", "G<T>.G()");
            Test(text, "G()", "G<T>.G()");
            Test(text, "G.G", "G<T>.G()");
            Test(text, "G.G()", "G<T>.G()");
            Test(text, "G<T>.G", "G<T>.G()");
            Test(text, "G<t>.G()", "G<T>.G()");
            Test(text, "G<T>");
            Test(text, "G<T>()");
            Test(text, "G.G<T>");
            Test(text, ".ctor");
            Test(text, ".ctor()");
            Test(text, "C.ctor");
            Test(text, "C.ctor()");
            Test(text, "G.ctor");
            Test(text, "G<T>.ctor()");
            Test(text, "Finalize", "G<T>.~G()");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public void TestStaticConstructors()
        {
            var text =
@"class C
{
  static C()
  {
  }
}";
            Test(text, "C", "C.C()");
            Test(text, "C.C", "C.C()");
            Test(text, "C.C()", "C.C()");
            Test(text, "C()", "C.C()");
            Test(text, "C<T>");
            Test(text, "C<T>()");
            Test(text, "C(int i)");
            Test(text, "C(int)");
            Test(text, "C(i)");
            Test(text, "C.cctor");
            Test(text, "C.cctor()");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public void TestAllConstructors()
        {
            var text =
@"class C
{
  static C()
  {
  }

  public C(int i)
  {
  }
}";
            Test(text, "C", "C.C(int)", "C.C()");
            Test(text, "C.C", "C.C(int)", "C.C()");
            Test(text, "C.C()", "C.C()");
            Test(text, "C()", "C.C()");
            Test(text, "C<T>");
            Test(text, "C<T>()");
            Test(text, "C(int i)", "C.C(int)");
            Test(text, "C(int)", "C.C(int)");
            Test(text, "C(i)", "C.C(int)");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public void TestPartialMethods()
        {
            var text =
@"partial class C
{
  partial int M1();

  partial void M2() { }

  partial void M2();

  partial int M3();

  partial int M3(int x) { return 0; }

  partial void M4() { }
}";
            Test(text, "M1");
            Test(text, "C.M1");
            Test(text, "M2", "C.M2()");
            Test(text, "M3", "C.M3(int)");
            Test(text, "M3()");
            Test(text, "M3(y)", "C.M3(int)");
            Test(text, "M4", "C.M4()");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public void TestLeadingAndTrailingText()
        {
            var text =
@"class C
{
  void Foo() { };
}";
            Test(text, "Foo;", "C.Foo()");
            Test(text, "Foo();", "C.Foo()");
            Test(text, "  Foo;", "C.Foo()");
            Test(text, "  Foo;;");
            Test(text, "  Foo; ;");
            Test(text, "Foo(); ", "C.Foo()");
            Test(text, " Foo (  )  ; ", "C.Foo()");
            Test(text, "Foo(); // comment", "C.Foo()");
            Test(text, "/*comment*/Foo(/* params */); /* comment", "C.Foo()");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public void TestEscapedKeywords()
        {
            var text =
@"struct @true { }
class @foreach
{
    void where(@true @this) { }
    void @false() { }
}";
            Test(text, "where", "@foreach.where(@true)");
            Test(text, "@where", "@foreach.where(@true)");
            Test(text, "@foreach.where", "@foreach.where(@true)");
            Test(text, "foreach.where");
            Test(text, "@foreach.where(true)");
            Test(text, "@foreach.where(@if)", "@foreach.where(@true)");
            Test(text, "false");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public void TestAliasQualifiedNames()
        {
            var text =
@"extern alias A
class C
{
    void Foo(D d) { }
}";
            Test(text, "A::Foo");
            Test(text, "A::Foo(A::B)");
            Test(text, "A::Foo(A::B)");
            Test(text, "A::C.Foo");
            Test(text, "C.Foo(A::Q)", "C.Foo(D)");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public void TestNestedTypesAndNamespaces()
        {
            var text =
@"namespace N1
{
  class C
  {
    void Foo() { }
  }
  namespace N2
  {
    class C { }
  }
  namespace N3
  {
    class D { }
  }
  namespace N4
  {
    class C
    {
      void Foo(double x) { }

      class D
      {
        void Foo() { }

        class E
        {
          void Foo() { }
        }
      }
    }
  }
  namespace N5 { }
}";

            Test(text, "Foo", "N1.N4.C.Foo(double)", "N1.N4.C.D.Foo()", "N1.N4.C.D.E.Foo()", "N1.C.Foo()");
            Test(text, "C.Foo", "N1.N4.C.Foo(double)", "N1.C.Foo()");
            Test(text, "D.Foo", "N1.N4.C.D.Foo()");
            Test(text, "N1.N4.C.D.Foo", "N1.N4.C.D.Foo()");
            Test(text, "N1.Foo");
            Test(text, "N3.C.Foo");
            Test(text, "N5.C.Foo");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public void TestInterfaces()
        {
            var text =
@"interface I1
{
  void Foo();
}
class C1 : I1
{
  void I1.Foo() { }
}";

            Test(text, "Foo", "C1.Foo()");
            Test(text, "I1.Foo");
            Test(text, "C1.Foo", "C1.Foo()");
            Test(text, "C1.I1.Moo");
        }
    }
}
