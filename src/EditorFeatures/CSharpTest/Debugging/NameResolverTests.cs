// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Debugging;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Debugging;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.DebuggingNameResolver)]
public sealed class NameResolverTests
{
    private static async Task TestAsync(string text, string searchText, params string[] expectedNames)
    {
        using var workspace = EditorTestWorkspace.CreateCSharp(text);

        var nameResolver = new BreakpointResolver(workspace.CurrentSolution, searchText);
        var results = await nameResolver.DoAsync(CancellationToken.None);

        Assert.Equal(expectedNames, results.Select(r => r.LocationNameOpt));
    }

    [Fact]
    public async Task TestCSharpLanguageDebugInfoCreateNameResolver()
    {
        using var workspace = EditorTestWorkspace.CreateCSharp(" ");

        var debugInfo = new CSharpBreakpointResolutionService();
        var results = await debugInfo.ResolveBreakpointsAsync(workspace.CurrentSolution, "goo", CancellationToken.None);
        Assert.Equal(0, results.Count());
    }

    [Fact]
    public async Task TestSimpleNameInClass()
    {
        var text =
            """
            class C
            {
              void Goo()
              {
              }
            }
            """;
        await TestAsync(text, "Goo", "C.Goo()");
        await TestAsync(text, "goo");
        await TestAsync(text, "C.Goo", "C.Goo()");
        await TestAsync(text, "N.C.Goo");
        await TestAsync(text, "Goo<T>");
        await TestAsync(text, "C<T>.Goo");
        await TestAsync(text, "Goo()", "C.Goo()");
        await TestAsync(text, "Goo(int i)");
        await TestAsync(text, "Goo(int)");
    }

    [Fact]
    public async Task TestSimpleNameInNamespace()
    {
        var text =
            """
            namespace N
            {
              class C
              {
                void Goo()
                {
                }
              }
            }
            """;
        await TestAsync(text, "Goo", "N.C.Goo()");
        await TestAsync(text, "goo");
        await TestAsync(text, "C.Goo", "N.C.Goo()");
        await TestAsync(text, "N.C.Goo", "N.C.Goo()");
        await TestAsync(text, "Goo<T>");
        await TestAsync(text, "C<T>.Goo");
        await TestAsync(text, "Goo()", "N.C.Goo()");
        await TestAsync(text, "C.Goo()", "N.C.Goo()");
        await TestAsync(text, "N.C.Goo()", "N.C.Goo()");
        await TestAsync(text, "Goo(int i)");
        await TestAsync(text, "Goo(int)");
        await TestAsync(text, "Goo(a)");
    }

    [Fact]
    public async Task TestSimpleNameInGenericClassNamespace()
    {
        var text =
            """
            namespace N
            {
              class C<T>
              {
                void Goo()
                {
                }
              }
            }
            """;
        await TestAsync(text, "Goo", "N.C<T>.Goo()");
        await TestAsync(text, "goo");
        await TestAsync(text, "C.Goo", "N.C<T>.Goo()");
        await TestAsync(text, "N.C.Goo", "N.C<T>.Goo()");
        await TestAsync(text, "Goo<T>");
        await TestAsync(text, "C<T>.Goo", "N.C<T>.Goo()");
        await TestAsync(text, "C<T>.Goo()", "N.C<T>.Goo()");
        await TestAsync(text, "Goo()", "N.C<T>.Goo()");
        await TestAsync(text, "C.Goo()", "N.C<T>.Goo()");
        await TestAsync(text, "N.C.Goo()", "N.C<T>.Goo()");
        await TestAsync(text, "Goo(int i)");
        await TestAsync(text, "Goo(int)");
        await TestAsync(text, "Goo(a)");
    }

