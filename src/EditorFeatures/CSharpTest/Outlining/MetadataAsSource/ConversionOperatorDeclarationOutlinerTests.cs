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
    public class ConversionOperatorDeclarationOutlinerTests :
        AbstractOutlinerTests<ConversionOperatorDeclarationSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(ConversionOperatorDeclarationSyntax node)
        {
            var outliner = new MaSOutliners.ConversionOperatorDeclarationOutliner();
            return outliner.GetOutliningSpans(node, CancellationToken.None).WhereNotNull();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void NoCommentsOrAttributes()
        {
            var tree = ParseCode(
@"class Foo
{
    public static explicit operator Foo (byte b);
}");
            var typeDecl = tree.DigToFirstTypeDeclaration();
            var operatorMethod = typeDecl.DigToFirstNodeOfType<ConversionOperatorDeclarationSyntax>();

            Assert.Empty(GetRegions(operatorMethod));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithAttributes()
        {
            var tree = ParseCode(
@"class Foo
{
    [Blah]
    public static explicit operator Foo (byte b);
}");
            var typeDecl = tree.DigToFirstTypeDeclaration();
            var operatorMethod = typeDecl.DigToFirstNodeOfType<ConversionOperatorDeclarationSyntax>();

            var actualRegion = GetRegion(operatorMethod);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(18, 30),
                TextSpan.FromBounds(18, 75),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithCommentsAndAttributes()
        {
            var tree = ParseCode(
@"class Foo
{
    // Summary:
    //     This is a summary.
    [Blah]
    public static explicit operator Foo (byte b);
}");
            var typeDecl = tree.DigToFirstTypeDeclaration();
            var operatorMethod = typeDecl.DigToFirstNodeOfType<ConversionOperatorDeclarationSyntax>();

            var actualRegion = GetRegion(operatorMethod);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(18, 78),
                TextSpan.FromBounds(18, 123),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }
    }
}
