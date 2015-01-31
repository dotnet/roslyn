// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.CSharp.Debugging;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.Debugging
{
    [Trait(Traits.Feature, Traits.Features.DebuggingBreakpoints)]
    public class BreakpointLocationValidatorTests
    {
        #region Helpers 

        private void TestSpan(string markup, ParseOptions options = null)
        {
            Test(markup, isMissing: false, isLine: false, options: options);
        }

        private void TestMissing(string markup)
        {
            Test(markup, isMissing: true, isLine: false);
        }

        private void TestLine(string markup)
        {
            Test(markup, isMissing: false, isLine: true);
        }

        private void Test(string markup, bool isMissing, bool isLine, ParseOptions options = null)
        {
            int? position;
            TextSpan? expectedSpan;
            string source;
            MarkupTestFile.GetPositionAndSpan(markup, out source, out position, out expectedSpan);
            var tree = SyntaxFactory.ParseSyntaxTree(source, options);

            TextSpan breakpointSpan;
            bool hasBreakpoint = BreakpointGetter.TryGetBreakpointSpan(
                tree, position.Value, CancellationToken.None, out breakpointSpan);

            if (isLine)
            {
                Assert.True(hasBreakpoint);
                Assert.True(breakpointSpan.Length == 0);
            }
            else if (isMissing)
            {
                Assert.False(hasBreakpoint);
            }
            else
            {
                Assert.True(hasBreakpoint);
                Assert.Equal(expectedSpan.Value, breakpointSpan);
            }
        }

        private void TestAll(string markup)
        {
            int position;
            IList<TextSpan> expectedSpans;
            string source;
            MarkupTestFile.GetPositionAndSpans(markup, out source, out position, out expectedSpans);

            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var root = tree.GetRoot();

            var actualSpans = GetBreakpointSequence(root, position).ToArray();

            AssertEx.Equal(expectedSpans, actualSpans,
                itemSeparator: "\r\n",
                itemInspector: span => "[|" + source.Substring(span.Start, span.Length) + "|]");
        }

        public static IEnumerable<TextSpan> GetBreakpointSequence(SyntaxNode root, int position)
        {
            int endPosition = root.Span.End;
            int lastSpanEnd = 0;
            while (position < endPosition)
            {
                TextSpan span;
                if (root.TryGetClosestBreakpointSpan(position, out span) && span.End > lastSpanEnd)
                {
                    position = lastSpanEnd = span.End;
                    yield return span;
                }
                else
                {
                    position++;
                }
            }
        }

        #endregion

        [Fact]
        public void GetBreakpointSequence1()
        {
            TestAll(@"
class C
{
    void Foo()
    $$[|{|]
        [|int d = 5;|]
        [|int a = 1|], [|b = 2|], [|c = 3|];
        for ([|int i = 0|], [|j = 1|], [|k = 2|]; [|i < 10|]; [|i++|], [|j++|], [|k--|])
            [|while (b > 0)|]
            [|{|]
                [|if (c < b)|]
                    try
                    [|{|]
                        [|System.Console.WriteLine(a);|]
                    [|}|]
                    [|catch (Exception e)|]
                    [|{|]
                        [|System.Console.WriteLine(e);|]   
                    [|}|]
                    finally
                    [|{|]
                    [|}|]
                else [|if (b < 10)|]
                    [|System.Console.WriteLine(b);|]
                else
                    [|System.Console.WriteLine(c);|]
            [|}|]
    [|}|]
}");
        }

        [Fact]
        public void GetBreakpointSequence2()
        {
            TestAll(@"
class C
{
    void Foo()
    $$[|{|]
        do
        [|{|]
            label:
               [|i++;|]
            [|var l = new List<int>()
            {
               F(),
               F(),
            };|]
            [|break;|]
            [|continue;|]
        [|}|]
        [|while (a);|]
        [|goto label;|]
    [|}|]
}");
        }

        [Fact]
        public void GetBreakpointSequence3()
        {
            TestAll(@"
class C
{
    int Foo()
    $$[|{|]
        [|switch(a)|]
        {
            case 1:
            case 2:
                [|break;|]
            
            case 3:
                [|goto case 4;|]
        
            case 4: 
                [|throw new Exception();|]
                [|goto default;|]
            
            default:
                [|return 1;|]
        }
        [|return 2;|]
    [|}|]
}");
        }

        [Fact]
        public void GetBreakpointSequence4()
        {
            TestAll(@"
class C
{
    IEnumerable<int> Foo()
    $$[|{|]
        [|yield return 1;|]
        [|foreach|] ([|var f|] [|in|] [|Foo()|])
        [|{|]
            [|using (z)|]
            using ([|var q = null|])
            using ([|var u = null|], [|v = null|])
            [|{|]
                [|while (a)|] [|yield return 2;|]
            [|}|]
        [|}|]
        fixed([|int* a = new int[1]|], [|b = new int[1]|], [|c = new int[1]|]) [|{|][|}|]
        [|yield break;|]
    [|}|]
}");
        }

        [Fact]
        public void GetBreakpointSequence5()
        {
            TestAll(@"
class C
{
    IEnumerable<int> Foo()
    $$[|{|][|while(t)|][|{|][|}|][|}|]
}");
        }

        [Fact]
        public void GetBreakpointSequence6()
        {
            TestAll(@"
class C
{
    IEnumerable<int> Foo()
    $$[|{|]
        checked 
        [|{|]
            const int a = 1;
            const int b = 2;
            unchecked 
            [|{|]
                unsafe
                [|{|]
                    [|lock(l)|]
                    [|{|]
                        [|;|]
                    [|}|]    
                [|}|]    
            [|}|]    
        [|}|]
    [|}|]
}");
        }

        [Fact]
        public void TestSimpleLambdaBody()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    Func<string> f = s => [|F$$oo()|];
  }
}");
        }

        [Fact]
        public void TestParenthesizedLambdaBody()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    Func<string> f = (s, i) => [|F$$oo()|];
  }
}");
        }

        [Fact]
        public void TestForStatementInitializer1a()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
$$    for ([|i = 0|], j = 0; i < 10 && j < 10; i++, j++)
    {
    }
  }
}");
        }

        [Fact]
        public void TestForStatementInitializer1b()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    f$$or ([|i = 0|], j = 0; i < 10 && j < 10; i++, j++)
    {
    }
  }
}");
        }

        [Fact]
        public void TestForStatementInitializer1c()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    for ([|i $$= 0|], j = 0; i < 10 && j < 10; i++, j++)
    {
    }
  }
}");
        }

        [Fact]
        public void TestForStatementInitializer1d()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    for 
