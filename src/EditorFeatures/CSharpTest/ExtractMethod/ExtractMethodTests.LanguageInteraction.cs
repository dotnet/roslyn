// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ExtractMethod;

[Trait(Traits.Feature, Traits.Features.ExtractMethod)]
public partial class ExtractMethodTests
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
    public sealed class LanguageInteraction : ExtractMethodBase
    {
        #region Generics

        [Fact]
        public Task SelectTypeParameterWithConstraints()
            => TestExtractMethodAsync("""
                using System;

                class Program
                {
                    object MyMethod1<TT>() where TT : ICloneable, new()
                    {
                        [|TT abcd; abcd = new TT();|]
                        return abcd;
                    }
                }
                """, """
                using System;

                class Program
                {
                    object MyMethod1<TT>() where TT : ICloneable, new()
                    {
                        TT abcd = NewMethod<TT>();
                        return abcd;
                    }

                    private static TT NewMethod<TT>() where TT : ICloneable, new()
                    {
                        return new TT();
                    }
                }
                """);

        [Fact]
        public Task SelectTypeParameterWithAllowsRefStructAntiConstraint()
            => TestExtractMethodAsync("""
                using System;

                class Program
                {
                    void MyMethod1<TT>(TT tt) where TT : IDisposable, allows ref struct
                    {
                        [|tt.Dispose();|]
                    }
                }
                """, """
                using System;

                class Program
                {
                    void MyMethod1<TT>(TT tt) where TT : IDisposable, allows ref struct
                    {
                        NewMethod(tt);
                    }
                
                    private static void NewMethod<TT>(TT tt) where TT : IDisposable, allows ref struct
                    {
                        tt.Dispose();
                    }
                }
                """);
        [Fact]
        public Task SelectTypeParameter()
            => TestExtractMethodAsync("""
                using System;

                class Program
                {
                    public string Method<T, R>()
                    {
                        T t;
                        R r;
                        [|t = default(T);
                        r = default(R);
                        string s = "hello";|]
                        return s;
                    }
                }
                """, """
                using System;

                class Program
                {
                    public string Method<T, R>()
                    {
                        T t;
                        R r;
                        string s;
                        NewMethod(out t, out r, out s);
                        return s;
                    }

                    private static void NewMethod<T, R>(out T t, out R r, out string s)
                    {
                        t = default(T);
                        r = default(R);
                        s = "hello";
                    }
                }
                """);

        [Fact]
        public Task SelectTypeOfTypeParameter()
            => TestExtractMethodAsync("""
                using System;

                class Program
                {
                    public static Type meth<U>(U u)
                    {
                        return [|typeof(U)|];
                    }
                }
                """, """
                using System;

                class Program
                {
                    public static Type meth<U>(U u)
                    {
                        return NewMethod<U>();
                    }

                    private static Type NewMethod<U>()
                    {
                        return typeof(U);
                    }
                }
                """);

        [Fact]
        public Task SelectTypeParameterDataFlowOut()
            => TestExtractMethodAsync("""
                using System;
                using System.Collections.Generic;
                using System.Linq;

                class Program
                {

                    public class Test
                    {
                        public int i = 5;
                    }

                    public string Method<T>()
                    {
                        T t;
                        [|t = (T)new Test();
                        t.i = 10;|]
                        return t.i.ToString();
                    }
                }
                """, """
                using System;
                using System.Collections.Generic;
                using System.Linq;

                class Program
                {

                    public class Test
                    {
                        public int i = 5;
                    }

                    public string Method<T>()
                    {
                        T t;
                        t = NewMethod<T>();
                        return t.i.ToString();
                    }

                    private static T NewMethod<T>()
                    {
                        T t = (T)new Test();
                        t.i = 10;
                        return t;
                    }
                }
                """);

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528198")]
        public Task BugFix6794()
            => TestExtractMethodAsync("""
                using System;
                class Program
                {
                    static void Main(string[] args)
                    {
                        int i = 2;
                        C<int> c = new C<int>(ref [|i|]) ;
                    }

                    private class C<T>
                    {
                        private int v;
                        public C(ref int v)
                        {
                            this.v = v;
                        }
                    }
                }
                """, """
                using System;
                class Program
                {
                    static void Main(string[] args)
                    {
                        int i = 2;
                        C<int> c = GetC(ref i);
                    }

                    private static C<int> GetC(ref int i)
                    {
                        return new C<int>(ref i);
                    }

                    private class C<T>
                    {
                        private int v;
                        public C(ref int v)
                        {
                            this.v = v;
                        }
                    }
                }
                """);

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528198")]
        public Task BugFix6794_1()
            => TestExtractMethodAsync("""
                using System;
                class Program
                {
                    static void Main(string[] args)
                    {
                        int i;
                        C<int> c = new C<int>(out [|i|]) ;
                    }

                    private class C<T>
                    {
                        public C(out int v)
                        {
                            v = 1;
                        }
                    }
                }
                """, """
                using System;
                class Program
                {
                    static void Main(string[] args)
                    {
                        int i;
                        C<int> c = GetC(out i);
                    }

                    private static C<int> GetC(out int i)
                    {
                        return new C<int>(out i);
                    }

                    private class C<T>
                    {
                        public C(out int v)
                        {
                            v = 1;
                        }
                    }
                }
                """);

        [Fact]
        public Task SelectDefaultOfT()
            => TestExtractMethodAsync("""
                using System;
                using System.Collections.Generic;
                using System.Linq;

                class Test11<T>
                {
                    T method()
                    {
                        T t = [|default(T)|];
                        return t;
                    }
                }
                """, """
                using System;
                using System.Collections.Generic;
                using System.Linq;

                class Test11<T>
                {
                    T method()
                    {
                        T t = GetT();
                        return t;
                    }

                    private static T GetT()
                    {
                        return default(T);
                    }
                }
                """);

        #endregion

        #region Operators

        [Fact]
        public Task SelectPostIncrementOperatorExtractWithRef()
            => TestExtractMethodAsync("""
                class A
                {
                    int method(int i)
                    {
                        return [|i++|];
                    }
                }
                """, """
                class A
                {
                    int method(int i)
                    {
                        return NewMethod(ref i);
                    }

                    private static int NewMethod(ref int i)
                    {
                        return i++;
                    }
                }
                """);

        [Fact]
        public Task SelectPostIncrementOperator()
            => TestExtractMethodAsync("""
                class A
                {
                    int method(int i)
                    {
                        return [|i++|];
                    }
                }
                """, """
                class A
                {
                    int method(int i)
                    {
                        return NewMethod(ref i);
                    }

                    private static int NewMethod(ref int i)
                    {
                        return i++;
                    }
                }
                """);

        [Fact]
        public Task SelectPreIncrementOperator()
            => TestExtractMethodAsync("""
                class A
                {
                    int method(int i)
                    {
                        return [|++i|];
                    }
                }
                """, """
                class A
                {
                    int method(int i)
                    {
                        return NewMethod(ref i);
                    }

                    private static int NewMethod(ref int i)
                    {
                        return ++i;
                    }
                }
                """);

        [Fact]
        public Task SelectPostDecrementOperator()
            => TestExtractMethodAsync("""
                class A
                {
                    int method(int i)
                    {
                        return [|i--|];
                    }
                }
                """, """
                class A
                {
                    int method(int i)
                    {
                        return NewMethod(ref i);
                    }

                    private static int NewMethod(ref int i)
                    {
                        return i--;
                    }
                }
                """);

        [Fact]
        public Task SelectPreDecrementOperator()
            => TestExtractMethodAsync("""
                class A
                {
                    int method(int i)
                    {
                        return [|--i|];
                    }
                }
                """, """
                class A
                {
                    int method(int i)
                    {
                        return NewMethod(ref i);
                    }

                    private static int NewMethod(ref int i)
                    {
                        return --i;
                    }
                }
                """);

        #endregion

        #region ExpressionBodiedMembers

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
        public Task ExpressionBodiedMethod()
            => TestExtractMethodAsync("""
                using System;
                class T
                {
                    int m;
                    int M1() => [|1|] + 2 + 3 + m;
                }
                """, """
                using System;
                class T
                {
                    int m;
                    int M1() => NewMethod() + 2 + 3 + m;

                    private static int NewMethod()
                    {
                        return 1;
                    }
                }
                """);

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
        public Task ExpressionBodiedOperator()
            => TestExtractMethodAsync("""
                using System;
                class Complex
                {
                    int real; int imaginary;
                    public static Complex operator +(Complex a, Complex b) => a.Add([|b.real + 1|]);

                    private Complex Add(int b)
                    {
                        throw new NotImplementedException();
                    }
                }
                """, """
                using System;
                class Complex
                {
                    int real; int imaginary;
                    public static Complex operator +(Complex a, Complex b) => a.Add(NewMethod(b));

                    private static int NewMethod(Complex b)
                    {
                        return b.real + 1;
                    }

                    private Complex Add(int b)
                    {
                        throw new NotImplementedException();
                    }
                }
                """);

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
        public Task ExpressionBodiedConversionOperator()
            => TestExtractMethodAsync("""
                using System;
                public struct DBBool
                {
                    public static readonly DBBool dbFalse = new DBBool(-1);
                    int value;

                    DBBool(int value)
                    {
                        this.value = value;
                    }

                    public static implicit operator DBBool(bool x) => x ? new DBBool([|1|]) : dbFalse;
                }
                """, """
                using System;
                public struct DBBool
                {
                    public static readonly DBBool dbFalse = new DBBool(-1);
                    int value;

                    DBBool(int value)
                    {
                        this.value = value;
                    }

                    public static implicit operator DBBool(bool x) => x ? new DBBool(NewMethod()) : dbFalse;

                    private static int NewMethod()
                    {
                        return 1;
                    }
                }
                """);

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
        public Task ExpressionBodiedProperty()
            => TestExtractMethodAsync("""
                using System;
                class T
                {
                    int M1 => [|1|] + 2;
                }
                """, """
                using System;
                class T
                {
                    int M1 => NewMethod() + 2;

                    private static int NewMethod()
                    {
                        return 1;
                    }
                }
                """);

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
        public Task ExpressionBodiedIndexer()
            => TestExtractMethodAsync("""
                using System;
                class SampleCollection<T>
                {
                    private T[] arr = new T[100];
                    public T this[int i] => i > 0 ? arr[[|i + 1|]] : arr[i + 2];
                }
                """, """
                using System;
                class SampleCollection<T>
                {
                    private T[] arr = new T[100];
                    public T this[int i] => i > 0 ? arr[NewMethod(i)] : arr[i + 2];

                    private static int NewMethod(int i)
                    {
                        return i + 1;
                    }
                }
                """);

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
        public Task ExpressionBodiedIndexer2()
            => TestExtractMethodAsync("""
                using System;
                class SampleCollection<T>
                {
                    private T[] arr = new T[100];
                    public T this[int i] => [|i > 0 ? arr[i + 1]|] : arr[i + 2];
                }
                """, """
                using System;
                class SampleCollection<T>
                {
                    private T[] arr = new T[100];
                    public T this[int i] => NewMethod(i);

                    private T NewMethod(int i)
                    {
                        return i > 0 ? arr[i + 1] : arr[i + 2];
                    }
                }
                """);

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
        public Task ExpressionBodiedMethodWithBlockBodiedAnonymousMethodExpression()
            => TestExtractMethodAsync("""
                using System;
                class TestClass
                {
                    Func<int, int> Y() => delegate (int x)
                    {
                        return [|9|];
                    };
                }
                """, """
                using System;
                class TestClass
                {
                    Func<int, int> Y() => delegate (int x)
                    {
                        return NewMethod();
                    };

                    private static int NewMethod()
                    {
                        return 9;
                    }
                }
                """);

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
        public Task ExpressionBodiedMethodWithSingleLineBlockBodiedAnonymousMethodExpression()
            => TestExtractMethodAsync("""
                using System;
                class TestClass
                {
                    Func<int, int> Y() => delegate (int x) { return [|9|]; };
                }
                """, """
                using System;
                class TestClass
                {
                    Func<int, int> Y() => delegate (int x) { return NewMethod(); };

                    private static int NewMethod()
                    {
                        return 9;
                    }
                }
                """);

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
        public Task ExpressionBodiedMethodWithBlockBodiedSimpleLambdaExpression()
            => TestExtractMethodAsync("""
                using System;
                class TestClass
                {
                    Func<int, int> Y() => f =>
                    {
                        return f * [|9|];
                    };
                }
                """, """
                using System;
                class TestClass
                {
                    Func<int, int> Y() => f =>
                    {
                        return f * NewMethod();
                    };

                    private static int NewMethod()
                    {
                        return 9;
                    }
                }
                """);

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
        public Task ExpressionBodiedMethodWithExpressionBodiedSimpleLambdaExpression()
            => TestExtractMethodAsync("""
                using System;
                class TestClass
                {
                    Func<int, int> Y() => f => f * [|9|];
                }
                """, """
                using System;
                class TestClass
                {
                    Func<int, int> Y() => f => f * NewMethod();

                    private static int NewMethod()
                    {
                        return 9;
                    }
                }
                """);

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
        public Task ExpressionBodiedMethodWithBlockBodiedParenthesizedLambdaExpression()
            => TestExtractMethodAsync("""
                using System;
                class TestClass
                {
                    Func<int, int> Y() => (f) =>
                    {
                        return f * [|9|];
                    };
                }
                """, """
                using System;
                class TestClass
                {
                    Func<int, int> Y() => (f) =>
                    {
                        return f * NewMethod();
                    };

                    private static int NewMethod()
                    {
                        return 9;
                    }
                }
                """);

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
        public Task ExpressionBodiedMethodWithExpressionBodiedParenthesizedLambdaExpression()
            => TestExtractMethodAsync("""
                using System;
                class TestClass
                {
                    Func<int, int> Y() => (f) => f * [|9|];
                }
                """, """
                using System;
                class TestClass
                {
                    Func<int, int> Y() => (f) => f * NewMethod();

                    private static int NewMethod()
                    {
                        return 9;
                    }
                }
                """);

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
        public Task ExpressionBodiedMethodWithBlockBodiedAnonymousMethodExpressionInMethodArgs()
            => TestExtractMethodAsync("""
                using System;
                class TestClass
                {
                    public int Prop => Method1(delegate()
                    {
                        return [|8|];
                    });

                    private int Method1(Func<int> p)
                    {
                        throw new NotImplementedException();
                    }
                }
                """, """
                using System;
                class TestClass
                {
                    public int Prop => Method1(delegate()
                    {
                        return NewMethod();
                    });

                    private static int NewMethod()
                    {
                        return 8;
                    }

                    private int Method1(Func<int> p)
                    {
                        throw new NotImplementedException();
                    }
                }
                """);

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
        public Task LeadingAndTrailingTriviaOnExpressionBodiedMethod()
            => TestExtractMethodAsync("""
                using System;
                class TestClass
                {
                    int M1() => 1 + 2 + /*not moved*/ [|3|] /*not moved*/;

                    void Cat() { }
                }
                """, """
                using System;
                class TestClass
                {
                    int M1() => 1 + 2 + /*not moved*/ NewMethod() /*not moved*/;

                    private static int NewMethod()
                    {
                        return 3;
                    }

                    void Cat() { }
                }
                """);

        #endregion

        #region Patterns

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/9244")]
        public Task PatternIsDisabled()
            => ExpectExtractMethodToFailAsync("""
                using System;
                class Program
                {
                    static void Main()
                    {
                        object o = null;
                        if (o is Program [|p|])
                        {

                        }
                    }
                }
                """);

        #endregion

        [Fact, WorkItem(11155, "DevDiv_Projects/Roslyn")]
        public Task AnonymousTypeMember1()
            => ExpectExtractMethodToFailAsync("""
                using System;

                class Program
                {
                    static void Main(string[] args)
                    {
                        var an = new { id = 123 };
                        Console.Write(an.[|id|]); // here
                    }
                }
                """);

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544259")]
        public Task ExtractMethod_ConstructorInitializer()
            => TestExtractMethodAsync("""
                class Program
                {
                    public Program(string a, int b)
                        : this(a, [|new Program()|])
                    {
                    }
                }
                """, """
                class Program
                {
                    public Program(string a, int b)
                        : this(a, NewMethod())
                    {
                    }

                    private static Program NewMethod()
                    {
                        return new Program();
                    }
                }
                """);

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543984")]
        public Task ExtractMethod_UnsafeAddressTaken()
            => ExpectExtractMethodToFailAsync("""
                class C
                {
                    unsafe void M()
                    {
                        int i = 5;
                        int* j = [|&i|];
                    }
                }
                """, """
                class C
                {
                    unsafe void M()
                    {
                        int i = 5;
                        int* j = GetJ(out i);
                    }

                    private static unsafe int* GetJ(out int i)
                    {
                        return &i;
                    }
                }
                """);

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544387")]
        public Task ExtractMethod_PointerType()
            => TestExtractMethodAsync("""
                class Test
                {
                    static int x = 0;
                    unsafe static void Main()
                    {
                        fixed (int* p1 = &x)
                        {
                            int a1 = [|*p1|];
                        }
                    }
                }
                """, """
                class Test
                {
                    static int x = 0;
                    unsafe static void Main()
                    {
                        fixed (int* p1 = &x)
                        {
                            int a1 = GetA1(p1);
                        }
                    }

                    private static unsafe int GetA1(int* p1)
                    {
                        return *p1;
                    }
                }
                """);

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544514")]
        public Task ExtractMethod_AnonymousType()
            => ExpectExtractMethodToFailAsync("""
                public class Test
                {
                    public static void Main()
                    {
                        var p1 = new { Price = 45 };
                        var p2 = new { Price = 50 };

                        [|p1 = p2;|]
                    }
                }
                """, """
                public class Test
                {
                    public static void Main()
                    {
                        var p1 = new { Price = 45 };
                        var p2 = new { Price = 50 };

                        p1 = NewMethod(p2);
                    }

                    private static global::System.Object NewMethod(global::System.Object p2)
                    {
                        return p2;
                    }
                }
                """);

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544920")]
        public Task ExtractMethod_StackAllocExpression()
            => TestExtractMethodAsync("""
                unsafe class C
                {
                    static void Main()
                    {
                        void* p = [|stackalloc int[10]|];
                    }
                }
                """, """
                unsafe class C
                {
                    static void Main()
                    {
                        NewMethod();
                    }

                    private static void NewMethod()
                    {
                        void* p = stackalloc int[10];
                    }
                }
                """);

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539310")]
        public Task Readonly_Field_WrittenTo()
            => ExpectExtractMethodToFailAsync("""
                class C
                {
                    private readonly int i;

                    C()
                    {
                        [|i = 1;|]
                    }
                }
                """);

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539310")]
        public Task Readonly_Field()
            => TestExtractMethodAsync("""
                class C
                {
                    private readonly int i;

                    C()
                    {
                        i = 1;
                        [|var x = i;|]
                    }
                }
                """, """
                class C
                {
                    private readonly int i;

                    C()
                    {
                        i = 1;
                        NewMethod();
                    }

                    private void NewMethod()
                    {
                        var x = i;
                    }
                }
                """);

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545180")]
        public Task NodeHasSyntacticErrors()
            => ExpectExtractMethodToFailAsync("""
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Linq.Expressions;

                class Program
                {
                    static void Main(string[] args)
                    {
                        Expression<Func<int>> f3 = ()=>[|switch {|]

                        };
                    }
                }
                """);

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545292")]
        public Task LocalConst()
            => ExpectExtractMethodToFailAsync("""
                class Test
                {
                    public static void Main()
                    {
                        const int v = [|3|];
                    }
                }
                """);

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545315")]
        public Task Nullable()
            => TestExtractMethodAsync("""
                using System;
                class Program
                {
                    static void Main()
                    {
                        int? q = 10;
                        [|Console.WriteLine(q);|]
                    }
                }
                """, """
                using System;
                class Program
                {
                    static void Main()
                    {
                        int? q = 10;
                        NewMethod(q);
                    }

                    private static void NewMethod(int? q)
                    {
                        Console.WriteLine(q);
                    }
                }
                """);

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545263")]
        public Task SyntacticErrorInSelection()
            => TestExtractMethodAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        if ((true)NewMethod()[|)|]
                        {
                        }
                    }

                    private static string NewMethod()
                    {
                        return "true";
                    }
                }
                """, """
                class Program
                {
                    static void Main(string[] args)
                    {
                        NewMethod1();
                    }

                    private static void NewMethod1()
                    {
                        if ((true)NewMethod())
                        {
                        }
                    }

                    private static string NewMethod()
                    {
                        return "true";
                    }
                }
                """);

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544497")]
        public Task StackAllocExpression()
            => TestExtractMethodAsync("""
                using System;
                class Test
                {
                    unsafe static void Main()
                    {
                        void* buffer = [|stackalloc char[16]|];
                    }
                }
                """, """
                using System;
                class Test
                {
                    unsafe static void Main()
                    {
                        NewMethod();
                    }

                    private static unsafe void NewMethod()
                    {
                        void* buffer = stackalloc char[16];
                    }
                }
                """);

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545503")]
        public Task MethodBodyInScript()
            => TestExtractMethodAsync("""
                #r "System.Management"
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Text;
                using System.Management; // WMI APIs

                var output = new StringBuilder();
                void CollectInfo(string title, string query, string[,] labelKeys)
                {
                    output.AppendLine(title);
                    output.AppendLine("-----------------------------------");
                    [|var info = new ManagementObjectSearcher(query);
                    foreach (var mgtobj in info.Get())
                    {
                        for (int row = 0; row < labelKeys.GetLength(0); row++)
                        {
                            output.AppendLine(labelKeys[row, 0] + mgtobj[labelKeys[row, 1]].ToString());
                        }
                    }
                    output.AppendLine();|]
                }
                """, """
                #r "System.Management"
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Text;
                using System.Management; // WMI APIs

                var output = new StringBuilder();
                void CollectInfo(string title, string query, string[,] labelKeys)
                {
                    output.AppendLine(title);
                    output.AppendLine("-----------------------------------");
                    NewMethod(query, labelKeys);
                }

                void NewMethod(string query, string[,] labelKeys)
                {
                    var info = new ManagementObjectSearcher(query);
                    foreach (var mgtobj in info.Get())
                    {
                        for (int row = 0; row < labelKeys.GetLength(0); row++)
                        {
                            output.AppendLine(labelKeys[row, 0] + mgtobj[labelKeys[row, 1]].ToString());
                        }
                    }
                    output.AppendLine();
                }
                """, parseOptions: new CSharpParseOptions(kind: SourceCodeKind.Script));

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544920")]
        public Task NoSimplificationForStackAlloc()
            => TestExtractMethodAsync("""
                using System;

                unsafe class C
                {
                    static void Main()
                    {
                        void* p = [|stackalloc int[10]|];
                        Console.WriteLine((int)p);
                    }
                }
                """, """
                using System;

                unsafe class C
                {
                    static void Main()
                    {
                        void* p = NewMethod();
                        Console.WriteLine((int)p);
                    }

                    private static void* NewMethod()
                    {
                        void* p = stackalloc int[10];
                        return p;
                    }
                }
                """);

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545553")]
        public Task CheckStatementContext1()
            => TestExtractMethodAsync("""
                using System;

                class X
                {
                    static void Goo(Func<X, byte> x, string y) { Console.WriteLine(1); }
                    static void Goo(Func<int?, byte> x, object y) { Console.WriteLine(2); }

                    const int Value = 1000;

                    static void Main()
                    {
                        unchecked
                        {
                            [|Goo(X => (byte)X.Value, null);|] // Extract method
                        }
                    }
                }
                """, """
                using System;

                class X
                {
                    static void Goo(Func<X, byte> x, string y) { Console.WriteLine(1); }
                    static void Goo(Func<int?, byte> x, object y) { Console.WriteLine(2); }

                    const int Value = 1000;

                    static void Main()
                    {
                        unchecked
                        {
                            NewMethod(); // Extract method
                        }
                    }

                    private static void NewMethod()
                    {
                        unchecked
                        {
                            Goo(X => (byte)X.Value, null);
                        }
                    }
                }
                """);

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545553")]
        public Task CheckStatementContext2()
            => TestExtractMethodAsync("""
                using System;

                class X
                {
                    static void Goo(Func<X, byte> x, string y) { Console.WriteLine(1); }
                    static void Goo(Func<int?, byte> x, object y) { Console.WriteLine(2); }

                    const int Value = 1000;

                    static void Main()
                    {
                        unchecked
                        [|{
                            Goo(X => (byte)X.Value, null); // Extract method
                        }|]
                    }
                }
                """, """
                using System;

                class X
                {
                    static void Goo(Func<X, byte> x, string y) { Console.WriteLine(1); }
                    static void Goo(Func<int?, byte> x, object y) { Console.WriteLine(2); }

                    const int Value = 1000;

                    static void Main()
                    {
                        NewMethod();
                    }

                    private static void NewMethod()
                    {
                        unchecked
                        {
                            Goo(X => (byte)X.Value, null); // Extract method
                        }
                    }
                }
                """);

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545553")]
        public Task CheckStatementContext3()
            => TestExtractMethodAsync("""
                using System;

                class X
                {
                    static void Goo(Func<X, byte> x, string y) { Console.WriteLine(1); }
                    static void Goo(Func<int?, byte> x, object y) { Console.WriteLine(2); }

                    const int Value = 1000;

                    static void Main()
                    {
                        unchecked
                        {
                            [|{
                                Goo(X => (byte)X.Value, null); // Extract method
                            }|]
                        }
                    }
                }
                """, """
                using System;

                class X
                {
                    static void Goo(Func<X, byte> x, string y) { Console.WriteLine(1); }
                    static void Goo(Func<int?, byte> x, object y) { Console.WriteLine(2); }

                    const int Value = 1000;

                    static void Main()
                    {
                        unchecked
                        {
                            NewMethod();
                        }
                    }

                    private static void NewMethod()
                    {
                        unchecked
                        {
                            Goo(X => (byte)X.Value, null); // Extract method
                        }
                    }
                }
                """);

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545553")]
        public Task CheckExpressionContext1()
            => TestExtractMethodAsync("""
                using System;

                class X
                {
                    static int Goo(Func<X, byte> x, string y) { return 1; } // This Goo is invoked before refactoring
                    static int Goo(Func<int?, byte> x, object y) { return 2; }

                    const int Value = 1000;

                    static void Main()
                    {
                        var s = unchecked(1 + [|Goo(X => (byte)X.Value, null)|]);
                    }
                }
                """, """
                using System;

                class X
                {
                    static int Goo(Func<X, byte> x, string y) { return 1; } // This Goo is invoked before refactoring
                    static int Goo(Func<int?, byte> x, object y) { return 2; }

                    const int Value = 1000;

                    static void Main()
                    {
                        var s = unchecked(1 + NewMethod());
                    }

                    private static int NewMethod()
                    {
                        return unchecked(Goo(X => (byte)X.Value, null));
                    }
                }
                """);

        [Fact]
        public Task AwaitExpression_Normal_SingleStatement()
            => TestExtractMethodAsync("""
                using System;
                using System.Threading.Tasks;

                class X
                {
                    public async void Test()
                    {
                        [|await Task.Run(() => { });|]
                    }
                }
                """, """
                using System;
                using System.Threading.Tasks;

                class X
                {
                    public async void Test()
                    {
                        await NewMethod();
                    }

                    private static async Task NewMethod()
                    {
                        await Task.Run(() => { });
                    }
                }
                """);

        [Fact]
        public Task AwaitExpression_Normal_Expression()
            => TestExtractMethodAsync("""
                using System;
                using System.Threading.Tasks;

                class X
                {
                    public async void Test()
                    {
                        [|await Task.Run(() => { })|];
                    }
                }
                """, """
                using System;
                using System.Threading.Tasks;

                class X
                {
                    public async void Test()
                    {
                        await NewMethod();
                    }

                    private static async Task NewMethod()
                    {
                        await Task.Run(() => { });
                    }
                }
                """);

        [Fact]
        public Task AwaitExpression_Normal_MultipleStatements()
            => TestExtractMethodAsync("""
                using System;
                using System.Threading.Tasks;

                class X
                {
                    public async void Test()
                    {
                        [|await Task.Run(() => { });

                        await Task.Run(() => 1);

                        return;|]
                    }
                }
                """, """
                using System;
                using System.Threading.Tasks;

                class X
                {
                    public async void Test()
                    {
                        await NewMethod();
                    }

                    private static async Task NewMethod()
                    {
                        await Task.Run(() => { });

                        await Task.Run(() => 1);

                        return;
                    }
                }
                """);

        [Fact]
        public Task AwaitExpression_Normal_ExpressionWithReturn()
            => TestExtractMethodAsync("""
                using System;
                using System.Threading.Tasks;

                class X
                {
                    public async void Test()
                    {
                        await Task.Run(() => { });

                        [|await Task.Run(() => 1)|];
                    }
                }
                """, """
                using System;
                using System.Threading.Tasks;

                class X
                {
                    public async void Test()
                    {
                        await Task.Run(() => { });

                        await NewMethod();
                    }

                    private static async Task<int> NewMethod()
                    {
                        return await Task.Run(() => 1);
                    }
                }
                """);

        [Fact]
        public Task AwaitExpression_Normal_ExpressionInAwaitExpression()
            => TestExtractMethodAsync("""
                using System;
                using System.Threading.Tasks;

                class X
                {
                    public async void Test()
                    {
                        await [|Task.Run(() => 1)|];
                    }
                }
                """, """
                using System;
                using System.Threading.Tasks;

                class X
                {
                    public async void Test()
                    {
                        await NewMethod();
                    }

                    private static Task<int> NewMethod()
                    {
                        return Task.Run(() => 1);
                    }
                }
                """);

        [Fact]
        public Task AwaitExpression_Normal_StatementWithAwaitExpressionWithReturn()
            => TestExtractMethodAsync("""
                using System;
                using System.Threading.Tasks;

                class X
                {
                    public async void Test()
                    {
                        [|await Task.Run(() => 1);|]
                    }
                }
                """, """
                using System;
                using System.Threading.Tasks;

                class X
                {
                    public async void Test()
                    {
                        await NewMethod();
                    }

                    private static async Task NewMethod()
                    {
                        await Task.Run(() => 1);
                    }
                }
                """);

        [Fact]
        public Task AwaitExpression_Normal_AwaitWithReturnParameter()
            => TestExtractMethodAsync("""
                using System;
                using System.Threading.Tasks;

                class X
                {
                    public async void Test(int i)
                    {
                        [|await Task.Run(() => i++);|]

                        Console.WriteLine(i);
                    }
                }
                """, """
                using System;
                using System.Threading.Tasks;

                class X
                {
                    public async void Test(int i)
                    {
                        i = await NewMethod(i);

                        Console.WriteLine(i);
                    }

                    private static async Task<int> NewMethod(int i)
                    {
                        await Task.Run(() => i++);
                        return i;
                    }
                }
                """);

        [Fact]
        public Task AwaitExpression_Normal_AwaitWithReturnParameter_Error()
            => ExpectExtractMethodToFailAsync("""
                using System;
                using System.Threading.Tasks;

                class X
                {
                    public async void Test(int i)
                    {
                        var i2 = [|await Task.Run(() => i++)|];

                        Console.WriteLine(i);
                    }
                }
                """);

        [Fact]
        public Task AwaitExpression_AsyncLambda()
            => TestExtractMethodAsync("""
                using System;
                using System.Threading.Tasks;

                class X
                {
                    public void Test(Func<Task<int>> a)
                    {
                        Test([|async () => await Task.Run(() => 1)|]);
                    }
                }
                """, """
                using System;
                using System.Threading.Tasks;

                class X
                {
                    public void Test(Func<Task<int>> a)
                    {
                        Test(NewMethod());
                    }

                    private static Func<Task<int>> NewMethod()
                    {
                        return async () => await Task.Run(() => 1);
                    }
                }
                """);

        [Fact]
        public Task AwaitExpression_AsyncLambda_Body()
            => TestExtractMethodAsync("""
                using System;
                using System.Threading.Tasks;

                class X
                {
                    public void Test(Func<Task<int>> a)
                    {
                        Test(async () => [|await Task.Run(() => 1)|]);
                    }
                }
                """, """
                using System;
                using System.Threading.Tasks;

                class X
                {
                    public void Test(Func<Task<int>> a)
                    {
                        Test(async () => await NewMethod());
                    }

                    private static async Task<int> NewMethod()
                    {
                        return await Task.Run(() => 1);
                    }
                }
                """);

        [Fact]
        public Task AwaitExpression_AsyncLambda_WholeExpression()
            => TestExtractMethodAsync("""
                using System;
                using System.Threading.Tasks;

                class X
                {
                    public void Test(Func<Task<int>> a)
                    {
                        [|Test(async () => await Task.Run(() => 1));|]
                    }
                }
                """, """
                using System;
                using System.Threading.Tasks;

                class X
                {
                    public void Test(Func<Task<int>> a)
                    {
                        NewMethod();
                    }

                    private void NewMethod()
                    {
                        Test(async () => await Task.Run(() => 1));
                    }
                }
                """);

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064798")]
        public Task ExpressionInStringInterpolation()
            => TestExtractMethodAsync("""
                using System;

                class X
                {
                    public void Test()
                    {
                        var s = $"Alpha Beta {[|int.Parse("12345")|]} Gamma";
                    }
                }
                """, """
                using System;

                class X
                {
                    public void Test()
                    {
                        var s = $"Alpha Beta {NewMethod()} Gamma";
                    }

                    private static int NewMethod()
                    {
                        return int.Parse("12345");
                    }
                }
                """);

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/859493")]
        public Task ExpressionInYieldReturnStatement()
            => TestExtractMethodAsync("""
                using System;
                using System.Collections.Generic;

                public class Test<T> 
                {
                    protected class Node
                    {
                        internal Node(T item) { this._item = item; }
                        internal T _item;
                    }
                    protected Node _current = null;

                    public IEnumerator<T> GetEnumerator()
                    {
                        Node _localCurrent = _current;

                        while (true)
                        {
                            yield return [|_localCurrent._item|];
                        }
                    }
                }
                """, """
                using System;
                using System.Collections.Generic;

                public class Test<T> 
                {
                    protected class Node
                    {
                        internal Node(T item) { this._item = item; }
                        internal T _item;
                    }
                    protected Node _current = null;

                    public IEnumerator<T> GetEnumerator()
                    {
                        Node _localCurrent = _current;

                        while (true)
                        {
                            yield return GetItem(_localCurrent);
                        }
                    }

                    private static T GetItem(Node _localCurrent)
                    {
                        return _localCurrent._item;
                    }
                }
                """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/3147")]
    public async Task HandleFormattableStringTargetTyping1()
    {
        const string code = CodeSnippets.FormattableStringType + """
            namespace N
            {
                using System;

                class C
                {
                    public void M()
                    {
                        var f = FormattableString.Invariant([|$""|]);
                    }
                }
            }
            """;

        const string expected = CodeSnippets.FormattableStringType + """
            namespace N
            {
                using System;

                class C
                {
                    public void M()
                    {
                        var f = FormattableString.Invariant(NewMethod());
                    }

                    private static FormattableString NewMethod()
                    {
                        return $"";
                    }
                }
            }
            """;

        await TestExtractMethodAsync(code, expected);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17971")]
    public Task BrokenForeachLoop()
        => TestExtractMethodAsync("""
            using System;
            namespace ConsoleApp1
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        [|Console.WriteLine(1);
                        foreach ()
                        Console.WriteLine(2);|]
                    }
                }
            }
            """, """
            using System;
            namespace ConsoleApp1
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                        NewMethod();
                    }

                    private static void NewMethod()
                    {
                        Console.WriteLine(1);
                        foreach ()
                            Console.WriteLine(2);
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22150")]
    public Task ExtractMethod_LocalVariableCrossingLocalFunction()
        => TestExtractMethodAsync("""
            using System;

            class C
            {
                public void Test()
                {
                    int x = 0;
                    [|void Local() { }
                    Console.WriteLine(x);|]
                }
            }
            """, """
            using System;

            class C
            {
                public void Test()
                {
                    int x = 0;
                    NewMethod(x);
                }

                private static void NewMethod(int x)
                {
                    void Local() { }
                    Console.WriteLine(x);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61555")]
    public Task Nullable_FlowAnalysisNotNull()
        => TestExtractMethodAsync("""
            class C
            {
                public void M(C? c)
                {
                    if (c == null)
                    {
                        return;
                    }

                    [|c.ToString();|]
                }
            }
            """, """
            class C
            {
                public void M(C? c)
                {
                    if (c == null)
                    {
                        return;
                    }

                    NewMethod(c);
                }

                private static void NewMethod(C c)
                {
                    c.ToString();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39329")]
    public Task SimpleUsingStatement()
        => TestExtractMethodAsync("""
            public class Goo : IDisposable
            {
                void M2() { }
                void M3() { }
                string S => "S";

                void M()
                {
                    using Goo g = [|new Goo();
                    var s = g.S;
                    g.M2();
                    g.M3();|]
                }

                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }
            """, """
            public class Goo : IDisposable
            {
                void M2() { }
                void M3() { }
                string S => "S";
            
                void M()
                {
                    NewMethod();
                }

                private static void NewMethod()
                {
                    using Goo g = new Goo();
                    var s = g.S;
                    g.M2();
                    g.M3();
                }
            
                public void Dispose()
                {
                    throw new NotImplementedException();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24136")]
    public Task WhenClause_SwitchStatement()
        => TestExtractMethodAsync("""
            class C
            {
                void DisplayMeasurements(int a, int b)
                {
                    switch ((a, b))
                    {
                        case ( > 0, > 0) when [|a == b|]:
                            Console.WriteLine($"Both measurements are valid and equal to {a}.");
                            break;

                        case ( > 0, > 0):
                            Console.WriteLine($"First measurement is {a}, second measurement is {b}.");
                            break;

                        default:
                            Console.WriteLine("One or both measurements are not valid.");
                            break;
                    }
                }
            }
            """, """
            class C
            {
                void DisplayMeasurements(int a, int b)
                {
                    switch ((a, b))
                    {
                        case ( > 0, > 0) when NewMethod(a, b):
                            Console.WriteLine($"Both measurements are valid and equal to {a}.");
                            break;

                        case ( > 0, > 0):
                            Console.WriteLine($"First measurement is {a}, second measurement is {b}.");
                            break;

                        default:
                            Console.WriteLine("One or both measurements are not valid.");
                            break;
                    }
                }

                private static bool NewMethod(int a, int b)
                {
                    return a == b;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24136")]
    public Task WhenClause_SwitchExpression()
        => TestExtractMethodAsync("""
            class C
            {
                string GetMeasurements(int a, int b)
                    => (a, b) switch
                    {
                        ( > 0, > 0) when [|a == b|] => $"Both measurements are valid and equal to {a}.",
                        ( > 0, > 0) => $"First measurement is {a}, second measurement is {b}.",
                        _ => "One or both measurements are not valid."
                    };
            }
            """, """
            class C
            {
                string GetMeasurements(int a, int b)
                    => (a, b) switch
                    {
                        ( > 0, > 0) when NewMethod(a, b) => $"Both measurements are valid and equal to {a}.",
                        ( > 0, > 0) => $"First measurement is {a}, second measurement is {b}.",
                        _ => "One or both measurements are not valid."
                    };

                private static bool NewMethod(int a, int b)
                {
                    return a == b;
                }
            }
            """);
}
