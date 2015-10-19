// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class SimpleLambdaExpressionOutlinerTests :
        AbstractOutlinerTests<SimpleLambdaExpressionSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(SimpleLambdaExpressionSyntax lambdaExpression)
        {
            var outliner = new SimpleLambdaExpressionOutliner();
            return outliner.GetOutliningSpans(lambdaExpression, CancellationToken.None).WhereNotNull();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestLambda()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  void Main()",
                                        "  {",
                                        "    f => {",
                                        "      x();",
                                        "    };",
                                        "  }",
                                        "}");

            var lambdaExpression = tree.GetRoot().FindFirstNodeOfType<SimpleLambdaExpressionSyntax>();
            var actualRegion = GetRegion(lambdaExpression);

            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(41, 62),
                TextSpan.FromBounds(36, 62),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: false);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestLambdaInForLoop()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  void Main()",
                                        "  {",
                                        "    for (Action a = x => { }; true; a()) { }",
                                        "  }",
                                        "}");

            var lambdaExpression = tree.GetRoot().FindFirstNodeOfType<SimpleLambdaExpressionSyntax>();
            var actualRegions = GetRegions(lambdaExpression).ToList();

            Assert.Equal(0, actualRegions.Count);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestLambdaInMethodCall1()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  void Main()",
                                        "  {",
                                        "    someMethod(42, \"test\", false, x => {",
                                        "      return x;",
                                        "      }, \"other arguments\");",
                                        "  }",
                                        "}");

            var lambdaExpression = tree.GetRoot().FindFirstNodeOfType<SimpleLambdaExpressionSyntax>();
            var actualRegion = GetRegion(lambdaExpression);

            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(71, 98),
                TextSpan.FromBounds(66, 98),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: false);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestLambdaInMethodCall2()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  void Main()",
                                        "  {",
                                        "    someMethod(42, \"test\", false, x => {",
                                        "      return x;",
                                        "      });",
                                        "  }",
                                        "}");

            var lambdaExpression = tree.GetRoot().FindFirstNodeOfType<SimpleLambdaExpressionSyntax>();
            var actualRegion = GetRegion(lambdaExpression);

            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(71, 98),
                TextSpan.FromBounds(66, 98),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: false);

            AssertRegion(expectedRegion, actualRegion);
        }
    }
}