$$    (          
[|i = 0|], j = 0; i < 10 && j < 10; i++, j++)
    {
    }
  }
}");
        }

        [Fact]
        public void TestForStatementInitializer2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    for (i = 0, [|$$j = 0|]; i < 10 && j < 10; i++, j++)
    {
    }
  }
}");
        }

        [Fact]
        public void TestForStatementInitializer3()
        {
            TestSpan("class C { void M() { for([|i = 0$$|]; ; }; }");
        }

        [Fact]
        public void TestForStatementCondition()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    for (i = 0, j = 0; [|i < 10 && j < $$10|]; i++, j++)
    {
    }
  }
}");
        }

        [Fact]
        public void TestForStatementIncrementor1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    for (i = 0, j = 0; i < 10 && j < 10; [|i+$$+|], j++)
    {
    }
  }
}");
        }

        [Fact]
        public void TestForStatementIncrementor2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    for (i = 0, j = 0; i < 10 && j < 10; i++, [|$$j++|])
    {
    }
  }
}");
        }

        [Fact]
        public void TestForEachStatementExpression()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    foreach (var v in [|Foo().B$$ar()|])
    {
    }
  }
}");
        }

        [Fact]
        public void TestFirstFromClauseExpression()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|var q = from x in bl$$ah()
            from y in quux().z()
            select y;|]
  }
}");
        }

        [Fact]
        public void TestSecondFromClauseExpression()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    var q = from x in blah()
            from y in [|quux().z$$()|]
            select y;
  }
}");
        }

        [Fact]
        public void TestFromInQueryContinuation1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    var q = from x in blah()
            from y in quux().z()
            group x by y into g
                from m in [|g.C$$ount()|]
                select m.Blah();
  }
}");
        }

        [Fact]
        public void TestFromInQueryContinuation2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    var q = from x in blah()
            from y in quux().z()
            group x by y into g
     $$          from m in [|g.Count()|]
                select m.Blah();
  }
}");
        }

        [Fact]
        public void TestJoinClauseLeftExpression()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    var q = from x in blah()
            join a in alpha on [|left().exp$$r()|] equals right().expr()
            select y;
  }
}");
        }

        [Fact]
        public void TestJoinClauseRightExpression()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    var q = from x in blah()
            join a in alpha on left().expr() equals [|righ$$t().expr()|]
            select y;
  }
}");
        }

        [Fact]
        public void TestLetClauseExpression()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    var q = from x in blah()
            join a in alpha on left().expr() equals right.expr()
            let a = [|expr().$$expr()|]
            select y;
  }
}");
        }

        [Fact]
        public void TestWhereClauseExpression()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    var q = from x in blah()
            join a in alpha on left().expr() equals right.expr()
            where [|expr().$$expr()|]
            select y;
  }
}");
        }

        [Fact]
        public void TestWhereClauseKeyword()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    var q = from x in blah()
            whe$$re [|expr().expr()|]
            select y;
  }
}");
        }

        [Fact]
        public void TestSimpleOrdering1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    var q = from x in blah()
            join a in alpha on left().expr() equals right.expr()
            orderby [|expr().$$expr()|]
            select y;
  }
}");
        }

        [Fact]
        public void TestSimpleOrdering2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    var q = from x in blah()
            join a in alpha on left().expr() equals right.expr()
            orderby foo, [|expr().$$expr()|]
            select y;
  }
}");
        }

        [Fact]
        public void TestAscendingOrdering1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    var q = from x in blah()
            join a in alpha on left().expr() equals right.expr()
            orderby [|expr().$$expr()|] ascending
            select y;
  }
}");
        }

        [Fact]
        public void TestAscendingOrdering2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    var q = from x in blah()
            join a in alpha on left().expr() equals right.expr()
            orderby foo, [|expr().$$expr()|] ascending
            select y;
  }
}");
        }

        [Fact]
        public void TestDescendingOrdering1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    var q = from x in blah()
            join a in alpha on left().expr() equals right.expr()
            orderby [|expr().$$expr()|] descending
            select y;
  }
}");
        }

        [Fact]
        public void TestDescendingOrdering2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    var q = from x in blah()
            join a in alpha on left().expr() equals right.expr()
            orderby foo, [|expr().$$expr()|] descending
            select y;
  }
}");
        }

        [Fact]
        public void TestOrderByKeyword()
        {
            TestSpan("class C { void M() { from string s in null ord$$erby [|s.A|] ascending } }");
        }

        [Fact]
        public void TestAscendingKeyword()
        {
            TestSpan("class C { void M() { from string s in null orderby [|s.A|] $$ascending } }");
        }

        [Fact]
        public void TestSelectExpression()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    var q = from x in blah()
            join a in alpha on left().expr() equals right.expr()
            orderby foo, expr().expr() descending
            select [|y.$$blah()|];
  }
}");
        }

        [Fact]
        public void TestAnonymousTypeAfterSelect()
        {
            TestSpan(
@"class C
{
    public void Test()
    {
        var q =
            from c in categories
            join p in products on c equals p.Category into ps
            select [|new { Category = c, $$Products = ps }|];
    }
}");
        }

        [Fact]
        public void TestGroupExpression()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    var q = from x in blah()
            join a in alpha on left().expr() equals right.expr()
            orderby foo, expr().expr() descending
            group [|bar()$$.foo()|] by blah().zap()
            select y.blah();
  }
}");
        }

        [Fact]
        public void TestGroupByKeyword()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    var q = from x in blah()
            join a in alpha on left().expr() equals right.expr()
            orderby foo, expr().expr() descending
            group [|bar().foo()|] b$$y blah().zap()
            select y.blah();
  }
}");
        }

        [Fact]
        public void TestGroupByExpression()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    var q = from x in blah()
            join a in alpha on left().expr() equals right.expr()
            orderby foo, expr().expr() descending
            group bar().foo() by [|blah()$$.zap()|]
            select y.blah();
  }
}");
        }

        [Fact]
        public void TestFieldDeclarator_WithoutInitializer1()
        {
            TestMissing(
@"class C
{
    int $$i;
}");
        }

        [Fact]
        public void TestFieldDeclarator_WithoutInitializer2()
        {
            TestMissing(
@"class C
{
    pri$$vate int i;
}");
        }

        [Fact]
        public void TestFieldDeclarator1()
        {
            TestSpan(
@"class C
{
    [|int $$i = 1;|]
}");
        }

        [Fact]
        public void TestFieldDeclarator2()
        {
            TestSpan(
@"class C
{
    [|private int $$i = 1;|]
}");
        }

        [Fact]
        public void TestFieldDeclarator3()
        {
            TestSpan(
@"class C
{
    [Foo]
    [|private int $$i = 0;|]
}");
        }

        [Fact]
        public void TestFieldDeclarator4()
        {
            TestSpan(
@"class C
{
    [|pri$$vate int i = 1;|]
}");
        }

        [Fact]
        public void TestFieldDeclarator5()
        {
            TestSpan(
@"class C
{
$$    [|private int i = 3;|]
}");
        }

        [Fact]
        public void TestConstVariableDeclarator0()
        {
            TestMissing("class C { void Foo() { const int a = $$1; } }");
        }

        [Fact]
        public void TestConstVariableDeclarator1()
        {
            TestMissing("class C { void Foo() { const $$int a = 1; } }");
        }

        [Fact]
        public void TestConstVariableDeclarator2()
        {
            TestMissing("class C { void Foo() { $$const int a = 1; } }");
        }

        [Fact]
        public void TestConstFieldVariableDeclarator0()
        {
            TestMissing("class C { const int a = $$1; }");
        }

        [Fact]
        public void TestConstFieldVariableDeclarator1()
        {
            TestMissing("class C { const $$int a = 1; }");
        }

        [Fact]
        public void TestConstFieldVariableDeclarator2()
        {
            TestMissing("class C { $$const int a = 1; }");
        }

        [Fact]
        [WorkItem(538777)]
        public void TestVariableDeclarator0()
        {
            TestMissing("class C { void Foo() { int$$ } }");
        }

        [Fact]
        public void TestVariableDeclarator1()
        {
            TestMissing(
@"class C
{
  void Foo()
  {
    int $$i;
  }
}");
        }

        [Fact]
        public void TestVariableDeclarator2a()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|int $$i = 0;|]
  }
}");
        }

        [Fact]
        public void TestVariableDeclarator2b()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
  $$  [|int i = 0;|]
  }
}");
        }

        [Fact]
        public void TestVariableDeclarator2c()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|$$int i = 0;|]
  }
}");
        }

        [Fact]
        public void TestVariableDeclarator3a()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    int i = 0, [|$$j = 3|];
  }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingBreakpoints)]
        public void TestVariableDeclarator3b()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|int i = 0|], $$j;
  }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.DebuggingBreakpoints)]
        public void TestVariableDeclarator3c()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    int $$i, [|j = 0|];
  }
}");
        }

        [Fact]
        public void TestVariableDeclarator4()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    int i = 0, [|j = $$1|];
  }
}");
        }

        [Fact]
        public void TestVariableDeclarator5()
        {
            TestSpan(
@"class C
{
  [|int $$i = 0;|]
}");
        }

        [Fact]
        public void TestVariableDeclarator6()
        {
            TestSpan(
@"class C
{
  [|int i = 0|], $$j;
}");
        }

        [Fact]
        public void TestVariableDeclarator7()
        {
            TestSpan(
@"class C
{
  private int i = 0, [|j = $$1|];
}");
        }

        [Fact]
        public void TestVariableDeclarator8()
        {
            TestSpan(
@"class C
{
  [|priv$$ate int i = 0|], j = 1;
}");
        }

        [Fact]
        public void TestVariableDeclarator9()
        {
            TestSpan(
@"class C
{
$$  [|private int i = 0|], j = 1;
}");
        }

        [Fact]
        public void TestVariableDeclarator10()
        {
            TestSpan("class C { void M() { [|int i = 0$$;|] } }");
        }

        [Fact]
        public void TestVariableDeclarator_Separators0()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
$$    [|int i = 0|], j = 1, k = 2;
  }
}");
        }

        [Fact]
        public void TestVariableDeclarator_Separators1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|int i = 0|]$$, j = 1, k = 2;
  }
}");
        }

        [Fact]
        public void TestVariableDeclarator_Separators2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    int i = 0, [|j = 1|]$$, k = 2;
  }
}");
        }

        [Fact]
        public void TestVariableDeclarator_Separators3()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    int i = 0, j = 1,$$ [|k = 2|];
  }
}");
        }

        [Fact]
        public void TestVariableDeclarator_Separators4()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    int i = 0, j = 1, [|k = 2|]$$;
  }
}");
        }

        [Fact]
        public void TestVariableDeclarator_Separators5()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|int i = 0|], j = 1, k = 2;$$
  }
}");
        }

        [Fact]
        public void TestVariableDeclarator_Separators6()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    int i = 1, j, $$k, [|l = 2|];
  }
}");
        }

        [Fact]
        public void TestVariableDeclarator_Separators7()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    int i$$, j, k, [|l = 2|];
  }
}");
        }

        [Fact]
        public void TestVariableDeclarator_Separators8()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|int i = 2|], j, k, l$$;
  }
}");
        }

        [Fact]
        public void TestVariableDeclarator_Separators9()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    int i, j, [|k = 1|], m, l = 2;$$
  }
}");
        }

        [Fact]
        public void TestEventFieldDeclarator1()
        {
            TestSpan(
@"class C
{
$$    [|public event EventHandler MyEvent = delegate { };|]
}");
        }

        [Fact]
        public void TestEventFieldDeclarator2()
        {
            TestSpan(
@"class C
{
    [|pub$$lic event EventHandler MyEvent = delegate { };|]
}");
        }

        [Fact]
        public void TestEventFieldDeclarator3()
        {
            TestSpan(
@"class C
{
    [|public ev$$ent EventHandler MyEvent = delegate { };|]
}");
        }

        [Fact]
        public void TestEventFieldDeclarator4()
        {
            TestSpan(
@"class C
{
    [|public event EventHan$$dler MyEvent = delegate { };|]
}");
        }

        [Fact]
        public void TestEventFieldDeclarator5()
        {
            TestSpan(
@"class C
{
    [|public event EventHandler MyE$$vent = delegate { };|]
}");
        }

        [Fact]
        public void TestEventFieldDeclarator6()
        {
            TestSpan(
@"class C
{
    [|public event EventHandler MyEvent $$= delegate { };|]
}");
        }

        [Fact]
        public void TestEventFieldDeclarator7()
        {
            TestSpan(
@"class C
{
    [|public event EventHandler MyEvent = del$$egate { };|]
}");
        }

        [Fact]
        public void TestEventFieldDeclarator8()
        {
            TestSpan(
@"class C
{
    public event EventHandler MyEvent = delegate [|{|] $$ };
}");
        }

        [Fact]
        public void TestEventAccessorAdd()
        {
            TestSpan("class C { eve$$nt Action Foo { add [|{|] } remove { } } }");
        }

        [Fact]
        public void TestEventAccessorAdd2()
        {
            TestSpan("class C { event Action Foo { ad$$d [|{|] } remove { } } }");
        }

        [Fact]
        public void TestEventAccessorRemove()
        {
            TestSpan("class C { event Action Foo { add { } $$remove [|{|] } } }");
        }

        [Fact]
        public void TestElseClauseWithBlock()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    if (bar)
    {
    }
    el$$se
    [|{|]
    }
  }
}");
        }

        [Fact]
        public void TestElseClauseWithStatement()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    if (bar)
    {
    }
    el$$se
      [|Foo();|]
  }
}");
        }

        [Fact]
        public void TestElseIf()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    if (bar)
    {
    }
    el$$se [|if (baz)|]
      Foo();
  }
}");
        }

        [Fact]
        public void TestEmptyCatch()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    try
    {
    }
    [|cat$$ch|]
    {
    }
  }
}");
        }

        [Fact]
        public void TestCatchWithType()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    try
    {
    }
    [|cat$$ch(Exception)|]
    {
    }
  }
}");
        }

        [Fact]
        public void TestCatchWithTypeInType()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    try
    {
    }
    [|catch(Exce$$ption)|]
    {
    }
  }
}");
        }

        [Fact]
        public void TestCatchWithTypeAndNameInType()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    try
    {
    }
    [|catch(Exce$$ption e)|]
    {
    }
  }
}");
        }

        [Fact]
        public void TestCatchWithTypeAndNameInName()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    try
    {
    }
    [|catch(Exception $$e)|]
    {
    }
  }
}");
        }

        [Fact]
        public void TestFilter1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    try
    {
    }
    $$catch(Exception e) [|when (e.Message != null)|]
    {
    }
  }
}");
        }

        [Fact]
        public void TestFilter3()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    try
    {
    }
    catch(Exception e)$$ [|when (e.Message != null)|]
    {
    }
  }
}");
        }

        [Fact]
        public void TestFilter4()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    try
    {
    }
    catch(Exception e) [|if$$ (e.Message != null)|]
    {
    }
  }
}");
        }

        [Fact]
        public void TestFilter5()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    try
    {
    }
    catch(Exception e) [|if (e.Message != null)|]      $$
    {
    }
  }
}");
        }

        [Fact]
        public void TestSimpleFinally()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    try
    {
    }
    final$$ly
    [|{|]
    }
  }
}");
        }

        [Fact]
        public void TestFinallyWithCatch()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    try
    {
    }
    catch
    {
    }
    final$$ly
    [|{|]
    }
  }
}");
        }

        [Fact]
        public void TestSwitchLabelWithBlock()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    switch (foo)
    {
        case $$1:
            [|{|]
            }
    }
  }
}");
        }

        [Fact]
        public void TestSwitchLabelWithStatement()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    switch (foo)
    {
        cas$$e 1:
            [|foo();|]
    }
  }
}");
        }

        [Fact]
        public void TestSwitchLabelWithStatement2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    switch (foo)
    {
        cas$$e 1:
            [|foo();|]
            bar();
    }
  }
}");
        }

        [Fact]
        public void TestSwitchLabelWithoutStatement()
        {
            TestSpan("class C { void M() { [|switch |]{ case 1$$: } } }");
        }

        [Fact]
        public void TestMultipleLabelsOnFirstLabel()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    switch (foo)
    {
        cas$$e 1:
        case 2:
            [|foo();|]

        case 3:
        default:
            bar();
    }
  }
}");
        }

        [Fact]
        public void TestMultipleLabelsOnSecondLabel()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    switch (foo)
    {
        case 1:
        cas$$e 2:
            [|foo();|]

        case 3:
        default:
            bar();
    }
  }
}");
        }

        [Fact]
        public void TestMultipleLabelsOnLabelWithDefault()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    switch (foo)
    {
        case 1:
        case 2:
            foo();

        cas$$e 3:
        default:
            [|bar();|]
    }
  }
}");
        }

        [Fact]
        public void TestMultipleLabelsOnDefault()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    switch (foo)
    {
        case 1:
        case 2:
            foo();

        case 3:
        default:$$
            [|bar();|]
    }
  }
}");
        }

        [Fact]
        public void TestBlockBeforeStartToken()
        {
            TestSpan(
@"class C
{
  void Foo()
  [|$${|]
    
  }
}");
        }

        [Fact]
        public void TestBlockBeforeStartToken2()
        {
            TestSpan(
@"class C
{
  void Foo()
  $$ [|{|]
    
  }
}");
        }

        [Fact]
        public void TestBlockAfterStartToken()
        {
            TestSpan(
@"class C
{
  void Foo()
  [|{$$|]
    
  }
}");
        }

        [Fact]
        public void TestBlockAfterStartToken2()
        {
            TestSpan(
@"class C
{
  void Foo()
  [|{|] $$
    
  }
}");
        }

        [Fact]
        public void TestBlockBeforeEndToken1()
        {
            TestSpan(
@"class C
{
  void Foo()
  { 
  $$[|}|]
}");
        }

        [Fact]
        public void TestBlockBeforeEndToken2()
        {
            TestSpan(
@"class C
{
  void Foo()
  { 
  $$ [|}|]
}");
        }

        [Fact]
        public void TestBlockAfterEndToken1()
        {
            TestSpan(
@"class C
{
  void Foo()
  { 
  [|}|]$$
}");
        }

        [Fact]
        public void TestBlockAfterEndToken2()
        {
            TestSpan(
@"class C
{
  void Foo()
  { 
  [|}|] $$
}");
        }

        [Fact]
        public void TestSingleDeclarationOnType()
        {
            TestMissing(
@"class C
{
  void Foo()
  {
    i$$nt i;
  }
}");
        }

        [Fact]
        public void TestMutlipleDeclarationsOnType()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|i$$nt i = 0|], j = 1;
  }
}");
        }

        [Fact]
        public void TestLabel()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    fo$$o:
        [|bar();|]
  }
}");
        }

        [Fact]
        public void TestWhileInWhile()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|w$$hile (expr)|]
    {
    }
  }
}");
        }

        [Fact]
        public void TestWhileInExpr()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|while (ex$$pr)|]
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnWhileBlock()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    while (expr)
  $$ [|{|]
    }
  }
}");
        }

        [Fact]
        public void TestOnDoKeyword()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    d$$o
    [|{|]
    }
    while(expr);
  }
}");
        }

        [Fact]
        public void TestOnDoBlock()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    do
  $$ [|{|]
    }
    while(expr);
  }
}");
        }

        [Fact]
        public void TestOnDoWhile()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    do
    {
    }
    [|wh$$ile(expr);|]
  }
}");
        }

        [Fact]
        public void TestOnDoWhile_MissingSemicolon()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    do
    {
    }
    [|wh$$ile(expr)|]
  }
}");
        }

        [Fact]
        public void TestOnDoExpression()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    do
    {
    }
    [|while(ex$$pr);|]
  }
}");
        }

        [Fact]
        public void TestOnForWithDeclaration1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    f$$or ([|int i = 0|], j = 1; i < 10 && j < 10; i++, j++)
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnForWithDeclaration2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    f$$or ([|int i = 0|]; i < 10 && j < 10; i++, j++)
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnForWithCondition()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    f$$or (; [|i < 10 && j < 10|]; i++, j++)
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnForWithIncrementor1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    f$$or (; ; [|i++|], j++)
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnForWithIncrementor2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    f$$or (; ; [|i++|])
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnEmptyFor()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    f$$or (; ; )
    [|{|]
    }
  }
}");
        }

        [Fact]
        public void TestOnForEachKeyword1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
