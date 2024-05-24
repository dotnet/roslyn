// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddUsing;

public partial class AddUsingTests
{
    [Fact]
    public async Task TestWhereExtension()
    {
        await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var q = args.[|Where|] }
}",
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var q = args.Where }
}");
    }

    [Fact]
    public async Task TestSelectExtension()
    {
        await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var q = args.[|Select|] }
}",
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var q = args.Select }
}");
    }

    [Fact]
    public async Task TestGroupByExtension()
    {
        await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var q = args.[|GroupBy|] }
}",
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var q = args.GroupBy }
}");
    }

    [Fact]
    public async Task TestJoinExtension()
    {
        await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        var q = args.[|Join|] }
}",
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var q = args.Join }
}");
    }

    [Fact]
    public async Task RegressionFor8455()
    {
        await TestMissingInRegularAndScriptAsync(
@"class C
{
    void M()
    {
        int dim = (int)Math.[|Min|]();
    }
}");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/772321")]
    public async Task TestExtensionWithThePresenceOfTheSameNameNonExtensionMethod()
    {
        await TestInRegularAndScriptAsync(
@"namespace NS1
{
    class Program
    {
        void Main()
        {
            [|new C().Goo(4);|]
        }
    }

    class C
    {
        public void Goo(string y)
        {
        }
    }
}

namespace NS2
{
    static class CExt
    {
        public static void Goo(this NS1.C c, int x)
        {
        }
    }
}",
@"using NS2;

namespace NS1
{
    class Program
    {
        void Main()
        {
            new C().Goo(4);
        }
    }

    class C
    {
        public void Goo(string y)
        {
        }
    }
}

namespace NS2
{
    static class CExt
    {
        public static void Goo(this NS1.C c, int x)
        {
        }
    }
}");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/920398")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/772321")]
    public async Task TestExtensionWithThePresenceOfTheSameNameNonExtensionPrivateMethod()
    {
        await TestInRegularAndScriptAsync(
@"namespace NS1
{
    class Program
    {
        void Main()
        {
            [|new C().Goo(4);|]
        }
    }

    class C
    {
        private void Goo(int x)
        {
        }
    }
}

namespace NS2
{
    static class CExt
    {
        public static void Goo(this NS1.C c, int x)
        {
        }
    }
}",
@"using NS2;

namespace NS1
{
    class Program
    {
        void Main()
        {
            new C().Goo(4);
        }
    }

    class C
    {
        private void Goo(int x)
        {
        }
    }
}

namespace NS2
{
    static class CExt
    {
        public static void Goo(this NS1.C c, int x)
        {
        }
    }
}");
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/920398")]
    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/772321")]
    public async Task TestExtensionWithThePresenceOfTheSameNameExtensionPrivateMethod()
    {
        await TestInRegularAndScriptAsync(
@"using NS2;

namespace NS1
{
    class Program
    {
        void Main()
        {
            [|new C().Goo(4);|]
        }
    }

    class C
    {
    }
}

namespace NS2
{
    static class CExt
    {
        private static void Goo(this NS1.C c, int x)
        {
        }
    }
}

namespace NS3
{
    static class CExt
    {
        public static void Goo(this NS1.C c, int x)
        {
        }
    }
}",
@"using NS2;
using NS3;

namespace NS1
{
    class Program
    {
        void Main()
        {
            new C().Goo(4);
        }
    }

    class C
    {
    }
}

namespace NS2
{
    static class CExt
    {
        private static void Goo(this NS1.C c, int x)
        {
        }
    }
}

namespace NS3
{
    static class CExt
    {
        public static void Goo(this NS1.C c, int x)
        {
        }
    }
}");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/269")]
    public async Task TestAddUsingForAddExtensionMethod()
    {
        await TestAsync(
@"using System;
using System.Collections;

class X : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        new X { [|1|] };
        return null;
    }
}

namespace Ext
{
    static class Extensions
    {
        public static void Add(this X x, int i)
        {
        }
    }
}",
@"using System;
using System.Collections;
using Ext;

class X : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        new X { 1 };
        return null;
    }
}

namespace Ext
{
    static class Extensions
    {
        public static void Add(this X x, int i)
        {
        }
    }
}",
parseOptions: null);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/269")]
    public async Task TestAddUsingForAddExtensionMethod2()
    {
        await TestAsync(
@"using System;
using System.Collections;

class X : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        new X { 1, 2, [|3|] };
        return null;
    }
}

