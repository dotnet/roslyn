// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.ChangeSignature;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeSignature;

[Trait(Traits.Feature, Traits.Features.ChangeSignature)]
public partial class ChangeSignatureTests : AbstractChangeSignatureTests
{
    [Fact]
    public async Task AddParameter_Delegates_ImplicitInvokeCalls()
    {
        var markup = """
            delegate void MyDelegate($$int x, string y, bool z);

            class C
            {
                void M()
                {
                    MyDelegate d1 = null;
                    d1(1, "Two", true);
                }
            }
            """;
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(2),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
            new AddedParameterOrExistingIndex(1)
        };
        var expectedUpdatedCode = """
            delegate void MyDelegate(bool z, int newIntegerParameter, string y);

            class C
            {
                void M()
                {
                    MyDelegate d1 = null;
                    d1(true, 12345, "Two");
                }
            }
            """;
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature,
            expectedUpdatedInvocationDocumentCode: expectedUpdatedCode, expectedSelectedIndex: 0);
    }

    [Fact]
    public async Task AddParameter_Delegates_ExplicitInvokeCalls()
    {
        var markup = """
            delegate void MyDelegate(int x, string $$y, bool z);

            class C
            {
                void M()
                {
                    MyDelegate d1 = null;
                    d1.Invoke(1, "Two", true);
                }
            }
            """;
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(2),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
            new AddedParameterOrExistingIndex(1)
        };
        var expectedUpdatedCode = """
            delegate void MyDelegate(bool z, int newIntegerParameter, string y);

            class C
            {
                void M()
                {
                    MyDelegate d1 = null;
                    d1.Invoke(true, 12345, "Two");
                }
            }
            """;
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature,
            expectedUpdatedInvocationDocumentCode: expectedUpdatedCode, expectedSelectedIndex: 1);
    }

    [Fact]
    public async Task AddParameter_Delegates_BeginInvokeCalls()
    {
        var markup = """
            delegate void MyDelegate(int x, string y, bool z$$);

            class C
            {
                void M()
                {
                    MyDelegate d1 = null;
                    d1.BeginInvoke(1, "Two", true, null, null);
                }
            }
            """;
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(2),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
            new AddedParameterOrExistingIndex(1)
        };
        var expectedUpdatedCode = """
            delegate void MyDelegate(bool z, int newIntegerParameter, string y);

            class C
            {
                void M()
                {
                    MyDelegate d1 = null;
                    d1.BeginInvoke(true, 12345, "Two", null, null);
                }
            }
            """;
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature,
            expectedUpdatedInvocationDocumentCode: expectedUpdatedCode, expectedSelectedIndex: 2);
    }

    [Fact]
    public async Task AddParameter_Delegates_AnonymousMethods()
    {
        var markup = """
            delegate void $$MyDelegate(int x, string y, bool z);

            class C
            {
                void M()
                {
                    MyDelegate d1 = null;
                    d1 = delegate (int e, string f, bool g) { var x = f.Length + (g ? 0 : 1); };
                    d1 = delegate { };
                }
            }
            """;
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(2),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
            new AddedParameterOrExistingIndex(1)
        };
        var expectedUpdatedCode = """
            delegate void MyDelegate(bool z, int newIntegerParameter, string y);

            class C
            {
                void M()
                {
                    MyDelegate d1 = null;
                    d1 = delegate (bool g, int newIntegerParameter, string f) { var x = f.Length + (g ? 0 : 1); };
                    d1 = delegate { };
                }
            }
            """;
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
    }

    [Fact]
    public async Task AddParameter_Delegates_Lambdas()
    {
        var markup = """
            delegate void $$MyDelegate(int x, string y, bool z);

            class C
            {
                void M()
                {
                    MyDelegate d1 = null;
                    d1 = (r, s, t) => { var x = s.Length + (t ? 0 : 1); };
                }
            }
            """;
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(2),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
            new AddedParameterOrExistingIndex(1)
        };
        var expectedUpdatedCode = """
            delegate void MyDelegate(bool z, int newIntegerParameter, string y);

            class C
            {
                void M()
                {
                    MyDelegate d1 = null;
                    d1 = (t, newIntegerParameter, s) => { var x = s.Length + (t ? 0 : 1); };
                }
            }
            """;
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
    }

    [Fact]
    public async Task AddParameter_Delegates_Lambdas_RemovingOnlyParameterIntroducesParentheses()
    {
        var markup = """
            delegate void $$MyDelegate(int x);

            class C
            {
                void M()
                {
                    MyDelegate d1 = null;
                    d1 = (r) => { System.Console.WriteLine("Test"); };
                    d1 = r => { System.Console.WriteLine("Test"); };
                    d1 = r => { System.Console.WriteLine("Test"); };
                }
            }
            """;
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
        };
        var expectedUpdatedCode = """
            delegate void MyDelegate(int newIntegerParameter);

            class C
            {
                void M()
                {
                    MyDelegate d1 = null;
                    d1 = (newIntegerParameter) => { System.Console.WriteLine("Test"); };
                    d1 = (int newIntegerParameter) => { System.Console.WriteLine("Test"); };
                    d1 = (int newIntegerParameter) => { System.Console.WriteLine("Test"); };
                }
            }
            """;
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
    }

    [Fact]
    public async Task AddParameter_Delegates_CascadeThroughMethodGroups_AssignedToVariable()
    {
        var markup = """
            delegate void $$MyDelegate(int x, string y, bool z);

            class C
            {
                void M()
                {
                    MyDelegate d1 = null;
                    d1 = Goo;
                    Goo(1, "Two", true);
                    Goo(1, false, false);
                }

                void Goo(int a, string b, bool c) { }
                void Goo(int a, object b, bool c) { }
            }
            """;
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(2),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
            new AddedParameterOrExistingIndex(1)
        };
        var expectedUpdatedCode = """
            delegate void MyDelegate(bool z, int newIntegerParameter, string y);

            class C
            {
                void M()
                {
                    MyDelegate d1 = null;
                    d1 = Goo;
                    Goo(true, 12345, "Two");
                    Goo(1, false, false);
                }

                void Goo(bool c, int newIntegerParameter, string b) { }
                void Goo(int a, object b, bool c) { }
            }
            """;
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
    }

    [Fact]
    public async Task AddParameter_Delegates_CascadeThroughMethodGroups_DelegateConstructor()
    {
        var markup = """
            delegate void $$MyDelegate(int x, string y, bool z);

            class C
            {
                void M()
                {
                    MyDelegate d1 = new MyDelegate(Goo);
                    Goo(1, "Two", true);
                    Goo(1, false, false);
                }

                void Goo(int a, string b, bool c) { }
                void Goo(int a, object b, bool c) { }
            }
            """;
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(2),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
            new AddedParameterOrExistingIndex(1)
        };
        var expectedUpdatedCode = """
            delegate void MyDelegate(bool z, int newIntegerParameter, string y);

            class C
            {
                void M()
                {
                    MyDelegate d1 = new MyDelegate(Goo);
                    Goo(true, 12345, "Two");
                    Goo(1, false, false);
                }

                void Goo(bool c, int newIntegerParameter, string b) { }
                void Goo(int a, object b, bool c) { }
            }
            """;
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
    }

    [Fact]
    public async Task AddParameter_Delegates_CascadeThroughMethodGroups_PassedAsArgument()
    {
        var markup = """
            delegate void $$MyDelegate(int x, string y, bool z);

            class C
            {
                void M()
                {
                    Target(Goo);
                    Goo(1, "Two", true);
                    Goo(1, false, false);
                }

                void Target(MyDelegate d) { }

                void Goo(int a, string b, bool c) { }
                void Goo(int a, object b, bool c) { }
            }
            """;
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(2),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
            new AddedParameterOrExistingIndex(1)
        };
        var expectedUpdatedCode = """
            delegate void MyDelegate(bool z, int newIntegerParameter, string y);

            class C
            {
                void M()
                {
                    Target(Goo);
                    Goo(true, 12345, "Two");
                    Goo(1, false, false);
                }

                void Target(MyDelegate d) { }

                void Goo(bool c, int newIntegerParameter, string b) { }
                void Goo(int a, object b, bool c) { }
            }
            """;
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
    }

    [Fact]
    public async Task AddParameter_Delegates_CascadeThroughMethodGroups_ReturnValue()
    {
        var markup = """
            delegate void $$MyDelegate(int x, string y, bool z);

            class C
            {
                void M()
                {
                    MyDelegate d1 = Result();
                    Goo(1, "Two", true);
                }

                private MyDelegate Result()
                {
                    return Goo;
                }

                void Goo(int a, string b, bool c) { }
                void Goo(int a, object b, bool c) { }
            }
            """;
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(2),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
            new AddedParameterOrExistingIndex(1)
        };
        var expectedUpdatedCode = """
            delegate void MyDelegate(bool z, int newIntegerParameter, string y);

            class C
            {
                void M()
                {
                    MyDelegate d1 = Result();
                    Goo(true, 12345, "Two");
                }

                private MyDelegate Result()
                {
                    return Goo;
                }

                void Goo(bool c, int newIntegerParameter, string b) { }
                void Goo(int a, object b, bool c) { }
            }
            """;
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
    }

    [Fact]
    public async Task AddParameter_Delegates_CascadeThroughMethodGroups_YieldReturnValue()
    {
        var markup = """
            using System.Collections.Generic;

            delegate void $$MyDelegate(int x, string y, bool z);

            class C
            {
                void M()
                {
                    Goo(1, "Two", true);
                }

                private IEnumerable<MyDelegate> Result()
                {
                    yield return Goo;
                }

                void Goo(int a, string b, bool c) { }
                void Goo(int a, object b, bool c) { }
            }
            """;
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(2),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
            new AddedParameterOrExistingIndex(1)
        };
        var expectedUpdatedCode = """
            using System.Collections.Generic;

            delegate void MyDelegate(bool z, int newIntegerParameter, string y);

            class C
            {
                void M()
                {
                    Goo(true, 12345, "Two");
                }

                private IEnumerable<MyDelegate> Result()
                {
                    yield return Goo;
                }

                void Goo(bool c, int newIntegerParameter, string b) { }
                void Goo(int a, object b, bool c) { }
            }
            """;
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
    }

    [Fact]
    public async Task AddParameter_Delegates_ReferencingLambdas_MethodArgument()
    {
        var markup = """
            delegate void $$MyDelegate(int x, string y, bool z);

            class C
            {
                void M6()
                {
                    Target((m, n, o) => { var x = n.Length + (o ? 0 : 1); });
                }

                void Target(MyDelegate d) { }
            }
            """;
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(2),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
            new AddedParameterOrExistingIndex(1)
        };
        var expectedUpdatedCode = """
            delegate void MyDelegate(bool z, int newIntegerParameter, string y);

            class C
            {
                void M6()
                {
                    Target((o, newIntegerParameter, n) => { var x = n.Length + (o ? 0 : 1); });
                }

                void Target(MyDelegate d) { }
            }
            """;
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
    }

    [Fact]
    public async Task AddParameter_Delegates_ReferencingLambdas_YieldReturn()
    {
        var markup = """
            using System.Collections.Generic;

            delegate void $$MyDelegate(int x, string y, bool z);
            class C
            {
                private IEnumerable<MyDelegate> Result3()
                {
                    yield return (g, h, i) => { var x = h.Length + (i ? 0 : 1); };
                }
            }
            """;
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(2),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
            new AddedParameterOrExistingIndex(1)
        };
        var expectedUpdatedCode = """
            using System.Collections.Generic;

            delegate void MyDelegate(bool z, int newIntegerParameter, string y);
            class C
            {
                private IEnumerable<MyDelegate> Result3()
                {
                    yield return (i, newIntegerParameter, h) => { var x = h.Length + (i ? 0 : 1); };
                }
            }
            """;
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
    }

    [Fact]
    public async Task AddParameter_Delegates_Recursive()
    {
        var markup = """
            delegate RecursiveDelegate $$RecursiveDelegate(int x, string y, bool z);

            class C
            {
                void M()
                {
                    RecursiveDelegate rd = null;
                    rd(1, "Two", true)(1, "Two", true)(1, "Two", true)(1, "Two", true)(1, "Two", true);
                }
            }
            """;
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(2),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
            new AddedParameterOrExistingIndex(1)
        };
        var expectedUpdatedCode = """
            delegate RecursiveDelegate RecursiveDelegate(bool z, int newIntegerParameter, string y);

            class C
            {
                void M()
                {
                    RecursiveDelegate rd = null;
                    rd(true, 12345, "Two")(true, 12345, "Two")(true, 12345, "Two")(true, 12345, "Two")(true, 12345, "Two");
                }
            }
            """;
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
    }

    [Fact]
    public async Task AddParameter_Delegates_DocComments()
    {
        var markup = """
            /// <summary>
            /// This is <see cref="MyDelegate"/>, which has these methods:
            ///     <see cref="MyDelegate.MyDelegate(object, IntPtr)"/>
            ///     <see cref="MyDelegate.Invoke(int, string, bool)"/>
            ///     <see cref="MyDelegate.EndInvoke(IAsyncResult)"/>
            ///     <see cref="MyDelegate.BeginInvoke(int, string, bool, AsyncCallback, object)"/>
            /// </summary>
            /// <param name="x">x!</param>
            /// <param name="y">y!</param>
            /// <param name="z">z!</param>
            delegate void $$MyDelegate(int x, string y, bool z);

            class C
            {
                void M()
                {
                    MyDelegate d1 = Goo;
                    Goo(1, "Two", true);
                }

                /// <param name="a"></param>
                /// <param name="b"></param>
                /// <param name="c"></param>
                void Goo(int a, string b, bool c) { }
            }
            """;
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(2),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
            new AddedParameterOrExistingIndex(1)
        };
        var expectedUpdatedCode = """
            /// <summary>
            /// This is <see cref="MyDelegate"/>, which has these methods:
            ///     <see cref="MyDelegate.MyDelegate(object, IntPtr)"/>
            ///     <see cref="MyDelegate.Invoke(bool, int, string)"/>
            ///     <see cref="MyDelegate.EndInvoke(IAsyncResult)"/>
            ///     <see cref="MyDelegate.BeginInvoke(int, string, bool, AsyncCallback, object)"/>
            /// </summary>
            /// <param name="z">z!</param>
            /// <param name="newIntegerParameter"></param>
            /// <param name="y">y!</param>
            delegate void MyDelegate(bool z, int newIntegerParameter, string y);

            class C
            {
                void M()
                {
                    MyDelegate d1 = Goo;
                    Goo(true, 12345, "Two");
                }

                /// <param name="c"></param>
                /// <param name="newIntegerParameter"></param>
                /// <param name="b"></param>
                void Goo(bool c, int newIntegerParameter, string b) { }
            }
            """;
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
    }

    [Fact]
    public async Task AddParameter_Delegates_CascadeThroughEventAdd()
    {
        var markup = """
            delegate void $$MyDelegate(int x, string y, bool z);

            class Program
            {
                void M()
                {
                    MyEvent += Program_MyEvent;
                }

                event MyDelegate MyEvent;
                void Program_MyEvent(int a, string b, bool c) { }
            }
            """;
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(2),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
            new AddedParameterOrExistingIndex(1)
        };
        var expectedUpdatedCode = """
            delegate void MyDelegate(bool z, int newIntegerParameter, string y);

            class Program
            {
                void M()
                {
                    MyEvent += Program_MyEvent;
                }

                event MyDelegate MyEvent;
                void Program_MyEvent(bool c, int newIntegerParameter, string b) { }
            }
            """;
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
    }

    [Fact]
    public async Task AddParameter_Delegates_Generics1()
    {
        var markup = """
            public class DP16a
            {
                public delegate void D<T>($$T t);
                public event D<int> E1;
                public event D<int> E2;

                public void M1(int i) { }
                public void M2(int i) { }
                public void M3(int i) { }

                void B()
                {
                    D<int> d = new D<int>(M1);
                    E1 += new D<int>(M2);
                    E2 -= new D<int>(M3);
                }
            }
            """;
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
        };
        var expectedUpdatedCode = """
            public class DP16a
            {
                public delegate void D<T>(int newIntegerParameter);
                public event D<int> E1;
                public event D<int> E2;

                public void M1(int newIntegerParameter) { }
                public void M2(int newIntegerParameter) { }
                public void M3(int newIntegerParameter) { }

                void B()
                {
                    D<int> d = new D<int>(M1);
                    E1 += new D<int>(M2);
                    E2 -= new D<int>(M3);
                }
            }
            """;
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
    }

    [Fact]
    public async Task AddParameter_Delegates_Generics2()
    {
        var markup = """
            public class D17<T>
            {
                public delegate void $$D(T t);
            }
            public class D17Test
            {
                void Test() { var x = new D17<string>.D(M17); }
                internal void M17(string s) { }
            }
            """;
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
        };
        var expectedUpdatedCode = """
            public class D17<T>
            {
                public delegate void D(int newIntegerParameter);
            }
            public class D17Test
            {
                void Test() { var x = new D17<string>.D(M17); }
                internal void M17(int newIntegerParameter) { }
            }
            """;
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
    }

    [Fact]
    public async Task AddParameter_Delegates_GenericParams()
    {
        var markup = """
            class DA
            {
                void M(params int[] i) { }
                void B()
                {
                    DP20<int>.D d = new DP20<int>.D(M);
                    d();
                    d(0);
                    d(0, 1);
                }
            }
            public class DP20<T>
            {
                public delegate void $$D(params T[] t);
                public void M1(params T[] t) { }

                void B()
                {
                    D d = new D(M1);
                }
            }
            """;
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
        };
        var expectedUpdatedCode = """
            class DA
            {
                void M(int newIntegerParameter) { }
                void B()
                {
                    DP20<int>.D d = new DP20<int>.D(M);
                    d(12345);
                    d(12345);
                    d(12345);
                }
            }
            public class DP20<T>
            {
                public delegate void D(int newIntegerParameter);
                public void M1(int newIntegerParameter) { }

                void B()
                {
                    D d = new D(M1);
                }
            }
            """;
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
    }

    [Fact]
    public async Task AddParameter_Delegates_Generic_RemoveArgumentAtReference()
    {
        var markup = """
            public class CD<T>
            {
                public delegate void D(T t);
            }
            class Test
            {
                public void M()
                {
                    var dele = new CD<int>.$$D((int x) => { });
                }
            }
            """;
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int")
        };
        var expectedUpdatedCode = """
            public class CD<T>
            {
                public delegate void D(int newIntegerParameter);
            }
            class Test
            {
                public void M()
                {
                    var dele = new CD<int>.D((int newIntegerParameter) => { });
                }
            }
            """;
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature,
            expectedUpdatedInvocationDocumentCode: expectedUpdatedCode, expectedSelectedIndex: 0);
    }

    [Fact]
    public async Task AddParameter_Delegate_Generics_RemoveStaticArgument()
    {
        var markup = """
            public class C2<T>
            {
                public delegate void D(T t);
            }

            public class D2
            {
                public static D2 Instance = null;
                void M(D2 m) { }

                void B()
                {
                    C2<D2>.D d = new C2<D2>.D(M);
                    $$d(D2.Instance);
                }
            }
            """;
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int")
        };
        var expectedUpdatedCode = """
            public class C2<T>
            {
                public delegate void D(int newIntegerParameter);
            }

            public class D2
            {
                public static D2 Instance = null;
                void M(int newIntegerParameter) { }

                void B()
                {
                    C2<D2>.D d = new C2<D2>.D(M);
                    d(12345);
                }
            }
            """;
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
    }

    [Fact]
    public async Task TestAddParameter_Delegates_Relaxation_ParameterlessFunctionToFunction()
    {
        var markup = """
            class C0
            {
                delegate int $$MyFunc(int x, string y, bool z);

                class C
                {
                    public void M()
                    {
                        MyFunc f = Test();
                    }

                    private MyFunc Test()
                    {
                        return null;
                    }
                }
            }
            """;
        var updatedSignature = new[] {
            new AddedParameterOrExistingIndex(2),
            new AddedParameterOrExistingIndex(new AddedParameter(null, "int", "newIntegerParameter", CallSiteKind.Value, "12345"), "int"),
            new AddedParameterOrExistingIndex(1)
        };
        var expectedUpdatedCode = """
            class C0
            {
                delegate int MyFunc(bool z, int newIntegerParameter, string y);

                class C
                {
                    public void M()
                    {
                        MyFunc f = Test();
                    }

                    private MyFunc Test()
                    {
                        return null;
                    }
                }
            }
            """;
        await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: expectedUpdatedCode);
    }
}