$$    [|foreach|] (var v in expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnForEachKeyword2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|fo$$reach|] (var v in expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnForEachKeyword3()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|foreach|]    $$    
(var v in expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnForEachKeyword4()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|foreach|]        
$$         (var v in expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnForEachKeyword5()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|foreach|] $$(var v in expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnForEachType1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    foreach (   $$   
[|var v|] in expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnForEachType2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    foreach ([|v$$ar v|] in expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnForEachIdentifier()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    foreach ([|var v$$v|] in expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnForEachIn1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    foreach (var v [|i$$n|] expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnForEachIn2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    foreach (var v 
$$         [|in|] expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnForEachIn3()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    foreach (var v 
         [|in|] $$
expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnForEachExpr1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    foreach (var v in [|expr($$).blah()|])
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnForEachExpr2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    foreach (var v in [|expr().blah()|]   
     $$    )
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnForEachExpr3()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    foreach (var v in 
   $$ [|expr().blah()|]   
     )
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnForEachStatement()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|foreach|](var v in expr().blah())    $$ 
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnForEachBlock1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    foreach (var v in expr().blah())
  $$ [|{|]
    }
  }
}");
        }

        [Fact]
        public void TestOnUsingWithDecl1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    us$$ing ([|var v = foo()|])
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnUsingWithDecl2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    us$$ing ([|var v = foo()|], x = bar())
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnUsingWithDeclType()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    using ([|v$$ar v = foo()|])
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnUsingWithDeclIdentifier1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    using ([|var v$$v = foo()|])
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnUsingWithDeclIdentifier2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    using ([|var vv = foo()|])     $$
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnUsingWithDeclIdentifier3()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
$$    using ([|var vv = foo()|])     
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnUsingWithDeclExpression()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    using ([|var vv = fo$$o()|])
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnUsingWithExpression1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|usi$$ng (foo().bar())|]
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnUsingWithExpression2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|using (foo$$().bar())|]
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnFixed1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    fi$$xed ([|int* i = &j|])
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnFixed2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    fi$$xed ([|int* i = &j|], k = &m)
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnFixed3()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    fixed ([|i$$nt* i = &j|])
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnFixed4()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    fixed ([|int* $$i = &j|])
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnFixed5()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    fixed ([|int* i $$= &j|])
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnFixed6()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    fixed ([|int* i = &$$j|])
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnChecked1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    che$$cked
    [|{|]
    }
  }
}");
        }

        [Fact]
        public void TestOnUnchecked1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    unche$$cked
    [|{|]
    }
  }
}");
        }

        [Fact]
        public void TestOnUnsafe1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    uns$$afe
    [|{|]
    }
  }
}");
        }

        [Fact]
        public void TestOnLock1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|lo$$ck (expr)|]
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnLock2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|lock (ex$$pr)|]
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnIf1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|i$$f (foo().bar())|]
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnIf2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|if (fo$$o().bar())|]
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnIfBlock()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    if (foo().bar())
   $$ [|{|]
    }
  }
}");
        }

        [Fact]
        public void TestOnSwitch1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|swi$$tch (expr)|]
    {
        default:
            foo();
    }
  }
}");
        }

        [Fact]
        public void TestOnSwitch2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|switch (ex$$pr)|]
    {
        default:
            foo();
    }
  }
}");
        }

        [Fact]
        public void TestOnSwitch3()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|switch (expr)|]
  $$ {
        default:
            foo();
    }
  }
}");
        }

        [Fact]
        public void TestOnSwitch4()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|switch (expr)|]
    {
        default:
            foo();
  $$ }
  }
}");
        }

        [Fact]
        public void TestOnTry1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    t$$ry
    [|{|]
    }
    finally
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnTry2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    try
  $$ [|{|]
    }
    finally
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnGotoStatement1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|g$$oto foo;|]
  }
}");
        }

        [Fact]
        public void TestOnGotoStatement2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|goto fo$$o;|]
  }
}");
        }

        [Fact]
        public void TestOnGotoCaseStatement1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    switch (expr)
    {
        case 1:
            [|go$$to case 2;|]
    }
  }
}");
        }

        [Fact]
        public void TestOnGotoCaseStatement2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    switch (expr)
    {
        case 1:
            [|goto ca$$se 2;|]
    }
  }
}");
        }

        [Fact]
        public void TestOnGotoCaseStatement3()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    switch (expr)
    {
        case 1:
            [|goto case $$2;|]
    }
  }
}");
        }

        [Fact]
        public void TestOnGotoDefault1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    switch (expr)
    {
        case 1:
            [|go$$to default;|]
    }
  }
}");
        }

        [Fact]
        public void TestOnGotoDefault2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    switch (expr)
    {
        case 1:
            [|goto defau$$lt;|]
    }
  }
}");
        }

        [Fact]
        public void TestOnBreak1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    while (true)
    {
        [|bre$$ak;|]
    }
  }
}");
        }

        [Fact]
        public void TestOnContinue1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    while (true)
    {
        [|cont$$inue;|]
    }
  }
}");
        }

        [Fact]
        public void TestOnReturn1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|retu$$rn;|]
  }
}");
        }

        [Fact]
        public void TestOnReturn2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|retu$$rn expr();|]
  }
}");
        }

        [Fact]
        public void TestOnReturn3()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|return expr$$().bar();|]
  }
}");
        }

        [Fact]
        public void TestOnYieldReturn1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|yi$$eld return foo().bar();|]
  }
}");
        }

        [Fact]
        public void TestOnYieldReturn2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|yield re$$turn foo().bar();|]
  }
}");
        }

        [Fact]
        public void TestOnYieldReturn3()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|yield return foo()$$.bar();|]
  }
}");
        }

        [Fact]
        public void TestOnYieldBreak1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|yi$$eld break;|]
  }
}");
        }

        [Fact]
        public void TestOnYieldBreak2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|yield brea$$k;|]
  }
}");
        }

        [Fact]
        public void TestOnThrow1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|th$$row;|]
  }
}");
        }

        [Fact]
        public void TestOnThrow2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|thr$$ow new Foo();|]
  }
}");
        }

        [Fact]
        public void TestOnThrow3()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|throw ne$$w Foo();|]
  }
}");
        }

        [Fact]
        public void TestOnThrow4()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|throw new Fo$$o();|]
  }
}");
        }

        [Fact]
        public void TestOnExpressionStatement1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|foo().$$bar();|]
  }
}");
        }

        [Fact]
        public void TestOnEmptyStatement1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|$$;|]
  }
}");
        }

        [Fact]
        public void TestOnEmptyStatement2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    while (true)
    {
   $$ [|;|]
    }
  }
}");
        }

        [Fact]
        public void TestOnPropertyAccessor1()
        {
            TestSpan(
@"class C
{
  int Foo
  {
    g$$et
    [|{|]
    }
  }
}");
        }

        [Fact]
        public void TestOnPropertyAccessor2()
        {
            TestSpan(
@"class C
{
  int Foo
  {
    [|g$$et;|]
  }
}");
        }

        [Fact]
        public void TestOnPropertyAccessor3()
        {
            TestSpan(
@"class C
{
  int Foo
  {
    get
    {
    }

    s$$et
    [|{|]
    }
  }
}");
        }

        [Fact]
        public void TestOnPropertyAccessor4()
        {
            TestSpan(
@"class C
{
  int Foo
  {
    [|s$$et;|]
  }
}");
        }

        [Fact]
        public void TestOnProperty1()
        {
            TestSpan(
@"class C
{
  int F$$oo
  {
    get
    [|{|]
    }
    
    set
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnProperty2()
        {
            TestSpan(
@"class C
{
  int F$$oo
  {
    [|get;|]
    set {} 
  }
}");
        }

        [WorkItem(932711)]
        [Fact]
        public void TestOnPropertyWithInitializer()
        {
            TestSpan(
@"class C
{
    public int Id { get; set; } = [|int.Pa$$rse(""42"")|];
}");

            TestSpan(
@"class C
{
    public int$$ Id { [|get;|] set; } = int.Parse(""42"");
}");

            TestSpan(
@"class C
{
    public int Id { get; [|set;|] $$} = int.Parse(""42"");
}");

            TestSpan(
@"class C
{
    public int Id { get; [|set;|] }$$ = int.Parse(""42"");
}");

            TestSpan(
@"class C
{
    public int Id { get; set; } =$$ [|int.Parse(""42"")|];
}");
        }

        [Fact]
        public void TestOnPropertyExpressionBody1()
        {
            TestSpan(
@"class C
{
    public int Id => [|12$$3|];
}");
        }

        [Fact]
        public void TestOnPropertyExpressionBody2()
        {
            TestSpan(
@"class C
{
    public int Id $$=> [|123|];
}");
        }

        [Fact]
        public void TestOnPropertyExpressionBody3()
        {
            TestSpan(
@"class C
{
    $$public int Id => [|123|];
}");
        }

        [Fact]
        public void TestOnPropertyExpressionBody4()
        {
            TestSpan(
@"class C
{
    public int Id => [|123|];   $$
}");
        }

        [Fact]
        public void TestOnIndexerExpressionBody1()
        {
            TestSpan(
@"class C
{
    public int this[int a] => [|12$$3|];
}");
        }

        [Fact]
        public void TestOnIndexer1()
        {
            TestSpan(
@"class C
{
  int this[int$$ a]
  {
    get
    [|{|]
    }
    
    set
    {
    }
  }
}");
        }

        [Fact]
        public void TestOnIndexer2()
        {
            TestSpan(
@"class C
{
  int this[int$$ a]
  {
    [|get;|]
    set { }
  }
}");
        }

        [Fact]
        public void TestOnIndexerExpressionBody2()
        {
            TestSpan(
@"class C
{
    public int this[int a] $$=> [|123|];
}");
        }

        [Fact]
        public void TestOnIndexerExpressionBody3()
        {
            TestSpan(
@"class C
{
    $$public int this[int a] => [|123|];
}");
        }

        [Fact]
        public void TestOnIndexerExpressionBody4()
        {
            TestSpan(
@"class C
{
    public int this[int a] => [|123|];   $$
}");
        }

        [Fact]
        public void TestOnIndexerExpressionBody5()
        {
            TestSpan(
@"class C
{
    public int this[int $$a] => [|123|];   
}");
        }

        [Fact]
        public void TestOnMethod1()
        {
            TestSpan(
@"class C
{
    v$$oid Foo()
    [|{|]
    }
}");
        }

        [Fact]
        public void TestOnMethod2()
        {
            TestSpan(
@"class C
{
    void F$$oo()
    [|{|]
    }
}");
        }

        [Fact]
        public void TestOnMethod3()
        {
            TestSpan(
@"class C
{
    void Foo(in$$t i)
    [|{|]
    }
}");
        }

        [Fact]
        public void TestOnMethod4()
        {
            TestSpan(
@"class C
{
    void Foo(int $$i)
    [|{|]
    }
}");
        }

        [Fact]
        public void TestOnMethod5()
        {
            TestSpan(
@"class C
{
    void Foo(int i = f$$oo)
    [|{|]
    }
}");
        }

        [Fact]
        public void TestOnMethodWithExpressionBody1()
        {
            TestSpan(
@"class C
{
    v$$oid Foo() => [|123|];
}");
        }

        [Fact]
        public void TestOnMethodWithExpressionBody2()
        {
            TestSpan(
@"class C
{
    void Foo() =>$$ [|123|];
}");
        }

        [Fact]
        public void TestOnMethodWithExpressionBody3()
        {
            TestSpan(
@"class C
{
    void Foo() => [|123|]; $$
}");
        }

        [Fact]
        public void TestOnMethodWithExpressionBody4()
        {
            TestSpan(
@"class C
{
    void Foo() => [|12$$3|]; 
}");
        }

        [Fact]
        public void TestMissingOnMethod()
        {
            TestMissing(
@"class C
{
    void Foo($$);
}");
        }

        [Fact]
        public void InstanceConstructor_NoInitializer()
        {
            // a sequence point for base constructor call
            TestSpan(
@"class C
{
    [|pub$$lic C()|]
    {
    }
}");
        }

        [Fact]
        public void InstanceConstructor_NoInitializer_Attributes()
        {
            TestSpan(
@"class C
{
    [Attribute1,$$ Attribute2]
    [Attribute3]
    [|public 

        C()|]
    {
    }
}");
        }

        [Fact]
        public void InstanceConstructor_BaseInitializer()
        {
            // a sequence point for base constructor call
            TestSpan(
@"class C
{
    pub$$lic C()
        : [|base(42)|]
    {
    }
}");
        }

        [Fact]
        public void InstanceConstructor_ThisInitializer()
        {
            // a sequence point for this constructor call
            TestSpan(
@"class C
{
    pub$$lic C()
        : [|this(42)|]
    {
    }
}");
        }

        [Fact]
        public void StaticConstructor()
        {
            TestSpan(
@"class C
{
    $$static C()
    [|{|]
    }
}");
        }

        [Fact]
        public void InstanceConstructorInitializer()
        {
            // a sequence point for this constructor call
            TestSpan(
@"class Derived : Base
{
    public Derived()
        : [|this($$42)|]
    {
    }
}");
        }

        [WorkItem(543968)]
        [Fact]
        public void TestConstructorInitializer()
        {
            // a sequence point for base constructor call
            TestSpan(
@"class Derived : Base
{
    public Derived()
        : [|base($$42)|]
    {
    }
}
");
        }

        [Fact]
        public void TestOnStaticConstructor()
        {
            TestSpan(
@"class C
{
    st$$atic C()
    [|{|]
    }
}");
        }

        [Fact]
        public void TestOnDestructor()
        {
            TestSpan(
@"class C
{
    ~C$$()
    [|{|]
    }
}");
        }

        [Fact]
        public void TestOnOperator()
        {
            TestSpan(
@"class C
{
    public static int op$$erator+(C c1, C c2)
    [|{|]
    }
}");
        }

        [Fact]
        public void TestOnOperatorWithExpressionBody1()
        {
            TestSpan(
@"class C
{
    public static int op$$erator+(C c1, C c2) => [|c1|];
}");
        }

        [Fact]
        public void TestOnOperatorWithExpressionBody2()
        {
            TestSpan(
@"class C
{
    public static int operator+(C c1, C c2) =>$$ [|c1|];
}");
        }

        [Fact]
        public void TestOnOperatorWithExpressionBody3()
        {
            TestSpan(
@"class C
{
    public static int operator+(C c1, C c2) => [|c1|]; $$
}");
        }

        [Fact]
        public void TestOnOperatorWithExpressionBody4()
        {
            TestSpan(
@"class C
{
    public static int operator+(C c1, C c2) => [|c$$1|];
}");
        }

        [Fact]
        public void TestOnConversionOperator()
        {
            TestSpan(
@"class C
{
    public static op$$erator DateTime(C c1)
    [|{|]
    }
}");
        }

        [Fact]
        public void TestOnConversionOperatorWithExpressionBody1()
        {
            TestSpan(
@"class C
{
    public static op$$erator DateTime(C c1) => [|DataTime.Now|];
}");
        }

        [Fact]
        public void TestOnConversionOperatorWithExpressionBody2()
        {
            TestSpan(
@"class C
{
    public static operator DateTime(C c1) =>$$ [|DataTime.Now|];
}");
        }

        [Fact]
        public void TestOnConversionOperatorWithExpressionBody3()
        {
            TestSpan(
@"class C
{
    public static operator DateTime(C c1) => [|DataTime.Now|];$$
}");
        }

        [Fact]
        public void TestOnConversionOperatorWithExpressionBody4()
        {
            TestSpan(
@"class C
{
    public static operator DateTime(C c1) => [|DataTime$$.Now|];
}");
        }

        [Fact]
        public void TestInFrontOfFirstFromClause()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    [|var q =
    $$   from x in blah()
        from y in zap()
        let m = quux()
        join a in alpha on left().expr() equals right.expr()
        orderby foo, expr().expr() descending
        group bar().foo() by blah().zap() into g
        select y.blah();|]
  }
}");
        }

        [Fact]
        public void TestInFrontOfSecondFromClause()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    var q =
        from x in blah()
    $$   from y in [|zap()|]
        let m = quux()
        join a in alpha on left().expr() equals right.expr()
        orderby foo, expr().expr() descending
        group bar().foo() by blah().zap() into g
        select y.blah();
  }
}");
        }

        [Fact]
        public void TestInFrontOfLetClause()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    var q =
        from x in blah()
        from y in zap()
    $$   let m = [|quux()|]
        join a in alpha on left().expr() equals right.expr()
        orderby foo, expr().expr() descending
        group bar().foo() by blah().zap() into g
        select y.blah();
  }
}");
        }

        [Fact]
        public void TestInFrontOfJoinClause()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    var q =
        from x in blah()
        from y in zap()
        let m = quux()
    $$   join a in alpha on [|left().expr()|] equals right.expr()
        orderby foo, expr().expr() descending
        group bar().foo() by blah().zap() into g
        select y.blah();
  }
}");
        }

        [Fact]
        public void TestInFrontOfOrderByClause()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    var q =
        from x in blah()
        from y in zap()
        let m = quux()
        join a in alpha on left().expr() equals right.expr()
    $$   orderby [|foo|], expr().expr() descending
        group bar().foo() by blah().zap() into g
        select y.blah();
  }
}");
        }

        [Fact]
        public void TestInFrontOfGroupByClause()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    var q =
        from x in blah()
        from y in zap()
        let m = quux()
        join a in alpha on left().expr() equals right.expr()
        orderby foo, expr().expr() descending
    $$   group [|bar().foo()|] by blah().zap() into g
        select y.blah();
  }");
        }

        [Fact]
        public void TestInFrontOfSelectClause()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    var q =
        from x in blah()
        from y in zap()
        let m = quux()
        join a in alpha on left().expr() equals right.expr()
        orderby foo, expr().expr() descending
        group bar().foo() by blah().zap() into g
    $$   select [|y.blah()|];
  }");
        }

        [WorkItem(3557, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void TestInFrontOfAttribute()
        {
            TestSpan(
@"class C
{
$$ [method: Obsolete]
  void Foo()
  [|{|]
  }
}");
        }

        [WorkItem(538058)]
        [Fact]
        public void TestInInactivePPRegion()
        {
            TestLine(
@"

#if blahblah
$$fooby
#endif");
        }

        [WorkItem(538777)]
        [Fact]
        public void TestWithIncompleteDeclaration()
        {
            TestMissing(
@"
clas C
{
    void Foo()
    {
$$        int
    }
}");
        }

        [WorkItem(937290)]
        [Fact]
        public void TestOnGetter()
        {
            TestSpan(
@"class C
{
    public int $$Id { [|get;|] set; }
}");

            TestSpan(
@"class C
{
    public int Id { [|g$$et;|] set; }
}");

            TestSpan(
@"class C
{
    public int Id { g$$et [|{|] return 42; } set {} }
}");

            TestSpan(
@"class C
{
    public int$$ Id { get [|{|] return 42; } set {} }
}");
        }

        [WorkItem(937290)]
        [Fact]
        public void TestOnSetter()
        {
            TestSpan(
@"class C
{
    public int Id { get; [|se$$t;|] }
}");

            TestSpan(
@"class C
{
    public int Id { get; [|set;|] $$ }
}");

            TestSpan(
@"class C
{
    public int $$Id { [|set;|] get; }
}");

            TestSpan(
@"class C
{
    public int Id { get { return 42; } s$$et [|{|] } }
}");

            TestSpan(
@"class C
{
    public int Id { get { return 42; } set { [|}|] $$}
}");
        }
    }
}
