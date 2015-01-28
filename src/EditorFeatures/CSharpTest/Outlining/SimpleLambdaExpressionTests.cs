// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Compilers.CSharp;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.Services.CSharp.Outlining;
using Microsoft.CodeAnalysis.Services.Internal;
using Microsoft.CodeAnalysis.Services.Outlining;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Services.CSharp.UnitTests.Outlining
{
    public class SimpleLambdaExpressionTests :
        AbstractOutlinerTests<SimpleLambdaExpressionSyntax>
    {
        protected override IEnumerable<OutliningRegion> GetRegions(SyntaxTree tree, SimpleLambdaExpressionSyntax lambdaExpression)
        {
            var outliner = new SimpleLambdaExpressionOutliner();
            return outliner.GetRegions(tree, lambdaExpression).WhereNotNull();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestLambda()
        {
            var tree = Utils.ParseLines("class C",
                                        "{",
                                        "  void Main()",
                                        "  {",
                                        "    f => {",
                                        "      x();",
                                        "    };",
                                        "  }",
                                        "}");

            var lambdaExpression = tree.Root.FindFirstNodeOfType<SimpleLambdaExpressionSyntax>();
            var actualRegion = GetRegion(tree, lambdaExpression);

            var expectedRegion = new OutliningRegion(
                Span.FromBounds(41, 62),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: false);

            AssertRegion(expectedRegion, actualRegion);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestLambdaInForLoop()
        {
            var tree = Utils.ParseLines("class C",
                                        "{",
                                        "  void Main()",
                                        "  {",
                                        "    for (Action a = x => { }; true; a()) { }",
                                        "  }",
                                        "}");

            var lambdaExpression = tree.Root.FindFirstNodeOfType<SimpleLambdaExpressionSyntax>();
            var actualRegions = GetRegions(tree, lambdaExpression).ToList();

            Assert.Equal(0, actualRegions.Count);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestLambdaInMethodCall1()
        {
            var tree = Utils.ParseLines("class C",
                                        "{",
                                        "  void Main()",
                                        "  {",
                                        "    someMethod(42, \"test\", false, x => {",
                                        "      return x;",
                                        "      }, \"other arguments\");",
                                        "  }",
                                        "}");

            var lambdaExpression = tree.Root.FindFirstNodeOfType<SimpleLambdaExpressionSyntax>();
            var actualRegion = GetRegion(tree, lambdaExpression);

            var expectedRegion = new OutliningRegion(
                Span.FromBounds(71, 98),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: false);

            AssertRegion(expectedRegion, actualRegion);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestLambdaInMethodCall2()
        {
            var tree = Utils.ParseLines("class C",
                                        "{",
                                        "  void Main()",
                                        "  {",
                                        "    someMethod(42, \"test\", false, x => {",
                                        "      return x;",
                                        "      });",
                                        "  }",
                                        "}");

            var lambdaExpression = tree.Root.FindFirstNodeOfType<SimpleLambdaExpressionSyntax>();
            var actualRegion = GetRegion(tree, lambdaExpression);

            var expectedRegion = new OutliningRegion(
                Span.FromBounds(71, 98),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: false);

            AssertRegion(expectedRegion, actualRegion);
        }
    }
}
