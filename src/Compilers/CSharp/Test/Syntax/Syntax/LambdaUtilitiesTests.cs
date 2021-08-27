// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class LambdaUtilitiesTests : CSharpTestBase
    {
        private void TestLambdaBody(string markedExpression, bool isLambdaBody, bool isReducedLambdaBody = false)
        {
            string markedSource = @"
using System;
using System.Linq;

class C 
{ 
    void M() 
    { 
        var expr = " + markedExpression + @"; 
    }
 
    static T F<T>(T x) => x;
}";
            string source;
            int? position;
            TextSpan? span;
            MarkupTestFile.GetPositionAndSpan(markedSource, out source, out position, out span);

            Assert.Null(position);
            Assert.NotNull(span);

            var tree = SyntaxFactory.ParseSyntaxTree(source);
            var compilation = CreateCompilationWithMscorlib45(new[] { tree }, new[] { SystemCoreRef });
            compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Verify();
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);

            var enclosingMethod = (IMethodSymbol)model.GetEnclosingSymbol(span.Value.Start);
            var enclosingSyntax = enclosingMethod.DeclaringSyntaxReferences.Single().GetSyntax();
            bool expected = enclosingMethod.MethodKind == MethodKind.LambdaMethod && enclosingSyntax.Span.Contains(span.Value);

            var node = tree.GetRoot().FindNode(span.Value);
            Assert.False(isLambdaBody && isReducedLambdaBody);
            Assert.Equal(expected, LambdaUtilities.IsLambdaBody(node, allowReducedLambdas: true));
            Assert.Equal(isLambdaBody || isReducedLambdaBody, expected);
            Assert.Equal(isLambdaBody, LambdaUtilities.IsLambdaBody(node));

            var methodDef = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(d => d.Identifier.ValueText == "M").Single();
            Assert.Equal("C", model.GetEnclosingSymbol(methodDef.SpanStart).ToTestDisplayString());
            Assert.Equal("C", model.GetEnclosingSymbol(methodDef.ParameterList.CloseParenToken.SpanStart).ToTestDisplayString());
            Assert.Equal("void C.M()", model.GetEnclosingSymbol(methodDef.Body.SpanStart).ToTestDisplayString());
        }

        [Fact]
        public void IsLambdaBody_AnonymousFunction1()
        {
            TestLambdaBody("new Func<int>(() => [|1|])", isLambdaBody: true);
            TestLambdaBody("new Func<int, int>(x => [|x|])", isLambdaBody: true);
            TestLambdaBody("new Func<int, int>((x) => [|x|])", isLambdaBody: true);
            TestLambdaBody("new Func<int, int>(x => [|{ return x; }|])", isLambdaBody: true);
            TestLambdaBody("new Func<int>(delegate [|{ return 1; }|] )", isLambdaBody: true);
        }

        [Fact]
        public void IsLambdaBody_From1()
        {
            TestLambdaBody(
                "from x in [|new[] { 1 }|] select x", isLambdaBody: false);

            TestLambdaBody(
                "from y in new[] { 1 } from x in [|new[] { 2 }|] select x", isLambdaBody: true);
        }

        [Fact]
        public void IsLambdaBody_Join1()
        {
            TestLambdaBody(
                "from y in new[] { 1 } join x in [|new[] { 2 }|] on y equals x select x", isLambdaBody: false);

            TestLambdaBody(
                "from y in new[] { 1 } join x in new[] { 2 } on [|y|] equals x select x", isLambdaBody: true);

            TestLambdaBody(
                "from y in new[] { 1 } join x in new[] { 2 } on y equals [|x|] select x", isLambdaBody: true);
        }

        [Fact]
        public void IsLambdaBody_OrderBy1()
        {
            TestLambdaBody(
                "from x in new[] { 1 } orderby [|x|] ascending select x", isLambdaBody: true);

            TestLambdaBody(
                "from x in new[] { 1 } orderby x descending, [|x|] ascending select x", isLambdaBody: true);
        }

        [Fact]
        public void IsLambdaBody_Where1()
        {
            TestLambdaBody(
                "from x in new[] { 1 } where [|x > 0|] select x", isLambdaBody: true);
        }

        [Fact]
        public void IsLambdaBody_Let1()
        {
            TestLambdaBody(
                "from x in new[] { 1 } let y = [|0|] select y", isLambdaBody: true);
        }

        [Fact]
        public void IsLambdaBody_Select1()
        {
            TestLambdaBody(
                "from x in new[] { 1 } select [|x|]", isLambdaBody: true);

            TestLambdaBody(
                "from x in new[] { 1 } where x > 0 select [|x|]", isLambdaBody: false, isReducedLambdaBody: true);

            TestLambdaBody(
                "from x in new[] { 1 } where x > 0 select [|@x|]", isLambdaBody: false, isReducedLambdaBody: true);

            TestLambdaBody(
                "from x in new[] { 1 } orderby F(x), F(x) descending select [|x|]", isLambdaBody: false, isReducedLambdaBody: true);

            TestLambdaBody(
                "from x in new[] { 1 } orderby x where x > 0 select [|x|]", isLambdaBody: false, isReducedLambdaBody: true);

            TestLambdaBody(
                "from x in new[] { 1 } select x into y where y > 0 select [|y|]", isLambdaBody: false, isReducedLambdaBody: true);

            TestLambdaBody(
                "from x in new[] { 1 } select x into y orderby y select [|y|]", isLambdaBody: false, isReducedLambdaBody: true);

            TestLambdaBody(
                "from x in new[] { 1 } select [|x|] into y where y > 0 select y", isLambdaBody: true);

            TestLambdaBody(
                "from x in new[] { 1 } where x > 0 select [|x|] into y where y > 0 select y", isLambdaBody: false, isReducedLambdaBody: true);

            TestLambdaBody(
                "from x in new[] { 1 } where x > 0 select x into y where y > 0 select [|y|]", isLambdaBody: false, isReducedLambdaBody: true);

            TestLambdaBody(
                "from x in new[] { 1 } orderby x let z = x where x > 0 select [|x|]", isLambdaBody: true);

            TestLambdaBody(
                "from x in new[] { 1 } from y in new[] { 2 } select [|x|]", isLambdaBody: true);

            TestLambdaBody(
                "from x in new[] { 1 } from y in new[] { 2 } select [|y|]", isLambdaBody: true);

            TestLambdaBody(
                "from x in new[] { 1 } join y in new[] { 2 } on x equals y select [|x|]", isLambdaBody: true);
        }

        [Fact]
        public void IsLambdaBody_GroupBy1()
        {
            TestLambdaBody(
                "from x in new[] { 1 } group [|x|] by x", isLambdaBody: false, isReducedLambdaBody: true);

            TestLambdaBody(
                "from x in new[] { 1 } group x by [|x|]", isLambdaBody: true);

            TestLambdaBody(
                "from x in new[] { 1 } where x > 0 group [|x|] by x", isLambdaBody: false, isReducedLambdaBody: true);

            TestLambdaBody(
                "from x in new[] { 1 } where x > 0 group x by [|x|]", isLambdaBody: true);

            TestLambdaBody(
                "from x in new[] { 1 } let y = x group [|x|] by x", isLambdaBody: true);

            TestLambdaBody(
                "from x in new[] { 1 } group [|x|] by x + 1 into y group y by y.Key + 2", isLambdaBody: false, isReducedLambdaBody: true);

            TestLambdaBody(
                "from x in new[] { 1 } group x by x + 1 into y group [|y|] by y.Key + 2", isLambdaBody: false, isReducedLambdaBody: true);

            TestLambdaBody(
                "from x in new[] { 1 } from y in new[] { 2 } group [|x|] by x", isLambdaBody: true);

            TestLambdaBody(
                "from x in new[] { 1 } from y in new[] { 2 } group [|y|] by y", isLambdaBody: true);

            TestLambdaBody(
                "from x in new[] { 1 } join y in new[] { 2 } on x equals y group [|x|] by x", isLambdaBody: true);
        }

        [Fact]
        public void AreEquivalentIgnoringLambdaBodies1()
        {
            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(1)"),
                SyntaxFactory.ParseExpression("F(1)")));

            Assert.False(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(1)"),
                SyntaxFactory.ParseExpression("F(2)")));

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(a => 1)"),
                SyntaxFactory.ParseExpression("F(a => 2)")));

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(() => 1)"),
                SyntaxFactory.ParseExpression("F(() => 2)")));

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(delegate { return 1; })"),
                SyntaxFactory.ParseExpression("F(delegate { return 2; })")));

            Assert.False(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(delegate (int a) { return 1; })"),
                SyntaxFactory.ParseExpression("F(delegate (bool a) { return 1; })")));

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(delegate (int a) { return 1; })"),
                SyntaxFactory.ParseExpression("F(delegate (int a) { return 2; })")));

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(() => { return 1; })"),
                SyntaxFactory.ParseExpression("F(() => { return 1; })")));

            Assert.False(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(() => { return 1; })"),
                SyntaxFactory.ParseExpression("F((a) => { return 1; })")));

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } select a)"),
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } select a + 1)")));

            Assert.False(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } where a > 0 select a)"),
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } where a > 0 select a + 1)")));

            Assert.False(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } orderby a select a)"),
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } orderby a select a + 1)")));

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } let b = 1 select a)"),
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } let b = 1 select a + 1)")));

            Assert.False(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } select a)"),
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } where b > 0 select a)")));

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } from b in new[] { 3, 4 } where b > 0 select a)"),
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } from b in new[] { 3, 4, 5 } where b > 1 select a + 1)")));

            Assert.False(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } join b in new[] { 3, 4 } on a equals b select a)"),
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } join b in new[] { 3, 4, 5 } on a equals b select a)")));

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } join b in new[] { 3, 4 } on a equals b select a)"),
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } join b in new[] { 3, 4 } on a + 1 equals b + 1 select a)")));

            Assert.False(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } select a)"),
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } join b in new[] { 3, 4 } on a equals b select a)")));

            Assert.False(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } group a by a into g select g)"),
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } group a + 1 by a into g select g)")));

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } group a by a into g select g)"),
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } group a by a + 1 into g select g)")));

            Assert.False(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } group a by a into g select g)"),
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } group a by a into q select q)")));

            Assert.True(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } orderby a, a descending, a ascending select a)"),
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } orderby a + 1, a - 1 descending, a + 1 ascending select a)")));

            Assert.False(LambdaUtilities.AreEquivalentIgnoringLambdaBodies(
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } orderby a, a descending, a ascending select a)"),
                SyntaxFactory.ParseExpression("F(from a in new[] { 1, 2 } orderby a, a descending, a descending select a)")));
        }
    }
}
