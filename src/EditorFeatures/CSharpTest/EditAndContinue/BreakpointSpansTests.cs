// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.EditAndContinue;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.Debugging
{
    [Trait(Traits.Feature, Traits.Features.DebuggingBreakpoints)]
    public class BreakpointSpansTests
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
            MarkupTestFile.GetPositionAndSpan(
                markup, out var source, out var position, out TextSpan? expectedSpan);
            var tree = SyntaxFactory.ParseSyntaxTree(source, options);

            var hasBreakpoint = BreakpointSpans.TryGetBreakpointSpan(
                tree, position.Value, CancellationToken.None, out var breakpointSpan);

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
            MarkupTestFile.GetPositionAndSpans(markup,
                out var source, out var position, out ImmutableArray<TextSpan> expectedSpans);

            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var root = tree.GetRoot();

            var actualSpans = GetBreakpointSequence(root, position).ToArray();

            AssertEx.Equal(expectedSpans, actualSpans,
                itemSeparator: "\r\n",
                itemInspector: span => "[|" + source.Substring(span.Start, span.Length) + "|]");
        }

        public static IEnumerable<TextSpan> GetBreakpointSequence(SyntaxNode root, int position)
        {
            var endPosition = root.Span.End;
            var lastSpanEnd = 0;
            while (position < endPosition)
            {
                if (BreakpointSpans.TryGetClosestBreakpointSpan(root, position, out var span) && span.End > lastSpanEnd)
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
    void Goo()
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
    void Goo()
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
    int Goo()
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
    IEnumerable<int> Goo()
    $$[|{|]
        [|yield return 1;|]
        [|foreach|] ([|var f|] [|in|] [|Goo()|])
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
    IEnumerable<int> Goo()
    $$[|{|][|while(t)|][|{|][|}|][|}|]
}");
        }

        [Fact]
        public void GetBreakpointSequence6()
        {
            TestAll(@"
class C
{
    IEnumerable<int> Goo()
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
        public void ForStatementInitializer1a()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
$$    for ([|i = 0|], j = 0; i < 10 && j < 10; i++, j++)
    {
    }
  }
}");
        }

        [Fact]
        public void ForStatementInitializer1b()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    f$$or ([|i = 0|], j = 0; i < 10 && j < 10; i++, j++)
    {
    }
  }
}");
        }

        [Fact]
        public void ForStatementInitializer1c()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    for ([|i $$= 0|], j = 0; i < 10 && j < 10; i++, j++)
    {
    }
  }
}");
        }

        [Fact]
        public void ForStatementInitializer1d()
        {
            TestSpan(
@"class C
{
  void Goo()
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
        public void ForStatementInitializer2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    for (i = 0, [|$$j = 0|]; i < 10 && j < 10; i++, j++)
    {
    }
  }
}");
        }

        [Fact]
        public void ForStatementInitializer3()
        {
            TestSpan("class C { void M() { for([|i = 0$$|]; ; }; }");
        }

        [Fact]
        public void ForStatementCondition()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    for (i = 0, j = 0; [|i < 10 && j < $$10|]; i++, j++)
    {
    }
  }
}");
        }

        [Fact]
        public void ForStatementIncrementor1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    for (i = 0, j = 0; i < 10 && j < 10; [|i+$$+|], j++)
    {
    }
  }
}");
        }

        [Fact]
        public void ForStatementIncrementor2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    for (i = 0, j = 0; i < 10 && j < 10; i++, [|$$j++|])
    {
    }
  }
}");
        }

        [Fact]
        public void ForEachStatementExpression()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    foreach (var v in [|Goo().B$$ar()|])
    {
    }
  }
}");
        }

        [Fact]
        public void ForEachDeconstructionStatementExpression()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    foreach (var (x, y) in [|Goo().B$$ar()|])
    {
    }
  }
}");
        }

        #region Lambdas

        [Fact]
        public void SimpleLambdaBody()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    Func<string> f = s => [|G$$oo()|];
  }
}");
        }

        [Fact]
        public void ParenthesizedLambdaBody()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    Func<string> f = (s, i) => [|G$$oo()|];
  }
}");
        }

        [Fact]
        public void AnonymousMethod1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    Func<int> f = delegate [|$${|] return 1; };
  }
}");
        }

        [Fact]
        public void AnonymousMethod2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    Func<int> f = delegate { [|$$return 1;|] };
  }
}");
        }

        #endregion

        #region Queries

        [Fact]
        public void FirstFromClauseExpression()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|var q = from x in bl$$ah()
            from y in quux().z()
            select y;|]
  }
}");
        }

        [Fact]
        public void SecondFromClauseExpression()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    var q = from x in blah()
            from y in [|quux().z$$()|]
            select y;
  }
}");
        }

        [Fact]
        public void FromInQueryContinuation1()
        {
            TestSpan(
@"class C
{
  void Goo()
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
        public void FromInQueryContinuation2()
        {
            TestSpan(
@"class C
{
  void Goo()
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
        public void JoinClauseLeftExpression()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    var q = from x in blah()
            join a in alpha on [|left().exp$$r()|] equals right().expr()
            select y;
  }
}");
        }

        [Fact]
        public void JoinClauseRightExpression()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    var q = from x in blah()
            join a in alpha on left().expr() equals [|righ$$t().expr()|]
            select y;
  }
}");
        }

        [Fact]
        public void LetClauseExpression()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    var q = from x in blah()
            join a in alpha on left().expr() equals right.expr()
            let a = [|expr().$$expr()|]
            select y;
  }
}");
        }

        [Fact]
        public void WhereClauseExpression()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    var q = from x in blah()
            join a in alpha on left().expr() equals right.expr()
            where [|expr().$$expr()|]
            select y;
  }
}");
        }

        [Fact]
        public void WhereClauseKeyword()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    var q = from x in blah()
            whe$$re [|expr().expr()|]
            select y;
  }
}");
        }

        [Fact]
        public void SimpleOrdering1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    var q = from x in blah()
            join a in alpha on left().expr() equals right.expr()
            orderby [|expr().$$expr()|]
            select y;
  }
}");
        }

        [Fact]
        public void SimpleOrdering2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    var q = from x in blah()
            join a in alpha on left().expr() equals right.expr()
            orderby goo, [|expr().$$expr()|]
            select y;
  }
}");
        }

        [Fact]
        public void AscendingOrdering1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    var q = from x in blah()
            join a in alpha on left().expr() equals right.expr()
            orderby [|expr().$$expr()|] ascending
            select y;
  }
}");
        }

        [Fact]
        public void AscendingOrdering2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    var q = from x in blah()
            join a in alpha on left().expr() equals right.expr()
            orderby goo, [|expr().$$expr()|] ascending
            select y;
  }
}");
        }

        [Fact]
        public void DescendingOrdering1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    var q = from x in blah()
            join a in alpha on left().expr() equals right.expr()
            orderby [|expr().$$expr()|] descending
            select y;
  }
}");
        }

        [Fact]
        public void DescendingOrdering2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    var q = from x in blah()
            join a in alpha on left().expr() equals right.expr()
            orderby goo, [|expr().$$expr()|] descending
            select y;
  }
}");
        }

        [Fact]
        public void OrderByKeyword()
        {
            TestSpan("class C { void M() { from string s in null ord$$erby [|s.A|] ascending } }");
        }

        [Fact]
        public void AscendingKeyword()
        {
            TestSpan("class C { void M() { from string s in null orderby [|s.A|] $$ascending } }");
        }

        [Fact]
        public void SelectExpression()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    var q = from x in blah()
            join a in alpha on left().expr() equals right.expr()
            orderby goo, expr().expr() descending
            select [|y.$$blah()|];
  }
}");
        }

        [Fact]
        public void AnonymousTypeAfterSelect()
        {
            TestSpan(
@"class C
{
    public void ()
    {
        var q =
            from c in categories
            join p in products on c equals p.Category into ps
            select [|new { Category = c, $$Products = ps }|];
    }
}");
        }

        [Fact]
        public void GroupExpression()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    var q = from x in blah()
            join a in alpha on left().expr() equals right.expr()
            orderby goo, expr().expr() descending
            group [|bar()$$.goo()|] by blah().zap()
            select y.blah();
  }
}");
        }

        [Fact]
        public void GroupByKeyword()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    var q = from x in blah()
            join a in alpha on left().expr() equals right.expr()
            orderby goo, expr().expr() descending
            group [|bar().goo()|] b$$y blah().zap()
            select y.blah();
  }
}");
        }

        [Fact]
        public void GroupByExpression()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    var q = from x in blah()
            join a in alpha on left().expr() equals right.expr()
            orderby goo, expr().expr() descending
            group bar().goo() by [|blah()$$.zap()|]
            select y.blah();
  }
}");
        }

        [Fact]
        public void InFrontOfFirstFromClause()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|var q =
    $$   from x in blah()
        from y in zap()
        let m = quux()
        join a in alpha on left().expr() equals right.expr()
        orderby goo, expr().expr() descending
        group bar().goo() by blah().zap() into g
        select y.blah();|]
  }
}");
        }

        [Fact]
        public void InFrontOfSecondFromClause()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    var q =
        from x in blah()
    $$   from y in [|zap()|]
        let m = quux()
        join a in alpha on left().expr() equals right.expr()
        orderby goo, expr().expr() descending
        group bar().goo() by blah().zap() into g
        select y.blah();
  }
}");
        }

        [Fact]
        public void InFrontOfLetClause()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    var q =
        from x in blah()
        from y in zap()
    $$   let m = [|quux()|]
        join a in alpha on left().expr() equals right.expr()
        orderby goo, expr().expr() descending
        group bar().goo() by blah().zap() into g
        select y.blah();
  }
}");
        }

        [Fact]
        public void InFrontOfJoinClause()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    var q =
        from x in blah()
        from y in zap()
        let m = quux()
    $$   join a in alpha on [|left().expr()|] equals right.expr()
        orderby goo, expr().expr() descending
        group bar().goo() by blah().zap() into g
        select y.blah();
  }
}");
        }

        [Fact]
        public void InFrontOfOrderByClause()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    var q =
        from x in blah()
        from y in zap()
        let m = quux()
        join a in alpha on left().expr() equals right.expr()
    $$   orderby [|goo|], expr().expr() descending
        group bar().goo() by blah().zap() into g
        select y.blah();
  }
}");
        }

        [Fact]
        public void InFrontOfGroupByClause()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    var q =
        from x in blah()
        from y in zap()
        let m = quux()
        join a in alpha on left().expr() equals right.expr()
        orderby goo, expr().expr() descending
    $$   group [|bar().goo()|] by blah().zap() into g
        select y.blah();
  }");
        }

        [Fact]
        public void InFrontOfSelectClause()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    var q =
        from x in blah()
        from y in zap()
        let m = quux()
        join a in alpha on left().expr() equals right.expr()
        orderby goo, expr().expr() descending
        group bar().goo() by blah().zap() into g
    $$   select [|y.blah()|];
  }");
        }

        [Fact]
        public void Select1()
        {
            TestSpan(
@"class C
{
  IEnumerable<int> Goo() => from x in new[] { 1 } select [|$$x|];
}
");
        }

        [Fact]
        public void Select_NoLambda1()
        {
            TestSpan(
@"class C
{
  IEnumerable<int> Goo() => [|from x in new[] { 1 } where x > 0 select $$x|];
}
");
        }

        [Fact]
        public void Select_NoLambda2()
        {
            TestSpan(
@"class C
{
  IEnumerable<int> Goo() => [|from x in new[] { 1 } select x into y orderby y select $$y|];
}
");
        }

        [Fact]
        public void GroupBy1()
        {
            TestSpan(
@"class C
{
  IEnumerable<int> Goo() => from x in new[] { 1 } group x by [|$$x|];
}
");
        }

        [Fact]
        public void GroupBy_NoLambda1()
        {
            TestSpan(
@"class C
{
  IEnumerable<int> Goo() => [|from x in new[] { 1 } group $$x by x|];
}
");
        }

        [Fact]
        public void GroupBy_NoLambda2()
        {
            TestSpan(
@"class C
{
  IEnumerable<int> Goo() => [|from x in new[] { 1 } group $$x by x + 1 into y group y by y.Key + 2|];
}
");
        }

        [Fact]
        public void GroupBy_NoLambda3()
        {
            TestSpan(
@"class C
{
  IEnumerable<int> Goo() => [|from x in new[] { 1 } group x by x + 1 into y group $$y by y.Key + 2|];
}
");
        }

        #endregion

        [Fact]
        public void FieldDeclarator_WithoutInitializer1()
        {
            TestMissing(
@"class C
{
    int $$i;
}");
        }

        [Fact]
        public void FieldDeclarator_WithoutInitializer2()
        {
            TestMissing(
@"class C
{
    pri$$vate int i;
}");
        }

        [Fact]
        public void FieldDeclarator1()
        {
            TestSpan(
@"class C
{
    [|int $$i = 1;|]
}");
        }

        [Fact]
        public void FieldDeclarator2()
        {
            TestSpan(
@"class C
{
    [|private int $$i = 1;|]
}");
        }

        [Fact]
        public void FieldDeclarator3()
        {
            TestSpan(
@"class C
{
    [Goo]
    [|private int $$i = 0;|]
}");
        }

        [Fact]
        public void FieldDeclarator4()
        {
            TestSpan(
@"class C
{
    [|pri$$vate int i = 1;|]
}");
        }

        [Fact]
        public void FieldDeclarator5()
        {
            TestSpan(
@"class C
{
$$    [|private int i = 3;|]
}");
        }

        [Fact]
        public void ConstVariableDeclarator0()
        {
            TestMissing("class C { void Goo() { const int a = $$1; } }");
        }

        [Fact]
        public void ConstVariableDeclarator1()
        {
            TestMissing("class C { void Goo() { const $$int a = 1; } }");
        }

        [Fact]
        public void ConstVariableDeclarator2()
        {
            TestMissing("class C { void Goo() { $$const int a = 1; } }");
        }

        [Fact]
        public void ConstFieldVariableDeclarator0()
        {
            TestMissing("class C { const int a = $$1; }");
        }

        [Fact]
        public void ConstFieldVariableDeclarator1()
        {
            TestMissing("class C { const $$int a = 1; }");
        }

        [Fact]
        public void ConstFieldVariableDeclarator2()
        {
            TestMissing("class C { $$const int a = 1; }");
        }

        [Fact]
        [WorkItem(538777, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538777")]
        public void VariableDeclarator0()
        {
            TestMissing("class C { void Goo() { int$$ } }");
        }

        [Fact]
        public void VariableDeclarator1()
        {
            TestMissing(
@"class C
{
  void Goo()
  {
    int $$i;
  }
}");
        }

        [Fact]
        public void VariableDeclarator2a()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|int $$i = 0;|]
  }
}");
        }

        [Fact]
        public void VariableDeclarator2b()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
  $$  [|int i = 0;|]
  }
}");
        }

        [Fact]
        public void VariableDeclarator2c()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|$$int i = 0;|]
  }
}");
        }

        [Fact]
        public void VariableDeclarator3a()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    int i = 0, [|$$j = 3|];
  }
}");
        }

        [Fact]
        public void VariableDeclarator3b()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|int i = 0|], $$j;
  }
}");
        }

        [Fact]
        public void VariableDeclarator3c()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    int $$i, [|j = 0|];
  }
}");
        }

        [Fact]
        public void VariableDeclarator4()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    int i = 0, [|j = $$1|];
  }
}");
        }

        [Fact]
        public void VariableDeclarator5()
        {
            TestSpan(
@"class C
{
  [|int $$i = 0;|]
}");
        }

        [Fact]
        public void VariableDeclarator6()
        {
            TestSpan(
@"class C
{
  [|int i = 0|], $$j;
}");
        }

        [Fact]
        public void VariableDeclarator7()
        {
            TestSpan(
@"class C
{
  private int i = 0, [|j = $$1|];
}");
        }

        [Fact]
        public void VariableDeclarator8()
        {
            TestSpan(
@"class C
{
  [|priv$$ate int i = 0|], j = 1;
}");
        }

        [Fact]
        public void VariableDeclarator9()
        {
            TestSpan(
@"class C
{
$$  [|private int i = 0|], j = 1;
}");
        }

        [Fact]
        public void VariableDeclarator10()
        {
            TestSpan("class C { void M() { [|int i = 0$$;|] } }");
        }

        [Fact]
        public void VariableDeclarator_Separators0()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
$$    [|int i = 0|], j = 1, k = 2;
  }
}");
        }

        [Fact]
        public void VariableDeclarator_Separators1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|int i = 0|]$$, j = 1, k = 2;
  }
}");
        }

        [Fact]
        public void VariableDeclarator_Separators2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    int i = 0, [|j = 1|]$$, k = 2;
  }
}");
        }

        [Fact]
        public void VariableDeclarator_Separators3()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    int i = 0, j = 1,$$ [|k = 2|];
  }
}");
        }

        [Fact]
        public void VariableDeclarator_Separators4()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    int i = 0, j = 1, [|k = 2|]$$;
  }
}");
        }

        [Fact]
        public void VariableDeclarator_Separators5()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|int i = 0|], j = 1, k = 2;$$
  }
}");
        }

        [Fact]
        public void VariableDeclarator_Separators6()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    int i = 1, j, $$k, [|l = 2|];
  }
}");
        }

        [Fact]
        public void VariableDeclarator_Separators7()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    int i$$, j, k, [|l = 2|];
  }
}");
        }

        [Fact]
        public void VariableDeclarator_Separators8()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|int i = 2|], j, k, l$$;
  }
}");
        }

        [Fact]
        public void VariableDeclarator_Separators9()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    int i, j, [|k = 1|], m, l = 2;$$
  }
}");
        }

        [Fact]
        public void EventFieldDeclarator1()
        {
            TestSpan(
@"class C
{
$$    [|public event EventHandler MyEvent = delegate { };|]
}");
        }

        [Fact]
        public void EventFieldDeclarator2()
        {
            TestSpan(
@"class C
{
    [|pub$$lic event EventHandler MyEvent = delegate { };|]
}");
        }

        [Fact]
        public void EventFieldDeclarator3()
        {
            TestSpan(
@"class C
{
    [|public ev$$ent EventHandler MyEvent = delegate { };|]
}");
        }

        [Fact]
        public void EventFieldDeclarator4()
        {
            TestSpan(
@"class C
{
    [|public event EventHan$$dler MyEvent = delegate { };|]
}");
        }

        [Fact]
        public void EventFieldDeclarator5()
        {
            TestSpan(
@"class C
{
    [|public event EventHandler MyE$$vent = delegate { };|]
}");
        }

        [Fact]
        public void EventFieldDeclarator6()
        {
            TestSpan(
@"class C
{
    [|public event EventHandler MyEvent $$= delegate { };|]
}");
        }

        [Fact]
        public void EventFieldDeclarator7()
        {
            TestSpan(
@"class C
{
    [|public event EventHandler MyEvent = del$$egate { };|]
}");
        }

        [Fact]
        public void EventFieldDeclarator8()
        {
            TestSpan(
@"class C
{
    public event EventHandler MyEvent = delegate [|{|] $$ };
}");
        }

        [Fact]
        public void EventAccessorAdd()
        {
            TestSpan("class C { eve$$nt Action Goo { add [|{|] } remove { } } }");
        }

        [Fact]
        public void EventAccessorAdd2()
        {
            TestSpan("class C { event Action Goo { ad$$d [|{|] } remove { } } }");
        }

        [Fact]
        public void EventAccessorRemove()
        {
            TestSpan("class C { event Action Goo { add { } $$remove [|{|] } } }");
        }

        [Fact]
        public void ElseClauseWithBlock()
        {
            TestSpan(
@"class C
{
  void Goo()
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
        public void ElseClauseWithStatement()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    if (bar)
    {
    }
    el$$se
      [|Goo();|]
  }
}");
        }

        [Fact]
        public void ElseIf()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    if (bar)
    {
    }
    el$$se [|if (baz)|]
      Goo();
  }
}");
        }

        [Fact]
        public void EmptyCatch()
        {
            TestSpan(
@"class C
{
  void Goo()
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
        public void CatchWithType()
        {
            TestSpan(
@"class C
{
  void Goo()
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
        public void CatchWithTypeInType()
        {
            TestSpan(
@"class C
{
  void Goo()
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
        public void CatchWithTypeAndNameInType()
        {
            TestSpan(
@"class C
{
  void Goo()
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
        public void CatchWithTypeAndNameInName()
        {
            TestSpan(
@"class C
{
  void Goo()
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
        public void Filter1()
        {
            TestSpan(
@"class C
{
  void Goo()
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
        public void Filter3()
        {
            TestSpan(
@"class C
{
  void Goo()
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
        public void Filter4()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    try
    {
    }
    catch(Exception e) [|when$$ (e.Message != null)|]
    {
    }
  }
}");
        }

        [Fact]
        public void Filter5()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    try
    {
    }
    catch(Exception e) [|when (e.Message != null)|]      $$
    {
    }
  }
}");
        }

        [Fact]
        public void SimpleFinally()
        {
            TestSpan(
@"class C
{
  void Goo()
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
        public void FinallyWithCatch()
        {
            TestSpan(
@"class C
{
  void Goo()
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
        public void SwitchLabelWithBlock()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    switch (goo)
    {
        case $$1:
            [|{|]
            }
    }
  }
}");
        }

        [Fact]
        public void SwitchLabelWithStatement()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    switch (goo)
    {
        cas$$e 1:
            [|goo();|]
    }
  }
}");
        }

        [Fact]
        public void SwitchLabelWithStatement2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    switch (goo)
    {
        cas$$e 1:
            [|goo();|]
            bar();
    }
  }
}");
        }

        [Fact]
        public void SwitchLabelWithoutStatement()
        {
            TestSpan("class C { void M() { [|switch |]{ case 1$$: } } }");
        }

        [Fact]
        public void MultipleLabelsOnFirstLabel()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    switch (goo)
    {
        cas$$e 1:
        case 2:
            [|goo();|]

        case 3:
        default:
            bar();
    }
  }
}");
        }

        [Fact]
        public void MultipleLabelsOnSecondLabel()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    switch (goo)
    {
        case 1:
        cas$$e 2:
            [|goo();|]

        case 3:
        default:
            bar();
    }
  }
}");
        }

        [Fact]
        public void MultipleLabelsOnLabelWithDefault()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    switch (goo)
    {
        case 1:
        case 2:
            goo();

        cas$$e 3:
        default:
            [|bar();|]
    }
  }
}");
        }

        [Fact]
        public void MultipleLabelsOnDefault()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    switch (goo)
    {
        case 1:
        case 2:
            goo();

        case 3:
        default:$$
            [|bar();|]
    }
  }
}");
        }

        [Fact]
        public void BlockBeforeStartToken()
        {
            TestSpan(
@"class C
{
  void Goo()
  [|$${|]
    
  }
}");
        }

        [Fact]
        public void BlockBeforeStartToken2()
        {
            TestSpan(
@"class C
{
  void Goo()
  $$ [|{|]
    
  }
}");
        }

        [Fact]
        public void BlockAfterStartToken()
        {
            TestSpan(
@"class C
{
  void Goo()
  [|{$$|]
    
  }
}");
        }

        [Fact]
        public void BlockAfterStartToken2()
        {
            TestSpan(
@"class C
{
  void Goo()
  [|{|] $$
    
  }
}");
        }

        [Fact]
        public void BlockBeforeEndToken1()
        {
            TestSpan(
@"class C
{
  void Goo()
  { 
  $$[|}|]
}");
        }

        [Fact]
        public void BlockBeforeEndToken2()
        {
            TestSpan(
@"class C
{
  void Goo()
  { 
  $$ [|}|]
}");
        }

        [Fact]
        public void BlockAfterEndToken1()
        {
            TestSpan(
@"class C
{
  void Goo()
  { 
  [|}|]$$
}");
        }

        [Fact]
        public void BlockAfterEndToken2()
        {
            TestSpan(
@"class C
{
  void Goo()
  { 
  [|}|] $$
}");
        }

        [Fact]
        public void SingleDeclarationOnType()
        {
            TestMissing(
@"class C
{
  void Goo()
  {
    i$$nt i;
  }
}");
        }

        [Fact]
        public void MultipleDeclarationsOnType()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|i$$nt i = 0|], j = 1;
  }
}");
        }

        [Fact]
        public void Label()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    go$$o:
        [|bar();|]
  }
}");
        }

        [Fact]
        public void WhileInWhile()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|w$$hile (expr)|]
    {
    }
  }
}");
        }

        [Fact]
        public void WhileInExpr()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|while (ex$$pr)|]
    {
    }
  }
}");
        }

        [Fact]
        public void OnWhileBlock()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    while (expr)
  $$ [|{|]
    }
  }
}");
        }

        [Fact]
        public void OnDoKeyword()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    d$$o
    [|{|]
    }
    while(expr);
  }
}");
        }

        [Fact]
        public void OnDoBlock()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    do
  $$ [|{|]
    }
    while(expr);
  }
}");
        }

        [Fact]
        public void OnDoWhile()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    do
    {
    }
    [|wh$$ile(expr);|]
  }
}");
        }

        [Fact]
        public void OnDoWhile_MissingSemicolon()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    do
    {
    }
    [|wh$$ile(expr)|]
  }
}");
        }

        [Fact]
        public void OnDoExpression()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    do
    {
    }
    [|while(ex$$pr);|]
  }
}");
        }

        [Fact]
        public void OnForWithDeclaration1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    f$$or ([|int i = 0|], j = 1; i < 10 && j < 10; i++, j++)
    {
    }
  }
}");
        }

        [Fact]
        public void OnForWithDeclaration2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    f$$or ([|int i = 0|]; i < 10 && j < 10; i++, j++)
    {
    }
  }
}");
        }

        [Fact]
        public void OnForWithCondition()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    f$$or (; [|i < 10 && j < 10|]; i++, j++)
    {
    }
  }
}");
        }

        [Fact]
        public void OnForWithIncrementor1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    f$$or (; ; [|i++|], j++)
    {
    }
  }
}");
        }

        [Fact]
        public void OnForWithIncrementor2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    f$$or (; ; [|i++|])
    {
    }
  }
}");
        }

        [Fact]
        public void OnEmptyFor()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    f$$or (; ; )
    [|{|]
    }
  }
}");
        }

        [Fact]
        public void OnForEachKeyword1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
