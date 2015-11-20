// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Editor.UnitTests.Outlining;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining.Utils;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class DisabledTextOutlinerTests : AbstractOutlinerTests
    {
        private IEnumerable<OutliningSpan> GetRegions(SyntaxTree syntaxTree, SyntaxTrivia trivia)
        {
            var outliner = new DisabledTextTriviaOutliner();
            var spans = new List<OutliningSpan>();
            outliner.CollectOutliningSpans(syntaxTree, trivia, spans, CancellationToken.None);
            return spans;
        }

        private OutliningSpan GetRegion(SyntaxTree syntaxTree, SyntaxTrivia trivia)
        {
            return GetRegions(syntaxTree, trivia).SingleOrDefault();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestDisabledIf()
        {
            var tree = ParseLines("#if false",
                                        "Blah",
                                        "Blah",
                                        "Blah",
                                        "#endif");

            var trivia = (tree.GetRoot() as CompilationUnitSyntax).EndOfFileToken.LeadingTrivia;
            var disabledTextTrivia = trivia.ElementAt(1);
            Assert.Equal(SyntaxKind.DisabledTextTrivia, disabledTextTrivia.Kind());

            var actualRegion = GetRegion(tree, disabledTextTrivia);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(11, 27),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestDisabledElse()
        {
            var tree = ParseLines("#if true",
                                        "#else",
                                        "Blah",
                                        "Blah",
                                        "Blah",
                                        "#endif");

            var trivia = (tree.GetRoot() as CompilationUnitSyntax).EndOfFileToken.LeadingTrivia;
            var excludedTextTrivia = trivia.ElementAt(2);
            Assert.Equal(SyntaxKind.DisabledTextTrivia, excludedTextTrivia.Kind());

            var actualRegion = GetRegion(tree, excludedTextTrivia);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(17, 33),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestDisabledElIf()
        {
            var tree = ParseLines("#if true",
                                        "#elif false",
                                        "Blah",
                                        "Blah",
                                        "Blah",
                                        "#endif");

            var trivia = (tree.GetRoot() as CompilationUnitSyntax).EndOfFileToken.LeadingTrivia;
            var excludedTextTrivia = trivia.ElementAt(2);
            Assert.Equal(SyntaxKind.DisabledTextTrivia, excludedTextTrivia.Kind());

            var actualRegion = GetRegion(tree, excludedTextTrivia);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(23, 39),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        [WorkItem(531360)]
        public void DisabledCodeWithEmbeddedPreprocessorDirectivesShouldCollapseEntireDisabledRegion()
        {
            var tree = ParseCode(@"class P {
#if false
    void M()
    {
#region ""R""
       M();
#endregion
        }
#endif
    }
");
            var actualRegion = GetRegion(tree, tree.GetRoot().DescendantTrivia().First(t => t.IsKind(SyntaxKind.DisabledTextTrivia)));
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(22, 90), CSharpOutliningHelpers.Ellipsis, autoCollapse: true);
            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        [WorkItem(531360)]
        public void DisabledCodeShouldNotCollapseUnlessItFollowsADirective()
        {
            var tree = ParseCode(@"class P {
#if false
    void M()
    {
#region ""R""
       M();
#endregion
        }
#endif
    }
");
            var actualRegion = GetRegion(tree, tree.GetRoot().DescendantTrivia().Where(t => t.IsKind(SyntaxKind.DisabledTextTrivia)).Skip(1).First());
            Assert.Null(actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        [WorkItem(1070677)]
        public void NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion()
        {
            var tree = ParseCode(@"class P {
#if Foo
    void M()
    {
#if Bar
       M();
#endif
        }
#endif
    }
");
            var actualRegion = GetRegion(tree, tree.GetRoot().DescendantTrivia().First(t => t.IsKind(SyntaxKind.DisabledTextTrivia)));
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(20, 80), CSharpOutliningHelpers.Ellipsis, autoCollapse: true);
            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        [WorkItem(1070677)]
        public void NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion2()
        {
            var tree = ParseCode(@"class P {
#if Foo
    void M()
    {
#if Bar
       M();
       M();
#endif
        }
#endif
    }
");
            var actualRegion = GetRegion(tree, tree.GetRoot().DescendantTrivia().Where(t => t.IsKind(SyntaxKind.DisabledTextTrivia)).Skip(1).First());
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(50, 74), CSharpOutliningHelpers.Ellipsis, autoCollapse: true);
            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        [WorkItem(1070677)]
        public void NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion3()
        {
            var tree = ParseCode(@"class P {
#if Foo
    void M()
    {
#if Bar
       M();
       M();
        }
#endif
    }
");
            var actualRegion = GetRegion(tree, tree.GetRoot().DescendantTrivia().First(t => t.IsKind(SyntaxKind.DisabledTextTrivia)));
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(20, 39), CSharpOutliningHelpers.Ellipsis, autoCollapse: true);
            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        [WorkItem(1070677)]
        public void NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion4()
        {
            var tree = ParseCode(@"class P {
#if Foo
    void M()
    {
#if Bar
       M();
#line 10
        //some more text...
        //text
#if Car
        //random text
        //text
#endif
        // more text
        // text
#endif
    }
#endif
    }
");
            var actualRegion = GetRegion(tree, tree.GetRoot().DescendantTrivia().First(t => t.IsKind(SyntaxKind.DisabledTextTrivia)));
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(20, 226), CSharpOutliningHelpers.Ellipsis, autoCollapse: true);
            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        [WorkItem(1100600)]
        public void PreprocessorDirectivesInTrailingTrivia()
        {
            var tree = ParseCode(@"
class C
{
    void M()
    {
        extern mscorlib 
        {{
          a = x;
        }} 
    }

#if false
Disabled
Code
#endif
}");

            var actualRegion = GetRegion(tree, tree.GetRoot().DescendantTrivia().First(t => t.IsKind(SyntaxKind.DisabledTextTrivia)));
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(124, 138), CSharpOutliningHelpers.Ellipsis, autoCollapse: true);
            AssertRegion(expectedRegion, actualRegion);
        }
    }
}