    [Fact]
    public async Task TestGenericNameInClassNamespace()
    {
        var text =
            """
            namespace N
            {
              class C
              {
                void Goo<T>()
                {
                }
              }
            }
            """;
        await TestAsync(text, "Goo", "N.C.Goo<T>()");
        await TestAsync(text, "goo");
        await TestAsync(text, "C.Goo", "N.C.Goo<T>()");
        await TestAsync(text, "N.C.Goo", "N.C.Goo<T>()");
        await TestAsync(text, "Goo<T>", "N.C.Goo<T>()");
        await TestAsync(text, "Goo<X>", "N.C.Goo<T>()");
        await TestAsync(text, "Goo<T,X>");
        await TestAsync(text, "C<T>.Goo");
        await TestAsync(text, "C<T>.Goo()");
        await TestAsync(text, "Goo()", "N.C.Goo<T>()");
        await TestAsync(text, "C.Goo()", "N.C.Goo<T>()");
        await TestAsync(text, "N.C.Goo()", "N.C.Goo<T>()");
        await TestAsync(text, "Goo(int i)");
        await TestAsync(text, "Goo(int)");
        await TestAsync(text, "Goo(a)");
        await TestAsync(text, "Goo<T>(int i)");
        await TestAsync(text, "Goo<T>(int)");
        await TestAsync(text, "Goo<T>(a)");
    }

    [Fact]
    public async Task TestOverloadsInSingleClass()
    {
        var text =
            """
            class C
            {
              void Goo()
              {
              }

              void Goo(int i)
              {
              }
            }
            """;
        await TestAsync(text, "Goo", "C.Goo()", "C.Goo(int)");
        await TestAsync(text, "goo");
        await TestAsync(text, "C.Goo", "C.Goo()", "C.Goo(int)");
        await TestAsync(text, "N.C.Goo");
        await TestAsync(text, "Goo<T>");
        await TestAsync(text, "C<T>.Goo");
        await TestAsync(text, "Goo()", "C.Goo()");
        await TestAsync(text, "Goo(int i)", "C.Goo(int)");
        await TestAsync(text, "Goo(int)", "C.Goo(int)");
        await TestAsync(text, "Goo(i)", "C.Goo(int)");
    }

    [Fact]
    public async Task TestMethodsInMultipleClasses()
    {
        var text =
            """
            namespace N
            {
              class C
              {
                void Goo()
                {
                }
              }
            }

            namespace N1
            {
              class C
              {
                void Goo(int i)
                {
                }
              }
            }
            """;
        await TestAsync(text, "Goo", "N1.C.Goo(int)", "N.C.Goo()");
        await TestAsync(text, "goo");
        await TestAsync(text, "C.Goo", "N1.C.Goo(int)", "N.C.Goo()");
        await TestAsync(text, "N.C.Goo", "N.C.Goo()");
        await TestAsync(text, "N1.C.Goo", "N1.C.Goo(int)");
        await TestAsync(text, "Goo<T>");
        await TestAsync(text, "C<T>.Goo");
        await TestAsync(text, "Goo()", "N.C.Goo()");
        await TestAsync(text, "Goo(int i)", "N1.C.Goo(int)");
        await TestAsync(text, "Goo(int)", "N1.C.Goo(int)");
        await TestAsync(text, "Goo(i)", "N1.C.Goo(int)");
    }

    [Fact]
    public async Task TestMethodsWithDifferentArityInMultipleClasses()
    {
        var text =
            """
            namespace N
            {
              class C
              {
                void Goo()
                {
                }
              }
            }

            namespace N1
            {
              class C
              {
                void Goo<T>(int i)
                {
                }
              }
            }
            """;
        await TestAsync(text, "Goo", "N1.C.Goo<T>(int)", "N.C.Goo()");
        await TestAsync(text, "goo");
        await TestAsync(text, "C.Goo", "N1.C.Goo<T>(int)", "N.C.Goo()");
        await TestAsync(text, "N.C.Goo", "N.C.Goo()");
        await TestAsync(text, "N1.C.Goo", "N1.C.Goo<T>(int)");
        await TestAsync(text, "Goo<T>", "N1.C.Goo<T>(int)");
        await TestAsync(text, "C<T>.Goo");
        await TestAsync(text, "Goo()", "N.C.Goo()");
        await TestAsync(text, "Goo<T>()");
        await TestAsync(text, "Goo(int i)", "N1.C.Goo<T>(int)");
        await TestAsync(text, "Goo(int)", "N1.C.Goo<T>(int)");
        await TestAsync(text, "Goo(i)", "N1.C.Goo<T>(int)");
        await TestAsync(text, "Goo<T>(int i)", "N1.C.Goo<T>(int)");
        await TestAsync(text, "Goo<T>(int)", "N1.C.Goo<T>(int)");
        await TestAsync(text, "Goo<T>(i)", "N1.C.Goo<T>(int)");
    }

