// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.ConvertLinq;

[Trait(Traits.Feature, Traits.Features.CodeActionsConvertForEachToQuery)]
public sealed class ConvertForEachToLinqQueryTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CodeAnalysis.CSharp.ConvertLinq.ConvertForEachToLinqQuery.CSharpConvertForEachToLinqQueryProvider();

    #region Query Expressions

    [Fact]
    public async Task QueryForForWhere()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class Query
            {
                public IEnumerable<int> void Main(string[] args)
                {
                    List<int> c1 = new List<int>{1, 2, 3, 4, 5, 7};
                    List<int> c2 = new List<int>{10, 30, 40, 50, 60, 70};
                    [|foreach (var x1 in c1)
                    {
                        foreach (var x2 in c2)
                        {
                            if (object.Equals(x1, x2 / 10))
                            {
                                yield return x1 + x2;
                            }
                        }
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class Query
            {
                public IEnumerable<int> void Main(string[] args)
                {
                    List<int> c1 = new List<int>{1, 2, 3, 4, 5, 7};
                    List<int> c2 = new List<int>{10, 30, 40, 50, 60, 70};
                    return from x1 in c1
                           from x2 in c2
                           where object.Equals(x1, x2 / 10)
                           select x1 + x2;
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class Query
            {
                public IEnumerable<int> void Main(string[] args)
                {
                    List<int> c1 = new List<int>{1, 2, 3, 4, 5, 7};
                    List<int> c2 = new List<int>{10, 30, 40, 50, 60, 70};
                    return c1.SelectMany(x1 => c2.Where(x2 => object.Equals(x1, x2 / 10)).Select(x2 => x1 + x2));
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task QueryWithEscapedSymbols()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class Query
            {
                public IEnumerable<int> void Main(string[] args)
                {
                    List<int> c1 = new List<int>{1, 2, 3, 4, 5, 7};
                    List<int> c2 = new List<int>{10, 30, 40, 50, 60, 70};
                    [|foreach (var @object in c1)
                    {
                        foreach (var x2 in c2)
                        {
                            yield return @object + x2;
                        }
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class Query
            {
                public IEnumerable<int> void Main(string[] args)
                {
                    List<int> c1 = new List<int>{1, 2, 3, 4, 5, 7};
                    List<int> c2 = new List<int>{10, 30, 40, 50, 60, 70};
                    return from @object in c1
                           from x2 in c2
                           select @object + x2;
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class Query
            {
                public IEnumerable<int> void Main(string[] args)
                {
                    List<int> c1 = new List<int>{1, 2, 3, 4, 5, 7};
                    List<int> c2 = new List<int>{10, 30, 40, 50, 60, 70};
                    return c1.SelectMany(@object => c2.Select(x2 => @object + x2));
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task QueryForVarForWhere()
    {
        var source = """
            using System.Linq;

            class C
            {
                IEnumerable<int> M()
                {
                    [|foreach (var num in new int[] { 1, 2 })
                    {
                        var n1 = num + 1;
                        foreach (var a in new int[] { 5, 6 })
                        {
                            foreach (var x1 in new int[] { 3, 4 })
                            {
                                if (object.Equals(num, x1))
                                {
                                    foreach (var x2 in new int[] { 7, 8 })
                                    {
                                        if (object.Equals(num, x2))
                                        {
                                            var n2 = x2 - 1;
                                            yield return n2 + n1;
                                        }
                                    }
                                }
                            }
                        }|]
                    }
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Linq;

            class C
            {
                IEnumerable<int> M()
                {
                    return from num in new int[] { 1, 2 }
                           let n1 = num + 1
                           from a in new int[] { 5, 6 }
                           from x1 in new int[] { 3, 4 }
                           where object.Equals(num, x1)
                           from x2 in new int[] { 7, 8 }
                           where object.Equals(num, x2)
                           let n2 = x2 - 1
                           select n2 + n1;
                }
            }
            """, index: 0);

        // No linq refactoring offered due to variable declaration within the outermost foreach.
        await TestActionCountAsync(source, count: 1);
    }

    [Fact]
    public async Task QueryForVarForWhere_02()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                IEnumerable<int> M()
                {
                    [|foreach (var num in new int[] { 1, 2 })
                    {
                        foreach (var a in new int[] { 5, 6 })
                        {
                            foreach (var x1 in new int[] { 3, 4 })
                            {
                                if (object.Equals(num, x1))
                                {
                                    foreach (var x2 in new int[] { 7, 8 })
                                    {
                                        if (object.Equals(num, x2))
                                        {
                                            var n1 = num + 1;
                                            var n2 = x2 - 1;
                                            yield return n2 + n1;
                                        }
                                    }
                                }
                            }
                        }|]
                    }
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                IEnumerable<int> M()
                {
                    return from num in new int[] { 1, 2 }
                           from a in new int[] { 5, 6 }
                           from x1 in new int[] { 3, 4 }
                           where object.Equals(num, x1)
                           from x2 in new int[] { 7, 8 }
                           where object.Equals(num, x2)
                           let n1 = num + 1
                           let n2 = x2 - 1
                           select n2 + n1;
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                IEnumerable<int> M()
                {
                    foreach (var (num, x2) in (new int[] { 1, 2 }).SelectMany(num => (new int[] { 5, 6 }).SelectMany(a => (new int[] { 3, 4 }).Where(x1 => object.Equals(num, x1)).SelectMany(x1 => (new int[] { 7, 8 }).Where(x2 => object.Equals(num, x2)).Select(x2 => (num, x2))))))
                    {
                        var n1 = num + 1;
                        var n2 = x2 - 1;
                        yield return n2 + n1;
                    }
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task QueryForVarForWhere_03()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                IEnumerable<int> M()
                {
                    [|foreach (var num in new int[] { 1, 2 })
                    {
                        foreach (var a in new int[] { 5, 6 })
                        {
                            foreach (var x1 in new int[] { 3, 4 })
                            {
                                var n1 = num + 1;
                                if (object.Equals(num, x1))
                                {
                                    foreach (var x2 in new int[] { 7, 8 })
                                    {
                                        if (object.Equals(num, x2))
                                        {
                                            var n2 = x2 - 1;
                                            yield return n2 + n1;
                                        }
                                    }
                                }
                            }
                        }|]
                    }
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                IEnumerable<int> M()
                {
                    return from num in new int[] { 1, 2 }
                           from a in new int[] { 5, 6 }
                           from x1 in new int[] { 3, 4 }
                           let n1 = num + 1
                           where object.Equals(num, x1)
                           from x2 in new int[] { 7, 8 }
                           where object.Equals(num, x2)
                           let n2 = x2 - 1
                           select n2 + n1;
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                IEnumerable<int> M()
                {
                    foreach (var (num, x1) in (new int[] { 1, 2 }).SelectMany(num => (new int[] { 5, 6 }).SelectMany(a => (new int[] { 3, 4 }).Select(x1 => (num, x1)))))
                    {
                        var n1 = num + 1;
                        if (object.Equals(num, x1))
                        {
                            foreach (var x2 in new int[] { 7, 8 })
                            {
                                if (object.Equals(num, x2))
                                {
                                    var n2 = x2 - 1;
                                    yield return n2 + n1;
                                }
                            }
                        }
                    }
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task QueryLet()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class Query
            {
                void M()
                {
                    List<int> c1 = new List<int>{ 1, 2, 3 };
                    List<int> r1 = new List<int>();
                    [|foreach (int x in c1)
                    {
                        var g = x * 10;
                        var z = g + x*100;
                        var a = 5 + z;
                        r1.Add(x + z - a);
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class Query
            {
                void M()
                {
                    List<int> c1 = new List<int>{ 1, 2, 3 };
                    List<int> r1 = (from int x in c1
                                    let g = x * 10
                                    let z = g + x * 100
                                    let a = 5 + z
                                    select x + z - a).ToList();
                }
            }
            """, index: 0);

        // No linq invocation refactoring offered due to variable declaration(s) in topmost foreach.
        await TestActionCountAsync(source, count: 1);
    }

    [Fact]
    public Task QueryEmptyDeclarations()
        => TestMissingInRegularAndScriptAsync("""
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class Query
            {
                void M()
                {
                    [|foreach (int x in new[] {1,2})
                    {
                        int a = 3, b, c = 1;
                        if (x > c)
                        {
                            b = 0;
                            Console.Write(a + x + b);
                        }
                    }|]
                }
            }
            """);

    [Fact]
    public async Task QueryWhereClause()
    {
        var source = """
            using System;
            using System.Linq;
            class C
            {
                IEnumerable<int> M()
                {
                    var nums = new int[] { 1, 2, 3, 4 };
                    [|foreach (var x in nums)
                    {
                        if (x > 2)
                        {
                            yield return x;
                        }
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System;
            using System.Linq;
            class C
            {
                IEnumerable<int> M()
                {
                    var nums = new int[] { 1, 2, 3, 4 };
                    return from x in nums
                           where x > 2
                           select x;
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System;
            using System.Linq;
            class C
            {
                IEnumerable<int> M()
                {
                    var nums = new int[] { 1, 2, 3, 4 };
                    return nums.Where(x => x > 2);
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task QueryOverQueries()
    {
        var source = """
            using System;
            using System.Linq;
            class C
            {
                IEnumerable<int> M()
                {
                    var nums = new int[] { 1, 2, 3, 4 };
                    [|foreach (var y in from x in nums select x)
                    {
                        foreach (var z in from x in nums select x)
                        {
                            yield return y;
                        }
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System;
            using System.Linq;
            class C
            {
                IEnumerable<int> M()
                {
                    var nums = new int[] { 1, 2, 3, 4 };
                    return from y in
                               from x in nums select x
                           from z in
                               from x in nums select x
                           select y;
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System;
            using System.Linq;
            class C
            {
                IEnumerable<int> M()
                {
                    var nums = new int[] { 1, 2, 3, 4 };
                    return (from x in nums select x).SelectMany(y => (from x in nums select x).Select(z => y));
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task QueryNoVariablesUsed()
    {
        var source = """
            using System;
            using System.Linq;
            class C
            {
                void M()
                {
                    [|foreach (var a in new[] { 1 })
                    {
                        foreach (var b in new[] { 2 })
                        {
                            System.Console.Write(0);
                        }
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System;
            using System.Linq;
            class C
            {
                void M()
                {
                    foreach (var _ in from a in new[] { 1 }
                                      from b in new[] { 2 }
                                      select new { })
                    {
                        System.Console.Write(0);
                    }
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System;
            using System.Linq;
            class C
            {
                void M()
                {
                    foreach (var _ in (new[] { 1 }).SelectMany(a => (new[] { 2 }).Select(b => new { })))
                    {
                        System.Console.Write(0);
                    }
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task QueryNoBlock()
    {
        var source = """
            using System;
            using System.Linq;
            class C
            {
                void M()
                {
                    [|foreach (var a in new[] { 1 })
                        foreach (var b in new[] { 2 })
                            System.Console.Write(a);|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System;
            using System.Linq;
            class C
            {
                void M()
                {
                    foreach (var a in from a in new[] { 1 }
                                      from b in new[] { 2 }
                                      select a)
                    {
                        System.Console.Write(a);
                    }
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System;
            using System.Linq;
            class C
            {
                void M()
                {
                    foreach (var a in (new[] { 1 }).SelectMany(a => (new[] { 2 }).Select(b => a)))
                    {
                        System.Console.Write(a);
                    }
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task QuerySelectExpression()
    {
        var source = """
            using System;
            using System.Linq;
            class C
            {
                void M()
                {
                    [|foreach (var a in new[] { 1 })
                        foreach (var b in new[] { 2 })
                            Console.Write(a + b);|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System;
            using System.Linq;
            class C
            {
                void M()
                {
                    foreach (var (a, b) in from a in new[] { 1 }
                                           from b in new[] { 2 }
                                           select (a, b))
                    {
                        Console.Write(a + b);
                    }
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System;
            using System.Linq;
            class C
            {
                void M()
                {
                    foreach (var (a, b) in (new[] { 1 }).SelectMany(a => (new[] { 2 }).Select(b => (a, b))))
                    {
                        Console.Write(a + b);
                    }
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task QuerySelectMultipleExpressions()
    {
        var source = """
            using System;
            using System.Linq;
            class C
            {
                void M()
                {
                    [|foreach (var a in new[] { 1 })
                        foreach (var b in new[] { 2 })
                        {
                            Console.Write(a + b);
                            Console.Write(a * b);
                        }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System;
            using System.Linq;
            class C
            {
                void M()
                {
                    foreach (var (a, b) in from a in new[] { 1 }
                                           from b in new[] { 2 }
                                           select (a, b))
                    {
                        Console.Write(a + b);
                        Console.Write(a * b);
                    }
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System;
            using System.Linq;
            class C
            {
                void M()
                {
                    foreach (var (a, b) in (new[] { 1 }).SelectMany(a => (new[] { 2 }).Select(b => (a, b))))
                    {
                        Console.Write(a + b);
                        Console.Write(a * b);
                    }
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task EmptyBody()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                        }
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    foreach (var _ in from int n1 in nums
                                      from int n2 in nums
                                      select new { })
                    {
                    }
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    foreach (var _ in nums.SelectMany(n1 => nums.Select(n2 => new { })))
                    {
                    }
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task EmptyBodyNoBlock()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums);
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    foreach (var _ in from int n1 in nums
                                      from int n2 in nums
                                      select new { })
                    {
                    }
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    foreach (var _ in nums.SelectMany(n1 => nums.Select(n2 => new { })))
                    {
                    }
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task AddUsingToExistingList()
    {
        var source = """
            using System.Collections.Generic;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums);
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(IEnumerable<int> nums)
                {
                    foreach (var _ in from int n1 in nums
                                      from int n2 in nums
                                      select new { })
                    {
                    }
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(IEnumerable<int> nums)
                {
                    foreach (var _ in nums.SelectMany(n1 => nums.Select(n2 => new { })))
                    {
                    }
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task AddFirstUsing()
    {
        var source = """
            class C
            {
                void M(int[] nums)
                {
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums);
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Linq;

            class C
            {
                void M(int[] nums)
                {
                    foreach (var _ in from int n1 in nums
                                      from int n2 in nums
                                      select new { })
                    {
                    }
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Linq;

            class C
            {
                void M(int[] nums)
                {
                    foreach (var _ in nums.SelectMany(n1 => nums.Select(n2 => new { })))
                    {
                    }
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task EmptyBodyDeclarationAsLast()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            var a = n1 + n2;
                        }
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    foreach (var _ in from int n1 in nums
                                      from int n2 in nums
                                      let a = n1 + n2
                                      select new { })
                    {
                    }
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    foreach (var (n1, n2) in nums.SelectMany(n1 => nums.Select(n2 => (n1, n2))))
                    {
                        var a = n1 + n2;
                    }
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task EmptyBodyMultipleDeclarationsAsLast()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            int a = n1 + n2, b = n1 * n2;
                        }
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    foreach (var _ in from int n1 in nums
                                      from int n2 in nums
                                      let a = n1 + n2
                                      let b = n1 * n2
                                      select new { })
                    {
                    }
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    foreach (var (n1, n2) in nums.SelectMany(n1 => nums.Select(n2 => (n1, n2))))
                    {
                        int a = n1 + n2, b = n1 * n2;
                    }
                }
            }
            """, index: 1);
    }

    #endregion

    #region Assignments, Declarations, Returns

    [Fact]
    public async Task ReturnInvocationAndYieldReturn()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                IEnumerable<int> M(IEnumerable<int> nums)
                {
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            yield return N(n1);
                        }
                    }|]
                }

                int N(int n) => n;
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                IEnumerable<int> M(IEnumerable<int> nums)
                {
                    return from int n1 in nums
                           from int n2 in nums
                           select N(n1);
                }

                int N(int n) => n;
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                IEnumerable<int> M(IEnumerable<int> nums)
                {
                    return nums.SelectMany(n1 => nums.Select(n2 => N(n1)));
                }

                int N(int n) => n;
            }
            """, index: 1);
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
                public IEnumerable<int> Query1 { get { [|foreach (var x in _nums) { yield return x + 1; }|] } }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            public class Test
            {
                private readonly int[] _nums = new int[] { 1, 2, 3, 4 };
                public IEnumerable<int> Query1 { get { return from x in _nums select x + 1; } }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            public class Test
            {
                private readonly int[] _nums = new int[] { 1, 2, 3, 4 };
                public IEnumerable<int> Query1 { get { return _nums.Select(x => x + 1); } }
            }
            """, index: 1);
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
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            yield return n1;
                        }
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                IEnumerable<int> M(IEnumerable<int> nums)
                {
                    return from int n1 in nums
                           from int n2 in nums
                           select n1;
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                IEnumerable<int> M(IEnumerable<int> nums)
                {
                    return nums.SelectMany(n1 => nums.Select(n2 => n1));
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task ReturnIEnumerableWithYieldReturnAndLocalFunction()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                IEnumerable<IEnumerable<int>> M(IEnumerable<int> nums)
                {
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            yield return f(n1);
                        }
                    }|]

                    yield break;

                    IEnumerable<int> f(int a)
                    {
                        yield return a;
                    }
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                IEnumerable<IEnumerable<int>> M(IEnumerable<int> nums)
                {
                    return from int n1 in nums
                           from int n2 in nums
                           select f(n1);
                    IEnumerable<int> f(int a)
                    {
                        yield return a;
                    }
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                IEnumerable<IEnumerable<int>> M(IEnumerable<int> nums)
                {
                    return nums.SelectMany(n1 => nums.Select(n2 => f(n1)));
                    IEnumerable<int> f(int a)
                    {
                        yield return a;
                    }
                }
            }
            """, index: 1);
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
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            yield return n1;
                        }
                    }|]

                    yield break;
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
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
                    return from int n1 in nums
                           from int n2 in nums
                           select n1;
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
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
                    return nums.SelectMany(n1 => nums.Select(n2 => n1));
                }
            }
            """, index: 1);
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
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            yield return n1;
                        }
                    }|]

                    yield break;
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
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
                    return from int n1 in nums
                           from int n2 in nums
                           select n1;
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
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
                    return nums.SelectMany(n1 => nums.Select(n2 => n1));
                }
            }
            """, index: 1);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31784")]
    public Task QueryWhichRequiresSelectManyWithIdentityLambda()
        => TestInRegularAndScriptAsync("""
            using System.Collections.Generic;

            class C
            {
                IEnumerable<int> M()
                {
                    [|foreach (var x in new[] { new[] { 1, 2, 3 }, new[] { 4, 5, 6 } })
                    {
                        foreach (var y in x)
                        {
                            yield return y;
                        }
                    }|]
                }
            }
            """, """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                IEnumerable<int> M()
                {
                    return (new[] { new[] { 1, 2, 3 }, new[] { 4, 5, 6 } }).SelectMany(x => x);
                }
            }
            """, index: 1);

    #endregion

    #region In foreach

    [Fact]
    public async Task QueryInForEachWithSameVariableNameAndDifferentType()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class A
            {
                public static implicit operator int(A x)
                {
                    throw null;
                }

                public static implicit operator A(int x)
                {
                    throw null;
                }
            }
            class B : A { }
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    [|foreach (B a in nums)
                    {
                        foreach (A c in nums)
                        {
                            Console.Write(a.ToString());
                        }
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class A
            {
                public static implicit operator int(A x)
                {
                    throw null;
                }

                public static implicit operator A(int x)
                {
                    throw null;
                }
            }
            class B : A { }
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    foreach (var a in from B a in nums
                                      from A c in nums
                                      select a)
                    {
                        Console.Write(a.ToString());
                    }
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class A
            {
                public static implicit operator int(A x)
                {
                    throw null;
                }

                public static implicit operator A(int x)
                {
                    throw null;
                }
            }
            class B : A { }
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    foreach (var a in nums.SelectMany(a => nums.Select(c => a)))
                    {
                        Console.Write(a.ToString());
                    }
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task QueryInForEachWithSameVariableNameAndSameType()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class A
            {
                public static implicit operator int(A x)
                {
                    throw null;
                }

                public static implicit operator A(int x)
                {
                    throw null;
                }
            }
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    [|foreach (A a in nums)
                    {
                        foreach (A c in nums)
                        {
                            Console.Write(a.ToString());
                        }
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class A
            {
                public static implicit operator int(A x)
                {
                    throw null;
                }

                public static implicit operator A(int x)
                {
                    throw null;
                }
            }
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    foreach (var a in from A a in nums
                                      from A c in nums
                                      select a)
                    {
                        Console.Write(a.ToString());
                    }
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class A
            {
                public static implicit operator int(A x)
                {
                    throw null;
                }

                public static implicit operator A(int x)
                {
                    throw null;
                }
            }
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    foreach (var a in nums.SelectMany(a => nums.Select(c => a)))
                    {
                        Console.Write(a.ToString());
                    }
                }
            }
            """, index: 1);
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

                IEnumerable<C> Test()
                {
                    [|foreach (var x in new[] { 1, 2, 3 })
                    {
                        yield return x;
                    }|]
                }
            }
            """;
        await TestAsync(source, """
            using System;
            using System.Collections.Generic;
            using System.Linq;

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

                IEnumerable<C> Test()
                {
                    return from x in new[] { 1, 2, 3 }
                           select x;
                }
            }
            """, new(parseOptions: null));
        await TestInRegularAndScriptAsync(source, """
            using System;
            using System.Collections.Generic;
            using System.Linq;

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

                IEnumerable<C> Test()
                {
                    return new[] { 1, 2, 3 };
                }
            }
            """, index: 1);
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
                    [|foreach (int n1 in nums.AsQueryable())
                    {
                        yield return n1;
                    }|]

                    yield break;
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                IEnumerable<int> M(IEnumerable<int> nums)
                {
                    return from int n1 in nums.AsQueryable()
                           select n1;
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                IEnumerable<int> M(IEnumerable<int> nums)
                {
                    return nums.AsQueryable();
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task ReturnIQueryableConvertedToIEnumerableInAssignment()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                IEnumerable<int> M(IEnumerable<int> nums)
                {
                    [|foreach (int n1 in nums.AsQueryable())
                    {
                        yield return n1;
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                IEnumerable<int> M(IEnumerable<int> nums)
                {
                    return from int n1 in nums.AsQueryable()
                           select n1;
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                IEnumerable<int> M(IEnumerable<int> nums)
                {
                    return nums.AsQueryable();
                }
            }
            """, index: 1);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80781")]
    public Task TestStatementTrailingTrivia1()
        => TestInRegularAndScriptAsync("""
            using System.Collections.Generic;
            using System.Linq;

            class D
            {
                public void M(IEnumerable<string> strings)
                {
                    [|foreach (var x in Enumerable.Empty<string>())
                    {
                        bool b = true;
                        A(); // A
                    }|]
                }

                void A() { }
            }
            """, """
            using System.Collections.Generic;
            using System.Linq;
            
            class D
            {
                public void M(IEnumerable<string> strings)
                {
                    foreach (var _ in from x in Enumerable.Empty<string>()
                                      let b = true
                                      select new { })
                    {
                        A(); // A
                    }
                }
            
                void A() { }
            }
            """, index: 0);

    #endregion

    #region In ToList

    [Fact]
    public async Task ToListLastDeclarationMerge()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums)
                {
                    List<int> list0 = new List<int>(), list = new List<int>();
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            list.Add(n1);
                        }
                    }|]

                    return list;
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums)
                {
                    List<int> list0 = new List<int>();
                    return (from int n1 in nums
                            from int n2 in nums
                            select n1).ToList();
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums)
                {
                    List<int> list0 = new List<int>();
                    return nums.SelectMany(n1 => nums.Select(n2 => n1)).ToList();
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task ToListParameterizedConstructor()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums)
                {
                    List<int> list = new List<int>(nums);
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            list.Add(n1);
                        }
                    }|]

                    return list;
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums)
                {
                    List<int> list = new List<int>(nums);
                    list.AddRange(from int n1 in nums
                                  from int n2 in nums
                                  select n1);
                    return list;
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums)
                {
                    List<int> list = new List<int>(nums);
                    list.AddRange(nums.SelectMany(n1 => nums.Select(n2 => n1)));
                    return list;
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task ToListWithListInitializer()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums)
                {
                    List<int> list = new List<int>() { 1, 2, 3 };
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            list.Add(n1);
                        }
                    }|]

                    return list;
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums)
                {
                    List<int> list = new List<int>() { 1, 2, 3 };
                    list.AddRange(from int n1 in nums
                                  from int n2 in nums
                                  select n1);
                    return list;
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums)
                {
                    List<int> list = new List<int>() { 1, 2, 3 };
                    list.AddRange(nums.SelectMany(n1 => nums.Select(n2 => n1)));
                    return list;
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task ToListWithEmptyArgumentList()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums)
                {
                    List<int> list = new List<int> { };
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            list.Add(n1);
                        }
                    }|]

                    return list;
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums)
                {
                    return (from int n1 in nums
                            from int n2 in nums
                            select n1).ToList();
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums)
                {
                    return nums.SelectMany(n1 => nums.Select(n2 => n1)).ToList();
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task ToListNotLastDeclaration()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums)
                {
                    List<int> list = new List<int>(), list1 = new List<int>();
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            list.Add(n1);
                        }
                    }|]

                    return list;
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums)
                {
                    List<int> list = new List<int>(), list1 = new List<int>();
                    list.AddRange(from int n1 in nums
                                  from int n2 in nums
                                  select n1);
                    return list;
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums)
                {
                    List<int> list = new List<int>(), list1 = new List<int>();
                    list.AddRange(nums.SelectMany(n1 => nums.Select(n2 => n1)));
                    return list;
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task ToListAssignToParameter()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums, List<int> list)
                {
                    list = new List<int>();
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            list.Add(n1);
                        }
                    }|]

                    return list;
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums, List<int> list)
                {
                    list = (from int n1 in nums
                            from int n2 in nums
                            select n1).ToList();
                    return list;
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums, List<int> list)
                {
                    list = nums.SelectMany(n1 => nums.Select(n2 => n1)).ToList();
                    return list;
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task ToListToArrayElement()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums, List<int>[] lists)
                {
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            lists[0].Add(n1);
                        }
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums, List<int>[] lists)
                {
                    lists[0].AddRange(from int n1 in nums
                                      from int n2 in nums
                                      select n1);
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums, List<int>[] lists)
                {
                    lists[0].AddRange(nums.SelectMany(n1 => nums.Select(n2 => n1)));
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task ToListToNewArrayElement()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums, List<int>[] lists)
                {
                    lists[0] = new List<int>();
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            lists[0].Add(n1);
                        }
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums, List<int>[] lists)
                {
                    lists[0] = (from int n1 in nums
                                from int n2 in nums
                                select n1).ToList();
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums, List<int>[] lists)
                {
                    lists[0] = nums.SelectMany(n1 => nums.Select(n2 => n1)).ToList();
                }
            }
            """, index: 1);
    }

    [Fact]
    public Task ToListHashSetNoConversion()
        => TestMissingAsync("""
            using System.Collections.Generic;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    var hashSet = new HashSet<int>();
                    [|foreach (int n1 in nums)
                    {
                        hashSet.Add(n1);
                    }|]
                }
            }
            """);

    [Fact]
    public async Task ToListMergeWithReturn()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums)
                {
                    var list = new List<int>();
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            list.Add(n1);
                        }
                    }|]

                    return list;
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums)
                {
                    return (from int n1 in nums
                            from int n2 in nums
                            select n1).ToList();
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums)
                {
                    return nums.SelectMany(n1 => nums.Select(n2 => n1)).ToList();
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task ToListSeparateDeclarationAndAssignmentMergeWithReturn()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums)
                {
                    List<int> list;
                    list = new List<int>();
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            list.Add(n1);
                        }
                    }|]

                    return list;
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums)
                {
                    List<int> list;
                    return (from int n1 in nums
                            from int n2 in nums
                            select n1).ToList();
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums)
                {
                    List<int> list;
                    return nums.SelectMany(n1 => nums.Select(n2 => n1)).ToList();
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task ToListSeparateDeclarationAndAssignment()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums)
                {
                    List<int> list;
                    list = new List<int>();
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            list.Add(n1);
                        }
                    }|]

                    return list.Count;
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums)
                {
                    List<int> list;
                    list = (from int n1 in nums
                            from int n2 in nums
                            select n1).ToList();
                    return list.Count;
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums)
                {
                    List<int> list;
                    list = nums.SelectMany(n1 => nums.Select(n2 => n1)).ToList();
                    return list.Count;
                }
            }
            """, index: 1);
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
                    C r1 = new C();
                    [|foreach (int x in c1)
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
                    }|]

                    Console.WriteLine(r1);
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
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
                    C r1 = (from int x in c1
                            from int y in c2
                            from int z in c3
                            let g = x + y + z
                            where x + y / 10 + z / 100 < 6
                            select g).ToList();
                    Console.WriteLine(r1);
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
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
                    foreach (var (x, y, z) in c1.SelectMany(x => c2.SelectMany(y => c3.Select(z => (x, y, z)))))
                    {
                        var g = x + y + z;
                        if (x + y / 10 + z / 100 < 6)
                        {
                            r1.Add(g);
                        }
                    }

                    Console.WriteLine(r1);
                }
            }
            """, index: 1);
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
                    C r1 = new C();
                    [|foreach (int x in c1)
                    {
                        foreach (var y in c2)
                        {
                            if (Equals(x, y / 10))
                            {
                                var z = x + y;
                                r1.Add(z);
                            }
                        }
                    }|]

                    Console.WriteLine(r1);
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Linq;
            using System;
            using C = System.Collections.Generic.List<int>;
            class Query
            {
                public static void Main(string[] args)
                {
                    C c1 = new C { 1, 2, 3 };
                    C c2 = new C { 10, 20, 30 };
                    C r1 = (from int x in c1
                            from y in c2
                            where Equals(x, y / 10)
                            let z = x + y
                            select z).ToList();
                    Console.WriteLine(r1);
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
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
                    foreach (var (x, y) in c1.SelectMany(x => c2.Where(y => Equals(x, y / 10)).Select(y => (x, y))))
                    {
                        var z = x + y;
                        r1.Add(z);
                    }

                    Console.WriteLine(r1);
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task ToListPropertyAssignment()
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
                    c.A = new List<int>();
                    [|foreach (var x in nums)
                    {
                        c.A.Add(x + 1);
                    }|]
                }

                class C
                {
                    public List<int> A { get; set; }
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            public class Test
            {
                public static void Main()
                {
                    var nums = new int[] { 1, 2, 3, 4 };
                    var c = new C();
                    c.A = (from x in nums
                           select x + 1).ToList();
                }

                class C
                {
                    public List<int> A { get; set; }
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            public class Test
            {
                public static void Main()
                {
                    var nums = new int[] { 1, 2, 3, 4 };
                    var c = new C();
                    c.A = nums.Select(x => x + 1).ToList();
                }

                class C
                {
                    public List<int> A { get; set; }
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task ToListPropertyAssignmentNoDeclaration()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            public class Test
            {
                void M()
                {
                    var nums = new int[] { 1, 2, 3, 4 };
                    var c = new C();
                    [|foreach (var x in nums)
                    {
                        c.A.Add(x + 1);
                    }|]
                }

                class C
                {
                    public List<int> A { get; set; }
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            public class Test
            {
                void M()
                {
                    var nums = new int[] { 1, 2, 3, 4 };
                    var c = new C();
                    c.A.AddRange(from x in nums
                                 select x + 1);
                }

                class C
                {
                    public List<int> A { get; set; }
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            public class Test
            {
                void M()
                {
                    var nums = new int[] { 1, 2, 3, 4 };
                    var c = new C();
                    c.A.AddRange(nums.Select(x => x + 1));
                }

                class C
                {
                    public List<int> A { get; set; }
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task ToListNoInitialization()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            public class Test
            {
                public List<int> A { get; set; }

                void M()
                {
                    [|foreach (var x in new int[] { 1, 2, 3, 4 })
                    {
                        A.Add(x + 1);
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            public class Test
            {
                public List<int> A { get; set; }

                void M()
                {
                    A.AddRange(from x in new int[] { 1, 2, 3, 4 }
                               select x + 1);
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            public class Test
            {
                public List<int> A { get; set; }

                void M()
                {
                    A.AddRange((new int[] { 1, 2, 3, 4 }).Select(x => x + 1));
                }
            }
            """, index: 1);
    }

    [Fact]
    public Task ToListOverride()
        => TestMissingAsync("""
            using System.Collections.Generic;
            using System.Linq;

            public static class C
            { 
               public static void Add<T>(this List<T> list, T value, T anotherValue) { }
            }
            public class Test
            {
                void M()
                {
                    var list = new List<int>();
                    [|foreach (var x in new int[] { 1, 2, 3, 4 })
                    {
                        list.Add(x + 1, x);
                    }|]
                }
            }
            """);

    #endregion

    #region In Count

    [Fact]
    public async Task CountInMultipleDeclarationLast()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    int i = 0, cnt = 0;
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            cnt++;
                        }
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    int i = 0, cnt = (from int n1 in nums
                                      from int n2 in nums
                                      select n1).Count();
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    int i = 0, cnt = nums.SelectMany(n1 => nums.Select(n2 => n1)).Count();
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task CountInMultipleDeclarationNotLast()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    int cnt = 0, i = 0;
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            cnt++;
                        }
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    int cnt = 0, i = 0;
                    cnt += (from int n1 in nums
                            from int n2 in nums
                            select n1).Count();
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    int cnt = 0, i = 0;
                    cnt += nums.SelectMany(n1 => nums.Select(n2 => n1)).Count();
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task CountInParameter()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums, int c)
                {
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            c++;
                        }
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums, int c)
                {
                    c += (from int n1 in nums
                          from int n2 in nums
                          select n1).Count();
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums, int c)
                {
                    c += nums.SelectMany(n1 => nums.Select(n2 => n1)).Count();
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task CountInParameterAssignedToZero()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums, int c)
                {
                    c = 0;
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            c++;
                        }
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums, int c)
                {
                    c = (from int n1 in nums
                         from int n2 in nums
                         select n1).Count();
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums, int c)
                {
                    c = nums.SelectMany(n1 => nums.Select(n2 => n1)).Count();
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task CountInParameterAssignedToNonZero()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums, int c)
                {
                    c = 5;
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            c++;
                        }
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums, int c)
                {
                    c = 5;
                    c += (from int n1 in nums
                          from int n2 in nums
                          select n1).Count();
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums, int c)
                {
                    c = 5;
                    c += nums.SelectMany(n1 => nums.Select(n2 => n1)).Count();
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task CountInDeclarationMergeToReturn()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums)
                {
                    var cnt = 0;
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            cnt++;
                        }
                    }|]

                    return cnt;
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums)
                {
                    return (from int n1 in nums
                            from int n2 in nums
                            select n1).Count();
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums)
                {
                    return nums.SelectMany(n1 => nums.Select(n2 => n1)).Count();
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task CountInDeclarationConversion()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                double M(IEnumerable<int> nums)
                {
                    double c = 0;
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            c++;
                        }
                    }|]

                    return c;
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                double M(IEnumerable<int> nums)
                {
                    return (from int n1 in nums
                            from int n2 in nums
                            select n1).Count();
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                double M(IEnumerable<int> nums)
                {
                    return nums.SelectMany(n1 => nums.Select(n2 => n1)).Count();
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task CountInMultipleDeclarationMergeToReturnLast()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums)
                {
                    int c = 0, cnt = 0;
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            cnt++;
                        }
                    }|]

                    return cnt;
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums)
                {
                    int c = 0;
                    return (from int n1 in nums
                            from int n2 in nums
                            select n1).Count();
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums)
                {
                    int c = 0;
                    return nums.SelectMany(n1 => nums.Select(n2 => n1)).Count();
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task CountInMultipleDeclarationLastButNotZero()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums)
                {
                    int c = 0, cnt = 5;
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            cnt++;
                        }
                    }|]

                    return cnt;
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums)
                {
                    int c = 0, cnt = 5;
                    cnt += (from int n1 in nums
                            from int n2 in nums
                            select n1).Count();
                    return cnt;
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums)
                {
                    int c = 0, cnt = 5;
                    cnt += nums.SelectMany(n1 => nums.Select(n2 => n1)).Count();
                    return cnt;
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task CountInMultipleDeclarationMergeToReturnNotLast()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums)
                {
                    int cnt = 0, c = 0;
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            cnt++;
                        }
                    }|]

                    return cnt;
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums)
                {
                    int cnt = 0, c = 0;
                    cnt += (from int n1 in nums
                            from int n2 in nums
                            select n1).Count();
                    return cnt;
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums)
                {
                    int cnt = 0, c = 0;
                    cnt += nums.SelectMany(n1 => nums.Select(n2 => n1)).Count();
                    return cnt;
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task CountInMultipleDeclarationNonZeroToReturnNotLast()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums)
                {
                    int cnt = 5, c = 0;
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            cnt++;
                        }
                    }|]

                    return cnt;
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums)
                {
                    int cnt = 5, c = 0;
                    cnt += (from int n1 in nums
                            from int n2 in nums
                            select n1).Count();
                    return cnt;
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums)
                {
                    int cnt = 5, c = 0;
                    cnt += nums.SelectMany(n1 => nums.Select(n2 => n1)).Count();
                    return cnt;
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task CountInAssignmentToZero()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums)
                {
                    int cnt;
                    cnt = 0;
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            cnt++;
                        }
                    }|]

                    return cnt;
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums)
                {
                    int cnt;
                    return (from int n1 in nums
                            from int n2 in nums
                            select n1).Count();
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums)
                {
                    int cnt;
                    return nums.SelectMany(n1 => nums.Select(n2 => n1)).Count();
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task CountInAssignmentToNonZero()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums)
                {
                    int cnt;
                    cnt = 5;
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            cnt++;
                        }
                    }|]

                    return cnt;
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums)
                {
                    int cnt;
                    cnt = 5;
                    cnt += (from int n1 in nums
                            from int n2 in nums
                            select n1).Count();
                    return cnt;
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums)
                {
                    int cnt;
                    cnt = 5;
                    cnt += nums.SelectMany(n1 => nums.Select(n2 => n1)).Count();
                    return cnt;
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task CountInParameterAssignedToZeroAndReturned()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums, int c)
                {
                    c = 0;
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            c++;
                        }
                    }|]
                    return c;
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums, int c)
                {
                    c = (from int n1 in nums
                         from int n2 in nums
                         select n1).Count();
                    return c;
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums, int c)
                {
                    c = nums.SelectMany(n1 => nums.Select(n2 => n1)).Count();
                    return c;
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task CountDeclareWithNonZero()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    var count = 5;
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            count++;
                        }
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    var count = 5;
                    count += (from int n1 in nums
                              from int n2 in nums
                              select n1).Count();
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    var count = 5;
                    count += nums.SelectMany(n1 => nums.Select(n2 => n1)).Count();
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task CountAssignWithZero()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    int count = 1;
                    count = 0;
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            count++;
                        }
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    int count = 1;
                    count = (from int n1 in nums
                             from int n2 in nums
                             select n1).Count();
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    int count = 1;
                    count = nums.SelectMany(n1 => nums.Select(n2 => n1)).Count();
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task CountAssignWithNonZero()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    var count = 0;
                    count = 4;
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            count++;
                        }
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    var count = 0;
                    count = 4;
                    count += (from int n1 in nums
                              from int n2 in nums
                              select n1).Count();
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    var count = 0;
                    count = 4;
                    count += nums.SelectMany(n1 => nums.Select(n2 => n1)).Count();
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task CountAssignPropertyAssignedToZero()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class A { public int B { get; set; }}
            class C
            {
                void M(IEnumerable<int> nums, A a)
                {
                    a.B = 0;
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            a.B++;
                        }
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class A { public int B { get; set; }}
            class C
            {
                void M(IEnumerable<int> nums, A a)
                {
                    a.B = (from int n1 in nums
                           from int n2 in nums
                           select n1).Count();
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class A { public int B { get; set; }}
            class C
            {
                void M(IEnumerable<int> nums, A a)
                {
                    a.B = nums.SelectMany(n1 => nums.Select(n2 => n1)).Count();
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task CountAssignPropertyAssignedToNonZero()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class A { public int B { get; set; }}
            class C
            {
                void M(IEnumerable<int> nums, A a)
                {
                    a.B = 5;
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            a.B++;
                        }
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class A { public int B { get; set; }}
            class C
            {
                void M(IEnumerable<int> nums, A a)
                {
                    a.B = 5;
                    a.B += (from int n1 in nums
                            from int n2 in nums
                            select n1).Count();
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class A { public int B { get; set; }}
            class C
            {
                void M(IEnumerable<int> nums, A a)
                {
                    a.B = 5;
                    a.B += nums.SelectMany(n1 => nums.Select(n2 => n1)).Count();
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task CountAssignPropertyNotKnownAssigned()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class A { public int B { get; set; }}
            class C
            {
                void M(IEnumerable<int> nums, A a)
                {
                    [|foreach (int n1 in nums)
                    {
                        foreach (int n2 in nums)
                        {
                            a.B++;
                        }
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class A { public int B { get; set; }}
            class C
            {
                void M(IEnumerable<int> nums, A a)
                {
                    a.B += (from int n1 in nums
                            from int n2 in nums
                            select n1).Count();
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class A { public int B { get; set; }}
            class C
            {
                void M(IEnumerable<int> nums, A a)
                {
                    a.B += nums.SelectMany(n1 => nums.Select(n2 => n1)).Count();
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task CountIQueryableInInvocation()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(IEnumerable<int> nums)
                {
                    int c = 0;
                    [|foreach (int n1 in nums.AsQueryable())
                    {
                        c++;
                    }|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(IEnumerable<int> nums)
                {
                    int c = (from int n1 in nums.AsQueryable()
                             select n1).Count();
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(IEnumerable<int> nums)
                {
                    int c = nums.AsQueryable().Count();
                }
            }
            """, index: 1);
    }

    #endregion

    #region Comments

    [Fact]
    public async Task CommentsYieldReturn()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                IEnumerable<int> M(IEnumerable<int> nums)
                {
                    [|// 1
                    foreach /* 2 */( /* 3 */ var /* 4 */ x /* 5 */ in /* 6 */ nums /* 7 */)// 8
                    {
                        // 9
                        /* 10 */
                        foreach /* 11 */ (/* 12 */ int /* 13 */ y /* 14 */ in /* 15 */ nums /* 16 */)/* 17 */ // 18
                        {// 19
                         /*20 */
                            if /* 21 */(/* 22 */ x > 2 /* 23 */) // 24
                            { // 25
                              /* 26 */
                                yield /* 27 */ return /* 28 */ x * y /* 29 */; // 30
                                /* 31 */
                            }// 32
                             /* 33 */
                        } // 34
                          /* 35 */
                    }|] /* 36 */
                      /* 37 */
                    yield  /* 38 */ break/* 39*/; // 40
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                IEnumerable<int> M(IEnumerable<int> nums)
                {
                    return
                    // 25
                    // 1
                    from/* 3 *//* 2 *//* 4 */x /* 5 */ in/* 6 */nums/* 7 */// 8
                                                                           // 9
                        /* 10 */
                    from/* 12 *//* 11 */int /* 13 */ y /* 14 */ in/* 15 */nums/* 16 *//* 17 */// 18
                                                                                              // 19
                        /*20 */
                    where/* 21 *//* 22 */x > 2/* 23 */// 24
                    /* 26 *//* 27 *//* 28 */
                    select x * y/* 29 *//* 31 */// 32
                    /* 33 */// 34
                    /* 35 *//* 36 */// 30
                    /* 37 *//* 38 *//* 39*/// 40
                    ;
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                IEnumerable<int> M(IEnumerable<int> nums)
                {
                    return nums /* 7 */.SelectMany(
                    // 1
                    /* 2 */// 25
                    /* 4 */x /* 5 */ => nums /* 16 */.Where(
                    /*20 *//* 21 */// 19
                    y =>
            /* 22 */x > 2/* 23 */// 24
                    ).Select(
                    // 9
                    /* 10 *//* 11 *//* 13 */y /* 14 */ =>
            /* 26 *//* 27 *//* 28 */x * y/* 29 *//* 31 */// 32
                    /* 33 */// 34
                    /* 35 *//* 36 */// 30
                    /* 37 *//* 38 *//* 39*/// 40
                    /* 12 *//* 15 *//* 17 */// 18
                    )/* 3 *//* 6 */// 8
                    );
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task CommentsToList()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums)
                {
                    /* 1 */ var /* 2 */ list /* 3 */ = /* 4 */ new List<int>(); // 5
                    /* 6 */ [|foreach /* 7 */ (/* 8 */ var /* 9 */ x /* 10 */ in /* 11 */ nums /* 12 */) // 13
                          /* 14 */{ // 15
                        /* 16 */var /* 17 */ y /* 18 */ = /* 19 */ x + 1 /* 20 */; //21
                        /* 22 */ list.Add(/* 23 */y /* 24 */) /* 25 */;//26
                    /*27*/} //28|]
                    /*29*/return /*30*/ list /*31*/; //32
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums)
                {
                    /*29*/
                    return /*30*/ /* 1 *//* 2 *//* 3 *//* 4 */// 5
                   /*31*//* 6 */
                   (from/* 8 *//* 7 *//* 9 */x /* 10 */ in/* 11 */nums/* 12 */// 13
                        /* 14 */// 15
                        /* 16 *//* 17 */
                    let y /* 18 */ = /* 19 */ x + 1/* 20 *///21
                    select y)/* 24 *//*27*///28
            .ToList()/* 22 *//* 23 *//* 25 *///26
            ; //32
                }
            }
            """, index: 0);

        // No linq refactoring offered due to variable declaration in outermost foreach.
        await TestActionCountAsync(source, count: 1);
    }

    [Fact]
    public async Task CommentsToList_02()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums)
                {
                    /* 1 */ var /* 2 */ list /* 3 */ = /* 4 */ new List<int>(); // 5
                    /* 6 */ [|foreach /* 7 */ (/* 8 */ var /* 9 */ x /* 10 */ in /* 11 */ nums /* 12 */) // 13
                          /* 14 */{ // 15
                            /* 16 */
                            list.Add(/* 17 */ x + 1 /* 18 */) /* 19 */;//20
                    /*21*/} //22|]
                    /*23*/return /*24*/ list /*25*/; //26
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums)
                {
                    /*23*/
                    return /*24*/ /* 1 *//* 2 *//* 3 *//* 4 */// 5
             /*25*//* 14 */// 15
             /* 6 */
             (from/* 8 *//* 7 *//* 9 */x /* 10 */ in/* 11 */nums/* 12 */// 13
              select x + 1)/* 18 *//*21*///22
            .ToList()/* 16 *//* 17 *//* 19 *///20
            ; //26
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                List<int> M(IEnumerable<int> nums)
                {
                    /*23*/
                    return /*24*/ /* 1 *//* 2 *//* 3 *//* 4 */// 5
            /*25*/
            nums /* 12 */.Select(
            /* 6 *//* 7 *//* 14 */// 15
            /* 9 */x /* 10 */ => x + 1/* 18 *//*21*///22
            /* 8 *//* 11 */// 13
            ).ToList()/* 16 *//* 17 *//* 19 *///20
            ; //26
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task CommentsCount()
    {
        var source = """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums)
                {
                    /* 1 */ var /* 2 */ c /* 3 */ = /* 4 */ 0; // 5
                    /* 6 */ [| foreach /* 7 */ (/* 8 */ var /* 9 */ x /* 10 */ in /* 11 */ nums /* 12 */) // 13
                    /* 14 */{ // 15
                        /* 16 */ c++ /* 17 */;//18
                    /*19*/}|] //20
                    /*21*/return /*22*/ c /*23*/; //24
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums)
                {
                    /*21*/
                    return /*22*/ /* 1 *//* 2 *//* 3 *//* 4 */// 5
             /*23*//* 14 */// 15
             /* 6 */
             (from/* 8 *//* 7 *//* 9 */x /* 10 */ in/* 11 */nums/* 12 */// 13
              select x)/* 10 *//*19*///20
            .Count()/* 16 *//* 17 *///18
            ; //24
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                int M(IEnumerable<int> nums)
                {
                    /*21*/
                    return /*22*/ /* 1 *//* 2 *//* 3 *//* 4 */// 5
            /*23*/
            nums /* 12 *//* 6 *//* 7 *//* 14 */// 15
            /* 9 *//* 10 *//* 10 *//*19*///20
            /* 8 *//* 11 */// 13
            .Count()/* 16 *//* 17 *///18
            ; //24
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task CommentsDefault()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    [|/* 1 */ foreach /* 2 */(int /* 3 */ n1 /* 4 */in /* 5 */ nums /* 6 */)// 7
                    /* 8*/{// 9
                        /* 10 */int /* 11 */ a /* 12 */ = /* 13 */ n1 + n1 /* 14*/, /* 15 */ b /*16*/ = /*17*/ n1 * n1/*18*/;//19
                        /*20*/Console.WriteLine(a + b);//21
                    /*22*/}/*23*/|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    foreach (var (a /* 12 */ , b /*16*/ ) in
            /* 1 */from/* 2 */int /* 3 */ n1 /* 4 */in/* 5 */nums/* 6 */// 7
                       /* 8*/// 9
                       /* 10 *//* 11 */
                   let a /* 12 */ = /* 13 */ n1 + n1/* 14*//* 15 */
                   let b /*16*/ = /*17*/ n1 * n1/*18*///19
                   select (a /* 12 */ , b /*16*/ )/*22*//*23*/)
                    {
                        /*20*/
                        Console.WriteLine(a + b);//21
                    }
                }
            }
            """, index: 0);

        // No linq refactoring offered due to variable declaration(s) in outermost foreach.
        await TestActionCountAsync(source, count: 1);
    }

    [Fact]
    public async Task CommentsDefault_02()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    [|/* 1 */ foreach /* 2 */(int /* 3 */ n1 /* 4 */in /* 5 */ nums /* 6 */)// 7
                    /* 8*/{// 9
                            /* 10 */ if /* 11 */ (/* 12 */ n1 /* 13 */ > /* 14 */ 0/* 15 */ ) // 16
                            /* 17 */{ // 18
                                /*19*/Console.WriteLine(n1);//20
                            /* 21 */} // 22
                    /*23*/}/*24*/|]
                }
            }
            """;
        await TestInRegularAndScriptAsync(source, """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    foreach (var n1 /* 4 */in
                    /* 17 */// 18
                    /* 1 */from/* 2 */int /* 3 */ n1 /* 4 */in/* 5 */nums/* 6 */// 7
                               /* 8*/// 9
                               /* 10 */
                           where/* 11 *//* 12 */n1 /* 13 */ > /* 14 */ 0/* 15 */// 16
                           select n1/* 4 *//* 21 */// 22
                                /*23*//*24*/
                                )
                    {
                        /*19*/
                        Console.WriteLine(n1);//20
                    }
                }
            }
            """, index: 0);
        await TestInRegularAndScriptAsync(source, """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                void M(IEnumerable<int> nums)
                {
                    foreach (var n1 /* 4 */in nums /* 6 */.Where(
                    /* 10 *//* 11 *//* 8*/// 9
                    n1 =>
            /* 12 */n1 /* 13 */ > /* 14 */ 0/* 15 */// 16
                    )
                    /* 1 *//* 2 *//* 17 */// 18
                    /* 3 *//* 4 *//* 4 *//* 21 */// 22
                    /*23*//*24*//* 5 */// 7
                    )
                    {
                        /*19*/
                        Console.WriteLine(n1);//20
                    }
                }
            }
            """, index: 1);
    }

    #endregion

    #region Preprocessor directives

    [Fact]
    public Task NoConversionPreprocessorDirectives()
        => TestMissingAsync("""
            using System.Collections.Generic;
            using System.Linq;
            class C
            {
                IEnumerable<int> M(IEnumerable<int> nums)
                {
                    [|foreach(var x in nums)
                    {
            #if (true)
                        yield return x + 1;
            #endif
                    }|]
                }
            }
            """);

    #endregion
}
