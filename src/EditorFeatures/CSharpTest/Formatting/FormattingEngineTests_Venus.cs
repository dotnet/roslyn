// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Formatting;

public sealed class FormattingEngineTests_Venus : CSharpFormattingEngineTestBase
{
    public FormattingEngineTests_Venus(ITestOutputHelper output) : base(output) { }

    [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting), Trait(Traits.Feature, Traits.Features.Venus)]
    public Task SimpleOneLineNugget()
        => AssertFormatWithBaseIndentAsync("""
            public class Default
            {
                void PreRender()
                {
            #line "Goo.aspx", 1
                       int x = 1;
            #line hidden
            #line default
            }
            }
            """, """
            public class Default
            {
                void PreRender()
                {
            #line "Goo.aspx", 1[|
            int x=1 ;
            |]#line hidden
            #line default
                }
            }
            """, baseIndentation: 7);

    [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting), Trait(Traits.Feature, Traits.Features.Venus)]
    public Task SimpleMultiLineNugget()
        => AssertFormatWithBaseIndentAsync("""
            public class Default
            {
                void PreRender()
                {
            #line "Goo.aspx", 1
                   if (true)
                   {
                       Console.WriteLine(5);
                   }
            #line hidden
            #line default
            }
            }
            """, """
            public class Default
            {
                void PreRender()
                {
            #line "Goo.aspx", 1[|
            if(true)
            {
            Console.WriteLine(5);}
            |]#line hidden
            #line default
                }
            }
            """, baseIndentation: 3);

    [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting), Trait(Traits.Feature, Traits.Features.Venus)]
    public Task SimpleQueryWithinNugget()
        => AssertFormatWithBaseIndentAsync("""
            public class Default
            {
                void PreRender()
                {
            #line "Goo.aspx", 1
                       int[] numbers = { 5, 4, 1 };
                       var even = from n in numbers
                                  where n % 2 == 0
                                  select n;
            #line hidden
            #line default
            }
            }
            """, """
            public class Default
            {
                void PreRender()
                {
            #line "Goo.aspx", 1[|
            int[] numbers = {  5,  4,  1  };
            var even =  from     n      in  numbers
                               where  n %   2 ==   0
                                      select    n;
            |]#line hidden
            #line default
                }
            }
            """, baseIndentation: 7);

    [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting), Trait(Traits.Feature, Traits.Features.Venus)]
    public Task LambdaExpressionInNugget()
        => AssertFormatWithBaseIndentAsync("""
            public class Default
            {
                void PreRender()
                {
            #line "Goo.aspx", 1
                   int[] source = new[] { 3, 8, 4, 6, 1, 7, 9, 2, 4, 8 };

                   foreach (int i in source.Where(x => x > 5))
                       Console.WriteLine(i);
            #line hidden
            #line default
            }
            }
            """, """
            public class Default
            {
                void PreRender()
                {
            #line "Goo.aspx", 1[|
            int[] source = new [] {   3,   8, 4,   6, 1, 7, 9, 2, 4, 8} ;

            foreach(int i   in source.Where(x  =>  x  > 5))
                Console.WriteLine(i);
            |]#line hidden
            #line default
                }
            }
            """, baseIndentation: 3);

    [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/576457")]
    [WpfFact, Trait(Traits.Feature, Traits.Features.Formatting), Trait(Traits.Feature, Traits.Features.Venus)]
    public Task StatementLambdaInNugget()
        => AssertFormatWithBaseIndentAsync("""
            public class Default
            {
                void PreRender()
                {
            #line "Goo.aspx", 1
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
            }
            """, """
            public class Default
            {
                void PreRender()
                {
            #line "Goo.aspx", 1[|
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
            }
            """, baseIndentation: 3);
}
