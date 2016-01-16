// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private async Task TestAsync(string text, string searchText, params string[] expectedNames)
        {
            using (var workspace = await TestWorkspaceFactory.CreateCSharpWorkspaceFromFileAsync(text))
            {
                var nameResolver = new BreakpointResolver(workspace.CurrentSolution, searchText);
                var results = await nameResolver.DoAsync(CancellationToken.None);

                Assert.Equal(expectedNames, results.Select(r => r.LocationNameOpt));
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public async Task TestCSharpLanguageDebugInfoCreateNameResolver()
        {
            using (var workspace = await TestWorkspaceFactory.CreateCSharpWorkspaceFromFileAsync(" "))
            {
                var debugInfo = new CSharpBreakpointResolutionService();
                var results = await debugInfo.ResolveBreakpointsAsync(workspace.CurrentSolution, "foo", CancellationToken.None);
                Assert.Equal(0, results.Count());
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public async Task TestSimpleNameInClass()
        {
            var text =
@"class C
{
  void Foo()
  {
  }
}";
            await TestAsync(text, "Foo", "C.Foo()");
            await TestAsync(text, "foo");
            await TestAsync(text, "C.Foo", "C.Foo()");
            await TestAsync(text, "N.C.Foo");
            await TestAsync(text, "Foo<T>");
            await TestAsync(text, "C<T>.Foo");
            await TestAsync(text, "Foo()", "C.Foo()");
            await TestAsync(text, "Foo(int i)");
            await TestAsync(text, "Foo(int)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public async Task TestSimpleNameInNamespace()
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
            await TestAsync(text, "Foo", "N.C.Foo()");
            await TestAsync(text, "foo");
            await TestAsync(text, "C.Foo", "N.C.Foo()");
            await TestAsync(text, "N.C.Foo", "N.C.Foo()");
            await TestAsync(text, "Foo<T>");
            await TestAsync(text, "C<T>.Foo");
            await TestAsync(text, "Foo()", "N.C.Foo()");
            await TestAsync(text, "C.Foo()", "N.C.Foo()");
            await TestAsync(text, "N.C.Foo()", "N.C.Foo()");
            await TestAsync(text, "Foo(int i)");
            await TestAsync(text, "Foo(int)");
            await TestAsync(text, "Foo(a)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public async Task TestSimpleNameInGenericClassNamespace()
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
            await TestAsync(text, "Foo", "N.C<T>.Foo()");
            await TestAsync(text, "foo");
            await TestAsync(text, "C.Foo", "N.C<T>.Foo()");
            await TestAsync(text, "N.C.Foo", "N.C<T>.Foo()");
            await TestAsync(text, "Foo<T>");
            await TestAsync(text, "C<T>.Foo", "N.C<T>.Foo()");
            await TestAsync(text, "C<T>.Foo()", "N.C<T>.Foo()");
            await TestAsync(text, "Foo()", "N.C<T>.Foo()");
            await TestAsync(text, "C.Foo()", "N.C<T>.Foo()");
            await TestAsync(text, "N.C.Foo()", "N.C<T>.Foo()");
            await TestAsync(text, "Foo(int i)");
            await TestAsync(text, "Foo(int)");
            await TestAsync(text, "Foo(a)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public async Task TestGenericNameInClassNamespace()
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
            await TestAsync(text, "Foo", "N.C.Foo<T>()");
            await TestAsync(text, "foo");
            await TestAsync(text, "C.Foo", "N.C.Foo<T>()");
            await TestAsync(text, "N.C.Foo", "N.C.Foo<T>()");
            await TestAsync(text, "Foo<T>", "N.C.Foo<T>()");
            await TestAsync(text, "Foo<X>", "N.C.Foo<T>()");
            await TestAsync(text, "Foo<T,X>");
            await TestAsync(text, "C<T>.Foo");
            await TestAsync(text, "C<T>.Foo()");
            await TestAsync(text, "Foo()", "N.C.Foo<T>()");
            await TestAsync(text, "C.Foo()", "N.C.Foo<T>()");
            await TestAsync(text, "N.C.Foo()", "N.C.Foo<T>()");
            await TestAsync(text, "Foo(int i)");
            await TestAsync(text, "Foo(int)");
            await TestAsync(text, "Foo(a)");
            await TestAsync(text, "Foo<T>(int i)");
            await TestAsync(text, "Foo<T>(int)");
            await TestAsync(text, "Foo<T>(a)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public async Task TestOverloadsInSingleClass()
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
            await TestAsync(text, "Foo", "C.Foo()", "C.Foo(int)");
            await TestAsync(text, "foo");
            await TestAsync(text, "C.Foo", "C.Foo()", "C.Foo(int)");
            await TestAsync(text, "N.C.Foo");
            await TestAsync(text, "Foo<T>");
            await TestAsync(text, "C<T>.Foo");
            await TestAsync(text, "Foo()", "C.Foo()");
            await TestAsync(text, "Foo(int i)", "C.Foo(int)");
            await TestAsync(text, "Foo(int)", "C.Foo(int)");
            await TestAsync(text, "Foo(i)", "C.Foo(int)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public async Task TestMethodsInMultipleClasses()
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
            await TestAsync(text, "Foo", "N1.C.Foo(int)", "N.C.Foo()");
            await TestAsync(text, "foo");
            await TestAsync(text, "C.Foo", "N1.C.Foo(int)", "N.C.Foo()");
            await TestAsync(text, "N.C.Foo", "N.C.Foo()");
            await TestAsync(text, "N1.C.Foo", "N1.C.Foo(int)");
            await TestAsync(text, "Foo<T>");
            await TestAsync(text, "C<T>.Foo");
            await TestAsync(text, "Foo()", "N.C.Foo()");
            await TestAsync(text, "Foo(int i)", "N1.C.Foo(int)");
            await TestAsync(text, "Foo(int)", "N1.C.Foo(int)");
            await TestAsync(text, "Foo(i)", "N1.C.Foo(int)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public async Task TestMethodsWithDifferentArityInMultipleClasses()
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
            await TestAsync(text, "Foo", "N1.C.Foo<T>(int)", "N.C.Foo()");
            await TestAsync(text, "foo");
            await TestAsync(text, "C.Foo", "N1.C.Foo<T>(int)", "N.C.Foo()");
            await TestAsync(text, "N.C.Foo", "N.C.Foo()");
            await TestAsync(text, "N1.C.Foo", "N1.C.Foo<T>(int)");
            await TestAsync(text, "Foo<T>", "N1.C.Foo<T>(int)");
            await TestAsync(text, "C<T>.Foo");
            await TestAsync(text, "Foo()", "N.C.Foo()");
            await TestAsync(text, "Foo<T>()");
            await TestAsync(text, "Foo(int i)", "N1.C.Foo<T>(int)");
            await TestAsync(text, "Foo(int)", "N1.C.Foo<T>(int)");
            await TestAsync(text, "Foo(i)", "N1.C.Foo<T>(int)");
            await TestAsync(text, "Foo<T>(int i)", "N1.C.Foo<T>(int)");
            await TestAsync(text, "Foo<T>(int)", "N1.C.Foo<T>(int)");
            await TestAsync(text, "Foo<T>(i)", "N1.C.Foo<T>(int)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public async Task TestOverloadsWithMultipleParametersInSingleClass()
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
            await TestAsync(text, "Foo", "C.Foo(int)", "C.Foo(int, [string])", "C.Foo(__arglist)");
            await TestAsync(text, "foo");
            await TestAsync(text, "C.Foo", "C.Foo(int)", "C.Foo(int, [string])", "C.Foo(__arglist)");
            await TestAsync(text, "N.C.Foo");
            await TestAsync(text, "Foo<T>");
            await TestAsync(text, "C<T>.Foo");
            await TestAsync(text, "Foo()", "C.Foo(__arglist)");
            await TestAsync(text, "Foo(int i)", "C.Foo(int)");
            await TestAsync(text, "Foo(int)", "C.Foo(int)");
            await TestAsync(text, "Foo(int x = 42)", "C.Foo(int)");
            await TestAsync(text, "Foo(i)", "C.Foo(int)");
            await TestAsync(text, "Foo(int i, int b)", "C.Foo(int, [string])");
            await TestAsync(text, "Foo(int, bool)", "C.Foo(int, [string])");
            await TestAsync(text, "Foo(i, s)", "C.Foo(int, [string])");
            await TestAsync(text, "Foo(,)", "C.Foo(int, [string])");
            await TestAsync(text, "Foo(int x = 42,)", "C.Foo(int, [string])");
            await TestAsync(text, "Foo(int x = 42, y = 42)", "C.Foo(int, [string])");
            await TestAsync(text, "Foo([attr] x = 42, y = 42)", "C.Foo(int, [string])");
            await TestAsync(text, "Foo(int i, int b, char c)");
            await TestAsync(text, "Foo(int, bool, char)");
            await TestAsync(text, "Foo(i, s, c)");
            await TestAsync(text, "Foo(__arglist)", "C.Foo(int)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public async Task AccessorTests()
        {
            var text =
@"class C
{
  int Property1 { get { return 42; } }
  int Property2 { set { } }
  int Property3 { get; set;}
}";
            await TestAsync(text, "Property1", "C.Property1");
            await TestAsync(text, "Property2", "C.Property2");
            await TestAsync(text, "Property3", "C.Property3");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public async Task NegativeTests()
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
            await TestAsync(text, "AbstractMethod");
            await TestAsync(text, "Field");
            await TestAsync(text, "Delegate");
            await TestAsync(text, "Event");
            await TestAsync(text, "this");
            await TestAsync(text, "C.this[int]");
            await TestAsync(text, "C.get_Item");
            await TestAsync(text, "C.get_Item(i)");
            await TestAsync(text, "C[i]");
            await TestAsync(text, "ABCD");
            await TestAsync(text, "C.ABCD(int)");
            await TestAsync(text, "42");
            await TestAsync(text, "Foo", "C.Foo()", "C.Foo([int], [int])"); // just making sure it would normally resolve before trying bad syntax
            await TestAsync(text, "Foo Foo");
            await TestAsync(text, "Foo()asdf");
            await TestAsync(text, "Foo(),");
            await TestAsync(text, "Foo(),f");
            await TestAsync(text, "Foo().Foo");
            await TestAsync(text, "Foo(");
            await TestAsync(text, "(Foo");
            await TestAsync(text, "Foo)");
            await TestAsync(text, "(Foo)");
            await TestAsync(text, "Foo(x = 42, y = 42)", "C.Foo([int], [int])"); // just making sure it would normally resolve before trying bad syntax
            await TestAsync(text, "int x = 42");
            await TestAsync(text, "Foo(int x = 42, y = 42");
            await TestAsync(text, "C");
            await TestAsync(text, "C.C");
            await TestAsync(text, "~");
            await TestAsync(text, "~C");
            await TestAsync(text, "C.~C()");
            await TestAsync(text, "");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public async Task TestInstanceConstructors()
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
            await TestAsync(text, "C", "C.C()");
            await TestAsync(text, "C.C", "C.C()");
            await TestAsync(text, "C.C()", "C.C()");
            await TestAsync(text, "C()", "C.C()");
            await TestAsync(text, "C<T>");
            await TestAsync(text, "C<T>()");
            await TestAsync(text, "C(int i)");
            await TestAsync(text, "C(int)");
            await TestAsync(text, "C(i)");
            await TestAsync(text, "G", "G<T>.G()");
            await TestAsync(text, "G()", "G<T>.G()");
            await TestAsync(text, "G.G", "G<T>.G()");
            await TestAsync(text, "G.G()", "G<T>.G()");
            await TestAsync(text, "G<T>.G", "G<T>.G()");
            await TestAsync(text, "G<t>.G()", "G<T>.G()");
            await TestAsync(text, "G<T>");
            await TestAsync(text, "G<T>()");
            await TestAsync(text, "G.G<T>");
            await TestAsync(text, ".ctor");
            await TestAsync(text, ".ctor()");
            await TestAsync(text, "C.ctor");
            await TestAsync(text, "C.ctor()");
            await TestAsync(text, "G.ctor");
            await TestAsync(text, "G<T>.ctor()");
            await TestAsync(text, "Finalize", "G<T>.~G()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public async Task TestStaticConstructors()
        {
            var text =
@"class C
{
  static C()
  {
  }
}";
            await TestAsync(text, "C", "C.C()");
            await TestAsync(text, "C.C", "C.C()");
            await TestAsync(text, "C.C()", "C.C()");
            await TestAsync(text, "C()", "C.C()");
            await TestAsync(text, "C<T>");
            await TestAsync(text, "C<T>()");
            await TestAsync(text, "C(int i)");
            await TestAsync(text, "C(int)");
            await TestAsync(text, "C(i)");
            await TestAsync(text, "C.cctor");
            await TestAsync(text, "C.cctor()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public async Task TestAllConstructors()
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
            await TestAsync(text, "C", "C.C(int)", "C.C()");
            await TestAsync(text, "C.C", "C.C(int)", "C.C()");
            await TestAsync(text, "C.C()", "C.C()");
            await TestAsync(text, "C()", "C.C()");
            await TestAsync(text, "C<T>");
            await TestAsync(text, "C<T>()");
            await TestAsync(text, "C(int i)", "C.C(int)");
            await TestAsync(text, "C(int)", "C.C(int)");
            await TestAsync(text, "C(i)", "C.C(int)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public async Task TestPartialMethods()
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
            await TestAsync(text, "M1");
            await TestAsync(text, "C.M1");
            await TestAsync(text, "M2", "C.M2()");
            await TestAsync(text, "M3", "C.M3(int)");
            await TestAsync(text, "M3()");
            await TestAsync(text, "M3(y)", "C.M3(int)");
            await TestAsync(text, "M4", "C.M4()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public async Task TestLeadingAndTrailingText()
        {
            var text =
@"class C
{
  void Foo() { };
}";
            await TestAsync(text, "Foo;", "C.Foo()");
            await TestAsync(text, "Foo();", "C.Foo()");
            await TestAsync(text, "  Foo;", "C.Foo()");
            await TestAsync(text, "  Foo;;");
            await TestAsync(text, "  Foo; ;");
            await TestAsync(text, "Foo(); ", "C.Foo()");
            await TestAsync(text, " Foo (  )  ; ", "C.Foo()");
            await TestAsync(text, "Foo(); // comment", "C.Foo()");
            await TestAsync(text, "/*comment*/Foo(/* params */); /* comment", "C.Foo()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public async Task TestEscapedKeywords()
        {
            var text =
@"struct @true { }
class @foreach
{
    void where(@true @this) { }
    void @false() { }
}";
            await TestAsync(text, "where", "@foreach.where(@true)");
            await TestAsync(text, "@where", "@foreach.where(@true)");
            await TestAsync(text, "@foreach.where", "@foreach.where(@true)");
            await TestAsync(text, "foreach.where");
            await TestAsync(text, "@foreach.where(true)");
            await TestAsync(text, "@foreach.where(@if)", "@foreach.where(@true)");
            await TestAsync(text, "false");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public async Task TestAliasQualifiedNames()
        {
            var text =
@"extern alias A
class C
{
    void Foo(D d) { }
}";
            await TestAsync(text, "A::Foo");
            await TestAsync(text, "A::Foo(A::B)");
            await TestAsync(text, "A::Foo(A::B)");
            await TestAsync(text, "A::C.Foo");
            await TestAsync(text, "C.Foo(A::Q)", "C.Foo(D)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public async Task TestNestedTypesAndNamespaces()
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

            await TestAsync(text, "Foo", "N1.N4.C.Foo(double)", "N1.N4.C.D.Foo()", "N1.N4.C.D.E.Foo()", "N1.C.Foo()");
            await TestAsync(text, "C.Foo", "N1.N4.C.Foo(double)", "N1.C.Foo()");
            await TestAsync(text, "D.Foo", "N1.N4.C.D.Foo()");
            await TestAsync(text, "N1.N4.C.D.Foo", "N1.N4.C.D.Foo()");
            await TestAsync(text, "N1.Foo");
            await TestAsync(text, "N3.C.Foo");
            await TestAsync(text, "N5.C.Foo");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
        public async Task TestInterfaces()
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

            await TestAsync(text, "Foo", "C1.Foo()");
            await TestAsync(text, "I1.Foo");
            await TestAsync(text, "C1.Foo", "C1.Foo()");
            await TestAsync(text, "C1.I1.Moo");
        }
    }
}
