// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Analyzers.ForEachCast;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.ForEachCast;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ForEachCast;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpForEachCastDiagnosticAnalyzer,
    CSharpForEachCastCodeFixProvider>;

public sealed class ForEachCastTests
{
    private static Task TestWorkerAsync(
        string testCode, string fixedCode, string optionValue, ReferenceAssemblies? referenceAssemblies)
        => new VerifyCS.Test
        {
            TestCode = testCode,
            FixedCode = fixedCode,
            EditorConfig = """
            [*]
            dotnet_style_prefer_foreach_explicit_cast_in_source=
            """ + optionValue,
            ReferenceAssemblies = referenceAssemblies ?? ReferenceAssemblies.Default,
        }.RunAsync();

    private static Task TestAlwaysAsync(string markup, string alwaysMarkup, ReferenceAssemblies? referenceAssemblies = null)
        => TestWorkerAsync(markup, alwaysMarkup, "always", referenceAssemblies);

    private static Task TestWhenStronglyTypedAsync(string markup, string nonLegacyMarkup, ReferenceAssemblies? referenceAssemblies = null)
        => TestWorkerAsync(markup, nonLegacyMarkup, "when_strongly_typed", referenceAssemblies);

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
    public Task NonGenericObjectCollection_Always()
        => TestAlwaysAsync("""
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
            """, """
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
            """);

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

    [Fact, WorkItem(63470, "https://github.com/dotnet/roslyn/issues/63470")]
    public async Task TestRegex_GoodCast()
    {
        var test = """
            using System.Text.RegularExpressions;

            public static class Program
            {   
                public static void M(Regex regex, string text)
                {
                    foreach (Match m in regex.Matches(text))
                    {
                    }
                }
            }
            """;
        await TestAlwaysAsync(test, test, ReferenceAssemblies.Net.Net80);
        await TestWhenStronglyTypedAsync(test, test, ReferenceAssemblies.Net.Net80);
    }

    [Fact, WorkItem(63470, "https://github.com/dotnet/roslyn/issues/63470")]
    public async Task TestRegex_BadCast()
    {
        var test = """
            using System.Text.RegularExpressions;

            public static class Program
            {   
                public static void M(Regex regex, string text)
                {
                    [|foreach|] (string m in regex.Matches(text))
                    {
                    }
                }
            }
            """;
        var code = """
            using System.Linq;
            using System.Text.RegularExpressions;

            public static class Program
            {   
                public static void M(Regex regex, string text)
                {
                    foreach (string m in regex.Matches(text).Cast<string>())
                    {
                    }
                }
            }
            """;
        await TestAlwaysAsync(test, code, ReferenceAssemblies.Net.Net80);
        await TestWhenStronglyTypedAsync(test, code, ReferenceAssemblies.Net.Net80);
    }

    [Fact, WorkItem(63470, "https://github.com/dotnet/roslyn/issues/63470")]
    public async Task WeaklyTypedGetEnumeratorWithIEnumerableOfT()
    {
        var test = """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Text.RegularExpressions;

            public class C : IEnumerable<Match>
            {
                public IEnumerator GetEnumerator() => new Enumerator(); // compiler picks this for the foreach loop.

                IEnumerator<Match> IEnumerable<Match>.GetEnumerator() => null; // compiler doesn't use this.

                public static void M(C c)
                {
                    // The compiler adds a cast here from 'object' to 'Match',
                    // and it will fail at runtime because GetEnumerator().Current will return a string.
                    // This is due to badly implemented type 'C', and is rare enough. So, we don't report here
                    // to reduce false positives.
                    foreach (Match x in c)
                    {
                    }
                }

                private class Enumerator : IEnumerator
                {
                    public object Current => "String";

                    public bool MoveNext()
                    {
                        return true;
                    }

                    public void Reset()
                    {
                    }
                }
            }
            """;
        await TestAlwaysAsync(test, test);
        await TestWhenStronglyTypedAsync(test, test);
    }

    [Fact, WorkItem(63470, "https://github.com/dotnet/roslyn/issues/63470")]
    public async Task WeaklyTypedGetEnumeratorWithIEnumerableOfT_DifferentTypeUsedInForEach()
    {
        var code = """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            public class C : IEnumerable<string>
            {
                public IEnumerator GetEnumerator() => new Enumerator(); // compiler picks this for the foreach loop.

                IEnumerator<string> IEnumerable<string>.GetEnumerator() => null; // compiler doesn't use this.

                public static void M(C c)
                {
                    [|foreach|] (C x in c)
                    {
                    }
                }

                private class Enumerator : IEnumerator
                {
                    public object Current => "String";

                    public bool MoveNext()
                    {
                        return true;
                    }

                    public void Reset()
                    {
                    }
                }
            }
            """;

        var fixedCode = """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Linq;

            public class C : IEnumerable<string>
            {
                public IEnumerator GetEnumerator() => new Enumerator(); // compiler picks this for the foreach loop.

                IEnumerator<string> IEnumerable<string>.GetEnumerator() => null; // compiler doesn't use this.

                public static void M(C c)
                {
                    foreach (C x in c.Cast<C>())
                    {
                    }
                }

                private class Enumerator : IEnumerator
                {
                    public object Current => "String";

                    public bool MoveNext()
                    {
                        return true;
                    }

                    public void Reset()
                    {
                    }
                }
            }
            """;
        await TestAlwaysAsync(code, fixedCode);
        await TestWhenStronglyTypedAsync(code, fixedCode);
    }

    [Fact, WorkItem(63470, "https://github.com/dotnet/roslyn/issues/63470")]
    public async Task WeaklyTypedGetEnumeratorWithMultipleIEnumerableOfT()
    {
        // NOTE: The analyzer only considers the first IEnumerable<T> implementation.
        // That is why the following tests produces a diagnostic for the implicit string cast, but not for the implicit int cast.
        var test = """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            public class C : IEnumerable<int>, IEnumerable<string>
            {
                public IEnumerator GetEnumerator() => null;

                IEnumerator<int> IEnumerable<int>.GetEnumerator() => null;

                IEnumerator<string> IEnumerable<string>.GetEnumerator() => null;

                public static void M(C c)
                {
                    foreach (int x in c)
                    {
                    }

                    [|foreach|] (string x in c)
                    {
                    }
                }
            }
            """;

        var fixedCode = """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Linq;

            public class C : IEnumerable<int>, IEnumerable<string>
            {
                public IEnumerator GetEnumerator() => null;

                IEnumerator<int> IEnumerable<int>.GetEnumerator() => null;

                IEnumerator<string> IEnumerable<string>.GetEnumerator() => null;

                public static void M(C c)
                {
                    foreach (int x in c)
                    {
                    }

                    foreach (string x in c.Cast<string>())
                    {
                    }
                }
            }
            """;
        await TestAlwaysAsync(test, fixedCode);
        await TestWhenStronglyTypedAsync(test, fixedCode);
    }
}
