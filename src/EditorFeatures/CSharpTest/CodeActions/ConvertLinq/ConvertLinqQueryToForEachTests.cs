// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using ICSharpCode.Decompiler.CSharp;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.ConvertLinq
{
    public class ConvertLinqQueryToForEachTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
          => new CodeAnalysis.CSharp.ConvertLinq.CSharpConvertLinqQueryToForEachProvider();

        #region Query Expressions

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task Select()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class Query
{
    public static void Main(string[] args)
    {
        List<int> c = new List<int>{ 1, 2, 3, 4, 5, 6, 7 };
        var r = [|from i in c select i+1|];
    }
}";
            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class Query
{
    public static void Main(string[] args)
    {
        List<int> c = new List<int>{ 1, 2, 3, 4, 5, 6, 7 };
        IEnumerable<int> enumerable()
        {
            foreach (var i in c)
            {
                yield return i + 1;
            }
        }

        var r = enumerable();
    }
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task GroupBy01()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class Query
{
    public static void Main(string[] args)
    {
        List<int> c = new List<int>(1, 2, 3, 4, 5, 6, 7);
        var r = [|from i in c group i by i % 2|];
        Console.WriteLine(r);
    }
}";

            // Group by is not supported
            await TestMissingAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task GroupBy02()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class Query
{
    public static void Main(string[] args)
    {
        List<int> c = new List<int>(1, 2, 3, 4, 5, 6, 7);
        var r = [|from i in c group 10+i by i % 2|];
        Console.WriteLine(r);
    }
}";

            // Group by is not supported
            await TestMissingAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task FromJoinSelect01()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class Query
{
    public static void Main(string[] args)
    {
        List<int> c1 = new List<int>{1, 2, 3, 4, 5, 7};
        List<int> c2 = new List<int>{10, 30, 40, 50, 60, 70};
        var r = [|from x1 in c1
                      join x2 in c2 on x1 equals x2/10
                      select x1+x2|];
    }
}
";
            string output = @"
using System.Collections.Generic;
using System.Linq;
class Query
{
    public static void Main(string[] args)
    {
        List<int> c1 = new List<int>{1, 2, 3, 4, 5, 7};
        List<int> c2 = new List<int>{10, 30, 40, 50, 60, 70};
        IEnumerable<int> enumerable()
        {
            foreach (var x1 in c1)
            {
                foreach (var x2 in c2)
                {
                    if (object.Equals(x1, x2 / 10))
                    {
                        yield return x1 + x2;
                    }
                }
            }
        }

        var r = enumerable();
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task FromJoinSelect02()
        {
            string source = @"
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var q1 = [|from num in new int[] { 1, 2 }
                    from a in new int[] { 5, 6 }
                    join x1 in new int[] { 3, 4 } on num equals x1
                    select x1 + 5|];
    }
}";
            string output = @"
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        System.Collections.Generic.IEnumerable<int> enumerable()
        {
            var v1 = new int[] { 1, 2 };
            var v = new int[] { 3, 4 };
            foreach (var num in v1)
            {
                foreach (var a in new int[] { 5, 6 })
                {
                    foreach (var x1 in v)
                    {
                        if (object.Equals(num, x1))
                        {
                            yield return x1 + 5;
                        }
                    }
                }
            }
        }

        var q1 = enumerable();
    }
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task FromJoinSelect03()
        {
            string source = @"
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        var q1 = [|from num in new int[] { 1, 2 }
                    from a in new int[] { 5, 6 }
                    join x1 in new int[] { 3, 4 } on num equals x1
                    join x2 in new int[] { 7, 8 } on num equals x2
                    select x1 + 5|];
    }
}";
            string output = @"
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        System.Collections.Generic.IEnumerable<int> enumerable()
        {
            var v2 = new int[] { 1, 2 };
            var v1 = new int[] { 3, 4 };
            var v = new int[] { 7, 8 };
            foreach (var num in v2)
            {
                foreach (var a in new int[] { 5, 6 })
                {
                    foreach (var x1 in v1)
                    {
                        if (object.Equals(num, x1))
                        {
                            foreach (var x2 in v)
                            {
                                if (object.Equals(num, x2))
                                {
                                    yield return x1 + 5;
                                }
                            }
                        }
                    }
                }
            }
        }

        var q1 = enumerable();
    }
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task OrderBy()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class Query
{
    public static void Main(string[] args)
    {
        List<int> c = new List<int>(28, 51, 27, 84, 27, 27, 72, 64, 55, 46, 39);
        var r =
            [|from i in c
            orderby i/10 descending, i%10
            select i|];
        Console.WriteLine(r);
    }
}";
            // order by is not supported by foreach.
            await TestMissingAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task Let()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class Query
{
    public static void Main(string[] args)
    {
        List<int> c1 = new List<int>{ 1, 2, 3 };
        List<int> r1 =
            ([|from int x in c1
            let g = x * 10
            let z = g + x*100
            let a = 5 + z
            select x + z - a|]).ToList();
            Console.WriteLine(r1);
    }
}";
            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class Query
{
    public static void Main(string[] args)
    {
        List<int> c1 = new List<int>{ 1, 2, 3 };
        List<int> r1 = new List<int>();
        foreach (int x in c1)
        {
            var g = x * 10;
            var z = g + x*100;
            var a = 5 + z;
            r1.Add(x + z - a);
        }

        Console.WriteLine(r1);
    }
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task GroupJoin()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class Query
{
    public static void Main(string[] args)
    {
        List<int> c1 = new List<int> { 1, 2, 3, 4, 5, 7 };
        List<int> c2 = new List<int> { 12, 34, 42, 51, 52, 66, 75 };
        List<string> r1 = ([|from x1 in c1
                      join x2 in c2 on x1 equals x2 / 10 into g
                      where x1 < 7
                      select x1 + "":"" + g.ToString()|]).ToList();
        Console.WriteLine(r1);
    }
}
";
            // GroupJoin is not supported
            await TestMissingAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task SelectFromType01()
        {
            string source = @"using System;
using System.Collections.Generic;
 
class C
{
    static void Main()
    {
        var q = [|from x in C select x|];
    }

    static IEnumerable<int> Select<T>(Func<int, T> f) { return null; }
}";
            string output = @"using System;
using System.Collections.Generic;
 
class C
{
    static void Main()
    {
        IEnumerable<int> enumerable()
        {
            foreach (var x in C)
            {
                yield return x;
            }
        }

        var q = enumerable();
    }

    static IEnumerable<int> Select<T>(Func<int, T> f) { return null; }
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task SelectFromType02()
        {
            string source = @"using System;
using System.Collections.Generic;
 
class C
{
    static void Main()
    {
        var q = [|from x in C select x|];
    }

    static Func<Func<int, object>, IEnumerable<object>> Select = null;
}";
            string output = @"using System;
using System.Collections.Generic;
 
class C
{
    static void Main()
    {
        IEnumerable<object> enumerable()
        {
            foreach (var x in C)
            {
                yield return x;
            }
        }

        var q = enumerable();
    }

    static Func<Func<int, object>, IEnumerable<object>> Select = null;
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task JoinClause()
        {
            string source = @"
using System;
using System.Linq;
class Program
{
    static void Main()
    {
        var q2 =
           [|from a in Enumerable.Range(1, 2)
           join b in Enumerable.Range(1, 13) on 4 * a equals b
           select a|];

        foreach (var q in q2)
        {
            System.Console.Write(q);
        }
    }
}";
            string output = @"
using System;
using System.Linq;
class Program
{
    static void Main()
    {
        System.Collections.Generic.IEnumerable<int> enumerable2()
        {
            var enumerable1 = Enumerable.Range(1, 2);
            var enumerable = Enumerable.Range(1, 13);
            foreach (var a in enumerable1)
            {
                foreach (var b in enumerable)
                {
                    if (object.Equals(4 * a, b))
                    {
                        yield return a;
                    }
                }
            }
        }

        var q2 = enumerable2();

        foreach (var q in q2)
        {
            System.Console.Write(q);
        }
    }
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task WhereClause()
        {
            string source = @"
using System;
using System.Linq;
class Program
{
    static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };

        var q2 = [|from x in nums
                where (x > 2)
                select x|];

        string serializer = String.Empty;
        foreach (var q in q2)
        {
            serializer = serializer + q + "" "";
        }
        System.Console.Write(serializer.Trim());
    }
}";

            string output = @"
using System;
using System.Linq;
class Program
{
    static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        System.Collections.Generic.IEnumerable<int> enumerable()
        {
            foreach (var x in nums)
            {
                if (x > 2)
                {
                    yield return x;
                }
            }
        }

        var q2 = enumerable();

        string serializer = String.Empty;
        foreach (var q in q2)
        {
            serializer = serializer + q + "" "";
        }
        System.Console.Write(serializer.Trim());
    }
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task WhereDefinedInType()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class Y
{
    public int Where(Func<int, bool> predicate)
    {
        return 45;
    }
}

class P
{
    static void Main()
    {
        var src = new Y();
        var query = [|from x in src
                where x > 0
                select x|]];

        Console.Write(query);
    }
}";
            //  should not provide a conversion because of the custom Where.
            await TestMissingAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task QueryContinuation()
        {
            string source = @"
using System;
using System.Linq;
public class Test
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };

        var q2 = [|from x in nums
                 select x into w
                 select w|];
    }
}";
            string output = @"
using System;
using System.Linq;
public class Test
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        System.Collections.Generic.IEnumerable<int> enumerable()
        {
            foreach (var x in nums)
            {
                int w = x;
                yield return w;
            }
        }

        var q2 = enumerable();
    }
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task SelectInto()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };

        var q2 = [|from x in nums
                 select x+1 into w
                 select w+1|];
    }
}";
            string output = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        IEnumerable<int> enumerable()
        {
            foreach (var x in nums)
            {
                int w = x + 1;
                yield return w + 1;
            }
        }

        var q2 = enumerable();
    }
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task ComputeQueryVariableType()
        {
            string source = @"
using System.Linq;
public class Test
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };

        var q2 = [|from x in nums
                 select 5|];
    }
}";
            string output = @"
