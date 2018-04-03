// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting
{
    public class FormattingEngineTests_Venus : FormattingEngineTestBase
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting), Trait(Traits.Feature, Traits.Features.Venus)]
        public async Task SimpleOneLineNugget()
        {
            var code = @"public class Default
{
    void PreRender()
    {
#line ""Goo.aspx"", 1[|
int x=1 ;
|]#line hidden
#line default
    }
}";

            var expected = @"public class Default
{
    void PreRender()
    {
#line ""Goo.aspx"", 1
           int x = 1;
#line hidden
#line default
}
}";

            await AssertFormatWithBaseIndentAsync(expected, code, baseIndentation: 7);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting), Trait(Traits.Feature, Traits.Features.Venus)]
        public async Task SimpleMultiLineNugget()
        {
            var code = @"public class Default
{
    void PreRender()
    {
#line ""Goo.aspx"", 1[|
if(true)
{
Console.WriteLine(5);}
|]#line hidden
#line default
    }
}";

            var expected = @"public class Default
{
    void PreRender()
    {
#line ""Goo.aspx"", 1
       if (true)
       {
           Console.WriteLine(5);
       }
#line hidden
#line default
}
}";

            await AssertFormatWithBaseIndentAsync(expected, code, baseIndentation: 3);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting), Trait(Traits.Feature, Traits.Features.Venus)]
        public async Task SimpleQueryWithinNugget()
        {
            var code = @"public class Default
{
    void PreRender()
    {
#line ""Goo.aspx"", 1[|
int[] numbers = {  5,  4,  1  };
var even =  from     n      in  numbers
                   where  n %   2 ==   0
                          select    n;
|]#line hidden
#line default
    }
}";

            var expected = @"public class Default
{
    void PreRender()
    {
#line ""Goo.aspx"", 1
           int[] numbers = { 5, 4, 1 };
           var even = from n in numbers
                      where n % 2 == 0
                      select n;
#line hidden
#line default
}
}";

            await AssertFormatWithBaseIndentAsync(expected, code, baseIndentation: 7);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting), Trait(Traits.Feature, Traits.Features.Venus)]
        public async Task LambdaExpressionInNugget()
        {
            var code = @"public class Default
{
    void PreRender()
    {
#line ""Goo.aspx"", 1[|
int[] source = new [] {   3,   8, 4,   6, 1, 7, 9, 2, 4, 8} ;
 
foreach(int i   in source.Where(x  =>  x  > 5))
    Console.WriteLine(i);
|]#line hidden
#line default
    }
}";

            var expected = @"public class Default
{
    void PreRender()
    {
#line ""Goo.aspx"", 1
       int[] source = new[] { 3, 8, 4, 6, 1, 7, 9, 2, 4, 8 };

       foreach (int i in source.Where(x => x > 5))
           Console.WriteLine(i);
#line hidden
#line default
}
}";

            await AssertFormatWithBaseIndentAsync(expected, code, baseIndentation: 3);
        }

        [WorkItem(576457, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/576457")]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting), Trait(Traits.Feature, Traits.Features.Venus)]
        public async Task StatementLambdaInNugget()
        {
            var code = @"public class Default
{
    void PreRender()
    {
#line ""Goo.aspx"", 1[|
       int[] source = new[] { 3, 8, 4, 6, 1, 7, 9, 2, 4, 8 };

    foreach (int i in source.Where(
           x   =>
           { 
                if (x <= 3)
    return true;
                   else if (x >= 7)
           return true;
                   return false;
               }
       ))
            Console.WriteLine(i);
|]#line hidden
#line default
    }
}";

            var expected = @"public class Default
{
    void PreRender()
    {
#line ""Goo.aspx"", 1
       int[] source = new[] { 3, 8, 4, 6, 1, 7, 9, 2, 4, 8 };

       foreach (int i in source.Where(
              x =>
              {
                  if (x <= 3)
                      return true;
                  else if (x >= 7)
                      return true;
                  return false;
              }
          ))
           Console.WriteLine(i);
#line hidden
#line default
}
}";

            // It is somewhat odd that the 'x' and the ')' maintain their
            // position relative to 'foreach', but the block doesn't, but that isn't
            // Venus specific, just the way the formatting engine is.
            await AssertFormatWithBaseIndentAsync(expected, code, baseIndentation: 3);
        }
    }
}
