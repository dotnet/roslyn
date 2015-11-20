// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;
using MaSOutliners = Microsoft.CodeAnalysis.Editor.CSharp.Outlining.MetadataAsSource;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining.MetadataAsSource
{
    public class DestructorDeclarationOutlinerTests : AbstractOutlinerTests<DestructorDeclarationSyntax>
    {
        internal override AbstractSyntaxNodeOutliner<DestructorDeclarationSyntax> CreateOutliner()
        {
            return new MaSOutliners.DestructorDeclarationOutliner();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void NoCommentsOrAttributes()
        {
            const string code = @"
class Foo
{
    $$~Foo();
}";

            NoRegions(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithAttributes()
        {
            const string code = @"
class Foo
{
    {|hint:{|collapse:[Bar]
    |}$$~Foo();|}
}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithCommentsAndAttributes()
        {
            const string code = @"
class Foo
{
    {|hint:{|collapse:// Summary:
    //     This is a summary.
    [Bar]
    |}$$~Foo();|}
}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }
    }
}
