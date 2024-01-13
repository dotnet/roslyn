// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.ConvertLinq
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToForEach)]
    public class ConvertLinqQueryToForEachTests : AbstractCSharpCodeActionTest_NoEditor
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
          => new CodeAnalysis.CSharp.ConvertLinq.CSharpConvertLinqQueryToForEachProvider();

        #region Query Expressions

        [Fact]
        public async Task Select()
        {
            var source = """
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
                }
                """;
            var output = """
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
                }
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task GroupBy01()
        {
            var source = """
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
                }
                """;

            // Group by is not supported
            await TestMissingAsync(source);
        }

        [Fact]
        public async Task GroupBy02()
        {
            var source = """
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
                }
                """;

            // Group by is not supported
            await TestMissingAsync(source);
        }

        [Fact]
        public async Task FromJoinSelect01()
        {
            var source = """
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
                """;
            var output = """
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
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task FromJoinSelect02()
        {
            var source = """
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
                }
                """;
            var output = """
                using System.Linq;

                class Program
                {
                    static void Main(string[] args)
                    {
                        System.Collections.Generic.IEnumerable<int> enumerable()
                        {
                            var ints1 = new int[] { 1, 2 };
                            var ints = new int[] { 3, 4 };
                            foreach (var num in ints1)
                            {
                                foreach (var a in new int[] { 5, 6 })
                                {
                                    foreach (var x1 in ints)
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
                }
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task FromJoinSelect03()
        {
            var source = """
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
                }
                """;
            var output = """
                using System.Linq;

                class Program
                {
                    static void Main(string[] args)
                    {
                        System.Collections.Generic.IEnumerable<int> enumerable()
                        {
                            var ints2 = new int[] { 1, 2 };
                            var ints1 = new int[] { 3, 4 };
                            var ints = new int[] { 7, 8 };
                            foreach (var num in ints2)
                            {
                                foreach (var a in new int[] { 5, 6 })
                                {
                                    foreach (var x1 in ints1)
                                    {
                                        if (object.Equals(num, x1))
                                        {
                                            foreach (var x2 in ints)
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
                }
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task OrderBy()
        {
            var source = """
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
                }
                """;
            // order by is not supported by foreach.
            await TestMissingAsync(source);
        }

        [Fact]
        public async Task Let()
        {
            var source = """
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
                }
                """;
            var output = """
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
                }
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task GroupJoin()
        {
            var source = """
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
                                      select x1 + ":" + g.ToString()|]).ToList();
                        Console.WriteLine(r1);
                    }
                }
                """;
            // GroupJoin is not supported
            await TestMissingAsync(source);
        }

        [Fact]
        public async Task SelectFromType01()
        {
            var source = """
                using System;
                using System.Collections.Generic;

                class C
                {
                    static void Main()
                    {
                        var q = [|from x in C select x|];
                    }

                    static IEnumerable<int> Select<T>(Func<int, T> f) { return null; }
                }
                """;
            var output = """
                using System;
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
                }
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task SelectFromType02()
        {
            var source = """
                using System;
                using System.Collections.Generic;

                class C
                {
                    static void Main()
                    {
                        var q = [|from x in C select x|];
                    }

                    static Func<Func<int, object>, IEnumerable<object>> Select = null;
                }
                """;
            var output = """
                using System;
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
                }
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task JoinClause()
        {
            var source = """
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
                }
                """;
            var output = """
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
                }
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task WhereClause()
        {
            var source = """
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
                            serializer = serializer + q + " ";
                        }
                        System.Console.Write(serializer.Trim());
                    }
                }
                """;

            var output = """
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
                            serializer = serializer + q + " ";
                        }
                        System.Console.Write(serializer.Trim());
                    }
                }
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task WhereDefinedInType()
        {
            var source = """
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
                }
                """;
            //  should not provide a conversion because of the custom Where.
            await TestMissingAsync(source);
        }

        [Fact]
        public async Task QueryContinuation()
        {
            var source = """
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
                }
                """;
            var output = """
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
                }
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task SelectInto()
        {
            var source = """
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
                }
                """;
            var output = """
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
                }
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task ComputeQueryVariableType()
        {
            var source = """
                using System.Linq;
                public class Test
                {
                    public static void Main()
                    {
                        var nums = new int[] { 1, 2, 3, 4 };

                        var q2 = [|from x in nums
                                 select 5|];
                    }
                }
                """;
            var output = """
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
                }
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task JoinIntoClause()
        {
            var source = """
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
                }
                """;
            // GroupJoin is not supported
            await TestMissingAsync(source);
        }

        [Fact]
        public async Task SemanticErrorInQuery()
        {
            var source = """
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
                }
                """;

            // Error: Range variable already being declared.
            await TestMissingAsync(source);
        }

        [Fact]
        public async Task SelectFromVoid()
        {
            var source = """
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
                """;

            await TestMissingAsync(source);
        }

        #endregion

        #region Assignments, Declarations, Returns

        [Fact]
        public async Task AssignExpression()
        {
            var source = """
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
                """;

            var output = """
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
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task MultipleAssignments()
        {
            var source = """
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
                }
                """;

            var output = """
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
                }
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task PropertyAssignment()
        {
            var source = """
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
                }
                """;

            var output = """
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
                }
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task MultipleDeclarationsFirst()
        {
            var source = """
                using System.Collections.Generic;
                using System.Linq;
                public class Test
                {
                    public static void Main()
                    {
                        var nums = new int[] { 1, 2, 3, 4 };
                        IEnumerable<int> q1 = [|from x in nums select x + 1|], q2 = from x in nums select x + 1;
                    }
                }
                """;

            var output = """
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
                }
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task MultipleDeclarationsSecond()
        {
            var source = """
                using System.Collections.Generic;
                using System.Linq;
                public class Test
                {
                    public static void Main()
                    {
                        var nums = new int[] { 1, 2, 3, 4 };
                        IEnumerable<int> q1 = from x in nums select x + 1, q2 = [|from x in nums select x + 1|];
                    }
                }
                """;

            var output = """
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
                }
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        // TODO support tuples in the test class, follow CodeGenTupleTests
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/25639")]
        public async Task TupleDeclaration()
        {
            var source = """
                using System.Collections.Generic;
                using System.Linq;
                public class Test
                {
                    public static void Main()
                    {
                        var nums = new int[] { 1, 2, 3, 4 };
                        var q = ([|from x in nums select x + 1|], from x in nums select x + 1);
                    }
                }
                """;

            var output = """
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
                }
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task AssignAndReturnIEnumerable()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task BlockBodiedProperty()
        {
            var source = """
                using System.Collections.Generic;
                using System.Linq;
                public class Test
                {
                    private readonly int[] _nums = new int[] { 1, 2, 3, 4 };
                    public IEnumerable<int> Query1 { get { return [|from x in _nums select x + 1|]; } }
                }
                """;

            var output = """
                using System.Collections.Generic;
                using System.Linq;
                public class Test
                {
                    private readonly int[] _nums = new int[] { 1, 2, 3, 4 };
                    public IEnumerable<int> Query1 { get { foreach (var x in _nums) { yield return x + 1; } yield break; } }
                }
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task AnonymousType()
        {
            var source = """
                using System.Collections.Generic;
                using System.Linq;
                class C
                {
                    void M(IEnumerable<int> nums)
                    {
                        var q = [|from a in nums from b in nums select new { a, b }|];
                    }
                }
                """;
            // No conversion can be made because it expects to introduce a local function but the return type contains anonymous.
            await TestMissingAsync(source);
        }

        [Fact]
        public async Task AnonymousTypeInternally()
        {
            var source = """
                using System.Collections.Generic;
                using System.Linq;
                class C
                {
                    void M(IEnumerable<int> nums)
                    {
                        var q = [|from a in nums from b in nums select new { a, b } into c select c.a|];
                    }
                }
                """;
            // No conversion can be made because it expects to introduce a local function but the return type contains anonymous.
            await TestMissingAsync(source);
        }

        [Fact]
        public async Task DuplicateIdentifiers()
        {
            var source = """
                using System.Collections.Generic;
                using System.Linq;
                class C
                {
                    void M()
                    {
                        var q = [|from x in new[] { 1 } select x + 2 into x where x > 0 select 7 into y let x = "aaa" select x|];
                    }
                }
                """;
            // Duplicate identifiers are not allowed.
            await TestMissingAsync(source);
        }

        [Fact]
        public async Task ReturnIEnumerable()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task ReturnIEnumerablePartialMethod()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task ReturnIEnumerableExtendedPartialMethod()
        {
            var source = """
                using System.Collections.Generic;
                using System.Linq;
                partial class C
                {
                    public partial IEnumerable<int> M(IEnumerable<int> nums);
                }
                partial class C
                {
                    public partial IEnumerable<int> M(IEnumerable<int> nums)
                    {
                        return [|from int n1 in nums 
                                 from int n2 in nums
                                 select n1|];
                    }
                }
                """;

            var output = """
                using System.Collections.Generic;
                using System.Linq;
                partial class C
                {
                    public partial IEnumerable<int> M(IEnumerable<int> nums);
                }
                partial class C
                {
                    public partial IEnumerable<int> M(IEnumerable<int> nums)
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task ReturnIEnumerableWithOtherReturn()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task ReturnObject()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task ExtraParenthesis()
        {
            var source = """
                using System.Collections.Generic;
                using System.Linq;
                public class Test
                {
                    IEnumerable<int> M()
                    {
                        var nums = new int[] { 1, 2, 3, 4 };
                        return ([|from x in nums select x + 1|]);
                    }
                }
                """;

            var output = """
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
                }
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        // TODO support tuples in the test class
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/25639")]
        public async Task InReturningTuple()
        {
            var source = """
                using System.Collections.Generic;
                using System.Linq;
                public class Test
                {
                    (IEnumerable<int>, int) M(IEnumerable<int> q)
                    {
                        return (([|from a in q select a * a|]), 1);
                    }
                }
                """;

            var output = """
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
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        // TODO support tuples in the test class
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/25639")]
        public async Task InInvocationReturningInTuple()
        {
            var source = """
                using System.Collections.Generic;
                using System.Linq;
                public class Test
                {
                    (int, int) M(IEnumerable<int> q)
                    {
                        return (([|from a in q select a * a|]).Count(), 1);
                    }
                }
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task RangeVariables()
        {
            var source = """
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
                }
                """;
            var output = """
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
                }
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task CallingMethodWithIEnumerable()
        {
            var source = """
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
                """;

            var output = """
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
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task ReturnFirstOrDefault()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task IncompleteQueryWithSyntaxErrors()
        {
            var source = """
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
                """;

            await TestMissingAsync(source);
        }

        [Fact]
        public async Task ErrorNameDoesNotExistsInContext()
        {
            var source = """
                using System.Collections.Generic;
                using System.Linq;
                class C
                {
                    IEnumerable<int> M()
                    {
                        return [|from int n1 in nums select n1|];
                    }
                }
                """;

            await TestMissingAsync(source);
        }

        [Fact]
        public async Task InArrayInitialization()
        {
            var source = """
                using System.Collections.Generic;
                using System.Linq;
                public class Test
                {
                    IEnumerable<int>[] M(IEnumerable<int> q)
                    {
                        return new[] { [|from a in q select a * a|] };
                    }
                }
                """;

            var output = """
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
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task InCollectionInitialization()
        {
            var source = """
                using System.Collections.Generic;
                using System.Linq;
                public class Test
                {
                    List<IEnumerable<int>> M(IEnumerable<int> q)
                    {
                        return new List<IEnumerable<int>> { [|from a in q select a * a|] };
                    }
                }
                """;

            var output = """
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
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task InStructInitialization()
        {
            var source = """
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
                """;

            var output = """
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
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task InClassInitialization()
        {
            var source = """
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
                """;

            var output = """
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
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task InConstructor()
        {
            var source = """
                using System.Collections.Generic;
                using System.Linq;
                public class Test
                {
                    List<int> M(IEnumerable<int> q)
                    {
                        return new List<int>([|from a in q select a * a|]);
                    }
                }
                """;

            var output = """
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
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task InInlineConstructor()
        {
            var source = """
                using System.Collections.Generic;
                using System.Linq;
                public class Test
                {
                    List<int> M(IEnumerable<int> q)
                        => new List<int>([|from a in q select a * a|]);
                }
                """;

            // No support for expression bodied constructors yet.
            await TestMissingAsync(source);
        }

        [Fact]
        public async Task IninlineIf()
        {
            var source = """
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
                """;

            var output = """
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
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        #endregion

        #region In foreach

        [Fact]
        public async Task UsageInForEach()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task UsageInForEachSameVariableName()
        {
            var source = """
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
                """;

            var output = """
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
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task QueryInForEach()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task QueryInForEachSameVariableNameNoType()
        {
            var source = """
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
                """;

            var output = """
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
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task QueryInForEachWithExpressionBody()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task QueryInForEachWithSameVariableNameAndDifferentType()
        {
            var source = """
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
                """;

            var output = """
                using System;
                using System.Collections.Generic;
                using System.Linq;
                class A { }
                class B : A { }
                class C
                {
                    void M(IEnumerable<int> nums)
                    {
                        IEnumerable<B> @as()
                        {
                            foreach (B a in nums)
                            {
                                foreach (A c in nums)
                                {
                                    yield return a;
                                }
                            }
                        }

                        foreach (A a in @as())
                        {
                            Console.Write(a.ToString());
                        }
                    }
                }
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task QueryInForEachWithSameVariableNameAndSameType()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task QueryInForEachVariableUsedInBody()
        {
            var source = """
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
                """;

            var output = """
                using System;
                using System.Collections.Generic;
                using System.Linq;
                class C
                {
                    void M(IEnumerable<int> nums)
                    {
                        IEnumerable<int> bs()
                        {
                            foreach (int n1 in nums)
                            {
                                foreach (int n2 in nums)
                                {
                                    yield return n1;
                                }
                            }
                        }

                        foreach (var b in bs())
                        {
                            int n1 = 5;
                            Console.WriteLine(b);
                        }
                    }
                }
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task QueryInForEachWithConvertedType()
        {
            var source = """
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
                """;

            var output = """
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
                        IEnumerable<C> xes()
                        {
                            foreach (var x in new[] { 1, 2, 3, })
                            {
                                yield return x;
                            }
                        }

                        foreach (int x in xes())
                        {
                            Console.Write(x);
                        }
                    }
                }
                """;
            await TestAsync(source, output, parseOptions: null);
        }

        [Fact]
        public async Task QueryInForEachWithSelectIdentifierButNotVariable()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestAsync(source, output, parseOptions: null);
        }

        [Fact]
        public async Task IQueryable()
        {
            var source = """
                using System.Collections.Generic;
                using System.Linq;

                class C
                {
                    IQueryable<int> M(IEnumerable<int> nums)
                    {
                        return [|from int n1 in nums.AsQueryable() select n1|];
                    }
                }
                """;

            await TestMissingAsync(source);
        }

        [Fact]
        public async Task IQueryableConvertedToIEnumerableInReturn()
        {
            var source = """
                using System.Collections.Generic;
                using System.Linq;

                class C
                {
                    IEnumerable<int> M(IEnumerable<int> nums)
                    {
                        return [|from int n1 in nums.AsQueryable() select n1|];
                    }
                }
                """;
            var output = """
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
                }
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task IQueryableConvertedToIEnumerableInAssignment()
        {
            var source = """
                using System.Collections.Generic;
                using System.Linq;

                class C
                {
                    void M(IEnumerable<int> nums)
                    {
                        IEnumerable<int> q = [|from int n1 in nums.AsQueryable() select n1|];
                    }
                }
                """;

            var output = """
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
                }
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task IQueryableInInvocation()
        {
            var source = """
                using System.Collections.Generic;
                using System.Linq;

                class C
                {
                    void M(IEnumerable<int> nums)
                    {
                        int c = ([|from int n1 in nums.AsQueryable() select n1|]).Count();
                    }
                }
                """;

            var output = """
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
                }
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        #endregion

        #region In ToList

        [Fact]
        public async Task PropertyAssignmentInInvocation()
        {
            var source = """
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
                }
                """;

            var output = """
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
                }
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task NullablePropertyAssignmentInInvocation()
        {
            var source = """
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
                }
                """;

            var output = """
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
                }
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task AssignList()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task AssignToListToParameter()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task AssignToListToArrayElement()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task AssignListWithTypeArgument()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task AssignListToObject()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task AssignListWithNullableToList()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task ReturnList()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task ReturnListNameGeneration()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task ToListTypeReplacement01()
        {
            var source = """
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
                """;
            var output = """
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
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task ToListTypeReplacement02()
        {
            var source = """
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
                """;
            var output = """
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
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task ToListOverloadAssignTo()
        {
            var source = """
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
                """;

            var output = """
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
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task ToListRefOverload()
        {
            var source = """
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
                """;

            var output = """
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
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        #endregion

        #region In Count

        [Fact]
        public async Task CountInMultipleDeclaration()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task CountInNonLocalDeclaration()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task CountInDeclaration()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task ReturnCount()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task ReturnCountExtraParethesis()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task CountAsArgument()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task CountAsArgumentExpressionBody()
        {
            var source = """
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
                """;

            await TestMissingAsync(source);
        }

        [Fact]
        public async Task ReturnCountNameGeneration()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task CountNameUsedAfter()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task ReturnCountNameUsedBefore()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task CountOverload()
        {
            var source = """
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
                """;

            var output = """
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
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task CountOverloadAssignTo()
        {
            var source = """
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
                """;

            var output = """
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
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task CountRefOverload()
        {
            var source = """
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
                """;

            var output = """
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
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        #endregion

        #region Expression Bodied

        [Fact]
        public async Task ExpressionBodiedProperty()
        {
            var source = """
                using System.Collections.Generic;
                using System.Linq;
                public class Test
                {
                    private readonly int[] _nums = new int[] { 1, 2, 3, 4 };
                    public IEnumerable<int> Query => [|from x in _nums select x + 1|];
                }
                """;
            // Cannot convert in expression bodied property
            await TestMissingAsync(source);
        }

        [Fact]
        public async Task ExpressionBodiedField()
        {
            var source = """
                using System.Collections.Generic;
                using System.Linq;
                public class Test
                {
                    private static readonly int[] _nums = new int[] { 1, 2, 3, 4 };
                    public List<int> Query = ([|from x in _nums select x + 1|]).ToList();
                }
                """;
            await TestMissingAsync(source);
        }

        [Fact]
        public async Task Field()
        {
            var source = """
                using System.Collections.Generic;
                using System.Linq;
                public class Test
                {
                    private static readonly int[] _nums = new int[] { 1, 2, 3, 4 };
                    public IEnumerable<int> Query = [|from x in _nums select x + 1|];
                }
                """;
            await TestMissingAsync(source);
        }

        [Fact]
        public async Task ExpressionBodiedMethod()
        {
            var source = """
                using System.Collections.Generic;
                using System.Linq;
                public class Test
                {
                    private readonly int[] _nums = new int[] { 1, 2, 3, 4 };
                    public IEnumerable<int> Query() => [|from x in _nums select x + 1|];
                }
                """;
            // Cannot convert in expression bodied method
            await TestMissingAsync(source);
        }

        [Fact]
        public async Task ExpressionBodiedMethodUnderInvocation()
        {
            var source = """
                using System.Collections.Generic;
                using System.Linq;
                public class Test
                {
                    private readonly int[] _nums = new int[] { 1, 2, 3, 4 };
                    public List<int> Query() => ([|from x in _nums select x + 1|]).ToList();
                }
                """;
            // Cannot convert in expression bodied method
            await TestMissingAsync(source);
        }

        [Fact]
        public async Task ExpressionBodiedenumerable()
        {
            var source = """
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
                """;
            // Cannot convert in expression bodied property
            await TestMissingAsync(source);
        }

        [Fact]
        public async Task ExpressionBodiedAccessor()
        {
            var source = """
                using System.Collections.Generic;
                using System.Linq;
                public class Test
                {
                    private readonly int[] _nums = new int[] { 1, 2, 3, 4 };
                    public IEnumerable<int> Query { get => [|from x in _nums select x + 1|]; }
                }
                """;
            // Cannot convert in expression bodied property
            await TestMissingAsync(source);
        }

        [Fact]
        public async Task InInlineLambda()
        {
            var source = """
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
                """;
            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task InParameterLambda()
        {
            var source = """
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
                """;
            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task InParenthesizedLambdaWithBody()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task InSimplifiedLambdaWithBody()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task InAnonymousMethod()
        {
            var source = """
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
                """;

            var output = """
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
                """;

            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task InWhen()
        {
            var source = """
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
                """;
            // In when is not supported
            await TestMissingAsync(source);
        }

        #endregion

        #region Comments and Preprocessor directives

        [Fact]
        public async Task InlineComments()
        {
            var source = """
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
                }
                """;
            // Cannot convert expressions with comments
            await TestMissingAsync(source);
        }

        [Fact]
        public async Task Comments()
        {
            var source = """
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
                }
                """;
            // Cannot convert expressions with comments
            await TestMissingAsync(source);
        }

        [Fact]
        public async Task PreprocessorDirectives()
        {

            var source = """
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
                }
                """;

            // Cannot convert expressions with preprocessor directives
            await TestMissingAsync(source);
        }

        #endregion

        #region Name Generation

        [Fact]
        public async Task EnumerableFunctionDoesNotUseLocalFunctionName()
        {
            var source = """
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
                }
                """;
            var output = """
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
                }
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task EnumerableFunctionCanUseLocalFunctionParameterName()
        {
            var source = """
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
                }
                """;
            var output = """
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
                }
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact]
        public async Task EnumerableFunctionDoesNotUseLambdaParameterNameWithCSharpLessThan8()
        {
            var source = """
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
                }
                """;
            var output = """
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
                }
                """;
            await TestInRegularAndScriptAsync(source, output, parseOptions: new CSharpParseOptions(CodeAnalysis.CSharp.LanguageVersion.CSharp7_3));
        }

        [Fact]
        public async Task EnumerableFunctionCanUseLambdaParameterNameInCSharp8()
        {
            var source = """
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
                }
                """;
            var output = """
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
                }
                """;
            await TestInRegularAndScriptAsync(source, output, parseOptions: new CSharpParseOptions(CodeAnalysis.CSharp.LanguageVersion.CSharp8));
        }

        #endregion

        #region CaretSelection
        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task DeclarationSelection()
        {
            var source = """
                using System;
                using System.Collections.Generic;
                using System.Linq;
                class Query
                {
                    public static void Main(string[] args)
                    {
                        List<int> c = new List<int>{ 1, 2, 3, 4, 5, 6, 7 };
                        var r = [|from i in c select i+1;|]
                    }
                }
                """;
            var output = """
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
                }
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/35180")]
        public async Task LocalAssignmentSelection()
        {
            var source = """
                using System;
                using System.Collections.Generic;
                using System.Linq;
                class Query
                {
                    public static void Main(string[] args)
                    {
                        List<int> c = new List<int>{ 1, 2, 3, 4, 5, 6, 7 };
                        IEnumerable<int> r;
                        [|r = from i in c select i+1;|]
                    }
                }
                """;
            var output = """
                using System;
                using System.Collections.Generic;
                using System.Linq;
                class Query
                {
                    public static void Main(string[] args)
                    {
                        List<int> c = new List<int>{ 1, 2, 3, 4, 5, 6, 7 };
                        IEnumerable<int> r;
                        IEnumerable<int> enumerable()
                        {
                            foreach (var i in c)
                            {
                                yield return i + 1;
                            }
                        }

                        r = enumerable();
                    }
                }
                """;
            await TestInRegularAndScriptAsync(source, output);
        }

        #endregion
    }
}
