// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddUsing
{
    public partial class AddUsingTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
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

        [WorkItem(772321, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/772321")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
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

        [WorkItem(772321, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/772321")]
        [WorkItem(920398, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/920398")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
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

        [WorkItem(772321, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/772321")]
        [WorkItem(920398, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/920398")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
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

        [WorkItem(269, "https://github.com/dotnet/roslyn/issues/269")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task TestAddUsingForAddExtentionMethod()
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

        [WorkItem(269, "https://github.com/dotnet/roslyn/issues/269")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task TestAddUsingForAddExtentionMethod2()
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

        [WorkItem(269, "https://github.com/dotnet/roslyn/issues/269")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task TestAddUsingForAddExtentionMethod3()
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

        [WorkItem(269, "https://github.com/dotnet/roslyn/issues/269")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task TestAddUsingForAddExtentionMethod4()
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

        [WorkItem(269, "https://github.com/dotnet/roslyn/issues/269")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task TestAddUsingForAddExtentionMethod5()
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

        [WorkItem(269, "https://github.com/dotnet/roslyn/issues/269")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task TestAddUsingForAddExtentionMethod6()
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

        [WorkItem(269, "https://github.com/dotnet/roslyn/issues/269")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task TestAddUsingForAddExtentionMethod7()
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

        [WorkItem(269, "https://github.com/dotnet/roslyn/issues/269")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task TestAddUsingForAddExtentionMethod8()
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

        [WorkItem(269, "https://github.com/dotnet/roslyn/issues/269")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task TestAddUsingForAddExtentionMethod9()
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

        [WorkItem(269, "https://github.com/dotnet/roslyn/issues/269")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task TestAddUsingForAddExtentionMethod10()
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

        [WorkItem(269, "https://github.com/dotnet/roslyn/issues/269")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task TestAddUsingForAddExtentionMethod11()
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

        [WorkItem(3818, "https://github.com/dotnet/roslyn/issues/3818")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
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

        [WorkItem(3818, "https://github.com/dotnet/roslyn/issues/3818")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
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

        [WorkItem(3818, "https://github.com/dotnet/roslyn/issues/3818")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
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

        [WorkItem(16547, "https://github.com/dotnet/roslyn/issues/16547")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddImport)]
        public async Task TestAddUsingForAddExtentionMethodWithSameNameAsProperty()
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
}");
        }
    }
}
