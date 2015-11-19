// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.EditAndContinue;
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
            int? position;
            TextSpan? expectedSpan;
            string source;
            MarkupTestFile.GetPositionAndSpan(markup, out source, out position, out expectedSpan);
            var tree = SyntaxFactory.ParseSyntaxTree(source, options);

            TextSpan breakpointSpan;
            bool hasBreakpoint = BreakpointSpans.TryGetBreakpointSpan(tree, position.Value, CancellationToken.None, out breakpointSpan);

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
                if (BreakpointSpans.TryGetClosestBreakpointSpan(root, position, out span) && span.End > lastSpanEnd)
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
        public void GetBreakpointSequence5()
        {
            TestAll(@"
class C
{
    IEnumerable<int> Foo()
    $$[|{|][|while(t)|][|{|][|}|][|}|]
}");
        }

        [WpfFact]
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

        [WpfFact]
        public void ForStatementInitializer1a()
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

        [WpfFact]
        public void ForStatementInitializer1b()
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

        [WpfFact]
        public void ForStatementInitializer1c()
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

        [WpfFact]
        public void ForStatementInitializer1d()
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

        [WpfFact]
        public void ForStatementInitializer2()
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

        [WpfFact]
        public void ForStatementInitializer3()
        {
            TestSpan("class C { void M() { for([|i = 0$$|]; ; }; }");
        }

        [WpfFact]
        public void ForStatementCondition()
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

        [WpfFact]
        public void ForStatementIncrementor1()
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

        [WpfFact]
        public void ForStatementIncrementor2()
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

        [WpfFact]
        public void ForEachStatementExpression()
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

        #region Lambdas

        [WpfFact]
        public void SimpleLambdaBody()
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

        [WpfFact]
        public void ParenthesizedLambdaBody()
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

        [WpfFact]
        public void AnonymousMethod1()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    Func<int> f = delegate [|$${|] return 1; };
  }
}");
        }

        [WpfFact]
        public void AnonymousMethod2()
        {
            TestSpan(
@"class C
{
  void Foo()
  {
    Func<int> f = delegate { [|$$return 1;|] };
  }
}");
        }

        #endregion

        #region Queries

        [WpfFact]
        public void FirstFromClauseExpression()
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

        [WpfFact]
        public void SecondFromClauseExpression()
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

        [WpfFact]
        public void FromInQueryContinuation1()
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

        [WpfFact]
        public void FromInQueryContinuation2()
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

        [WpfFact]
        public void JoinClauseLeftExpression()
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

        [WpfFact]
        public void JoinClauseRightExpression()
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

        [WpfFact]
        public void LetClauseExpression()
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

        [WpfFact]
        public void WhereClauseExpression()
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

        [WpfFact]
        public void WhereClauseKeyword()
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

        [WpfFact]
        public void SimpleOrdering1()
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

        [WpfFact]
        public void SimpleOrdering2()
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

        [WpfFact]
        public void AscendingOrdering1()
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

        [WpfFact]
        public void AscendingOrdering2()
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

        [WpfFact]
        public void DescendingOrdering1()
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

        [WpfFact]
        public void DescendingOrdering2()
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

        [WpfFact]
        public void OrderByKeyword()
        {
            TestSpan("class C { void M() { from string s in null ord$$erby [|s.A|] ascending } }");
        }

        [WpfFact]
        public void AscendingKeyword()
        {
            TestSpan("class C { void M() { from string s in null orderby [|s.A|] $$ascending } }");
        }

        [WpfFact]
        public void SelectExpression()
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

        [WpfFact]
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

        [WpfFact]
        public void GroupExpression()
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

        [WpfFact]
        public void GroupByKeyword()
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

        [WpfFact]
        public void GroupByExpression()
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

        [WpfFact]
        public void InFrontOfFirstFromClause()
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

        [WpfFact]
        public void InFrontOfSecondFromClause()
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

        [WpfFact]
        public void InFrontOfLetClause()
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

        [WpfFact]
        public void InFrontOfJoinClause()
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

        [WpfFact]
        public void InFrontOfOrderByClause()
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

        [WpfFact]
        public void InFrontOfGroupByClause()
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

        [WpfFact]
        public void InFrontOfSelectClause()
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

        [WpfFact]
        public void Select1()
        {
            TestSpan(
@"class C
{
  IEnumerable<int> Foo() => from x in new[] { 1 } select [|$$x|];
}
");
        }

        [WpfFact]
        public void Select_NoLambda1()
        {
            TestSpan(
@"class C
{
  IEnumerable<int> Foo() => [|from x in new[] { 1 } where x > 0 select $$x|];
}
");
        }

        [WpfFact]
        public void Select_NoLambda2()
        {
            TestSpan(
@"class C
{
  IEnumerable<int> Foo() => [|from x in new[] { 1 } select x into y orderby y select $$y|];
}
");
        }

        [WpfFact]
        public void GroupBy1()
        {
            TestSpan(
@"class C
{
  IEnumerable<int> Foo() => from x in new[] { 1 } group x by [|$$x|];
}
");
        }

        [WpfFact]
        public void GroupBy_NoLambda1()
        {
            TestSpan(
@"class C
{
  IEnumerable<int> Foo() => [|from x in new[] { 1 } group $$x by x|];
}
");
        }

        [WpfFact]
        public void GroupBy_NoLambda2()
        {
            TestSpan(
@"class C
{
  IEnumerable<int> Foo() => [|from x in new[] { 1 } group $$x by x + 1 into y group y by y.Key + 2|];
}
");
        }

        [WpfFact]
        public void GroupBy_NoLambda3()
        {
            TestSpan(
@"class C
{
  IEnumerable<int> Foo() => [|from x in new[] { 1 } group x by x + 1 into y group $$y by y.Key + 2|];
}
");
        }

        #endregion

        [WpfFact]
        public void FieldDeclarator_WithoutInitializer1()
        {
            TestMissing(
@"class C
{
    int $$i;
}");
        }

        [WpfFact]
        public void FieldDeclarator_WithoutInitializer2()
        {
            TestMissing(
@"class C
{
    pri$$vate int i;
}");
        }

        [WpfFact]
        public void FieldDeclarator1()
        {
            TestSpan(
@"class C
{
    [|int $$i = 1;|]
}");
        }

        [WpfFact]
        public void FieldDeclarator2()
        {
            TestSpan(
@"class C
{
    [|private int $$i = 1;|]
}");
        }

        [WpfFact]
        public void FieldDeclarator3()
        {
            TestSpan(
@"class C
{
    [Foo]
    [|private int $$i = 0;|]
}");
        }

        [WpfFact]
        public void FieldDeclarator4()
        {
            TestSpan(
@"class C
{
    [|pri$$vate int i = 1;|]
}");
        }

        [WpfFact]
        public void FieldDeclarator5()
        {
            TestSpan(
@"class C
{
$$    [|private int i = 3;|]
}");
        }

        [WpfFact]
        public void ConstVariableDeclarator0()
        {
            TestMissing("class C { void Foo() { const int a = $$1; } }");
        }

        [WpfFact]
        public void ConstVariableDeclarator1()
        {
            TestMissing("class C { void Foo() { const $$int a = 1; } }");
        }

        [WpfFact]
        public void ConstVariableDeclarator2()
        {
            TestMissing("class C { void Foo() { $$const int a = 1; } }");
        }

        [WpfFact]
        public void ConstFieldVariableDeclarator0()
        {
            TestMissing("class C { const int a = $$1; }");
        }

        [WpfFact]
        public void ConstFieldVariableDeclarator1()
        {
            TestMissing("class C { const $$int a = 1; }");
        }

        [WpfFact]
        public void ConstFieldVariableDeclarator2()
        {
            TestMissing("class C { $$const int a = 1; }");
        }

        [WpfFact]
        [WorkItem(538777)]
        public void VariableDeclarator0()
        {
            TestMissing("class C { void Foo() { int$$ } }");
        }

        [WpfFact]
        public void VariableDeclarator1()
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

        [WpfFact]
        public void VariableDeclarator2a()
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

        [WpfFact]
        public void VariableDeclarator2b()
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

        [WpfFact]
        public void VariableDeclarator2c()
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

        [WpfFact]
        public void VariableDeclarator3a()
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingBreakpoints)]
        public void VariableDeclarator3b()
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.DebuggingBreakpoints)]
        public void VariableDeclarator3c()
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

        [WpfFact]
        public void VariableDeclarator4()
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

        [WpfFact]
        public void VariableDeclarator5()
        {
            TestSpan(
@"class C
{
  [|int $$i = 0;|]
}");
        }

        [WpfFact]
        public void VariableDeclarator6()
        {
            TestSpan(
@"class C
{
  [|int i = 0|], $$j;
}");
        }

        [WpfFact]
        public void VariableDeclarator7()
        {
            TestSpan(
@"class C
{
  private int i = 0, [|j = $$1|];
}");
        }

        [WpfFact]
        public void VariableDeclarator8()
        {
            TestSpan(
@"class C
{
  [|priv$$ate int i = 0|], j = 1;
}");
        }

        [WpfFact]
        public void VariableDeclarator9()
        {
            TestSpan(
@"class C
{
$$  [|private int i = 0|], j = 1;
}");
        }

        [WpfFact]
        public void VariableDeclarator10()
        {
            TestSpan("class C { void M() { [|int i = 0$$;|] } }");
        }

        [WpfFact]
        public void VariableDeclarator_Separators0()
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

        [WpfFact]
        public void VariableDeclarator_Separators1()
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

        [WpfFact]
        public void VariableDeclarator_Separators2()
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

        [WpfFact]
        public void VariableDeclarator_Separators3()
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

        [WpfFact]
        public void VariableDeclarator_Separators4()
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

        [WpfFact]
        public void VariableDeclarator_Separators5()
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

        [WpfFact]
        public void VariableDeclarator_Separators6()
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

        [WpfFact]
        public void VariableDeclarator_Separators7()
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

        [WpfFact]
        public void VariableDeclarator_Separators8()
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

        [WpfFact]
        public void VariableDeclarator_Separators9()
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

        [WpfFact]
        public void EventFieldDeclarator1()
        {
            TestSpan(
@"class C
{
$$    [|public event EventHandler MyEvent = delegate { };|]
}");
        }

        [WpfFact]
        public void EventFieldDeclarator2()
        {
            TestSpan(
@"class C
{
    [|pub$$lic event EventHandler MyEvent = delegate { };|]
}");
        }

        [WpfFact]
        public void EventFieldDeclarator3()
        {
            TestSpan(
@"class C
{
    [|public ev$$ent EventHandler MyEvent = delegate { };|]
}");
        }

        [WpfFact]
        public void EventFieldDeclarator4()
        {
            TestSpan(
@"class C
{
    [|public event EventHan$$dler MyEvent = delegate { };|]
}");
        }

        [WpfFact]
        public void EventFieldDeclarator5()
        {
            TestSpan(
@"class C
{
    [|public event EventHandler MyE$$vent = delegate { };|]
}");
        }

        [WpfFact]
        public void EventFieldDeclarator6()
        {
            TestSpan(
@"class C
{
    [|public event EventHandler MyEvent $$= delegate { };|]
}");
        }

        [WpfFact]
        public void EventFieldDeclarator7()
        {
            TestSpan(
@"class C
{
    [|public event EventHandler MyEvent = del$$egate { };|]
}");
        }

        [WpfFact]
        public void EventFieldDeclarator8()
        {
            TestSpan(
@"class C
{
    public event EventHandler MyEvent = delegate [|{|] $$ };
}");
        }

        [WpfFact]
        public void EventAccessorAdd()
        {
            TestSpan("class C { eve$$nt Action Foo { add [|{|] } remove { } } }");
        }

        [WpfFact]
        public void EventAccessorAdd2()
        {
            TestSpan("class C { event Action Foo { ad$$d [|{|] } remove { } } }");
        }

        [WpfFact]
        public void EventAccessorRemove()
        {
            TestSpan("class C { event Action Foo { add { } $$remove [|{|] } } }");
        }

        [WpfFact]
        public void ElseClauseWithBlock()
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

        [WpfFact]
        public void ElseClauseWithStatement()
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

        [WpfFact]
        public void ElseIf()
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

        [WpfFact]
        public void EmptyCatch()
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

        [WpfFact]
        public void CatchWithType()
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

        [WpfFact]
        public void CatchWithTypeInType()
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

        [WpfFact]
        public void CatchWithTypeAndNameInType()
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

        [WpfFact]
        public void CatchWithTypeAndNameInName()
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

        [WpfFact]
        public void Filter1()
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

        [WpfFact]
        public void Filter3()
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

        [WpfFact]
        public void Filter4()
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

        [WpfFact]
        public void Filter5()
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

        [WpfFact]
        public void SimpleFinally()
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

        [WpfFact]
        public void FinallyWithCatch()
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

        [WpfFact]
        public void SwitchLabelWithBlock()
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

        [WpfFact]
        public void SwitchLabelWithStatement()
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

        [WpfFact]
        public void SwitchLabelWithStatement2()
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

        [WpfFact]
        public void SwitchLabelWithoutStatement()
        {
            TestSpan("class C { void M() { [|switch |]{ case 1$$: } } }");
        }

        [WpfFact]
        public void MultipleLabelsOnFirstLabel()
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

        [WpfFact]
        public void MultipleLabelsOnSecondLabel()
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

        [WpfFact]
        public void MultipleLabelsOnLabelWithDefault()
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

        [WpfFact]
        public void MultipleLabelsOnDefault()
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

        [WpfFact]
        public void BlockBeforeStartToken()
        {
            TestSpan(
@"class C
{
  void Foo()
  [|$${|]
    
  }
}");
        }

        [WpfFact]
        public void BlockBeforeStartToken2()
        {
            TestSpan(
@"class C
{
  void Foo()
  $$ [|{|]
    
  }
}");
        }

        [WpfFact]
        public void BlockAfterStartToken()
        {
            TestSpan(
@"class C
{
  void Foo()
  [|{$$|]
    
  }
}");
        }

        [WpfFact]
        public void BlockAfterStartToken2()
        {
            TestSpan(
@"class C
{
  void Foo()
  [|{|] $$
    
  }
}");
        }

        [WpfFact]
        public void BlockBeforeEndToken1()
        {
            TestSpan(
@"class C
{
  void Foo()
  { 
  $$[|}|]
}");
        }

        [WpfFact]
        public void BlockBeforeEndToken2()
        {
            TestSpan(
@"class C
{
  void Foo()
  { 
  $$ [|}|]
}");
        }

        [WpfFact]
        public void BlockAfterEndToken1()
        {
            TestSpan(
@"class C
{
  void Foo()
  { 
  [|}|]$$
}");
        }

        [WpfFact]
        public void BlockAfterEndToken2()
        {
            TestSpan(
@"class C
{
  void Foo()
  { 
  [|}|] $$
}");
        }

        [WpfFact]
        public void SingleDeclarationOnType()
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

        [WpfFact]
        public void MultipleDeclarationsOnType()
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

        [WpfFact]
        public void Label()
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

        [WpfFact]
        public void WhileInWhile()
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

        [WpfFact]
        public void WhileInExpr()
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

        [WpfFact]
        public void OnWhileBlock()
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

        [WpfFact]
        public void OnDoKeyword()
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

        [WpfFact]
        public void OnDoBlock()
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

        [WpfFact]
        public void OnDoWhile()
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

        [WpfFact]
        public void OnDoWhile_MissingSemicolon()
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

        [WpfFact]
        public void OnDoExpression()
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

        [WpfFact]
        public void OnForWithDeclaration1()
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

        [WpfFact]
        public void OnForWithDeclaration2()
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

        [WpfFact]
        public void OnForWithCondition()
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

        [WpfFact]
        public void OnForWithIncrementor1()
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

        [WpfFact]
        public void OnForWithIncrementor2()
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

        [WpfFact]
        public void OnEmptyFor()
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

        [WpfFact]
        public void OnForEachKeyword1()
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

        [WpfFact]
        public void OnForEachKeyword2()
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

        [WpfFact]
        public void OnForEachKeyword3()
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

        [WpfFact]
        public void OnForEachKeyword4()
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

        [WpfFact]
        public void OnForEachKeyword5()
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

        [WpfFact]
        public void OnForEachType1()
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

        [WpfFact]
        public void OnForEachType2()
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

        [WpfFact]
        public void OnForEachIdentifier()
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

        [WpfFact]
        public void OnForEachIn1()
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

        [WpfFact]
        public void OnForEachIn2()
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

        [WpfFact]
        public void OnForEachIn3()
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

        [WpfFact]
        public void OnForEachExpr1()
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

        [WpfFact]
        public void OnForEachExpr2()
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

        [WpfFact]
        public void OnForEachExpr3()
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

        [WpfFact]
        public void OnForEachStatement()
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

        [WpfFact]
        public void OnForEachBlock1()
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

        [WpfFact]
        public void OnUsingWithDecl1()
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

        [WpfFact]
        public void OnUsingWithDecl2()
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

        [WpfFact]
        public void OnUsingWithDeclType()
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

        [WpfFact]
        public void OnUsingWithDeclIdentifier1()
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

        [WpfFact]
        public void OnUsingWithDeclIdentifier2()
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

        [WpfFact]
        public void OnUsingWithDeclIdentifier3()
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

        [WpfFact]
        public void OnUsingWithDeclExpression()
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

        [WpfFact]
        public void OnUsingWithExpression1()
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

        [WpfFact]
        public void OnUsingWithExpression2()
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

        [WpfFact]
        public void OnFixed1()
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

        [WpfFact]
        public void OnFixed2()
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

        [WpfFact]
        public void OnFixed3()
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

        [WpfFact]
        public void OnFixed4()
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

        [WpfFact]
        public void OnFixed5()
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

        [WpfFact]
        public void OnFixed6()
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

        [WpfFact]
        public void OnChecked1()
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

        [WpfFact]
        public void OnUnchecked1()
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

        [WpfFact]
        public void OnUnsafe1()
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

        [WpfFact]
        public void OnLock1()
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

        [WpfFact]
        public void OnLock2()
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

        [WpfFact]
        public void OnIf1()
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

        [WpfFact]
        public void OnIf2()
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

        [WpfFact]
        public void OnIfBlock()
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

        [WpfFact]
        public void OnSwitch1()
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

        [WpfFact]
        public void OnSwitch2()
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

        [WpfFact]
        public void OnSwitch3()
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

        [WpfFact]
        public void OnSwitch4()
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

        [WpfFact]
        public void OnTry1()
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

        [WpfFact]
        public void OnTry2()
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

        [WpfFact]
        public void OnGotoStatement1()
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

        [WpfFact]
        public void OnGotoStatement2()
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

        [WpfFact]
        public void OnGotoCaseStatement1()
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

        [WpfFact]
        public void OnGotoCaseStatement2()
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

        [WpfFact]
        public void OnGotoCaseStatement3()
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

        [WpfFact]
        public void OnGotoDefault1()
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

        [WpfFact]
        public void OnGotoDefault2()
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

        [WpfFact]
        public void OnBreak1()
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

        [WpfFact]
        public void OnContinue1()
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

        [WpfFact]
        public void OnReturn1()
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

        [WpfFact]
        public void OnReturn2()
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

        [WpfFact]
        public void OnReturn3()
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

        [WpfFact]
        public void OnYieldReturn1()
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

        [WpfFact]
        public void OnYieldReturn2()
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

        [WpfFact]
        public void OnYieldReturn3()
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

        [WpfFact]
        public void OnYieldBreak1()
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

        [WpfFact]
        public void OnYieldBreak2()
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

        [WpfFact]
        public void OnThrow1()
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

        [WpfFact]
        public void OnThrow2()
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

        [WpfFact]
        public void OnThrow3()
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

        [WpfFact]
        public void OnThrow4()
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

        [WpfFact]
        public void OnExpressionStatement1()
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

        [WpfFact]
        public void OnEmptyStatement1()
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

        [WpfFact]
        public void OnEmptyStatement2()
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

        [WpfFact]
        public void OnPropertyAccessor1()
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

        [WpfFact]
        public void OnPropertyAccessor2()
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

        [WpfFact]
        public void OnPropertyAccessor3()
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

        [WpfFact]
        public void OnPropertyAccessor4()
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

        [WpfFact]
        public void OnProperty1()
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

        [WpfFact]
        public void OnProperty2()
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
        [WpfFact]
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

        [WpfFact]
        public void OnPropertyExpressionBody1()
        {
            TestSpan(
@"class C
{
    public int Id => [|12$$3|];
}");
        }

        [WpfFact]
        public void OnPropertyExpressionBody2()
        {
            TestSpan(
@"class C
{
    public int Id $$=> [|123|];
}");
        }

        [WpfFact]
        public void OnPropertyExpressionBody3()
        {
            TestSpan(
@"class C
{
    $$public int Id => [|123|];
}");
        }

        [WpfFact]
        public void OnPropertyExpressionBody4()
        {
            TestSpan(
@"class C
{
    public int Id => [|123|];   $$
}");
        }

        [WpfFact]
        public void OnIndexerExpressionBody1()
        {
            TestSpan(
@"class C
{
    public int this[int a] => [|12$$3|];
}");
        }

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
        public void OnIndexerExpressionBody2()
        {
            TestSpan(
@"class C
{
    public int this[int a] $$=> [|123|];
}");
        }

        [WpfFact]
        public void OnIndexerExpressionBody3()
        {
            TestSpan(
@"class C
{
    $$public int this[int a] => [|123|];
}");
        }

        [WpfFact]
        public void OnIndexerExpressionBody4()
        {
            TestSpan(
@"class C
{
    public int this[int a] => [|123|];   $$
}");
        }

        [WpfFact]
        public void OnIndexerExpressionBody5()
        {
            TestSpan(
@"class C
{
    public int this[int $$a] => [|123|];   
}");
        }

        [WpfFact]
        public void OnMethod1()
        {
            TestSpan(
@"class C
{
    v$$oid Foo()
    [|{|]
    }
}");
        }

        [WpfFact]
        public void OnMethod2()
        {
            TestSpan(
@"class C
{
    void F$$oo()
    [|{|]
    }
}");
        }

        [WpfFact]
        public void OnMethod3()
        {
            TestSpan(
@"class C
{
    void Foo(in$$t i)
    [|{|]
    }
}");
        }

        [WpfFact]
        public void OnMethod4()
        {
            TestSpan(
@"class C
{
    void Foo(int $$i)
    [|{|]
    }
}");
        }

        [WpfFact]
        public void OnMethod5()
        {
            TestSpan(
@"class C
{
    void Foo(int i = f$$oo)
    [|{|]
    }
}");
        }

        [WpfFact]
        public void OnMethodWithExpressionBody1()
        {
            TestSpan(
@"class C
{
    v$$oid Foo() => [|123|];
}");
        }

        [WpfFact]
        public void OnMethodWithExpressionBody2()
        {
            TestSpan(
@"class C
{
    void Foo() =>$$ [|123|];
}");
        }

        [WpfFact]
        public void OnMethodWithExpressionBody3()
        {
            TestSpan(
@"class C
{
    void Foo() => [|123|]; $$
}");
        }

        [WpfFact]
        public void OnMethodWithExpressionBody4()
        {
            TestSpan(
@"class C
{
    void Foo() => [|12$$3|]; 
}");
        }

        [WpfFact]
        public void MissingOnMethod()
        {
            TestMissing(
@"class C
{
    void Foo($$);
}");
        }

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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
        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
        public void OnOperatorWithExpressionBody1()
        {
            TestSpan(
@"class C
{
    public static int op$$erator+(C c1, C c2) => [|c1|];
}");
        }

        [WpfFact]
        public void OnOperatorWithExpressionBody2()
        {
            TestSpan(
@"class C
{
    public static int operator+(C c1, C c2) =>$$ [|c1|];
}");
        }

        [WpfFact]
        public void OnOperatorWithExpressionBody3()
        {
            TestSpan(
@"class C
{
    public static int operator+(C c1, C c2) => [|c1|]; $$
}");
        }

        [WpfFact]
        public void OnOperatorWithExpressionBody4()
        {
            TestSpan(
@"class C
{
    public static int operator+(C c1, C c2) => [|c$$1|];
}");
        }

        [WpfFact]
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

        [WpfFact]
        public void OnConversionOperatorWithExpressionBody1()
        {
            TestSpan(
@"class C
{
    public static op$$erator DateTime(C c1) => [|DataTime.Now|];
}");
        }

        [WpfFact]
        public void OnConversionOperatorWithExpressionBody2()
        {
            TestSpan(
@"class C
{
    public static operator DateTime(C c1) =>$$ [|DataTime.Now|];
}");
        }

        [WpfFact]
        public void OnConversionOperatorWithExpressionBody3()
        {
            TestSpan(
@"class C
{
    public static operator DateTime(C c1) => [|DataTime.Now|];$$
}");
        }

        [WpfFact]
        public void OnConversionOperatorWithExpressionBody4()
        {
            TestSpan(
@"class C
{
    public static operator DateTime(C c1) => [|DataTime$$.Now|];
}");
        }

        [WorkItem(3557, "DevDiv_Projects/Roslyn")]
        [WpfFact]
        public void InFrontOfAttribute()
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
        [WpfFact]
        public void InInactivePPRegion()
        {
            TestLine(
@"

#if blahblah
$$fooby
#endif");
        }

        [WorkItem(538777)]
        [WpfFact]
        public void WithIncompleteDeclaration()
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
        [WpfFact]
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

        [WorkItem(937290)]
        [WpfFact]
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
    }
}
