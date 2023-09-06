// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ExtractMethod
{
    [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
    public partial class ExtractMethodTests
    {
        [UseExportProvider]
        [Trait(Traits.Feature, Traits.Features.ExtractMethod)]
        public class LanguageInteraction : ExtractMethodBase
        {
            #region Generics

            [Fact]
            public async Task SelectTypeParameterWithConstraints()
            {
                var code = """
                    using System;

                    class Program
                    {
                        object MyMethod1<TT>() where TT : ICloneable, new()
                        {
                            [|TT abcd; abcd = new TT();|]
                            return abcd;
                        }
                    }
                    """;
                var expected = """
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
                    """;

                await TestExtractMethodAsync(code, expected);
            }

            [Fact]
            public async Task SelectTypeParameter()
            {
                var code = """
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
                    """;
                var expected = """
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
                    """;

                await TestExtractMethodAsync(code, expected);
            }

            [Fact]
            public async Task SelectTypeOfTypeParameter()
            {
                var code = """
                    using System;

                    class Program
                    {
                        public static Type meth<U>(U u)
                        {
                            return [|typeof(U)|];
                        }
                    }
                    """;
                var expected = """
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
                    """;

                await TestExtractMethodAsync(code, expected);
            }

            [Fact]
            public async Task SelectTypeParameterDataFlowOut()
            {
                var code = """
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
                    """;
                var expected = """
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
                    """;

                await TestExtractMethodAsync(code, expected);
            }

            [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528198")]
            public async Task BugFix6794()
            {
                var code = """
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
                    """;
                var expected = """
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
                    """;

                await TestExtractMethodAsync(code, expected);
            }

            [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528198")]
            public async Task BugFix6794_1()
            {
                var code = """
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
                    """;
                var expected = """
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
                    """;

                await TestExtractMethodAsync(code, expected);
            }

            [Fact]
            public async Task SelectDefaultOfT()
            {
                var code = """
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
                    """;
                var expected = """
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
                    """;

                await TestExtractMethodAsync(code, expected);
            }

            #endregion

            #region Operators

            [Fact]
            public async Task SelectPostIncrementOperatorExtractWithRef()
            {
                var code = """
                    class A
                    {
                        int method(int i)
                        {
                            return [|i++|];
                        }
                    }
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact]
            public async Task SelectPostIncrementOperator()
            {
                var code = """
                    class A
                    {
                        int method(int i)
                        {
                            return [|i++|];
                        }
                    }
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact]
            public async Task SelectPreIncrementOperator()
            {
                var code = """
                    class A
                    {
                        int method(int i)
                        {
                            return [|++i|];
                        }
                    }
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact]
            public async Task SelectPostDecrementOperator()
            {
                var code = """
                    class A
                    {
                        int method(int i)
                        {
                            return [|i--|];
                        }
                    }
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact]
            public async Task SelectPreDecrementOperator()
            {
                var code = """
                    class A
                    {
                        int method(int i)
                        {
                            return [|--i|];
                        }
                    }
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            #endregion

            #region ExpressionBodiedMembers

            [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
            public async Task ExpressionBodiedMethod()
            {
                var code = """
                    using System;
                    class T
                    {
                        int m;
                        int M1() => [|1|] + 2 + 3 + m;
                    }
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
            public async Task ExpressionBodiedOperator()
            {
                var code = """
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
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
            public async Task ExpressionBodiedConversionOperator()
            {
                var code = """
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
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
            public async Task ExpressionBodiedProperty()
            {
                var code = """
                    using System;
                    class T
                    {
                        int M1 => [|1|] + 2;
                    }
                    """;
                var expected = """
                    using System;
                    class T
                    {
                        int M1 => NewMethod() + 2;

                        private static int NewMethod()
                        {
                            return 1;
                        }
                    }
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
            public async Task ExpressionBodiedIndexer()
            {
                var code = """
                    using System;
                    class SampleCollection<T>
                    {
                        private T[] arr = new T[100];
                        public T this[int i] => i > 0 ? arr[[|i + 1|]] : arr[i + 2];
                    }
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
            public async Task ExpressionBodiedIndexer2()
            {
                var code = """
                    using System;
                    class SampleCollection<T>
                    {
                        private T[] arr = new T[100];
                        public T this[int i] => [|i > 0 ? arr[i + 1]|] : arr[i + 2];
                    }
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
            public async Task ExpressionBodiedMethodWithBlockBodiedAnonymousMethodExpression()
            {
                var code = """
                    using System;
                    class TestClass
                    {
                        Func<int, int> Y() => delegate (int x)
                        {
                            return [|9|];
                        };
                    }
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
            public async Task ExpressionBodiedMethodWithSingleLineBlockBodiedAnonymousMethodExpression()
            {
                var code = """
                    using System;
                    class TestClass
                    {
                        Func<int, int> Y() => delegate (int x) { return [|9|]; };
                    }
                    """;
                var expected = """
                    using System;
                    class TestClass
                    {
                        Func<int, int> Y() => delegate (int x) { return NewMethod(); };

                        private static int NewMethod()
                        {
                            return 9;
                        }
                    }
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
            public async Task ExpressionBodiedMethodWithBlockBodiedSimpleLambdaExpression()
            {
                var code = """
                    using System;
                    class TestClass
                    {
                        Func<int, int> Y() => f =>
                        {
                            return f * [|9|];
                        };
                    }
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
            public async Task ExpressionBodiedMethodWithExpressionBodiedSimpleLambdaExpression()
            {
                var code = """
                    using System;
                    class TestClass
                    {
                        Func<int, int> Y() => f => f * [|9|];
                    }
                    """;
                var expected = """
                    using System;
                    class TestClass
                    {
                        Func<int, int> Y() => f => f * NewMethod();

                        private static int NewMethod()
                        {
                            return 9;
                        }
                    }
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
            public async Task ExpressionBodiedMethodWithBlockBodiedParenthesizedLambdaExpression()
            {
                var code = """
                    using System;
                    class TestClass
                    {
                        Func<int, int> Y() => (f) =>
                        {
                            return f * [|9|];
                        };
                    }
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
            public async Task ExpressionBodiedMethodWithExpressionBodiedParenthesizedLambdaExpression()
            {
                var code = """
                    using System;
                    class TestClass
                    {
                        Func<int, int> Y() => (f) => f * [|9|];
                    }
                    """;
                var expected = """
                    using System;
                    class TestClass
                    {
                        Func<int, int> Y() => (f) => f * NewMethod();

                        private static int NewMethod()
                        {
                            return 9;
                        }
                    }
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
            public async Task ExpressionBodiedMethodWithBlockBodiedAnonymousMethodExpressionInMethodArgs()
            {
                var code = """
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
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/528")]
            public async Task LeadingAndTrailingTriviaOnExpressionBodiedMethod()
            {
                var code = """
                    using System;
                    class TestClass
                    {
                        int M1() => 1 + 2 + /*not moved*/ [|3|] /*not moved*/;

                        void Cat() { }
                    }
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            #endregion

            #region Patterns

            [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/9244")]
            public async Task PatternIsDisabled()
            {
                var code = """
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
                    """;

                await ExpectExtractMethodToFailAsync(code);
            }

            #endregion

            [Fact, WorkItem(11155, "DevDiv_Projects/Roslyn")]
            public async Task AnonymousTypeMember1()
            {
                var code = """
                    using System;

                    class Program
                    {
                        static void Main(string[] args)
                        {
                            var an = new { id = 123 };
                            Console.Write(an.[|id|]); // here
                        }
                    }
                    """;
                await ExpectExtractMethodToFailAsync(code);
            }

            [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544259")]
            public async Task ExtractMethod_ConstructorInitializer()
            {
                var code = """
                    class Program
                    {
                        public Program(string a, int b)
                            : this(a, [|new Program()|])
                        {
                        }
                    }
                    """;
                var expected = """
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
                    """;

                await TestExtractMethodAsync(code, expected);
            }

            [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543984")]
            public async Task ExtractMethod_UnsafeAddressTaken()
            {
                var code = """
                    class C
                    {
                        unsafe void M()
                        {
                            int i = 5;
                            int* j = [|&i|];
                        }
                    }
                    """;
                var expected = """
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
                    """;

                await ExpectExtractMethodToFailAsync(code, expected);
            }

            [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544387")]
            public async Task ExtractMethod_PointerType()
            {
                var code = """
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
                    """;
                var expected = """
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
                    """;

                await TestExtractMethodAsync(code, expected);
            }

            [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544514")]
            public async Task ExtractMethod_AnonymousType()
            {
                var code = """
                    public class Test
                    {
                        public static void Main()
                        {
                            var p1 = new { Price = 45 };
                            var p2 = new { Price = 50 };

                            [|p1 = p2;|]
                        }
                    }
                    """;
                var expected = """
                    public class Test
                    {
                        public static void Main()
                        {
                            var p1 = new { Price = 45 };
                            var p2 = new { Price = 50 };

                            p1 = NewMethod(p2);
                        }

                        private static object NewMethod(object p2)
                        {
                            return p2;
                        }
                    }
                    """;

                await ExpectExtractMethodToFailAsync(code, expected);
            }

            [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544920")]
            public async Task ExtractMethod_StackAllocExpression()
            {
                var code = """
                    unsafe class C
                    {
                        static void Main()
                        {
                            void* p = [|stackalloc int[10]|];
                        }
                    }
                    """;
                var expected = """
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
                    """;

                await TestExtractMethodAsync(code, expected);
            }

            [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539310")]
            public async Task Readonly_Field_WrittenTo()
            {
                var code = """
                    class C
                    {
                        private readonly int i;

                        C()
                        {
                            [|i = 1;|]
                        }
                    }
                    """;
                await ExpectExtractMethodToFailAsync(code);
            }

            [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539310")]
            public async Task Readonly_Field()
            {
                var code = """
                    class C
                    {
                        private readonly int i;

                        C()
                        {
                            i = 1;
                            [|var x = i;|]
                        }
                    }
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545180")]
            public async Task NodeHasSyntacticErrors()
            {
                var code = """
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
                    """;
                await ExpectExtractMethodToFailAsync(code);
            }

            [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545292")]
            public async Task LocalConst()
            {
                var code = """
                    class Test
                    {
                        public static void Main()
                        {
                            const int v = [|3|];
                        }
                    }
                    """;
                await ExpectExtractMethodToFailAsync(code);
            }

            [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545315")]
            public async Task Nullable()
            {
                var code = """
                    using System;
                    class Program
                    {
                        static void Main()
                        {
                            int? q = 10;
                            [|Console.WriteLine(q);|]
                        }
                    }
                    """;
                var expected = """
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
                    """;

                await TestExtractMethodAsync(code, expected);
            }

            [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545263")]
            public async Task SyntacticErrorInSelection()
            {
                var code = """
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
                    """;
                await ExpectExtractMethodToFailAsync(code);
            }

            [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544497")]
            public async Task StackAllocExpression()
            {
                var code = """
                    using System;
                    class Test
                    {
                        unsafe static void Main()
                        {
                            void* buffer = [|stackalloc char[16]|];
                        }
                    }
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545503")]
            public async Task MethodBodyInScript()
            {
                var code = """
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
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected, parseOptions: new CSharpParseOptions(kind: SourceCodeKind.Script));
            }

            [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544920")]
            public async Task NoSimplificationForStackAlloc()
            {
                var code = """
                    using System;

                    unsafe class C
                    {
                        static void Main()
                        {
                            void* p = [|stackalloc int[10]|];
                            Console.WriteLine((int)p);
                        }
                    }
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545553")]
            public async Task CheckStatementContext1()
            {
                var code = """
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
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545553")]
            public async Task CheckStatementContext2()
            {
                var code = """
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
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545553")]
            public async Task CheckStatementContext3()
            {
                var code = """
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
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545553")]
            public async Task CheckExpressionContext1()
            {
                var code = """
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
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact]
            public async Task AwaitExpression_Normal_SingleStatement()
            {
                var code = """
                    using System;
                    using System.Threading.Tasks;

                    class X
                    {
                        public async void Test()
                        {
                            [|await Task.Run(() => { });|]
                        }
                    }
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact]
            public async Task AwaitExpression_Normal_Expression()
            {
                var code = """
                    using System;
                    using System.Threading.Tasks;

                    class X
                    {
                        public async void Test()
                        {
                            [|await Task.Run(() => { })|];
                        }
                    }
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact]
            public async Task AwaitExpression_Normal_MultipleStatements()
            {
                var code = """
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
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact]
            public async Task AwaitExpression_Normal_ExpressionWithReturn()
            {
                var code = """
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
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact]
            public async Task AwaitExpression_Normal_ExpressionInAwaitExpression()
            {
                var code = """
                    using System;
                    using System.Threading.Tasks;

                    class X
                    {
                        public async void Test()
                        {
                            await [|Task.Run(() => 1)|];
                        }
                    }
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact]
            public async Task AwaitExpression_Normal_StatementWithAwaitExpressionWithReturn()
            {
                var code = """
                    using System;
                    using System.Threading.Tasks;

                    class X
                    {
                        public async void Test()
                        {
                            [|await Task.Run(() => 1);|]
                        }
                    }
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact]
            public async Task AwaitExpression_Normal_AwaitWithReturnParameter()
            {
                var code = """
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
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact]
            public async Task AwaitExpression_Normal_AwaitWithReturnParameter_Error()
            {
                var code = """
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
                    """;
                await ExpectExtractMethodToFailAsync(code);
            }

            [Fact]
            public async Task AwaitExpression_AsyncLambda()
            {
                // this is an error case. but currently, I didn't blocked this. but we could if we want to.
                var code = """
                    using System;
                    using System.Threading.Tasks;

                    class X
                    {
                        public void Test(Func<Task<int>> a)
                        {
                            Test([|async () => await Task.Run(() => 1)|]);
                        }
                    }
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact]
            public async Task AwaitExpression_AsyncLambda_Body()
            {
                // this is an error case. but currently, I didn't blocked this. but we could if we want to.
                var code = """
                    using System;
                    using System.Threading.Tasks;

                    class X
                    {
                        public void Test(Func<Task<int>> a)
                        {
                            Test(async () => [|await Task.Run(() => 1)|]);
                        }
                    }
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact]
            public async Task AwaitExpression_AsyncLambda_WholeExpression()
            {
                // this is an error case. but currently, I didn't blocked this. but we could if we want to.
                var code = """
                    using System;
                    using System.Threading.Tasks;

                    class X
                    {
                        public void Test(Func<Task<int>> a)
                        {
                            [|Test(async () => await Task.Run(() => 1));|]
                        }
                    }
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064798")]
            public async Task ExpressionInStringInterpolation()
            {
                var code = """
                    using System;

                    class X
                    {
                        public void Test()
                        {
                            var s = $"Alpha Beta {[|int.Parse("12345")|]} Gamma";
                        }
                    }
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }

            [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/859493")]
            public async Task ExpressionInYieldReturnStatement()
            {
                var code = """
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
                    """;
                var expected = """
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
                    """;
                await TestExtractMethodAsync(code, expected);
            }
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
        public async Task BrokenForeachLoop()
        {
            var code = """
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
                """;
            var expected = """
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
                """;

            await TestExtractMethodAsync(code, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22150")]
        public async Task ExtractMethod_LocalVariableCrossingLocalFunction()
        {
            var code = """
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
                """;
            var expected = """
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
                """;

            await TestExtractMethodAsync(code, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61555")]
        public async Task Nullable_FlowAnalysisNotNull()
        {
            var code = """
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
                """;

            var expected = """
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
                """;

            await TestExtractMethodAsync(code, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39329")]
        public Task SimpleUsingStatement()
        {
            var code = """
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
                """;

            var expected = """
                public class Goo : IDisposable
                {
                    void M2() { }
                    void M3() { }
                    string S => "S";
                
                    void M()
                    {
                        using Goo g = NewMethod();
                    }

                    private static Goo NewMethod()
                    {
                        Goo g = new Goo();
                        var s = g.S;
                        g.M2();
                        g.M3();
                        return g;
                    }
                
                    public void Dispose()
                    {
                        throw new NotImplementedException();
                    }
                }
                """;

            return TestExtractMethodAsync(code, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24136")]
        public async Task WhenClause_SwitchStatement()
        {
            var code = """
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
                """;

            var expected = """
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
                """;

            await TestExtractMethodAsync(code, expected);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24136")]
        public async Task WhenClause_SwitchExpression()
        {
            var code = """
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
                """;

            var expected = """
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
                """;

            await TestExtractMethodAsync(code, expected);
        }
    }
}
