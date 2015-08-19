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
    public class FieldDeclarationOutlinerTests :
        AbstractOutlinerTests<FieldDeclarationSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(FieldDeclarationSyntax fieldDecl)
        {
            var outliner = new FieldDeclarationOutliner();
            return outliner.GetOutliningSpans(fieldDecl, CancellationToken.None);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestFieldWithComments()
        {
            var tree = ParseLines("class C",
                                        "{",
                                        "  // Foo",
                                        "  // Bar",
                                        "  int F;",
                                        "}");

            var typeDecl = tree.DigToFirstTypeDeclaration();
            var fieldDecl = typeDecl.DigToFirstNodeOfType<FieldDeclarationSyntax>();

            var actualRegion = GetRegion(fieldDecl);

            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(14, 30),
                "// Foo ...",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }
    }
}
