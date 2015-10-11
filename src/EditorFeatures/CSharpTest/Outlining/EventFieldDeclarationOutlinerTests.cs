// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class EventFieldDeclarationOutlinerTests :
        AbstractOutlinerTests<EventFieldDeclarationSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(EventFieldDeclarationSyntax eventFieldDecl)
        {
            var outliner = new EventFieldDeclarationOutliner();
            return outliner.GetOutliningSpans(eventFieldDecl, CancellationToken.None);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestEventFieldWithComments()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  // Foo",
                                        "  // Bar",
                                        "  event EventHandler E;",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var eventFieldDecl = typeDecl.DigToFirstNodeOfType<EventFieldDeclarationSyntax>();

            var actualRegion = GetRegion(eventFieldDecl);

            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(14, 30),
                "// Foo ...",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }
    }
}
