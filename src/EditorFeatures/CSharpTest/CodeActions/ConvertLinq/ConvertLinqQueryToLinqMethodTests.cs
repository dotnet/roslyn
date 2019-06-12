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

        #region Diagnostics

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Conversion_WhereSelect()
        {
            await Test(
@"from x in new int[] { 0, 1, 2 } where x< 5 select x* x",
@"new int[] { 0, 1, 2 }.Where(x => x < 5).Select(x => x * x)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Conversion_GroupBy()
        {
            await Test("from a in new[] { 1 } group a/2 by a*2", "new[] { 1 }.GroupBy(a => a / 2, a => a * 2)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Conversion_SelectWithType()
        {
            await Test("from int a in new[] { 1 } select a", "new[] { 1 }.Cast<int>().Select(Function(x) x)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Conversion_MultipleFrom()
        {
            await Test(
"from x in new [] {0} from y in new [] {1} from z in new [] {2} where x + y + z < 5 select x * x",
"new [] {0}.SelectMany(x => new [] {1}, (x, y) => (x, y)).SelectMany(__queryIdentifier0 => new [] {2}, (__queryIdentifier0, z) => (__queryIdentifier0, z)).Where(__queryIdentifier1 => __queryIdentifier1.__queryIdentifier0.x + __queryIdentifier1.__queryIdentifier0.y + __queryIdentifier1.z < 5).Select(__queryIdentifier1 => __queryIdentifier1.__queryIdentifier0.x * __queryIdentifier1.__queryIdentifier0.x)");
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Conversion_TrivialSelect()
        {
            await Test("from a in new int[] { 1, 2, 3 } select a", "new int[] { 1, 2, 3 }.Select(a => a)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Conversion_DoubleFrom()
        {
            await Test(
@"from w in ""aaa bbb ccc"".Split(' ')
    from c in w
    select c",
@"""aaa bbb ccc"".Split(' ').SelectMany(w => w, (w, c) => c)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Conversion_IntoDoubleFrom()
        {
            await Test("from a in new[] { 1, 2, 3 } select a.ToString() into b from c in b select c",
                "new[] { 1, 2, 3 }.Select(a => a.ToString()).SelectMany(b => b, (b, c) => c).Select((b, c) => c)");
        }

        #endregion

        #region No Diagnostics

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task NoDiagnostics_Join()
        {
            await TestNoDiagnostics("from a in new[] { 1, 2, 3 } join b in new[] { 4 } on a equals b select a");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task NoDiagnostics_Let1()
        {
            await TestNoDiagnostics(
@"from sentence in new[] { ""aa bb"", ""ee ff"", ""ii"" }
    let words = sentence.Split(' ')
    from word in words
    let w = word.ToLower()
    where w[0] == 'a'
    select word");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task NoDiagnostics_Let2()
        {
            await TestNoDiagnostics(
@"from x in ""123"" 
    let z = x.ToString()
    select z into w
    select int.Parse(w)");
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
    IEnumerable<object> M()
    {
       return ##;
    }
}
";
            await TestInRegularAndScriptAsync(code.Replace("##", "[||]" + input), code.Replace("##", expectedOutput));
        }

        private async Task TestNoDiagnostics(string input)
        {
            const string code = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<object> M()
    {
       return ##;
    }
}
";
            await TestMissingInRegularAndScriptAsync(code.Replace("##", "[||]" + input));
        }

        #endregion
    }
}
