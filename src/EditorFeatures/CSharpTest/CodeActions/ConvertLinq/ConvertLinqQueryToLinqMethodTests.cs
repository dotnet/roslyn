// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.ConvertLinq
{
    public class ConvertLinqQueryToLinqMethodTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CodeAnalysis.CSharp.ConvertLinq.CSharpConvertLinqQueryToLinqMethodProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task Conversion_WhereOrderByTrivialSelect()
        {
            await Test(
@"from num in new int[] { 0, 1, 2 }
where num %2 == 0
orderby num
select num",
@"new int[] { 0, 1, 2 }.Where(num => num % 2 == 0
).OrderBy(num => num)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task Conversion_WhereSelect()
        {
            await Test(
@"from x in new int[] { 0, 1, 2 } where x< 5 select x* x",
@"new int[] { 0, 1, 2 }.Where(x => x < 5).Select(x => x * x)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task Conversion_GroupBy()
        {
            await Test("from a in new[] { 1 } group a/2 by a*2", "new[] { 1 }.GroupBy(a => a / 2, a => a * 2)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task Conversion_SelectWithType()
        {
            await Test("from int a in new[] { 1 } select a", "new[] { 1 }.Cast<int>()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task Conversion_GroupByWhereSelect()
        {
            await Test(
@"from w in new[]{ ""apples"", ""blueberries"", ""oranges"", ""bananas"", ""apricots"" }
    group w by w[0] into fruitGroup
    where fruitGroup.Count() >= 2
    select new { FirstLetter = fruitGroup.Key, Words = fruitGroup.Count() }",
@"new[]{ ""apples"", ""blueberries"", ""oranges"", ""bananas"", ""apricots"" }.GroupBy(w => w[0]).Where(fruitGroup => fruitGroup.Count() >= 2
).Select(fruitGroup => new { FirstLetter = fruitGroup.Key, Words = fruitGroup.Count() })");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task Conversion_MultipleFrom()
        {
            await Test(
"from x in new [] {0} from y in new [] {1} from z in new [] {2} where x + y + z < 5 select x * x",
"new [] {0}.SelectMany(x => new [] {1}, (x, y) => (x, y)).SelectMany(__queryIdentifier0 => new [] {2}, (__queryIdentifier0, z) => (__queryIdentifier0, z)).Where(__queryIdentifier1 => __queryIdentifier1.__queryIdentifier0.x + __queryIdentifier1.__queryIdentifier0.y + __queryIdentifier1.z < 5).Select(__queryIdentifier1 => __queryIdentifier1.__queryIdentifier0.x * __queryIdentifier1.__queryIdentifier0.x)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task Conversion_TrivialSelect()
        {
            await Test("from a in new int[] { 1, 2, 3 } select a", "new int[] { 1, 2, 3 }.Select(a => a)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task Conversion_DoubleFrom()
        {
            await Test(
@"from w in ""aaa bbb ccc"".Split(' ')
    from c1 in w
    select c1",
@"""aaa bbb ccc"".Split(' ').SelectMany(w => w, (w, c1) => c1)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task Conversion_IntoDoubleFrom()
        {
            await Test("from a in new[] { 1, 2, 3 } select a.ToString() into b from c in b select c",
                "new[] { 1, 2, 3 }.Select(a => a.ToString()).SelectMany(b => b, (b, c) => c).Select((b, c) => c)");
        }

        #region Missing
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task NoDiagnostics_Join()
        {
            await TestMissing("from a in new[] { 1, 2, 3 } join b in new[] { 4 } on a equals b select a");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task NoDiagnostics_Let1()
        {
            await TestMissing(
@"from sentence in new[] { ""aa bb"", ""ee ff"", ""ii"" }
    let words = sentence.Split(' ')
    from word in words
    let w = word.ToLower()
    where w[0] == 'a'
    select word");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task NoDiagnostics_Let2()
        {
            await TestMissing(
@"from x in ""123"" 
    let z = x.ToString()
    select z into w
    select int.Parse(w)");
        }

        #endregion

        #region Conversions

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task Select()
        {
            await Test("from i in c select i+1", "c.Select(i => i + 1)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task GroupBy01()
        {
            // Group by is not supported
            await TestMissingAsync("from i in c group i by i % 2");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task GroupBy02()
        {
            // Group by is not supported
            await TestMissingAsync("from i in c group 10+i by i % 2");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task FromJoinSelect01()
        {
            await TestMissing(@"from x1 in c1
                      join x2 in c2 on x1 equals x2/10
                      select x1+x2");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task FromJoinSelect02()
        {
            await TestMissing(@"from num in new int[] { 1, 2 }
                    from a in new int[] { 5, 6 }
                    join x1 in new int[] { 3, 4 } on num equals x1
                    select x1 + 5");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task FromJoinSelect03()
        {
            await TestMissing(@"from num in new int[] { 1, 2 }
                    from a in new int[] { 5, 6 }
                    join x1 in new int[] { 3, 4 } on num equals x1
                    join x2 in new int[] { 7, 8 } on num equals x2
                    select x1 + 5");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task OrderBy()
        {
            await Test(@"from i in c
            orderby i/10 descending, i%10
            select i",
            @"c.OrderByDescending(i => i / 10).ThenBy(i => i % 10)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task Let()
        {
            await TestMissing(@"from int x in c1
            let g = x * 10
            let z = g + x*100
            let a = 5 + z
            select x + z - a");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task GroupJoin()
        {
            // GroupJoin is not supported
            await TestMissing(@"from x1 in c
                      join x2 in d on x1 equals x2 / 10 into g
                      where x1 < 7
                      select x1 + "":"" + g.ToString()");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
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
        var q = C.Select(x => x);
    }

    static IEnumerable<int> Select<T>(Func<int, T> f) { return null; }
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
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
        var q = C.Select(x => x);
    }

    static Func<Func<int, object>, IEnumerable<object>> Select = null;
}";
            await TestInRegularAndScriptAsync(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task JoinClause()
        {
            await TestMissing(@"
from a in Enumerable.Range(1, 2)
join b in Enumerable.Range(1, 13) on 4 * a equals b
select a");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task WhereClause()
        {
            await Test(@"from x in c where (x > 2) select x",
                @"c.Where(x => x > 2).Select(x => x)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
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

            string output = @"
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
        var query = src.Where(x => x > 0).Select(x => x);

        Console.Write(query);
    }
}";

            await Test(source, output);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task QueryContinuation()
        {
            // Into is not supported
            await TestMissing(@"from x in c select x into w select w");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task SelectInto()
        {
            // Into is not supported
            await TestMissing(@"from x in c
                 select x+1 into w
                 select w+1");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task ComputeQueryVariableType()
        {
            await Test("from x in c select 5", "c.Select(x => 5)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task JoinIntoClause()
        {
            // GroupJoin is not supported
            await TestMissing(@"from x3 in new int[] { 0 }
                      join c in (new int[] { 0 }) on 5 equals 5 into d
                      select x8");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
        public async Task SemanticErrorInQuery()
        {
            // Error: Range variable already being declared.
            await TestMissing(@"from num in c
                    let num = 3
                    select num");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertQueryToLinqMethod)]
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

        #region Helpers
        private async Task Test(string input, string expectedOutput)
        {
            const string code = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M()
    {
       List<int> c = new List<int>{ 1, 2, 3, 4, 5, 6, 7 };
       List<int> d = new List<int>{10, 30, 40, 50, 60, 70};
       var q = ##;
    }
}
";
            await TestInRegularAndScriptAsync(code.Replace("##", "[||]" + input), code.Replace("##", expectedOutput));
        }

        private async Task TestMissing(string input)
        {
            const string code = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    void M()
    {
       var c = new []{ 1, 2, 3, 4, 5, 6, 7 };
       var d = new []{10, 30, 40, 50, 60, 70};
       var q = ##;
    }
}
";
            await TestMissingInRegularAndScriptAsync(code.Replace("##", "[||]" + input));
        }

        #endregion
    }
}