    [Fact]
    public async Task TestOverloadsWithMultipleParametersInSingleClass()
    {
        var text =
            """
            class C
            {
              void Goo(int a)
              {
              }

              void Goo(int a, string b = "bb")
              {
              }

              void Goo(__arglist)
              {
              }
            }
            """;
        await TestAsync(text, "Goo", "C.Goo(int)", "C.Goo(int, [string])", "C.Goo(__arglist)");
        await TestAsync(text, "goo");
        await TestAsync(text, "C.Goo", "C.Goo(int)", "C.Goo(int, [string])", "C.Goo(__arglist)");
        await TestAsync(text, "N.C.Goo");
        await TestAsync(text, "Goo<T>");
        await TestAsync(text, "C<T>.Goo");
        await TestAsync(text, "Goo()", "C.Goo(__arglist)");
        await TestAsync(text, "Goo(int i)", "C.Goo(int)");
        await TestAsync(text, "Goo(int)", "C.Goo(int)");
        await TestAsync(text, "Goo(int x = 42)", "C.Goo(int)");
        await TestAsync(text, "Goo(i)", "C.Goo(int)");
        await TestAsync(text, "Goo(int i, int b)", "C.Goo(int, [string])");
        await TestAsync(text, "Goo(int, bool)", "C.Goo(int, [string])");
        await TestAsync(text, "Goo(i, s)", "C.Goo(int, [string])");
        await TestAsync(text, "Goo(,)", "C.Goo(int, [string])");
        await TestAsync(text, "Goo(int x = 42,)", "C.Goo(int, [string])");
        await TestAsync(text, "Goo(int x = 42, y = 42)", "C.Goo(int, [string])");
        await TestAsync(text, "Goo([attr] x = 42, y = 42)", "C.Goo(int, [string])");
        await TestAsync(text, "Goo(int i, int b, char c)");
        await TestAsync(text, "Goo(int, bool, char)");
        await TestAsync(text, "Goo(i, s, c)");
        await TestAsync(text, "Goo(__arglist)", "C.Goo(int)");
    }

    [Fact]
    public async Task AccessorTests()
    {
        var text =
            """
            class C
            {
              int Property1 { get { return 42; } }
              int Property2 { set { } }
              int Property3 { get; set;}
            }
            """;
        await TestAsync(text, "Property1", "C.Property1");
        await TestAsync(text, "Property2", "C.Property2");
        await TestAsync(text, "Property3", "C.Property3");
    }

    [Fact]
    public async Task NegativeTests()
    {
        var text =
            """
            using System.Runtime.CompilerServices;
            abstract class C
            {
                public abstract void AbstractMethod(int a);
                int Field;
                delegate void Delegate();
                event Delegate Event;
                [IndexerName("ABCD")]
                int this[int i] { get { return i; } }
                void Goo() { }
                void Goo(int x = 1, int y = 2) { }
                ~C() { }
            }
            """;
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
        await TestAsync(text, "Goo", "C.Goo()", "C.Goo([int], [int])"); // just making sure it would normally resolve before trying bad syntax
        await TestAsync(text, "Goo Goo");
        await TestAsync(text, "Goo()asdf");
        await TestAsync(text, "Goo(),");
        await TestAsync(text, "Goo(),f");
        await TestAsync(text, "Goo().Goo");
        await TestAsync(text, "Goo(");
        await TestAsync(text, "(Goo");
        await TestAsync(text, "Goo)");
        await TestAsync(text, "(Goo)");
        await TestAsync(text, "Goo(x = 42, y = 42)", "C.Goo([int], [int])"); // just making sure it would normally resolve before trying bad syntax
        await TestAsync(text, "int x = 42");
        await TestAsync(text, "Goo(int x = 42, y = 42");
        await TestAsync(text, "C");
        await TestAsync(text, "C.C");
        await TestAsync(text, "~");
        await TestAsync(text, "~C");
        await TestAsync(text, "C.~C()");
        await TestAsync(text, "");
    }