$$    [|foreach|] (var v in expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachKeyword2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|fo$$reach|] (var v in expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachKeyword3()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|foreach|]    $$    
(var v in expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachKeyword4()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|foreach|]        
$$         (var v in expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachKeyword5()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|foreach|] $$(var v in expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachType1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    foreach (   $$   
[|var v|] in expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachType2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    foreach ([|v$$ar v|] in expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachIdentifier()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    foreach ([|var v$$v|] in expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachIn1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    foreach (var v [|i$$n|] expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachIn2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    foreach (var v 
$$         [|in|] expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachIn3()
        {
            TestSpan(
@"class C
{
  void Goo()
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
        public void OnForEachExpr1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    foreach (var v in [|expr($$).blah()|])
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachExpr2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    foreach (var v in [|expr().blah()|]   
     $$    )
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachExpr3()
        {
            TestSpan(
@"class C
{
  void Goo()
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
        public void OnForEachStatement()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|foreach|](var v in expr().blah())    $$ 
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachBlock1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    foreach (var v in expr().blah())
  $$ [|{|]
    }
  }
}");
        }

        [Fact]
        public void OnForEachDeconstructionKeyword1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
$$    [|foreach|] (var (x, y) in expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachDeconstructionKeyword2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|fo$$reach|] (var (x, y) in expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachDeconstructionKeyword3()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|foreach|]    $$    
(var (x, y) in expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachDeconstructionKeyword4()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|foreach|]        
$$         (var (x, y) in expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachDeconstructionKeyword5()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|foreach|] $$(var (x, y) in expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachDeconstructionType1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    foreach (   $$   
[|var (x, y)|] in expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachDeconstructionType2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    foreach ([|v$$ar (x, y)|] in expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachDeconstructionIdentifier()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    foreach ([|var (v$$v, y)|] in expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachDeconstructionIn1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    foreach (var (x, y) [|i$$n|] expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachDeconstructionIn2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    foreach (var (x, y) 
$$         [|in|] expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachDeconstructionIn3()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    foreach (var (x, y) 
         [|in|] $$
expr().blah())
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachDeconstructionExpr1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    foreach (var (x, y) in [|expr($$).blah()|])
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachDeconstructionExpr2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    foreach (var (x, y) in [|expr().blah()|]   
     $$    )
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachDeconstructionExpr3()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    foreach (var (x, y) in 
   $$ [|expr().blah()|]   
     )
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachDeconstructionStatement()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|foreach|](var (x, y) in expr().blah())    $$ 
    {
    }
  }
}");
        }

        [Fact]
        public void OnForEachDeconstructionBlock1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    foreach (var (x, y) in expr().blah())
  $$ [|{|]
    }
  }
}");
        }

        [Fact]
        public void OnUsingWithDecl1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    us$$ing ([|var v = goo()|])
    {
    }
  }
}");
        }

        [Fact]
        public void OnUsingWithDecl2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    us$$ing ([|var v = goo()|], x = bar())
    {
    }
  }
}");
        }

        [Fact]
        public void OnUsingWithDeclType()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    using ([|v$$ar v = goo()|])
    {
    }
  }
}");
        }

        [Fact]
        public void OnUsingWithDeclIdentifier1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    using ([|var v$$v = goo()|])
    {
    }
  }
}");
        }

        [Fact]
        public void OnUsingWithDeclIdentifier2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    using ([|var vv = goo()|])     $$
    {
    }
  }
}");
        }

        [Fact]
        public void OnUsingWithDeclIdentifier3()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