using System.Linq;
public class Test
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        System.Collections.Generic.IEnumerable<int> enumerable()
        {
            foreach (var x in nums)
            {
                yield return 5;
            }
        }

        var q2 = enumerable();
    }
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task JoinIntoClause()
        {
            string source = @"
using System;
using System.Linq;

static class Test
{
    static void Main()
    {
        var qie = [|from x3 in new int[] { 0 }
                      join x7 in (new int[] { 0 }) on 5 equals 5 into x8
                      select x8|];
    }
}";
            // GroupJoin is not supported
            await TestMissingAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task SemanticErrorInQuery()
        {
            string source = @"
using System.Linq;
class Program
{
    static void Main()
    {
        int[] nums = { 0, 1, 2, 3, 4, 5 };
        var query = [|from num in nums
                    let num = 3
                    select num|]; 
    }
}";

            // Error: Range variable already being declared.
            await TestMissingAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task SelectFromVoid()
        {
            string source = @"
using System.Linq;
class Test
{
    static void V()
    {
    }

    public static int Main()
    {
        var e1 = [|from i in V() select i|];
    }
}
";

            await TestMissingAsync(source);
        }

        #endregion

        #region Assignments, Declarations, Returns

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task AssignExpression()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        IEnumerable<int> q;
        q = [|from int n1 in nums 
                from int n2 in nums
                select n1|];

        N(q);
    }

    void N(IEnumerable<int> q) {}
}
";

            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        IEnumerable<int> q;
        IEnumerable<int> enumerable()
        {
            foreach (int n1 in nums)
            {
                foreach (int n2 in nums)
                {
                    yield return n1;
                }
            }
        }

        q = enumerable();

        N(q);
    }

    void N(IEnumerable<int> q) {}
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task MultipleAssignments()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        IEnumerable<int> q1, q2;
        q1 = q2 = [|from x in nums select x + 1|];
    }
}";

            string output = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        IEnumerable<int> q1, q2;
        IEnumerable<int> enumerable()
        {
            foreach (var x in nums)
            {
                yield return x + 1;
            }
        }

        q1 = q2 = enumerable();
    }
}";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task PropertyAssignment()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        var c = new C();
        c.A = [|from x in nums select x + 1|];
    }

    class C
    {
        public IEnumerable<int> A { get; set; }
    }
}";

            string output = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        var c = new C();
        IEnumerable<int> enumerable()
        {
            foreach (var x in nums)
            {
                yield return x + 1;
            }
        }

        c.A = enumerable();
    }

    class C
    {
        public IEnumerable<int> A { get; set; }
    }
}";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task MultipleDeclarationsFirst()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        IEnumerable<int> q1 = [|from x in nums select x + 1|], q2 = from x in nums select x + 1;
    }
}";

            string output = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        IEnumerable<int> enumerable()
        {
            foreach (var x in nums)
            {
                yield return x + 1;
            }
        }

        IEnumerable<int> q1 = enumerable(), q2 = from x in nums select x + 1;
    }
}";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task MultipleDeclarationsSecond()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        IEnumerable<int> q1 = from x in nums select x + 1, q2 = [|from x in nums select x + 1|];
    }
}";

            string output = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        IEnumerable<int> enumerable()
        {
            foreach (var x in nums)
            {
                yield return x + 1;
            }
        }

        IEnumerable<int> q1 = from x in nums select x + 1, q2 = enumerable();
    }
}";

            await TestInRegularAndScriptAsync(source, output);
        }

        // TODO support tuples in the test class, follow CodeGenTupleTests
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/25639"), Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task TupleDeclaration()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        var q = ([|from x in nums select x + 1|], from x in nums select x + 1);
    }
}";

            string output = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        IEnumerable<int> enumerable()
        {
            foreach (var x in nums)
            {
                yield return x + 1;
            }
        }

        var q = (enumerable(), from x in nums select x + 1);
    }
}";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task AssignAndReturnIEnumerable()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<int> M(IEnumerable<int> nums)
    {
        var q = [|from int n1 in nums
                from int n2 in nums
                select n1|];
        return q;
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<int> M(IEnumerable<int> nums)
    {
        IEnumerable<int> enumerable()
        {
            foreach (int n1 in nums)
            {
                foreach (int n2 in nums)
                {
                    yield return n1;
                }
            }
        }

        var q = enumerable();
        return q;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task BlockBodiedProperty()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    private readonly int[] _nums = new int[] { 1, 2, 3, 4 };
    public IEnumerable<int> Query1 { get { return [|from x in _nums select x + 1|]; } }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    private readonly int[] _nums = new int[] { 1, 2, 3, 4 };
    public IEnumerable<int> Query1 { get { foreach (var x in _nums) { yield return x + 1; } yield break; } }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task AnonymousType()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        var q = [|from a in nums from b in nums select new { a, b }|];
    }
}
";
            // No conversion can be made because it expects to introduce a local function but the return type contains anonymous.
            await TestMissingAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task AnonymousTypeInternally()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        var q = [|from a in nums from b in nums select new { a, b } into c select c.a|];
    }
}
";
            // No conversion can be made because it expects to introduce a local function but the return type contains anonymous.
            await TestMissingAsync(source);
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task DuplicateIdentifiers()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M()
    {
        var q = [|from x in new[] { 1 } select x + 2 into x where x > 0 select 7 into y let x = ""aaa"" select x|];
    }
}
";
            // Duplicate identifiers are not allowed.
            await TestMissingAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task ReturnIEnumerable()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<int> M(IEnumerable<int> nums)
    {
        return [|from int n1 in nums 
                 from int n2 in nums
                 select n1|];
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<int> M(IEnumerable<int> nums)
    {
        foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                yield return n1;
            }
        }

        yield break;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task ReturnIEnumerablePartialMethod()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
