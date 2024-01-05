// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Analyzers.ForEachCast;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.ForEachCast;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ForEachCast
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpForEachCastDiagnosticAnalyzer,
        CSharpForEachCastCodeFixProvider>;

    public class ForEachCastTests
    {
        private static async Task TestWorkerAsync(
            string testCode, string fixedCode, string optionValue)
        {
            await new VerifyCS.Test
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                EditorConfig = """
                [*]
                dotnet_style_prefer_foreach_explicit_cast_in_source=
                """ + optionValue,
            }.RunAsync();
        }

        private static Task TestAlwaysAsync(string markup, string alwaysMarkup)
            => TestWorkerAsync(markup, alwaysMarkup, "always");

        private static Task TestWhenStronglyTypedAsync(string markup, string nonLegacyMarkup)
            => TestWorkerAsync(markup, nonLegacyMarkup, "when_strongly_typed");

        [Fact]
        public async Task NonGenericIComparableCollection()
        {
            var test = """
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            [|foreach|] (string item in new A())
                            {
                            }
                        }
                    }
                    struct A
                    {
                        public Enumerator GetEnumerator() =>  new Enumerator();
                        public struct Enumerator
                        {
                            public System.IComparable Current => 42;
                            public bool MoveNext() => true;
                        }
                    }
                }
                """;

            await TestAlwaysAsync(test, test);
            await TestWhenStronglyTypedAsync(test, test);
        }

        [Fact]
        public async Task GenericObjectCollection()
        {
            var test = """
                using System.Collections.Generic;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            var x = new List<object>();
                            [|foreach|] (string item in x)
                            {
                            }
                        }
                    }
                }
                """;
            var fixedCode = """
                using System.Collections.Generic;
                using System.Linq;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            var x = new List<object>();
                            foreach (string item in x.Cast<string>())
                            {
                            }
                        }
                    }
                }
                """;

            await TestAlwaysAsync(test, fixedCode);
            await TestWhenStronglyTypedAsync(test, fixedCode);
        }

        [Fact]
        public async Task ObjectArray()
        {
            var test = """
                using System.Collections.Generic;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main(object[] x)
                        {
                            [|foreach|] (string item in x)
                            {
                            }
                        }
                    }
                }
                """;
            var fixedCode = """
                using System.Collections.Generic;
                using System.Linq;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main(object[] x)
                        {
                            foreach (string item in x.Cast<string>())
                            {
                            }
                        }
                    }
                }
                """;

            await TestAlwaysAsync(test, fixedCode);
            await TestWhenStronglyTypedAsync(test, fixedCode);
        }

        [Fact]
        public async Task IComparableArrayCollection()
        {
            var test = """
                using System;
                using System.Collections.Generic;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main(IComparable[] x)
                        {
                            [|foreach|] (string item in x)
                            {
                            }
                        }
                    }
                }
                """;
            var fixedCode = """
                using System;
                using System.Collections.Generic;
                using System.Linq;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main(IComparable[] x)
                        {
                            foreach (string item in x.Cast<string>())
                            {
                            }
                        }
                    }
                }
                """;

            await TestAlwaysAsync(test, fixedCode);
            await TestWhenStronglyTypedAsync(test, fixedCode);
        }

        [Fact]
        public async Task IEnumerableOfObjectCollection()
        {
            var test = """
                using System.Collections.Generic;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main(IEnumerable<object> x)
                        {
                            [|foreach|] (string item in x)
                            {
                            }
                        }
                    }
                }
                """;
            var fixedCode = """
                using System.Collections.Generic;
                using System.Linq;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main(IEnumerable<object> x)
                        {
                            foreach (string item in x.Cast<string>())
                            {
                            }
                        }
                    }
                }
                """;

            await TestAlwaysAsync(test, fixedCode);
            await TestWhenStronglyTypedAsync(test, fixedCode);
        }

        [Fact]
        public async Task IListOfObjectCollection()
        {
            var test = """
                using System.Collections.Generic;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main(IList<object> x)
                        {
                            [|foreach|] (string item in x)
                            {
                            }
                        }
                    }
                }
                """;
            var fixedCode = """
                using System.Collections.Generic;
                using System.Linq;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main(IList<object> x)
                        {
                            foreach (string item in x.Cast<string>())
                            {
                            }
                        }
                    }
                }
                """;

            await TestAlwaysAsync(test, fixedCode);
            await TestWhenStronglyTypedAsync(test, fixedCode);
        }

        [Fact]
        public async Task NonGenericObjectCollection_Always()
        {
            var test = """
                using System.Collections;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            var x = new ArrayList();
                            [|foreach|] (string item in x)
                            {
                            }
                        }
                    }
                }
                """;
            var fixedCode = """
                using System.Collections;
                using System.Linq;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            var x = new ArrayList();
                            foreach (string item in x.Cast<string>())
                            {
                            }
                        }
                    }
                }
                """;

            await TestAlwaysAsync(test, fixedCode);
        }

        [Fact]
        public async Task NonGenericObjectCollection_NonLegacy()
        {
            var test = """
                using System.Collections;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            var x = new ArrayList();
                            foreach (string item in x)
                            {
                            }
                        }
                    }
                }
                """;

            await TestWhenStronglyTypedAsync(test, test);
        }

        [Fact]
        public async Task SameType()
        {
            var test = """
                using System.Collections.Generic;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            var x = new List<string>();
                            foreach (string item in x)
                            {
                            }
                        }
                    }
                }
                """;

            await TestAlwaysAsync(test, test);
            await TestWhenStronglyTypedAsync(test, test);
        }

        [Fact]
        public async Task CastBaseToChild()
        {
            var test = """
                using System.Collections.Generic;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            var x = new List<A>();
                            [|foreach|] (B item in x)
                            {
                            }
                        }
                    }
                    class A { }
                    class B : A { }
                }
                """;
            var fixedCode = """
                using System.Collections.Generic;
                using System.Linq;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            var x = new List<A>();
                            foreach (B item in x.Cast<B>())
                            {
                            }
                        }
                    }
                    class A { }
                    class B : A { }
                }
                """;

            await TestAlwaysAsync(test, fixedCode);
            await TestWhenStronglyTypedAsync(test, fixedCode);
        }

        [Fact]
        public async Task ImplicitConversion()
        {
            var test = """
                using System.Collections.Generic;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            var x = new List<int>();
                            foreach (long item in x)
                            {
                            }
                        }
                    }
                }
                """;

            await TestAlwaysAsync(test, test);
            await TestWhenStronglyTypedAsync(test, test);
        }

        [Fact]
        public async Task UserDefinedImplicitConversion()
        {
            var test = """
                using System.Collections.Generic;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            var x = new List<A>();
                            foreach (B item in x)
                            {
                            }
                        }
                    }
                    class A { }
                    class B 
                    { 
                        public static implicit operator B(A a) => new B();
                    }
                }
                """;

            await TestAlwaysAsync(test, test);
            await TestWhenStronglyTypedAsync(test, test);
        }

        [Fact]
        public async Task ExplicitConversion()
        {
            var test = """
                using System.Collections.Generic;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            var x = new List<long>();
                            [|foreach|] (int item in x)
                            {
                            }
                        }
                    }
                }
                """;
            var fixedCode = """
                using System.Collections.Generic;
                using System.Linq;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            var x = new List<long>();
                            foreach (int item in x.Select(v => (int)v))
                            {
                            }
                        }
                    }
                }
                """;
            await TestAlwaysAsync(test, fixedCode);
            await TestWhenStronglyTypedAsync(test, fixedCode);
        }

        [Fact]
        public async Task UserDefinedExplicitConversion()
        {
            var test = """
                using System.Collections.Generic;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            var x = new List<A>();
                            [|foreach|] (B item in x)
                            {
                            }
                        }
                    }
                    class A { }
                    class B 
                    { 
                        public static explicit operator B(A a) => new B();
                    }
                }
                """;
            var fixedCode = """
                using System.Collections.Generic;
                using System.Linq;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            var x = new List<A>();
                            foreach (B item in x.Select(v => (B)v))
                            {
                            }
                        }
                    }
                    class A { }
                    class B 
                    { 
                        public static explicit operator B(A a) => new B();
                    }
                }
                """;

            await TestAlwaysAsync(test, fixedCode);
            await TestWhenStronglyTypedAsync(test, fixedCode);
        }

        [Fact]
        public async Task CastChildToBase()
        {
            var test = """
                using System.Collections.Generic;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            var x = new List<B>();
                            foreach (A item in x)
                            {
                            }
                        }
                    }
                    class A { }
                    class B : A { }
                }
                """;

            await TestAlwaysAsync(test, test);
            await TestWhenStronglyTypedAsync(test, test);
        }

        [Fact]
        public async Task InterfaceToClass()
        {
            var test = """
                using System;
                using System.Collections.Generic;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            var x = new List<IComparable>();
                            [|foreach|] (string s in x)
                            {
                            }
                        }
                    }
                }
                """;

            var fixedCode = """
                using System;
                using System.Collections.Generic;
                using System.Linq;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            var x = new List<IComparable>();
                            foreach (string s in x.Cast<string>())
                            {
                            }
                        }
                    }
                }
                """;
            await TestAlwaysAsync(test, fixedCode);
            await TestWhenStronglyTypedAsync(test, fixedCode);
        }

        [Fact]
        public async Task ClassToImplementedInterfase()
        {
            var test = """
                using System;
                using System.Collections.Generic;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            var x = new List<string>();
                            foreach (IComparable s in x)
                            {
                            }
                        }
                    }
                }
                """;

            await TestAlwaysAsync(test, test);
            await TestWhenStronglyTypedAsync(test, test);
        }

        [Fact]
        public async Task GenericTypes_Unrelated()
        {
            var test = """
                using System.Collections.Generic;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main<A, B>()
                        {
                            var x = new List<A>();
                            {|CS0030:foreach|} (B s in x)
                            {
                            }
                        }
                    }
                }
                """;

            await TestAlwaysAsync(test, test);
            await TestWhenStronglyTypedAsync(test, test);
        }

        [Fact]
        public async Task GenericTypes_Valid_Relationship()
        {
            var test = """
                using System.Collections.Generic;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main<A, B>() where A : B
                        {
                            var x = new List<A>();
                            foreach (B s in x)
                            {
                            }
                        }
                    }
                }
                """;

            await TestAlwaysAsync(test, test);
            await TestWhenStronglyTypedAsync(test, test);
        }

        [Fact]
        public async Task GenericTypes_Invalid_Relationship()
        {
            var test = """
                using System.Collections.Generic;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main<A, B>() where B : A
                        {
                            var x = new List<A>();
                            [|foreach|] (B s in x)
                            {
                            }
                        }
                    }
                }
                """;

            var fixedCode = """
                using System.Collections.Generic;
                using System.Linq;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main<A, B>() where B : A
                        {
                            var x = new List<A>();
                            foreach (B s in x.Select(v => (B)v))
                            {
                            }
                        }
                    }
                }
                """;

            await TestAlwaysAsync(test, fixedCode);
            await TestWhenStronglyTypedAsync(test, fixedCode);
        }

        [Fact]
        public async Task GenericTypes_Invalid_Relationship_ClassConstraint()
        {
            var test = """
                using System.Collections.Generic;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main<A, B>()
                            where A : class
                            where B : class, A
                        {
                            var x = new List<A>();
                            [|foreach|] (B s in x)
                            {
                            }
                        }
                    }
                }
                """;
            var fixedCode = """
                using System.Collections.Generic;
                using System.Linq;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main<A, B>()
                            where A : class
                            where B : class, A
                        {
                            var x = new List<A>();
                            foreach (B s in x.Cast<B>())
                            {
                            }
                        }
                    }
                }
                """;

            await TestAlwaysAsync(test, fixedCode);
            await TestWhenStronglyTypedAsync(test, fixedCode);
        }

        [Fact]
        public async Task CollectionFromMethodResult_Invalid()
        {
            var test = """
                using System;
                using System.Collections.Generic;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            [|foreach|] (string item in GenerateSequence())
                            {
                            }
                            IEnumerable<IComparable> GenerateSequence()
                            {
                                throw new NotImplementedException();
                            }
                        }
                    }
                }
                """;
            var fixedCode = """
                using System;
                using System.Collections.Generic;
                using System.Linq;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            foreach (string item in GenerateSequence().Cast<string>())
                            {
                            }
                            IEnumerable<IComparable> GenerateSequence()
                            {
                                throw new NotImplementedException();
                            }
                        }
                    }
                }
                """;

            await TestAlwaysAsync(test, fixedCode);
            await TestWhenStronglyTypedAsync(test, fixedCode);
        }

        [Fact]
        public async Task CollectionFromMethodResult_Valid()
        {
            var test = """
                using System;
                using System.Collections.Generic;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            foreach (IComparable item in GenerateSequence())
                            {
                            }
                            IEnumerable<IComparable> GenerateSequence()
                            {
                                throw new NotImplementedException();
                            }
                        }
                    }
                }
                """;

            await TestAlwaysAsync(test, test);
            await TestWhenStronglyTypedAsync(test, test);
        }

        [Fact]
        public async Task DynamicSameType()
        {
            var test = """
                using System.Collections.Generic;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            var x = new List<dynamic>();
                            foreach (dynamic s in x)
                            {
                            }
                        }
                    }
                }
                """;

            await TestAlwaysAsync(test, test);
            await TestWhenStronglyTypedAsync(test, test);
        }

        [Fact]
        public async Task DynamicToObject()
        {
            var test = """
                using System.Collections.Generic;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            var x = new List<dynamic>();
                            foreach (object s in x)
                            {
                            }
                        }
                    }
                }
                """;

            await TestAlwaysAsync(test, test);
            await TestWhenStronglyTypedAsync(test, test);
        }

        [Fact]
        public async Task DynamicToString()
        {
            var test = """
                using System.Collections.Generic;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            var x = new List<dynamic>();
                            [|foreach|] (string s in x)
                            {
                            }
                        }
                    }
                }
                """;
            var fixedCode = """
                using System.Collections.Generic;
                using System.Linq;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            var x = new List<dynamic>();
                            foreach (string s in x.Select(v => (string)v))
                            {
                            }
                        }
                    }
                }
                """;

            await TestAlwaysAsync(test, fixedCode);
            await TestWhenStronglyTypedAsync(test, fixedCode);
        }

        [Fact]
        public async Task DynamicToVar()
        {
            var test = """
                using System.Collections.Generic;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            var x = new List<dynamic>();
                            foreach (var s in x)
                            {
                            }
                        }
                    }
                }
                """;

            await TestAlwaysAsync(test, test);
            await TestWhenStronglyTypedAsync(test, test);
        }

        [Fact]
        public async Task TupleToVarTuple()
        {
            var test = """
                using System;
                using System.Collections.Generic;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            var x = new List<(int, IComparable)>();
                            foreach (var (i, j) in x)
                            {
                            }
                        }
                    }
                }
                """;

            await TestAlwaysAsync(test, test);
            await TestWhenStronglyTypedAsync(test, test);
        }

        [Fact]
        public async Task TupleToSameTuple()
        {
            var test = """
                using System;
                using System.Collections.Generic;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            var x = new List<(int, IComparable)>();
                            foreach ((int i,  IComparable j) in x)
                            {
                            }
                        }
                    }
                }
                """;

            await TestAlwaysAsync(test, test);
            await TestWhenStronglyTypedAsync(test, test);
        }

        [Fact]
        public async Task TupleToChildTuple()
        {
            var test = """
                using System;
                using System.Collections.Generic;
                namespace ConsoleApplication1
                {
                    class Program
                    {   
                        void Main()
                        {
                            var x = new List<(int, IComparable)>();
                            foreach ((int i, {|CS0266:int j|}) in x)
                            {
                            }
                        }
                    }
                }
                """;

            await TestAlwaysAsync(test, test);
            await TestWhenStronglyTypedAsync(test, test);
        }

        [Fact]
        public async Task TupleToChildTuple2()
        {
            var test = """
                using System;
                using System.Linq;

                public static class Program
                {   
                    public static void M((object, object)[] x)
                    {
                        [|foreach|] ((string, string) item in x)
                        {
                            Console.WriteLine(item.Item1);
                            Console.WriteLine(item.Item2);
                        }
                    }
                }
                """;

            var code = """
                using System;
                using System.Linq;

                public static class Program
                {   
                    public static void M((object, object)[] x)
                    {
                        foreach ((string, string) item in x.Select(v => ((string, string))v))
                        {
                            Console.WriteLine(item.Item1);
                            Console.WriteLine(item.Item2);
                        }
                    }
                }
                """;

            await TestAlwaysAsync(test, code);
            await TestWhenStronglyTypedAsync(test, code);
        }
    }
}