$$    using ([|var vv = goo()|])     
    {
    }
  }
}");
        }

        [Fact]
        public void OnUsingWithDeclExpression()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    using ([|var vv = go$$o()|])
    {
    }
  }
}");
        }

        [Fact]
        public void OnUsingWithExpression1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|usi$$ng (goo().bar())|]
    {
    }
  }
}");
        }

        [Fact]
        public void OnUsingWithExpression2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|using (goo$$().bar())|]
    {
    }
  }
}");
        }

        [Fact]
        public void OnFixed1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    fi$$xed ([|int* i = &j|])
    {
    }
  }
}");
        }

        [Fact]
        public void OnFixed2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    fi$$xed ([|int* i = &j|], k = &m)
    {
    }
  }
}");
        }

        [Fact]
        public void OnFixed3()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    fixed ([|i$$nt* i = &j|])
    {
    }
  }
}");
        }

        [Fact]
        public void OnFixed4()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    fixed ([|int* $$i = &j|])
    {
    }
  }
}");
        }

        [Fact]
        public void OnFixed5()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    fixed ([|int* i $$= &j|])
    {
    }
  }
}");
        }

        [Fact]
        public void OnFixed6()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    fixed ([|int* i = &$$j|])
    {
    }
  }
}");
        }

        [Fact]
        public void OnChecked1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    che$$cked
    [|{|]
    }
  }
}");
        }

        [Fact]
        public void OnUnchecked1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    unche$$cked
    [|{|]
    }
  }
}");
        }

        [Fact]
        public void OnUnsafe1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    uns$$afe
    [|{|]
    }
  }
}");
        }

        [Fact]
        public void OnLock1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|lo$$ck (expr)|]
    {
    }
  }
}");
        }

        [Fact]
        public void OnLock2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|lock (ex$$pr)|]
    {
    }
  }
}");
        }

        [Fact]
        public void OnIf1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|i$$f (goo().bar())|]
    {
    }
  }
}");
        }

        [Fact]
        public void OnIf2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|if (go$$o().bar())|]
    {
    }
  }
}");
        }

        [Fact]
        public void OnIfBlock()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    if (goo().bar())
   $$ [|{|]
    }
  }
}");
        }

        [Fact]
        public void OnSwitch1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|swi$$tch (expr)|]
    {
        default:
            goo();
    }
  }
}");
        }

        [Fact]
        public void OnSwitch2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|switch (ex$$pr)|]
    {
        default:
            goo();
    }
  }
}");
        }

        [Fact]
        public void OnSwitch3()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|switch (expr)|]
  $$ {
        default:
            goo();
    }
  }
}");
        }

        [Fact]
        public void OnSwitch4()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|switch (expr)|]
    {
        default:
            goo();
  $$ }
  }
}");
        }

        [Fact]
        public void OnTry1()
        {
            TestSpan(
@"class C
{
  void Goo()
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
        public void OnTry2()
        {
            TestSpan(
@"class C
{
  void Goo()
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
        public void OnGotoStatement1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|g$$oto goo;|]
  }
}");
        }

        [Fact]
        public void OnGotoStatement2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|goto go$$o;|]
  }
}");
        }

        [Fact]
        public void OnGotoCaseStatement1()
        {
            TestSpan(
@"class C
{
  void Goo()
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
        public void OnGotoCaseStatement2()
        {
            TestSpan(
@"class C
{
  void Goo()
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
        public void OnGotoCaseStatement3()
        {
            TestSpan(
@"class C
{
  void Goo()
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
        public void OnGotoDefault1()
        {
            TestSpan(
@"class C
{
  void Goo()
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
        public void OnGotoDefault2()
        {
            TestSpan(
@"class C
{
  void Goo()
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
        public void OnBreak1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    while (true)
    {
        [|bre$$ak;|]
    }
  }
}");
        }

        [Fact]
        public void OnContinue1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    while (true)
    {
        [|cont$$inue;|]
    }
  }
}");
        }

        [Fact]
        public void OnReturn1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|retu$$rn;|]
  }
}");
        }

        [Fact]
        public void OnReturn2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|retu$$rn expr();|]
  }
}");
        }

        [Fact]
        public void OnReturn3()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|return expr$$().bar();|]
  }
}");
        }

        [Fact]
        public void OnYieldReturn1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|yi$$eld return goo().bar();|]
  }
}");
        }

        [Fact]
        public void OnYieldReturn2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|yield re$$turn goo().bar();|]
  }
}");
        }

        [Fact]
        public void OnYieldReturn3()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|yield return goo()$$.bar();|]
  }
}");
        }

        [Fact]
        public void OnYieldBreak1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|yi$$eld break;|]
  }
}");
        }

        [Fact]
        public void OnYieldBreak2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|yield brea$$k;|]
  }
}");
        }

        [Fact]
        public void OnThrow1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|th$$row;|]
  }
}");
        }

        [Fact]
        public void OnThrow2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|thr$$ow new Goo();|]
  }
}");
        }

        [Fact]
        public void OnThrow3()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|throw ne$$w Goo();|]
  }
}");
        }

        [Fact]
        public void OnThrow4()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|throw new Go$$o();|]
  }
}");
        }

        [Fact]
        public void OnExpressionStatement1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|goo().$$bar();|]
  }
}");
        }

        [Fact]
        public void OnEmptyStatement1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|$$;|]
  }
}");
        }

        [Fact]
        public void OnEmptyStatement2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    while (true)
    {
   $$ [|;|]
    }
  }
}");
        }

        [Fact]
        public void OnPropertyAccessor1()
        {
            TestSpan(
@"class C
{
  int Goo
  {
    g$$et
    [|{|]
    }
  }
}");
        }

        [Fact]
        public void OnPropertyAccessor2()
        {
            TestSpan(
@"class C
{
  int Goo
  {
    [|g$$et;|]
  }
}");
        }

        [Fact]
        public void OnPropertyAccessor3()
        {
            TestSpan(
@"class C
{
  int Goo
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
        public void OnPropertyAccessor4()
        {
            TestSpan(
@"class C
{
  int Goo
  {
    [|s$$et;|]
  }
}");
        }

        [Fact]
        public void OnProperty1()
        {
            TestSpan(
@"class C
{
  int G$$oo
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
        public void OnProperty2()
        {
            TestSpan(
@"class C
{
  int G$$oo
  {
    [|get;|]
    set {} 
  }
}");
        }

        [WorkItem(932711, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/932711")]
        [Fact]
        public void OnPropertyWithInitializer()
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
        public void OnPropertyExpressionBody1()
        {
            TestSpan(
@"class C
{
    public int Id => [|12$$3|];
}");
        }

        [Fact]
        public void OnPropertyExpressionBody2()
        {
            TestSpan(
@"class C
{
    public int Id $$=> [|123|];
}");
        }

        [Fact]
        public void OnPropertyExpressionBody3()
        {
            TestSpan(
@"class C
{
    $$public int Id => [|123|];
}");
        }

        [Fact]
        public void OnPropertyExpressionBody4()
        {
            TestSpan(
@"class C
{
    public int Id => [|123|];   $$
}");
        }

        [Fact]
        public void OnIndexerExpressionBody1()
        {
            TestSpan(
@"class C
{
    public int this[int a] => [|12$$3|];
}");
        }

        [Fact]
        public void OnIndexer1()
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
        public void OnIndexer2()
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
        public void OnIndexerExpressionBody2()
        {
            TestSpan(
@"class C
{
    public int this[int a] $$=> [|123|];
}");
        }

        [Fact]
        public void OnIndexerExpressionBody3()
        {
            TestSpan(
@"class C
{
    $$public int this[int a] => [|123|];
}");
        }

        [Fact]
        public void OnIndexerExpressionBody4()
        {
            TestSpan(
@"class C
{
    public int this[int a] => [|123|];   $$
}");
        }

        [Fact]
        public void OnIndexerExpressionBody5()
        {
            TestSpan(
@"class C
{
    public int this[int $$a] => [|123|];   
}");
        }

        [Fact]
        public void OnMethod1()
        {
            TestSpan(
@"class C
{
    v$$oid Goo()
    [|{|]
    }
}");
        }

        [Fact]
        public void OnMethod2()
        {
            TestSpan(
@"class C
{
    void G$$oo()
    [|{|]
    }
}");
        }

        [Fact]
        public void OnMethod3()
        {
            TestSpan(
@"class C
{
    void Goo(in$$t i)
    [|{|]
    }
}");
        }

        [Fact]
        public void OnMethod4()
        {
            TestSpan(
@"class C
{
    void Goo(int $$i)
    [|{|]
    }
}");
        }

        [Fact]
        public void OnMethod5()
        {
            TestSpan(
@"class C
{
    void Goo(int i = g$$oo)
    [|{|]
    }
}");
        }

        [Fact]
        public void OnMethodWithExpressionBody1()
        {
            TestSpan(
@"class C
{
    v$$oid Goo() => [|123|];
}");
        }

        [Fact]
        public void OnMethodWithExpressionBody2()
        {
            TestSpan(
@"class C
{
    void Goo() =>$$ [|123|];
}");
        }

        [Fact]
        public void OnMethodWithExpressionBody3()
        {
            TestSpan(
@"class C
{
    void Goo() => [|123|]; $$
}");
        }

        [Fact]
        public void OnMethodWithExpressionBody4()
        {
            TestSpan(
@"class C
{
    void Goo() => [|12$$3|]; 
}");
        }

        [Fact]
        public void MissingOnMethod()
        {
            TestMissing(
@"class C
{
    void Goo($$);
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

        [WorkItem(543968, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543968")]
        [Fact]
        public void ConstructorInitializer()
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
        public void OnStaticConstructor()
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
        public void OnDestructor()
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
        public void OnOperator()
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
        public void OnOperatorWithExpressionBody1()
        {
            TestSpan(
@"class C
{
    public static int op$$erator+(C c1, C c2) => [|c1|];
}");
        }

        [Fact]
        public void OnOperatorWithExpressionBody2()
        {
            TestSpan(
@"class C
{
    public static int operator+(C c1, C c2) =>$$ [|c1|];
}");
        }

        [Fact]
        public void OnOperatorWithExpressionBody3()
        {
            TestSpan(
@"class C
{
    public static int operator+(C c1, C c2) => [|c1|]; $$
}");
        }

        [Fact]
        public void OnOperatorWithExpressionBody4()
        {
            TestSpan(
@"class C
{
    public static int operator+(C c1, C c2) => [|c$$1|];
}");
        }

        [Fact]
        public void OnConversionOperator()
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
        public void OnConversionOperatorWithExpressionBody1()
        {
            TestSpan(
@"class C
{
    public static op$$erator DateTime(C c1) => [|DataTime.Now|];
}");
        }

        [Fact]
        public void OnConversionOperatorWithExpressionBody2()
        {
            TestSpan(
@"class C
{
    public static operator DateTime(C c1) =>$$ [|DataTime.Now|];
}");
        }

        [Fact]
        public void OnConversionOperatorWithExpressionBody3()
        {
            TestSpan(
@"class C
{
    public static operator DateTime(C c1) => [|DataTime.Now|];$$
}");
        }

        [Fact]
        public void OnConversionOperatorWithExpressionBody4()
        {
            TestSpan(
@"class C
{
    public static operator DateTime(C c1) => [|DataTime$$.Now|];
}");
        }

        [WorkItem(3557, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void InFrontOfAttribute()
        {
            TestSpan(
@"class C
{
$$ [method: Obsolete]
  void Goo()
  [|{|]
  }
}");
        }

        [WorkItem(538058, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538058")]
        [Fact]
        public void InInactivePPRegion()
        {
            TestLine(
@"

#if blahblah
$$gooby
#endif");
        }

        [WorkItem(538777, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538777")]
        [Fact]
        public void WithIncompleteDeclaration()
        {
            TestMissing(
@"
clas C
{
    void Goo()
    {
$$        int
    }
}");
        }

        [WorkItem(937290, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/937290")]
        [Fact]
        public void OnGetter()
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

        [WorkItem(937290, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/937290")]
        [Fact]
        public void OnSetter()
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

        [Fact]
        public void WhenClause_1()
        {
            TestSpan(
@"class C
{
  string s;
  bool b;
  void Goo()
  {
    switch (s)
    {
$$      case string s [|when b|]:
        break;
    }
  }
}");
        }

        [Fact]
        public void WhenClause_2()
        {
            TestSpan(
@"class C
{
  string s;
  bool b;
  void Goo()
  {
    switch (s)
    {
      case string s [|whe$$n b|]:
        break;
    }
  }
}");
        }

        [Fact]
        public void WhenClause_3()
        {
            TestSpan(
@"class C
{
  string s;
  bool b;
  void Goo()
  {
    switch (s)
    {
      case string s [|when b|]:$$
        break;
    }
  }
}");
        }

        [Fact]
        public void PatternSwitchCase_1()
        {
            TestSpan(
@"class C
{
  string s;
  bool b;
  void Goo()
  {
    switch (s)
    {
$$      case string s:
      default:
        [|break;|]
    }
  }
}");
        }

        [Fact]
        public void PatternSwitchCase_2()
        {
            TestSpan(
@"class C
{
  string s;
  bool b;
  void Goo()
  {
    switch (s)
    {
      $$case string s:
      default:
        [|break;|]
    }
  }
}");
        }

        [Fact]
        public void PatternSwitchCase_3()
        {
            TestSpan(
@"class C
{
  string s;
  bool b;
  void Goo()
  {
    switch (s)
    {
      case string s:$$
      default:
        [|break;|]
    }
  }
}");
        }

        [Fact]
        public void DeconstructionDeclarationStatement_1()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
$$    [|var (x, y) = (1, 2);|]
  }
}");
        }

        [Fact]
        public void DeconstructionDeclarationStatement_2()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|var (x, y) = $$(1, 2);|]
  }
}");
        }

        [Fact, WorkItem(14438, "https://github.com/dotnet/roslyn/issues/14438")]
        public void OnAccessorExpressionBody1()
        {
            TestSpan(
@"class C
{
    public int Id { get => [|12$$3|]; }
}");
        }

        [Fact, WorkItem(14438, "https://github.com/dotnet/roslyn/issues/14438")]
        public void OnAccessorExpressionBody2()
        {
            TestSpan(
@"class C
{
    public int Id { get $$=> [|123|]; }
}");
        }

        [Fact, WorkItem(14438, "https://github.com/dotnet/roslyn/issues/14438")]
        public void OnAccessorExpressionBody3()
        {
            TestSpan(
@"class C
{
    $$public int Id { get => [|123|]; }
}");
        }

        [Fact, WorkItem(14438, "https://github.com/dotnet/roslyn/issues/14438")]
        public void OnAccessorExpressionBody4()
        {
            TestSpan(
@"class C
{
    public int Id { get => [|123|];   $$ }
}");
        }

        [Fact, WorkItem(14438, "https://github.com/dotnet/roslyn/issues/14438")]
        public void OnAccessorExpressionBody5()
        {
            TestSpan(
@"class C
{
$$    public event Action Goo { add => [|123|]; remove => 456; }
}");
        }

        [Fact, WorkItem(14438, "https://github.com/dotnet/roslyn/issues/14438")]
        public void OnAccessorExpressionBody6()
        {
            TestSpan(
@"class C
{
    public event Action Goo { add => [|123|];$$ remove => 456; }
}");
        }

        [Fact, WorkItem(14438, "https://github.com/dotnet/roslyn/issues/14438")]
        public void OnAccessorExpressionBody7()
        {
            TestSpan(
@"class C
{
    public event Action Goo { add => 123; $$remove => [|456|]; }
}");
        }

        [Fact, WorkItem(14438, "https://github.com/dotnet/roslyn/issues/14438")]
        public void OnAccessorExpressionBody8()
        {
            TestSpan(
@"class C
{
    public event Action Goo { add => 123; remove => [|456|]; }$$
}");
        }

        [Fact, WorkItem(14438, "https://github.com/dotnet/roslyn/issues/14438")]
        public void OnCtorExpressionBody1()
        {
            TestSpan(
@"class C
{
$$    public C() => [|x = 1|];
}");
        }

        [Fact, WorkItem(14438, "https://github.com/dotnet/roslyn/issues/14438")]
        public void OnCtorExpressionBody2()
        {
            TestSpan(
@"class C
{
    public C() => $$[|x = 1|];
}");
        }

        [Fact, WorkItem(14438, "https://github.com/dotnet/roslyn/issues/14438")]
        public void OnCtorExpressionBody3()
        {
            TestSpan(
@"class C
{
    public C() => [|x =$$ 1|];
}");
        }

        [Fact, WorkItem(14438, "https://github.com/dotnet/roslyn/issues/14438")]
        public void OnCtorExpressionBody4()
        {
            TestSpan(
@"class C
{
    public C() => [|x = 1|]$$;
}");
        }

        [Fact, WorkItem(14438, "https://github.com/dotnet/roslyn/issues/14438")]
        public void OnCtorExpressionBody5()
        {
            TestSpan(
@"class C
{
    public C() => [|x = 1|];$$
}");
        }

        [Fact, WorkItem(14438, "https://github.com/dotnet/roslyn/issues/14438")]
        public void OnDtorExpressionBody1()
        {
            TestSpan(
@"class C
{
$$    public ~C() => [|x = 1|];
}");
        }

        [Fact, WorkItem(14438, "https://github.com/dotnet/roslyn/issues/14438")]
        public void OnDtorExpressionBody2()
        {
            TestSpan(
@"class C
{
    public ~C() => $$[|x = 1|];
}");
        }

        [Fact, WorkItem(14438, "https://github.com/dotnet/roslyn/issues/14438")]
        public void OnDtorExpressionBody3()
        {
            TestSpan(
@"class C
{
    public ~C() => [|x =$$ 1|];
}");
        }

        [Fact, WorkItem(14438, "https://github.com/dotnet/roslyn/issues/14438")]
        public void OnDtorExpressionBody4()
        {
            TestSpan(
@"class C
{
    public ~C() => [|x = 1|]$$;
}");
        }

        [Fact, WorkItem(14438, "https://github.com/dotnet/roslyn/issues/14438")]
        public void OnDtorExpressionBody5()
        {
            TestSpan(
@"class C
{
    public ~C() => [|x = 1|];$$
}");
        }

        [Fact, WorkItem(14437, "https://github.com/dotnet/roslyn/issues/14437")]
        public void OnLocalFunctionDecl_1()
        {
            TestSpan(
@"class C
{
    static void M()
    {
$$        int Local(object[] a)
        [|{|]
            return a.Length;
        }
    }
}");
        }

        [Fact, WorkItem(14437, "https://github.com/dotnet/roslyn/issues/14437")]
        public void OnLocalFunctionDecl_2()
        {
            TestSpan(
@"class C
{
    static void M()
    {
        int Local(object[] a)$$
        [|{|]
            return a.Length;
        }
    }
}");
        }

        [Fact, WorkItem(14437, "https://github.com/dotnet/roslyn/issues/14437")]
        public void OnLocalFunctionDecl_3()
        {
            TestSpan(
@"class C
{
    static void M()
    {
        int Local(object[] a)
$$        [|{|]
            return a.Length;
        }
    }
}");
        }

        [Fact, WorkItem(14437, "https://github.com/dotnet/roslyn/issues/14437")]
        public void OnLocalFunctionDecl_4()
        {
            TestSpan(
@"class C
{
    static void M()
    {
$$        int Local(object[] a) => [|a.Length|];
    }
}");
        }

        [Fact, WorkItem(14437, "https://github.com/dotnet/roslyn/issues/14437")]
        public void OnLocalFunctionDecl_5()
        {
            TestSpan(
@"class C
{
    static void M()
    {
        int Local(object$$[] a) => [|a.Length|];
    }
}");
        }

        [Fact, WorkItem(14437, "https://github.com/dotnet/roslyn/issues/14437")]
        public void OnLocalFunctionDecl_6()
        {
            TestSpan(
@"class C
{
    static void M()
    {
        int Local(object[] a) => [|a.Length|];$$
    }
}");
        }

        [Fact, WorkItem(98990, "https://developercommunity.visualstudio.com/content/problem/98990/cant-set-breakpoint.html")]
        public void IncompleteExpressionStatement()
        {
            TestSpan(
@"class C
{
  void Goo()
  {
    [|$$aaa|]
  }
}");
        }
    }
}