partial class C
{
    partial IEnumerable<int> M(IEnumerable<int> nums);
}
partial class C
{
    partial IEnumerable<int> M(IEnumerable<int> nums)
    {
        return [|from int n1 in nums 
                 from int n2 in nums
                 select n1|];
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
partial class C
{
    partial IEnumerable<int> M(IEnumerable<int> nums);
}
partial class C
{
    partial IEnumerable<int> M(IEnumerable<int> nums)
    {
        foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                yield return n1;
            }
        }

        yield break;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task ReturnIEnumerableWithOtherReturn()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<int> M(IEnumerable<int> nums)
    {
        if (nums.Any())
        {
            return [|from int n1 in nums 
                     from int n2 in nums
                     select n1|];
        }
        else
        {
            return null;
        }
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<int> M(IEnumerable<int> nums)
    {
        if (nums.Any())
        {
            IEnumerable<int> enumerable()
            {
                foreach (int n1 in nums)
                {
                    foreach (int n2 in nums)
                    {
                        yield return n1;
                    }
                }
            }

            return enumerable();
        }
        else
        {
            return null;
        }
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task ReturnObject()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    object M(IEnumerable<int> nums)
    {
        return [|from int n1 in nums 
                 from int n2 in nums
                 select n1|];
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    object M(IEnumerable<int> nums)
    {
        IEnumerable<int> enumerable()
        {
            foreach (int n1 in nums)
            {
                foreach (int n2 in nums)
                {
                    yield return n1;
                }
            }
        }

        return enumerable();
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task ExtraParenthesis()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    IEnumerable<int> M()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        return ([|from x in nums select x + 1|]);
    }
}";

            string output = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    IEnumerable<int> M()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        foreach (var x in nums)
        {
            yield return x + 1;
        }

        yield break;
    }
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        // TODO support tuples in the test class
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/25639"), Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task InReturningTuple()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    (IEnumerable<int>, int) M(IEnumerable<int> q)
    {
        return (([|from a in q select a * a|]), 1);
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    (IEnumerable<int>, int) M(IEnumerable<int> q)
    {
        IEnumerable<int> enumerable()
        {
            foreach(var a in q)
            {
                yield return a * a;
            }
        }

        return (enumerable(), 1);
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        // TODO support tuples in the test class
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/25639"), Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task InInvocationReturningInTuple()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    (int, int) M(IEnumerable<int> q)
    {
        return (([|from a in q select a * a|]).Count(), 1);
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    (int, int) M(IEnumerable<int> q)
    {
        IEnumerable<int> enumerable()
        {
            foreach(var a in q)
            {
                yield return a * a;
            }
        }

        return (enumerable().Count(), 1);
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task RangeVariables()
        {
            string source = @"
using System;
using System.Linq;
class Query
{
    public static void Main(string[] args)
    {
        var c1 = new int[] {1, 2, 3};
        var c2 = new int[] {10, 20, 30};
        var c3 = new int[] {100, 200, 300};
        var r1 =
            [|from int x in c1
            from int y in c2
            from int z in c3
            select x + y + z|];
        Console.WriteLine(r1);
    }
}";
            string output = @"
using System;
using System.Linq;
class Query
{
    public static void Main(string[] args)
    {
        var c1 = new int[] {1, 2, 3};
        var c2 = new int[] {10, 20, 30};
        var c3 = new int[] {100, 200, 300};
        System.Collections.Generic.IEnumerable<int> enumerable()
        {
            foreach (int x in c1)
            {
                foreach (int y in c2)
                {
                    foreach (int z in c3)
                    {
                        yield return x + y + z;
                    }
                }
            }
        }

        var r1 = enumerable();
        Console.WriteLine(r1);
    }
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task CallingMethodWithIEnumerable()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        var q = [|from int n1 in nums 
                from int n2 in nums
                select n1|];
        N(q);
    }

    void N(IEnumerable<int> q) {}
}
";

            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        IEnumerable<int> enumerable()
        {
            foreach (int n1 in nums)
            {
                foreach (int n2 in nums)
                {
                    yield return n1;
                }
            }
        }

        var q = enumerable();
        N(q);
    }

    void N(IEnumerable<int> q) {}
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task ReturnFirstOrDefault()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    T M<T>(IEnumerable<T> nums)
    {
        return ([|from n1 in nums 
                 from n2 in nums
                 select n1|]).FirstOrDefault();
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    T M<T>(IEnumerable<T> nums)
    {
        IEnumerable<T> enumerable()
        {
            foreach (var n1 in nums)
            {
                foreach (var n2 in nums)
                {
                    yield return n1;
                }
            }
        }

        return enumerable().FirstOrDefault();
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task IncompleteQueryWithSyntaxErrors()
        {
            string source = @"
using System.Linq;

class Program
{
    static int Main()
    {
        int [] goo = new int [] {1};
        var q = [|from x in goo
                select x + 1 into z
                    select z.T|]
    }
}
";

            await TestMissingAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task ErrorNameDoesNotExistsInContext()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<int> M()
    {
        return [|from int n1 in nums select n1|];
    }
}
";

            await TestMissingAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task InArrayInitialization()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    IEnumerable<int>[] M(IEnumerable<int> q)
    {
        return new[] { [|from a in q select a * a|] };
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    IEnumerable<int>[] M(IEnumerable<int> q)
    {
        IEnumerable<int> enumerable()
        {
            foreach (var a in q)
            {
                yield return a * a;
            }
        }

        return new[] { enumerable() };
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task InCollectionInitialization()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    List<IEnumerable<int>> M(IEnumerable<int> q)
    {
        return new List<IEnumerable<int>> { [|from a in q select a * a|] };
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    List<IEnumerable<int>> M(IEnumerable<int> q)
    {
        IEnumerable<int> enumerable()
        {
            foreach (var a in q)
            {
                yield return a * a;
            }
        }

        return new List<IEnumerable<int>> { enumerable() };
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task InStructInitialization()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    struct X
    {
        public IEnumerable<int> P;
    }

    X M(IEnumerable<int> q)
    {
        return new X() { P = [|from a in q select a|] };
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    struct X
    {
        public IEnumerable<int> P;
    }

    X M(IEnumerable<int> q)
    {
        IEnumerable<int> enumerable()
        {
            foreach (var a in q)
            {
                yield return a;
            }
        }

        return new X() { P = enumerable() };
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task InClassInitialization()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    class X
    {
        public IEnumerable<int> P;
    }

    X M(IEnumerable<int> q)
    {
        return new X() { P = [|from a in q select a|] };
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    class X
    {
        public IEnumerable<int> P;
    }

    X M(IEnumerable<int> q)
    {
        IEnumerable<int> enumerable()
        {
            foreach (var a in q)
            {
                yield return a;
            }
        }

        return new X() { P = enumerable() };
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task InConstructor()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    List<int> M(IEnumerable<int> q)
    {
        return new List<int>([|from a in q select a * a|]);
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    List<int> M(IEnumerable<int> q)
    {
        IEnumerable<int> collection()
        {
            foreach (var a in q)
            {
                yield return a * a;
            }
        }

        return new List<int>(collection());
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task InInlineConstructor()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    List<int> M(IEnumerable<int> q)
        => new List<int>([|from a in q select a * a|]);
}
";

            // No support for expresison bodied constructors yet.
            await TestMissingAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task IninlineIf()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    List<int> M(IEnumerable<int> q)
    {
        if (true)
            return new List<int>([|from a in q select a * a|]);
        else
            return null;
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    List<int> M(IEnumerable<int> q)
    {
        if (true)
        {
            IEnumerable<int> collection()
            {
                foreach (var a in q)
                {
                    yield return a * a;
                }
            }

            return new List<int>(collection());
        }
        else
            return null;
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        #endregion

        #region In foreach

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task UsageInForEach()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        var q = [|from int n1 in nums 
                from int n2 in nums
                select n1|];
        foreach (var b in q)
        {
            Console.WriteLine(b);
        }
    }
}
";

            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        IEnumerable<int> enumerable()
        {
            foreach (int n1 in nums)
            {
                foreach (int n2 in nums)
                {
                    yield return n1;
                }
            }
        }

        var q = enumerable();
        foreach (var b in q)
        {
            Console.WriteLine(b);
        }
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task UsageInForEachSameVariableName()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        var q = [|from int n1 in nums 
                from int n2 in nums
                select n1|];
        foreach(var n1 in q)
        {
            Console.WriteLine(n1);
        }
    }
}
";

            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        IEnumerable<int> enumerable()
        {
            foreach (int n1 in nums)
            {
                foreach (int n2 in nums)
                {
                    yield return n1;
                }
            }
        }

        var q = enumerable();
        foreach(var n1 in q)
        {
            Console.WriteLine(n1);
        }
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task QueryInForEach()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        foreach(var b in [|from int n1 in nums 
                from int n2 in nums
                select n1|])
        {
            Console.WriteLine(b);
        }
    }
}
";

            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                var b = n1;
                Console.WriteLine(b);
            }
        }
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task QueryInForEachSameVariableNameNoType()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        foreach(var n1 in [|from int n1 in nums 
                          from int n2 in nums
                          select n1|])
        {
            Console.WriteLine(n1);
        }
    }
}
";

            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                Console.WriteLine(n1);
            }
        }
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task QueryInForEachWithExpressionBody()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        foreach(var b in [|from int n1 in nums 
                from int n2 in nums
                select n1|]) Console.WriteLine(b);
    }
}
";

            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                var b = n1;
                Console.WriteLine(b);
            }
        }
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task QueryInForEachWithSameVariableNameAndDifferentType()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class A { }
class B : A { }
class C
{
    void M(IEnumerable<int> nums)
    {
        foreach (A a in [|from B a in nums from A c in nums select a|])
        {
            Console.Write(a.ToString());
        }
    }
}
";

            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class A { }
class B : A { }
class C
{
    void M(IEnumerable<int> nums)
    {
        IEnumerable<B> enumerable()
        {
            foreach (B a in nums)
            {
                foreach (A c in nums)
                {
                    yield return a;
                }
            }
        }

        foreach (A a in enumerable())
        {
            Console.Write(a.ToString());
        }
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task QueryInForEachWithSameVariableNameAndSameType()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class A { }
class B : A { }
class C
{
    void M(IEnumerable<int> nums)
    {
        foreach (A a in [|from A a in nums from A c in nums select a|])
        {
            Console.Write(a.ToString());
        }
    }
}
";

            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class A { }
class B : A { }
class C
{
    void M(IEnumerable<int> nums)
    {
        foreach (A a in nums)
        {
            foreach (A c in nums)
            {
                Console.Write(a.ToString());
            }
        }
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task QueryInForEachVariableUsedInBody()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        foreach(var b in [|from int n1 in nums 
                from int n2 in nums
                select n1|])
        {
            int n1 = 5;
            Console.WriteLine(b);
        }
    }
}
";

            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        IEnumerable<int> enumerable()
        {
            foreach (int n1 in nums)
            {
                foreach (int n2 in nums)
                {
                    yield return n1;
                }
            }
        }

        foreach (var b in enumerable())
        {
            int n1 = 5;
            Console.WriteLine(b);
        }
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task QueryInForEachWithConvertedType()
        {
            string source = @"
using System;
using System.Collections.Generic;

static class Extensions
{
    public static IEnumerable<C> Select(this int[] x, Func<int, C> predicate) => throw null;
}

class C
{
    public static implicit operator int(C x)
    {
        throw null;
    }

    public static implicit operator C(int x)
    {
        throw null;
    }

    void Test()
    {
        foreach (int x in [|from x in new[] { 1, 2, 3, } select x|])
        {
            Console.Write(x);
        }
    }
}
";

            string output = @"
using System;
using System.Collections.Generic;

static class Extensions
{
    public static IEnumerable<C> Select(this int[] x, Func<int, C> predicate) => throw null;
}

class C
{
    public static implicit operator int(C x)
    {
        throw null;
    }

    public static implicit operator C(int x)
    {
        throw null;
    }

    void Test()
    {
        IEnumerable<C> enumerable()
        {
            foreach (var x in new[] { 1, 2, 3, })
            {
                yield return x;
            }
        }

        foreach (int x in enumerable())
        {
            Console.Write(x);
        }
    }
}
";
            await TestAsync(source, output, parseOptions: null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task QueryInForEachWithSelectIdentifierButNotVariable()
        {
            string source = @"
using System;
using System.Collections.Generic;

static class Extensions
{
    public static IEnumerable<C> Select(this int[] x, Func<int, Action> predicate) => throw null;
}

class C
{
    public static implicit operator int(C x)
    {
        throw null;
    }

    public static implicit operator C(int x)
    {
        throw null;
    }

    void Test()
    {
        foreach (int Test in [|from y in new[] { 1, 2, 3, } select Test|])
        {
            Console.Write(Test);
        }
    }
}
";

            string output = @"
using System;
using System.Collections.Generic;

static class Extensions
{
    public static IEnumerable<C> Select(this int[] x, Func<int, Action> predicate) => throw null;
}

class C
{
    public static implicit operator int(C x)
    {
        throw null;
    }

    public static implicit operator C(int x)
    {
        throw null;
    }

    void Test()
    {
        foreach (var y in new[] { 1, 2, 3, })
        {
            int Test = Test;
            Console.Write(Test);
        }
    }
}
";

            await TestAsync(source, output, parseOptions: null);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task IQueryable()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

class C
{
    IQueryable<int> M(IEnumerable<int> nums)
    {
        return [|from int n1 in nums.AsQueryable() select n1|];
    }
}";

            await TestMissingAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task IQueryableConvertedToIEnumerableInReturn()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

class C
{
    IEnumerable<int> M(IEnumerable<int> nums)
    {
        return [|from int n1 in nums.AsQueryable() select n1|];
    }
}";
            string output = @"
using System.Collections.Generic;
using System.Linq;

class C
{
    IEnumerable<int> M(IEnumerable<int> nums)
    {
        foreach (int n1 in nums.AsQueryable())
        {
            yield return n1;
        }

        yield break;
    }
}";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task IQueryableConvertedToIEnumerableInAssignment()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

class C
{
    void M(IEnumerable<int> nums)
    {
        IEnumerable<int> q = [|from int n1 in nums.AsQueryable() select n1|];
    }
}";

            string output = @"
using System.Collections.Generic;
using System.Linq;

class C
{
    void M(IEnumerable<int> nums)
    {
        IEnumerable<int> queryable()
        {
            foreach (int n1 in nums.AsQueryable())
            {
                yield return n1;
            }
        }

        IEnumerable<int> q = queryable();
    }
}";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task IQueryableInInvocation()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

class C
{
    void M(IEnumerable<int> nums)
    {
        int c = ([|from int n1 in nums.AsQueryable() select n1|]).Count();
    }
}";

            string output = @"
using System.Collections.Generic;
using System.Linq;

class C
{
    void M(IEnumerable<int> nums)
    {
        int c = 0;
        foreach (int n1 in nums.AsQueryable())
        {
            c++;
        }
    }
}";

            await TestInRegularAndScriptAsync(source, output);
        }

        #endregion

        #region In ToList

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task PropertyAssignmentInInvocation()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        var c = new C();
        c.A = ([|from x in nums select x + 1|]).ToList();
    }

    class C
    {
        public List<int> A { get; set; }
    }
}";

            string output = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        var c = new C();
        var list = new List<int>();
        foreach (var x in nums)
        {
            list.Add(x + 1);
        }

        c.A = list;
    }

    class C
    {
        public List<int> A { get; set; }
    }
}";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task NullablePropertyAssignmentInInvocation()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        var c = new C();
        c?.A = ([|from x in nums select x + 1|]).ToList();
    }

    class C
    {
        public List<int> A { get; set; }
    }
}";

