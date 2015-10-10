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
    public class DelegateDeclarationOutlinerTests :
        AbstractOutlinerTests<DelegateDeclarationSyntax>
    {
        internal override IEnumerable<OutliningSpan> GetRegions(DelegateDeclarationSyntax delegateDeclaration)
        {
            var outliner = new DelegateDeclarationOutliner();
            return outliner.GetOutliningSpans(delegateDeclaration, CancellationToken.None);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestDelegateWithComments()
        {
            var tree = ParseLines("// Foo",
                                        "// Bar",
                                        "public delegate void C();");

            var delegateDecl = tree.DigToFirstNodeOfType<DelegateDeclarationSyntax>();

            var actualRegion = GetRegion(delegateDecl);

            var expectedRegion = new OutliningSpan(
                TextSpan.FromBounds(0, 14),
                "// Foo ...",
                autoCollapse: true);

            AssertRegion(expectedRegion, actualRegion);
        }
    }
}