namespace Ext
{
    static class Extensions
    {
        public static void Add(this X x, int i)
        {
        }
    }
}",
@"using System;
using System.Collections;
using Ext;

class X : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        new X { 1, 2, 3 };
        return null;
    }
}

namespace Ext
{
    static class Extensions
    {
        public static void Add(this X x, int i)
        {
        }
    }
}",
parseOptions: null);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/269")]
    public async Task TestAddUsingForAddExtensionMethod3()
    {
        await TestAsync(
@"using System;
using System.Collections;

class X : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        new X { 1, [|2|], 3 };
        return null;
    }
}

namespace Ext
{
    static class Extensions
    {
        public static void Add(this X x, int i)
        {
        }
    }
}",
@"using System;
using System.Collections;
using Ext;

class X : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        new X { 1, 2, 3 };
        return null;
    }
}

namespace Ext
{
    static class Extensions
    {
        public static void Add(this X x, int i)
        {
        }
    }
}",
parseOptions: null);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/269")]
    public async Task TestAddUsingForAddExtensionMethod4()
    {
        await TestAsync(
@"using System;
using System.Collections;

class X : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        new X { { 1, 2, 3 }, [|{ 4, 5, 6 }|], { 7, 8, 9 } };
        return null;
    }
}

namespace Ext
{
    static class Extensions
    {
        public static void Add(this X x, int i)
        {
        }
    }
}",
@"using System;
using System.Collections;
using Ext;

class X : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        new X { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 } };
        return null;
    }
}

namespace Ext
{
    static class Extensions
    {
        public static void Add(this X x, int i)
        {
        }
    }
}",
parseOptions: null);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/269")]
    public async Task TestAddUsingForAddExtensionMethod5()
    {
        await TestAsync(
@"using System;
using System.Collections;

class X : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        new X { { 1, 2, 3 }, { 4, 5, 6 }, [|{ 7, 8, 9 }|] };
        return null;
    }
}

namespace Ext
{
    static class Extensions
    {
        public static void Add(this X x, int i)
        {
        }
    }
}",
@"using System;
using System.Collections;
using Ext;

class X : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        new X { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 } };
        return null;
    }
}

namespace Ext
{
    static class Extensions
    {
        public static void Add(this X x, int i)
        {
        }
    }
}",
parseOptions: null);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/269")]
    public async Task TestAddUsingForAddExtensionMethod6()
    {
        await TestAsync(
@"using System;
using System.Collections;

class X : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        new X { { 1, 2, 3 }, { ""Four"", ""Five"", ""Six"" }, [|{ '7', '8', '9' }|] };
        return null;
    }
}

namespace Ext
{
    static class Extensions
    {
        public static void Add(this X x, int i)
        {
        }
    }
}",
@"using System;
using System.Collections;
using Ext;

class X : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        new X { { 1, 2, 3 }, { ""Four"", ""Five"", ""Six"" }, { '7', '8', '9' } };
        return null;
    }
}

namespace Ext
{
    static class Extensions
    {
        public static void Add(this X x, int i)
        {
        }
    }
}",
parseOptions: null);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/269")]
    public async Task TestAddUsingForAddExtensionMethod7()
    {
        await TestAsync(
@"using System;
using System.Collections;

class X : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        new X { { 1, 2, 3 }, [|{ ""Four"", ""Five"", ""Six"" }|], { '7', '8', '9' } };
        return null;
    }
}

namespace Ext
{
    static class Extensions
    {
        public static void Add(this X x, int i)
        {
        }
    }
}",
@"using System;
using System.Collections;
using Ext;

class X : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        new X { { 1, 2, 3 }, { ""Four"", ""Five"", ""Six"" }, { '7', '8', '9' } };
        return null;
    }
}

namespace Ext
{
    static class Extensions
    {
        public static void Add(this X x, int i)
        {
        }
    }
}",
parseOptions: null);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/269")]
    public async Task TestAddUsingForAddExtensionMethod8()
    {
        await TestAsync(
@"using System;
using System.Collections;

class X : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        new X { [|{ 1, 2, 3 }|] };
        return null;
    }
}

