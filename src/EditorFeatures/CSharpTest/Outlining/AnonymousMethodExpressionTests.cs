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
    public class AnonymousMethodExpressionTests :
        AbstractOutlinerTests<AnonymousMethodExpressionSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(AnonymousMethodExpressionSyntax lambdaExpression)
        {
            var outliner = new AnonymousMethodExpressionOutliner();
            return outliner.GetOutliningSpans(lambdaExpression, CancellationToken.None).WhereNotNull();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestAnonymousMethod()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  void Main()",
                                        "  {",
                                        "    delegate {",
                                        "      x();",
                                        "    };",
                                        "  }",
                                        "}");

            var lambdaExpression = tree.GetRoot().FindFirstNodeOfType<AnonymousMethodExpressionSyntax>();
            var actualRegion = GetRegion(lambdaExpression);

            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(45, 66),
                TextSpan.FromBounds(36, 66),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: false);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestAnonymousMethodInForLoop()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  void Main()",
                                        "  {",
                                        "    for (Action a = delegate { }; true; a()) { }",
                                        "  }",
                                        "}");

            var lambdaExpression = tree.GetRoot().FindFirstNodeOfType<AnonymousMethodExpressionSyntax>();
            var actualRegions = GetRegions(lambdaExpression).ToList();

            Assert.Equal(0, actualRegions.Count);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestAnonymousMethodInMethodCall1()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  void Main()",
                                        "  {",
                                        "    someMethod(42, \"test\", false, delegate(int x, int y, int z) {",
                                        "      return x + y + z;",
                                        "      }, \"other arguments\");",
                                        "  }",
                                        "}");

            var lambdaExpression = tree.GetRoot().FindFirstNodeOfType<AnonymousMethodExpressionSyntax>();
            var actualRegion = GetRegion(lambdaExpression);

            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(96, 131),
                TextSpan.FromBounds(66, 131),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: false);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestAnonymousMethodInMethodCall2()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  void Main()",
                                        "  {",
                                        "    someMethod(42, \"test\", false, delegate(int x, int y, int z) {",
                                        "      return x + y + z;",
                                        "      });",
                                        "  }",
                                        "}");

            var lambdaExpression = tree.GetRoot().FindFirstNodeOfType<AnonymousMethodExpressionSyntax>();
            var actualRegion = GetRegion(lambdaExpression);

            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(96, 131),
                TextSpan.FromBounds(66, 131),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: false);

            AssertRegion(expectedRegion, actualRegion);
        }
    }
}
