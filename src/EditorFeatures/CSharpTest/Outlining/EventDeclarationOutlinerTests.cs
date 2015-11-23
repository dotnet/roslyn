// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class EventDeclarationOutlinerTests :
        AbstractOutlinerTests<EventDeclarationSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(EventDeclarationSyntax eventDeclaration)
        {
            var outliner = new EventDeclarationOutliner();
            return outliner.GetOutliningSpans(eventDeclaration, CancellationToken.None);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestEvent()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  event EventHandler E",
                                        "  {",
                                        "    add { }",
                                        "    remove { }",
                                        "  }",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var eventDecl = typeDecl.DigToFirstNodeOfType<EventDeclarationSyntax>();

            var actualRegion = GetRegion(eventDecl);

            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(34, 73),
                TextSpan.FromBounds(14, 73),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestEventWithComments()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  // Foo",
                                        "  // Bar",
                                        "  event EventHandler E",
                                        "  {",
                                        "    add { }",
                                        "    remove { }",
                                        "  }",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var eventDecl = typeDecl.DigToFirstNodeOfType<EventDeclarationSyntax>();

            var actualRegions = GetRegions(eventDecl).ToList();

            var expectedRegion1 = new OutliningSpan(
                TextSpan.FromBounds(14, 30),
                "// Foo ...",
                autoCollapse: true);

            AssertRegion(expectedRegion1, actualRegions[0]);

            var expectedRegion2 = new OutliningSpan(
                TextSpan.FromBounds(54, 93),
                TextSpan.FromBounds(34, 93),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion2, actualRegions[1]);
        }
    }
}