    [Fact]
    public async Task TestInstanceConstructors()
    {
        var text =
            """
            class C
            {
              public C() { }
            }

            class G<T>
            {
              public G() { }
              ~G() { }
            }
            """;
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

    [Fact]
    public async Task TestStaticConstructors()
    {
        var text =
            """
            class C
            {
              static C()
              {
              }
            }
            """;
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

    [Fact]
    public async Task TestAllConstructors()
    {
        var text =
            """
            class C
            {
              static C()
              {
              }

              public C(int i)
              {
              }
            }
            """;
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

    [Fact]
    public async Task TestPartialMethods()
    {
        var text =
            """
            partial class C
            {
              partial int M1();

              partial void M2() { }

              partial void M2();

              partial int M3();

              partial int M3(int x) { return 0; }

              partial void M4() { }
            }
            """;
        await TestAsync(text, "M1");
        await TestAsync(text, "C.M1");
        await TestAsync(text, "M2", "C.M2()");
        await TestAsync(text, "M3", "C.M3(int)");
        await TestAsync(text, "M3()");
        await TestAsync(text, "M3(y)", "C.M3(int)");
        await TestAsync(text, "M4", "C.M4()");
    }

    [Fact]
    public async Task TestLeadingAndTrailingText()
    {
        var text =
            """
            class C
            {
              void Goo() { };
            }
            """;
        await TestAsync(text, "Goo;", "C.Goo()");
        await TestAsync(text,
@"Goo();", "C.Goo()");
        await TestAsync(text, "  Goo;", "C.Goo()");
        await TestAsync(text, "  Goo;;");
        await TestAsync(text, "  Goo; ;");
        await TestAsync(text,
@"Goo();", "C.Goo()");
        await TestAsync(text,
@"Goo();", "C.Goo()");
        await TestAsync(text,
@"Goo(); // comment", "C.Goo()");
        await TestAsync(text,
            """
            /*comment*/
                       Goo(/* params */); /* comment
            """, "C.Goo()");
    }

    [Fact]
    public async Task TestEscapedKeywords()
    {
        var text =
            """
            struct @true { }
            class @foreach
            {
                void where(@true @this) { }
                void @false() { }
            }
            """;
        await TestAsync(text, "where", "@foreach.where(@true)");
        await TestAsync(text, "@where", "@foreach.where(@true)");
        await TestAsync(text, "@foreach.where", "@foreach.where(@true)");
        await TestAsync(text, "foreach.where");
        await TestAsync(text, "@foreach.where(true)");
        await TestAsync(text, "@foreach.where(@if)", "@foreach.where(@true)");
        await TestAsync(text, "false");
    }

    [Fact]
    public async Task TestAliasQualifiedNames()
    {
        var text =
            """
            extern alias A
            class C
            {
                void Goo(D d) { }
            }
            """;
        await TestAsync(text, "A::Goo");
        await TestAsync(text, "A::Goo(A::B)");
        await TestAsync(text, "A::Goo(A::B)");
        await TestAsync(text, "A::C.Goo");
        await TestAsync(text, "C.Goo(A::Q)", "C.Goo(D)");
    }

    [Fact]
    public async Task TestNestedTypesAndNamespaces()
    {
        var text =
            """
            namespace N1
            {
              class C
              {
                void Goo() { }
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
                  void Goo(double x) { }

                  class D
                  {
                    void Goo() { }

                    class E
                    {
                      void Goo() { }
                    }
                  }
                }
              }
              namespace N5 { }
            }
            """;

        await TestAsync(text, "Goo", "N1.N4.C.Goo(double)", "N1.N4.C.D.Goo()", "N1.N4.C.D.E.Goo()", "N1.C.Goo()");
        await TestAsync(text, "C.Goo", "N1.N4.C.Goo(double)", "N1.C.Goo()");
        await TestAsync(text, "D.Goo", "N1.N4.C.D.Goo()");
        await TestAsync(text, "N1.N4.C.D.Goo", "N1.N4.C.D.Goo()");
        await TestAsync(text, "N1.Goo");
        await TestAsync(text, "N3.C.Goo");
        await TestAsync(text, "N5.C.Goo");
    }

    [Fact]
    public async Task TestInterfaces()
    {
        var text =
            """
            interface I1
            {
              void Goo();
            }
            class C1 : I1
            {
              void I1.Goo() { }
            }
            """;

        await TestAsync(text, "Goo", "C1.Goo()");
        await TestAsync(text, "I1.Goo");
        await TestAsync(text, "C1.Goo", "C1.Goo()");
        await TestAsync(text, "C1.I1.Moo");
    }
}