            string output = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    public static void Main()
    {
        var nums = new int[] { 1, 2, 3, 4 };
        var c = new C();
        var list = new List<int>();
        foreach (var x in nums)
        {
            list.Add(x + 1);
        }

        c?.A = list;
    }

    class C
    {
        public List<int> A { get; set; }
    }
}";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task AssignList()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        var list = ([|from int n1 in nums 
                 from int n2 in nums
                 select n1|]).ToList();
        return list;
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        var list = new List<int>();
        foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                list.Add(n1);
            }
        }

        return list;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task AssignToListToParameter()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums, List<int> list)
    {
        list = ([|from int n1 in nums 
                 from int n2 in nums
                 select n1|]).ToList();
        return list;
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums, List<int> list)
    {
        list = new List<int>();
        foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                list.Add(n1);
            }
        }

        return list;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task AssignToListToArrayElement()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums, List<int>[] lists)
    {
        lists[0] = ([|from int n1 in nums 
                 from int n2 in nums
                 select n1|]).ToList();
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums, List<int>[] lists)
    {
        var list = new List<int>();
        foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                list.Add(n1);
            }
        }

        lists[0] = list;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task AssignListWithTypeArgument()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        var list = ([|from int n1 in nums 
                 from int n2 in nums
                 select n1|]).ToList<int>();
        return list;
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        var list = new List<int>();
        foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                list.Add(n1);
            }
        }

        return list;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task AssignListToObject()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    object M(IEnumerable<int> nums)
    {
        object list = ([|from int n1 in nums 
                 from int n2 in nums
                 select n1|]).ToList<int>();
        return list;
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    object M(IEnumerable<int> nums)
    {
        var list1 = new List<int>();
        foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                list1.Add(n1);
            }
        }

        object list = list1;
        return list;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task AssignListWithNullableToList()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        var list = ([|from int n1 in nums 
                 from int n2 in nums
                 select n1|])?.ToList<int>();
        return list;
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        IEnumerable<int> enumerable()
        {
            foreach (int n1 in nums)
            {
                foreach (int n2 in nums)
                {
                    yield return n1;
                }
            }
        }

        var list = enumerable()?.ToList<int>();
        return list;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task ReturnList()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        return ([|from int n1 in nums 
                 from int n2 in nums
                 select n1|]).ToList();
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        var list = new List<int>();
        foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                list.Add(n1);
            }
        }

        return list;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task ReturnListNameGeneration()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        var list = new List<int>();
        return ([|from int n1 in nums 
                 from int n2 in nums
                 select n1|]).ToList();
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    List<int> M(IEnumerable<int> nums)
    {
        var list = new List<int>();
        var list1 = new List<int>();
        foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                list1.Add(n1);
            }
        }

        return list1;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task ToListTypeReplacement01()
        {
            string source = @"
using System;
using System.Linq;
using C = System.Collections.Generic.List<int>;
class Query
{
    public static void Main(string[] args)
    {
        C c1 = new C { 1, 2, 3 };
        C c2 = new C { 10, 20, 30 };
        C c3 = new C { 100, 200, 300 };
        C r1 = ([|from int x in c1
                from int y in c2
                from int z in c3
                let g = x + y + z
                where (x + y / 10 + z / 100) < 6
                select g|]).ToList();
        Console.WriteLine(r1);
    }
}
";
            string output = @"
using System;
using System.Linq;
using C = System.Collections.Generic.List<int>;
class Query
{
    public static void Main(string[] args)
    {
        C c1 = new C { 1, 2, 3 };
        C c2 = new C { 10, 20, 30 };
        C c3 = new C { 100, 200, 300 };
        C r1 = new C();
        foreach (int x in c1)
        {
            foreach (int y in c2)
            {
                foreach (int z in c3)
                {
                    var g = x + y + z;
                    if (x + y / 10 + z / 100 < 6)
                    {
                        r1.Add(g);
                    }
                }
            }
        }

        Console.WriteLine(r1);
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task ToListTypeReplacement02()
        {
            string source = @"
using System.Linq;
using System;
using C = System.Collections.Generic.List<int>;
class Query
{
    public static void Main(string[] args)
    {
        C c1 = new C { 1, 2, 3 };
        C c2 = new C { 10, 20, 30 };
        C r1 =
            ([|from int x in c1
             join y in c2 on x equals y / 10
             let z = x + y
             select z|]).ToList();
        Console.WriteLine(r1);
    }
}
";
            string output = @"
using System.Linq;
using System;
using C = System.Collections.Generic.List<int>;
class Query
{
    public static void Main(string[] args)
    {
        C c1 = new C { 1, 2, 3 };
        C c2 = new C { 10, 20, 30 };
        C r1 = new C();
        foreach (int x in c1)
        {
            foreach (var y in c2)
            {
                if (Equals(x, y / 10))
                {
                    var z = x + y;
                    r1.Add(z);
                }
            }
        }

        Console.WriteLine(r1);
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task ToListOverloadAssignTo()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    static class Extensions
    {
        public static ref List<int> ToList(this IEnumerable<int> enumerable) => ref enumerable.ToList();
    }
    class Test
    {
        void M()
        {
            ([|from x in new[] { 1 } select x|]).ToList() = new List<int>();
        }
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    static class Extensions
    {
        public static ref List<int> ToList(this IEnumerable<int> enumerable) => ref enumerable.ToList();
    }
    class Test
    {
        void M()
        {
            IEnumerable<int> enumerable()
            {
                foreach (var x in new[] { 1 })
                {
                    yield return x;
                }
            }

            enumerable().ToList() = new List<int>();
        }
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task ToListRefOverload()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    static class Extensions
    {
        public static ref List<int> ToList(this IEnumerable<int> enumerable) => ref enumerable.ToList();
    }
    class Test
    {
        void M()
        {
            var a = ([|from x in new[] { 1 } select x|]).ToList();
        }
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    static class Extensions
    {
        public static ref List<int> ToList(this IEnumerable<int> enumerable) => ref enumerable.ToList();
    }
    class Test
    {
        void M()
        {
            IEnumerable<int> enumerable()
            {
                foreach (var x in new[] { 1 })
                {
                    yield return x;
                }
            }

            var a = enumerable().ToList();
        }
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        #endregion

        #region In Count

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task CountInMultipleDeclaration()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        int i = 0, cnt = ([|from int n1 in nums 
                 from int n2 in nums
                 select n1|]).Count();
        return cnt;
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        IEnumerable<int> enumerable()
        {
            foreach (int n1 in nums)
            {
                foreach (int n2 in nums)
                {
                    yield return n1;
                }
            }
        }

        int i = 0, cnt = enumerable().Count();
        return cnt;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task CountInNonLocalDeclaration()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        for(int i = ([|from int n1 in nums 
                 from int n2 in nums
                 select n1|]).Count(), i < 5; i++)
        {
            Console.WriteLine(i);
        }
    }
}
";

            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        IEnumerable<int> enumerable()
        {
            foreach (int n1 in nums)
            {
                foreach (int n2 in nums)
                {
                    yield return n1;
                }
            }
        }

        for (int i = enumerable().Count(), i < 5; i++)
        {
            Console.WriteLine(i);
        }
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task CountInDeclaration()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        var cnt = ([|from int n1 in nums 
                 from int n2 in nums
                 select n1|]).Count();
        return cnt;
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        var cnt = 0;
        foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                cnt++;
            }
        }

        return cnt;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task ReturnCount()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        return ([|from int n1 in nums 
                 from int n2 in nums
                 select n1|]).Count();
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        var count = 0;
        foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                count++;
            }
        }

        return count;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task ReturnCountExtraParethesis()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        return (([|from int n1 in nums 
                 from int n2 in nums
                 select n1|]).Count());
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        var count = 0;
        foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                count++;
            }
        }

        return count;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task CountAsArgument()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void N(int value) { }
    void M(IEnumerable<int> nums)
    {
        N(([|from int n1 in nums 
                 from int n2 in nums
                 select n1|]).Count());
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void N(int value) { }
    void M(IEnumerable<int> nums)
    {
        IEnumerable<int> enumerable()
        {
            foreach (int n1 in nums)
            {
                foreach (int n2 in nums)
                {
                    yield return n1;
                }
            }
        }

        N(enumerable().Count());
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task CountAsArgumentExpressionBody()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void N(int value) { }
    void M(IEnumerable<int> nums)
        => N(([|from int n1 in nums 
                 from int n2 in nums
                 select n1|]).Count());
}
";

            await TestMissingAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task ReturnCountNameGeneration()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        int count = 1;
        return ([|from int n1 in nums 
                 from int n2 in nums
                 select n1|]).Count();
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        int count = 1;
        var count1 = 0;
        foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                count1++;
            }
        }

        return count1;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task CountNameUsedAfter()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        if (true)
        {
            return ([|from int n1 in nums 
                        from int n2 in nums
                        select n1|]).Count();
        }

        int count = 1;
        return count;
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        if (true)
        {
            var count1 = 0;
            foreach (int n1 in nums)
            {
                foreach (int n2 in nums)
                {
                    count1++;
                }
            }

            return count1;
        }

        int count = 1;
        return count;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task ReturnCountNameUsedBefore()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        if (true)
        {
            int count = 1;
        }

        return ([|from int n1 in nums 
                 from int n2 in nums
                 select n1|]).Count();
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        if (true)
        {
            int count = 1;
        }

        var count1 = 0;
        foreach (int n1 in nums)
        {
            foreach (int n2 in nums)
            {
                count1++;
            }
        }

        return count1;
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task CountOverload()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        var cnt = ([|from int n1 in nums 
                 from int n2 in nums
                 select n1|]).Count(x => x > 2);
        return cnt;
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    int M(IEnumerable<int> nums)
    {
        IEnumerable<int> enumerable()
        {
            foreach (int n1 in nums)
            {
                foreach (int n2 in nums)
                {
                    yield return n1;
                }
            }
        }

        var cnt = enumerable().Count(x => x > 2);
        return cnt;
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task CountOverloadAssignTo()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    static class Extensions
    {
        public static ref int Count(this IEnumerable<int> enumerable) => ref enumerable.Count();
    }
    class Test
    {
        void M()
        {
            ([|from x in new[] { 1 } select x|]).Count() = 5;
        }
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    static class Extensions
    {
        public static ref int Count(this IEnumerable<int> enumerable) => ref enumerable.Count();
    }
    class Test
    {
        void M()
        {
            IEnumerable<int> enumerable()
            {
                foreach (var x in new[] { 1 })
                {
                    yield return x;
                }
            }

            enumerable().Count() = 5;
        }
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task CountRefOverload()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    static class Extensions
    {
        public static ref int Count(this IEnumerable<int> enumerable) => ref enumerable.Count();
    }
    class Test
    {
        void M()
        {
            int a = ([|from x in new[] { 1 } select x|]).Count();
        }
    }
}
";

            string output = @"
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    static class Extensions
    {
        public static ref int Count(this IEnumerable<int> enumerable) => ref enumerable.Count();
    }
    class Test
    {
        void M()
        {
            IEnumerable<int> enumerable()
            {
                foreach (var x in new[] { 1 })
                {
                    yield return x;
                }
            }

            int a = enumerable().Count();
        }
    }
}
";
            await TestInRegularAndScriptAsync(source, output);
        }

        #endregion

        #region Expression Bodied

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task ExpressionBodiedProperty()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    private readonly int[] _nums = new int[] { 1, 2, 3, 4 };
    public IEnumerable<int> Query => [|from x in _nums select x + 1|];
}
";
            // Cannot convert in expression bodied property
            await TestMissingAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task ExpressionBodiedField()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    private static readonly int[] _nums = new int[] { 1, 2, 3, 4 };
    public List<int> Query = ([|from x in _nums select x + 1|]).ToList();
}
";
            await TestMissingAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task Field()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    private static readonly int[] _nums = new int[] { 1, 2, 3, 4 };
    public IEnumerable<int> Query = [|from x in _nums select x + 1|];
}
";
            await TestMissingAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task ExpressionBodiedMethod()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    private readonly int[] _nums = new int[] { 1, 2, 3, 4 };
    public IEnumerable<int> Query() => [|from x in _nums select x + 1|];
}
";
            // Cannot convert in expression bodied method
            await TestMissingAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task ExpressionBodiedMethodUnderInvocation()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    private readonly int[] _nums = new int[] { 1, 2, 3, 4 };
    public List<int> Query() => ([|from x in _nums select x + 1|]).ToList();
}
";
            // Cannot convert in expression bodied method
            await TestMissingAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task ExpressionBodiedenumerable()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    private readonly int[] _nums = new int[] { 1, 2, 3, 4 };
    public void M()
    {
        IEnumerable<int> Query() => [|from x in _nums select x + 1|];
    }
}
";
            // Cannot convert in expression bodied property
            await TestMissingAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task ExpressionBodiedAccessor()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
