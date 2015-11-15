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
    public class ParenthesizedLambdaOutlinerTests :
        AbstractOutlinerTests<ParenthesizedLambdaExpressionSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(ParenthesizedLambdaExpressionSyntax lambdaExpression)
        {
            var outliner = new ParenthesizedLambdaExpressionOutliner();
            return outliner.GetOutliningSpans(lambdaExpression, CancellationToken.None).WhereNotNull();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestLambda()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  void Main()",
                                        "  {",
                                        "    () => {",
                                        "      x();",
                                        "    };",
                                        "  }",
                                        "}");

            var lambdaExpression = tree.GetRoot().FindFirstNodeOfType<ParenthesizedLambdaExpressionSyntax>();
            var actualRegion = GetRegion(lambdaExpression);

            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(42, 63),
                TextSpan.FromBounds(36, 63),
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
                                        "    for (Action a = () => { }; true; a()) { }",
                                        "  }",
                                        "}");

            var lambdaExpression = tree.GetRoot().FindFirstNodeOfType<ParenthesizedLambdaExpressionSyntax>();
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
                                        "    someMethod(42, \"test\", false, (x, y, z) => {",
                                        "      return x + y + z;",
                                        "      }, \"other arguments\");",
                                        "  }",
                                        "}");

            var lambdaExpression = tree.GetRoot().FindFirstNodeOfType<ParenthesizedLambdaExpressionSyntax>();
            var actualRegion = GetRegion(lambdaExpression);

            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(79, 114),
                TextSpan.FromBounds(66, 114),
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
                                        "    someMethod(42, \"test\", false, (x, y, z) => {",
                                        "      return x + y + z;",
                                        "      });",
                                        "  }",
                                        "}");

            var lambdaExpression = tree.GetRoot().FindFirstNodeOfType<ParenthesizedLambdaExpressionSyntax>();
            var actualRegion = GetRegion(lambdaExpression);

            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(79, 114),
                TextSpan.FromBounds(66, 114),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: false);

            AssertRegion(expectedRegion, actualRegion);
        }
    }
}
