// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertLinq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.ConvertLinq
{
    public class ConvertLinqQueryToLinqMethodTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
         => new CodeAnalysis.CSharp.ConvertLinq.CSharpConvertLinqQueryToLinqMethodProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Convert1()
        {
            await TestInt(
@"from num in numbers
where num % 2 == 0
orderby num
select num",
@"numbers.Where(num => num % 2 == 0).OrderBy(num => num)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Convert2()
        {
            await TestInt(
@"from a in new[] { 1, 2, 3 }
    select a",
@"new[] { 1, 2, 3 }");
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Convert3()
        {
            await TestInt(
@"from x in numbers where x< 5 select x* x",
@"numbers.Where(x => x < 5).Select(x => x * x)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Convert4()
        {
            await TestNoActionsInt(
@"from x in ""123"" 
    let z = x.ToString()
    select z into w
    select int.Parse(w)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Convert5()
        {
            await TestNoActionsInt(
@"from w in ""aaa bbb ccc"".Split(' ')
    from c in w
    select c");
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Convert7()
        {
            await TestNoActionsInt("from a in args join b in args on a equals b");
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Convert8()
        {
            await TestNoActionsInt("from a in args select a into b from c in b select c");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Convert10()
        {
            await TestNoActionsInt(@"
          from sentence in strings
          let words = sentence.Split(' ')
          from word in words
          let w = word.ToLower()
          where w[0] == 'a' || w[0] == 'e'
              || w[0] == 'i' || w[0] == 'o'
              || w[0] == 'u'
          select word");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Convert11()
        {
            await TestInt("from a in numbers group a/2 by a*2", "numbers.GroupBy(a => a * 2, a => a / 2)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Convert12()
        {
            await TestInt("from int a in numbers select a", "numbers.Select(a => a is int)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Convert13()
        {
            await TestString(@"from w in words
            group w by w[0] into fruitGroup
            where fruitGroup.Count() >= 2
            select new { FirstLetter = fruitGroup.Key, Words = fruitGroup.Count() }", "words.GroupBy(w => w[0], w => w).Where(fruitGroup => fruitGroup.Count() >= 2).Select(fruitGroup => new { FirstLetter = fruitGroup.Key, Words = fruitGroup.Count() })");
        }

        private async Task TestInt(string input, string expectedOutput)
        {
            const string code = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<int> M()
    {
       return ##;
    }
}
";
            await TestInRegularAndScriptAsync(code.Replace("##", "[||]" + input), code.Replace("##", expectedOutput));
        }

        private async Task TestNoActionsInt(string input)
        {
            const string code = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<int> M()
    {
       return ##;
    }
}
";
            await TestMissingInRegularAndScriptAsync(code.Replace("##", "[||]" + input));
        }

        private async Task TestString(string input, string expectedOutput)
        {
            const string code = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<object> M()
    {
        string[] words = { ""apples"", ""blueberries"", ""oranges"", ""bananas"", ""apricots"" };
        return ##;
    }
}
";
            await TestInRegularAndScriptAsync(code.Replace("##", "[||]" + input), code.Replace("##", expectedOutput));
        }
    }
}