public class Test
{
    private readonly int[] _nums = new int[] { 1, 2, 3, 4 };
    public IEnumerable<int> Query { get => [|from x in _nums select x + 1|]; }
}
";
            // Cannot convert in expression bodied property
            await TestMissingAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task InInlineLambda()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        Func<IEnumerable<int>> lambda = () => [|from x in new int[] { 1 } select x|];
    }
}
";
            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        IEnumerable<int> enumerable()
        {
            foreach (var x in new int[] { 1 })
            {
                yield return x;
            }
        }

        Func<IEnumerable<int>> lambda = () => enumerable();
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task InParameterLambda()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void N() 
    {
        M([|from x in new int[] { 1 } select x|]);
    }

    void M(IEnumerable<int> nums)
    {
    }
}
";
            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void N() 
    {
        IEnumerable<int> nums()
        {
            foreach (var x in new int[] { 1 })
            {
                yield return x;
            }
        }

        M(nums());
    }

    void M(IEnumerable<int> nums)
    {
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task InParenthesizedLambdaWithBody()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        Func<IEnumerable<int>> lambda = () => { return [|from x in new int[] { 1 } select x|]; };
    }
}
";

            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        Func<IEnumerable<int>> lambda = () => {
            IEnumerable<int> enumerable()
            {
                foreach (var x in new int[] { 1 })
                {
                    yield return x;
                }
            }

            return enumerable(); };
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task InSimplifiedLambdaWithBody()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        Func<int, IEnumerable<int>> lambda = n => { return [|from x in new int[] { 1 } select x|]; };
    }
}
";

            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        Func<int, IEnumerable<int>> lambda = n => {
            IEnumerable<int> enumerable()
            {
                foreach (var x in new int[] { 1 })
                {
                    yield return x;
                }
            }

            return enumerable(); };
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task InAnonymousMethod()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        Func<IEnumerable<int>> a = delegate () { return [|from x in new int[] { 1 } select x|]; };
    }
}
";

            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        Func<IEnumerable<int>> a = delegate () {
            IEnumerable<int> enumerable()
            {
                foreach (var x in new int[] { 1 })
                {
                    yield return x;
                }
            }

            return enumerable(); };
    }
}
";

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task InWhen()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M(IEnumerable<int> nums)
    {
        switch (nums.First())
        {
            case 0 when (nums == [|from x in new int[] { 1 } select x|]):
                return;
            default:
                return;
        }
    }
}
";
            // In when is not supported
            await TestMissingAsync(source);
        }

        #endregion

        #region Comments and Preprocessor directives

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task InlineComments()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<int> M(IEnumerable<int> nums)
    {
        return [|from int n1 in /* comment */ nums 
                 from int n2 in nums
                 select n1|];
    }
}";
            // Cannot convert expressions with comments
            await TestMissingAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task Comments()
        {
            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<int> M(IEnumerable<int> nums)
    {
        return [|from int n1 in nums // comment
                 from int n2 in nums
                 select n1|];
    }
}";
            // Cannot convert expressions with comments
            await TestMissingAsync(source);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task PreprocessorDirectives()
        {

            string source = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<int> M(IEnumerable<int> nums)
    {
        return [|from int n1 in nums
#if (true)
                 from int n2 in nums
#endif
                 select n1|];
    }
}";

            // Cannot convert expressions with preprocessor directives
            await TestMissingAsync(source);
        }

        #endregion

        #region Name Generation

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task EnumerableFunctionDoesNotUseLocalFunctionName()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class Query
{
    public static void Main(string[] args)
    {
        List<int> c = new List<int>{ 1, 2, 3, 4, 5, 6, 7 };
        var r = [|from i in c select i+1|];

        void enumerable() { }
    }
}";
            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class Query
{
    public static void Main(string[] args)
    {
        List<int> c = new List<int>{ 1, 2, 3, 4, 5, 6, 7 };
        IEnumerable<int> enumerable1()
        {
            foreach (var i in c)
            {
                yield return i + 1;
            }
        }

        var r = enumerable1();

        void enumerable() { }
    }
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task EnumerableFunctionCanUseLocalFunctionParameterName()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class Query
{
    public static void Main(string[] args)
    {
        List<int> c = new List<int>{ 1, 2, 3, 4, 5, 6, 7 };
        var r = [|from i in c select i+1|];

        void M(IEnumerable<int> enumerable) { }
    }
}";
            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class Query
{
    public static void Main(string[] args)
    {
        List<int> c = new List<int>{ 1, 2, 3, 4, 5, 6, 7 };
        IEnumerable<int> enumerable()
        {
            foreach (var i in c)
            {
                yield return i + 1;
            }
        }

        var r = enumerable();

        void M(IEnumerable<int> enumerable) { }
    }
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task EnumerableFunctionDoesNotUseLambdaParameterNameWithCSharpLessThan8()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class Query
{
    public static void Main(string[] args)
    {
        List<int> c = new List<int>{ 1, 2, 3, 4, 5, 6, 7 };
        var r = [|from i in c select i+1|];

        Action<int> myLambda = enumerable => { };
    }
}";
            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class Query
{
    public static void Main(string[] args)
    {
        List<int> c = new List<int>{ 1, 2, 3, 4, 5, 6, 7 };
        IEnumerable<int> enumerable1()
        {
            foreach (var i in c)
            {
                yield return i + 1;
            }
        }

        var r = enumerable1();

        Action<int> myLambda = enumerable => { };
    }
}";
            await TestInRegularAndScriptAsync(source, output, parseOptions: new CSharpParseOptions(CodeAnalysis.CSharp.LanguageVersion.CSharp7_3));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
        public async Task EnumerableFunctionCanUseLambdaParameterNameInCSharp8()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
class Query
{
    public static void Main(string[] args)
    {
        List<int> c = new List<int>{ 1, 2, 3, 4, 5, 6, 7 };
        var r = [|from i in c select i+1|];

        Action<int> myLambda = enumerable => { };
    }
}";
            string output = @"
using System;
using System.Collections.Generic;
using System.Linq;
class Query
{
    public static void Main(string[] args)
    {
        List<int> c = new List<int>{ 1, 2, 3, 4, 5, 6, 7 };
        IEnumerable<int> enumerable()
        {
            foreach (var i in c)
            {
                yield return i + 1;
            }
        }

        var r = enumerable();

        Action<int> myLambda = enumerable => { };
    }
}";
            await TestInRegularAndScriptAsync(source, output, parseOptions: new CSharpParseOptions(CodeAnalysis.CSharp.LanguageVersion.CSharp8));
        }

        #endregion
    }
}