namespace Ext
{
    static class Extensions
    {
        public static void Add(this X x, int i)
        {
        }
    }
}",
@"using System;
using System.Collections;
using Ext;

class X : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        new X { { 1, 2, 3 } };
        return null;
    }
}

namespace Ext
{
    static class Extensions
    {
        public static void Add(this X x, int i)
        {
        }
    }
}",
parseOptions: null);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/269")]
    public async Task TestAddUsingForAddExtensionMethod9()
    {
        await TestAsync(
@"using System;
using System.Collections;

class X : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        new X { [|""This""|] };
        return null;
    }
}

namespace Ext
{
    static class Extensions
    {
        public static void Add(this X x, int i)
        {
        }
    }
}",
@"using System;
using System.Collections;
using Ext;

class X : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        new X { ""This"" };
        return null;
    }
}

namespace Ext
{
    static class Extensions
    {
        public static void Add(this X x, int i)
        {
        }
    }
}",
parseOptions: null);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/269")]
    public async Task TestAddUsingForAddExtensionMethod10()
    {
        await TestAsync(
@"using System;
using System.Collections;

class X : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        new X { [|{ 1, 2, 3 }|], { ""Four"", ""Five"", ""Six"" }, { '7', '8', '9' } };
        return null;
    }
}

namespace Ext
{
    static class Extensions
    {
        public static void Add(this X x, int i)
        {
        }
    }
}

namespace Ext2
{
    static class Extensions
    {
        public static void Add(this X x, object[] i)
        {
        }
    }
}",
@"using System;
using System.Collections;
using Ext;

class X : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        new X { { 1, 2, 3 }, { ""Four"", ""Five"", ""Six"" }, { '7', '8', '9' } };
        return null;
    }
}

namespace Ext
{
    static class Extensions
    {
        public static void Add(this X x, int i)
        {
        }
    }
}

namespace Ext2
{
    static class Extensions
    {
        public static void Add(this X x, object[] i)
        {
        }
    }
}",
parseOptions: null);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/269")]
    public async Task TestAddUsingForAddExtensionMethod11()
    {
        await TestAsync(
@"using System;
using System.Collections;

class X : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        new X { [|{ 1, 2, 3 }|], { ""Four"", ""Five"", ""Six"" }, { '7', '8', '9' } };
        return null;
    }
}

namespace Ext
{
    static class Extensions
    {
        public static void Add(this X x, int i)
        {
        }
    }
}

namespace Ext2
{
    static class Extensions
    {
        public static void Add(this X x, object[] i)
        {
        }
    }
}",
@"using System;
using System.Collections;
using Ext2;

class X : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        new X { { 1, 2, 3 }, { ""Four"", ""Five"", ""Six"" }, { '7', '8', '9' } };
        return null;
    }
}

namespace Ext
{
    static class Extensions
    {
        public static void Add(this X x, int i)
        {
        }
    }
}

namespace Ext2
{
    static class Extensions
    {
        public static void Add(this X x, object[] i)
        {
        }
    }
}",
index: 1,
parseOptions: null);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/3818")]
    public async Task InExtensionMethodUnderConditionalAccessExpression()
    {
        var initialText =
@"<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly"" CommonReferences=""true"">
        <Document FilePath = ""Program"">
namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            string myString = ""Sample"";
            var other = myString?[|.StringExtension()|].Substring(0);
        }
    }
}
       </Document>
       <Document FilePath = ""Extensions"">
namespace Sample.Extensions
{
    public static class StringExtensions
    {
        public static string StringExtension(this string s)
        {
            return ""Ok"";
        }
    }
}
        </Document>
    </Project>
</Workspace>";

        var expectedText =
