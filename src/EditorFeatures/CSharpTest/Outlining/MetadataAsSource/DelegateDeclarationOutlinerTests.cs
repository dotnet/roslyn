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
    public class DelegateDeclarationOutlinerTests :
        AbstractOutlinerTests<DelegateDeclarationSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(DelegateDeclarationSyntax node)
        {
            var outliner = new MaSOutliners.DelegateDeclarationOutliner();
            return outliner.GetOutliningSpans(node, CancellationToken.None).WhereNotNull();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void NoCommentsOrAttributes()
        {
            var tree = ParseCode(@"public delegate TResult Blah<in T, out TResult>(T arg);");
            var delegateDecl = tree.DigToFirstNodeOfType<DelegateDeclarationSyntax>();

            Assert.Empty(GetRegions(delegateDecl));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithAttributes()
        {
            var tree = ParseCode(
@"[Foo]
public delegate TResult Blah<in T, out TResult>(T arg);");
            var delegateDecl = tree.DigToFirstNodeOfType<DelegateDeclarationSyntax>();

            var actualRegion = GetRegion(delegateDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 7),
                TextSpan.FromBounds(0, 62),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithCommentsAndAttributes()
        {
            var tree = ParseCode(
@"// Summary:
//     This is a summary.
[Foo]
delegate TResult Blah<in T, out TResult>(T arg);");
            var delegateDecl = tree.DigToFirstNodeOfType<DelegateDeclarationSyntax>();

            var actualRegion = GetRegion(delegateDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 47),
                TextSpan.FromBounds(0, 95),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithCommentsAttributesAndModifiers()
        {
            var tree = ParseCode(
@"// Summary:
//     This is a summary.
[Foo]
public delegate TResult Blah<in T, out TResult>(T arg);");
            var delegateDecl = tree.DigToFirstNodeOfType<DelegateDeclarationSyntax>();

            var actualRegion = GetRegion(delegateDecl);
            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 47),
                TextSpan.FromBounds(0, 102),
                CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }
    }
}
