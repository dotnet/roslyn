// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using MaSOutliners = Microsoft.CodeAnalysis.Editor.CSharp.Outlining.MetadataAsSource;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining.MetadataAsSource
{
    public class TypeDeclarationOutlinerTests :
        AbstractOutlinerTests<TypeDeclarationSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(TypeDeclarationSyntax node)
        {
            var outliner = new MaSOutliners.TypeDeclarationOutliner();
            return outliner.GetOutliningSpans(node, CancellationToken.None).WhereNotNull();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void NoCommentsOrAttributes()
        {
            var tree = ParseCode(
@"class C
{
    void M();
}");
            var typeDecl = tree.DigToFirstTypeDeclaration();

            Assert.Empty(GetRegions(typeDecl));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithAttributes()
        {
            var tree = ParseCode(
@"[Bar]
[Baz]
public class C
{
    void M();
}");
            var typeDecl = tree.DigToFirstTypeDeclaration();

            var actualRegion = GetRegion(typeDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 14),
                TextSpan.FromBounds(0, 28),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithCommentsAndAttributes()
        {
            var tree = ParseCode(
@"// Summary:
//     This is a doc comment.
[Bar, Baz]
public class C
{
    void M();
}");
            var typeDecl = tree.DigToFirstTypeDeclaration();

            var actualRegion = GetRegion(typeDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 56),
                TextSpan.FromBounds(0, 70),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }
    }
}