@"
using Sample.Extensions;

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            string myString = ""Sample"";
            var other = myString?.StringExtension().Substring(0);
        }
    }
}
       ";
        await TestInRegularAndScriptAsync(initialText, expectedText);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/3818")]
    public async Task InExtensionMethodUnderMultipleConditionalAccessExpressions()
    {
        var initialText =
@"<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly"" CommonReferences=""true"">
        <Document FilePath = ""Program"">
public class C
{
    public T F&lt;T&gt;(T x)
    {
        return F(new C())?.F(new C())?[|.Extn()|];
    }
}
       </Document>
       <Document FilePath = ""Extensions"">
namespace Sample.Extensions
{
    public static class Extensions
    {
        public static C Extn(this C obj)
        {
            return obj.F(new C());
        }
    }
}
        </Document>
    </Project>
</Workspace>";

        var expectedText =
@"
using Sample.Extensions;

public class C
{
    public T F<T>(T x)
    {
        return F(new C())?.F(new C())?.Extn();
    }
}
       ";
        await TestInRegularAndScriptAsync(initialText, expectedText);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/3818")]
    public async Task InExtensionMethodUnderMultipleConditionalAccessExpressions2()
    {
        var initialText =
@"<Workspace>
    <Project Language=""C#"" AssemblyName=""CSAssembly"" CommonReferences=""true"">
        <Document FilePath = ""Program"">
public class C
{
    public T F&lt;T&gt;(T x)
    {
        return F(new C())?.F(new C())[|.Extn()|]?.F(newC());
    }
}
       </Document>
       <Document FilePath = ""Extensions"">
namespace Sample.Extensions
{
    public static class Extensions
    {
        public static C Extn(this C obj)
        {
            return obj.F(new C());
        }
    }
}
        </Document>
    </Project>
</Workspace>";

        var expectedText =
@"
using Sample.Extensions;

public class C
{
    public T F<T>(T x)
    {
        return F(new C())?.F(new C()).Extn()?.F(newC());
    }
}
       ";
        await TestInRegularAndScriptAsync(initialText, expectedText);
    }

    [Fact]
    public async Task TestDeconstructExtension()
    {
        await TestAsync(
@"
class Program
{
    void M(Program p)
    {
        var (x, y) = [|p|];
    }
}

namespace N
{
    static class E
    {
        public static void Deconstruct(this Program p, out int x, out int y) { }
    }
}",
@"
using N;

class Program
{
    void M(Program p)
    {
        var (x, y) = [|p|];
    }
}

namespace N
{
    static class E
    {
        public static void Deconstruct(this Program p, out int x, out int y) { }
    }
}",
parseOptions: null);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/16547")]
    public async Task TestAddUsingForAddExtensionMethodWithSameNameAsProperty(TestHost testHost)
    {
        await TestAsync(
@"
namespace A
{
    public class Foo
    {
        public void Bar()
        {
            var self = this.[|Self()|];
        }

        public Foo Self
        {
            get { return this; }
        }
    }
}

namespace A.Extensions
{
    public static class FooExtensions
    {
        public static Foo Self(this Foo foo)
        {
            return foo;
        }
    }
}",
@"
using A.Extensions;

namespace A
{
    public class Foo
    {
        public void Bar()
        {
            var self = this.Self();
        }

        public Foo Self
        {
            get { return this; }
        }
    }
}

namespace A.Extensions
{
    public static class FooExtensions
    {
        public static Foo Self(this Foo foo)
        {
            return foo;
        }
    }
}", testHost);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/39155")]
    public async Task TestExtensionGetAwaiterOverload(TestHost testHost)
    {
        await TestAsync(
@"
using System;
using System.Runtime.CompilerServices;

namespace A
{
    public class Foo
    {
        async void M(Foo foo)
        {
            [|await foo|];
        }
    }

    public static class BarExtensions
    {
        public static Extension.FooAwaiter GetAwaiter(this string s) => default;
    }
}

namespace A.Extension
{
    public static class FooExtensions
    {
        public static FooAwaiter GetAwaiter(this Foo foo) => default;
    }

    public struct FooAwaiter : INotifyCompletion
    {
        public bool IsCompleted { get; }

        public void OnCompleted(Action continuation)
        {
        }

        public void GetResult()
        {
        }
    }
}
",
@"
using System;
using System.Runtime.CompilerServices;
using A.Extension;

namespace A
{
    public class Foo
    {
        async void M(Foo foo)
        {
            await foo;
        }
    }

    public static class BarExtensions
    {
        public static Extension.FooAwaiter GetAwaiter(this string s) => default;
    }
}

namespace A.Extension
{
    public static class FooExtensions
    {
        public static FooAwaiter GetAwaiter(this Foo foo) => default;
    }

    public struct FooAwaiter : INotifyCompletion
    {
        public bool IsCompleted { get; }

        public void OnCompleted(Action continuation)
        {
        }

        public void GetResult()
        {
        }
    }
}
", testHost);
    }

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/39155")]
    public async Task TestExtensionSelectOverload(TestHost testHost)
    {
        await TestAsync(
@"
using System;
using System.Collections.Generic;

namespace A
{
    public class Foo
    {
        void M(Foo foo)
        {
            _ = [|from x in foo|] select x;
        }
    }

    public static class BarExtensions
    {
        public static IEnumerable<int> Select(this string foo, Func<int, int> f) => null;
    }
}

namespace A.Extension
{
    public static class FooExtensions
    {
        public static IEnumerable<int> Select(this Foo foo, Func<int, int> f) => null;
    }
}
",
@"
using System;
using System.Collections.Generic;
using A.Extension;

namespace A
{
    public class Foo
    {
        void M(Foo foo)
        {
            _ = from x in foo select x;
        }
    }

    public static class BarExtensions
    {
        public static IEnumerable<int> Select(this string foo, Func<int, int> f) => null;
    }
}

namespace A.Extension
{
    public static class FooExtensions
    {
        public static IEnumerable<int> Select(this Foo foo, Func<int, int> f) => null;
    }
}
", testHost);
    }

    [Fact]
    public async Task TestExtensionDeconstructOverload()
    {
        await TestAsync(
@"
using System;
using System.Collections.Generic;

namespace A
{
    public class Foo
    {
        void M(Foo foo)
        {
            var (x, y) = [|foo|];
        }
    }

    public static class BarExtensions
    {
        public static void Deconstruct(this string foo, out int a, out int b) => throw null;
    }
}

namespace A.Extension
{
    public static class FooExtensions
    {
        public static void Deconstruct(this Foo foo, out int a, out int b) => throw null;
    }
}
",
@"
using System;
using System.Collections.Generic;
using A.Extension;

namespace A
{
    public class Foo
    {
        void M(Foo foo)
        {
            var (x, y) = foo;
        }
    }

    public static class BarExtensions
    {
        public static void Deconstruct(this string foo, out int a, out int b) => throw null;
    }
}

namespace A.Extension
{
    public static class FooExtensions
    {
        public static void Deconstruct(this Foo foo, out int a, out int b) => throw null;
    }
}
",
parseOptions: null);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55117")]
    public async Task TestMethodConflictWithGenericExtension()
    {
        await TestInRegularAndScriptAsync(
@"namespace A
{
    public abstract class Goo
    {
        public abstract object Bar( Type type );
    }

    public class Test
    {
        public void TestMethod(Goo arg)
        {
            arg.[|Bar<object>()|];

        }
    }
}

namespace A.Extensions
{
    public static class Extension
    {
        public static T Bar<T>( this Goo @this )
            => (T)@this.Bar( typeof( T ) );
    }
}",
@"using A.Extensions;

namespace A
{
    public abstract class Goo
    {
        public abstract object Bar( Type type );
    }

    public class Test
    {
        public void TestMethod(Goo arg)
        {
            arg.Bar<object>();

        }
    }
}

namespace A.Extensions
{
    public static class Extension
    {
        public static T Bar<T>( this Goo @this )
            => (T)@this.Bar( typeof( T ) );
    }
}");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/55117")]
    public async Task TestMethodConflictWithConditionalGenericExtension()
    {
        await TestInRegularAndScriptAsync(
@"namespace A
{
    public abstract class Goo
    {
        public abstract object Bar( Type type );
    }

    public class Test
    {
        public void TestMethod(Goo arg)
        {
            arg?.[|Bar<object>()|];

        }
    }
}

namespace A.Extensions
{
    public static class Extension
    {
        public static T Bar<T>( this Goo @this )
            => (T)@this.Bar( typeof( T ) );
    }
}",
@"using A.Extensions;

namespace A
{
    public abstract class Goo
    {
        public abstract object Bar( Type type );
    }

    public class Test
    {
        public void TestMethod(Goo arg)
        {
            arg?.Bar<object>();

        }
    }
}

namespace A.Extensions
{
    public static class Extension
    {
        public static T Bar<T>( this Goo @this )
            => (T)@this.Bar( typeof( T ) );
    }
}");
    }
}
