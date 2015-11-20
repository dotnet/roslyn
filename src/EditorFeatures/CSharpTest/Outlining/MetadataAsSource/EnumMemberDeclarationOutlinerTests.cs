// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;
using MaSOutliners = Microsoft.CodeAnalysis.Editor.CSharp.Outlining.MetadataAsSource;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining.MetadataAsSource
{
    public class EnumMemberDeclarationOutlinerTests : AbstractOutlinerTests<EnumMemberDeclarationSyntax>
    {
        internal override AbstractSyntaxNodeOutliner<EnumMemberDeclarationSyntax> CreateOutliner()
        {
            return new MaSOutliners.EnumMemberDeclarationOutliner();
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void NoCommentsOrAttributes()
        {
            const string code = @"
enum E
{
    $$Foo,
    Bar
}";

            NoRegions(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithAttributes()
        {
            const string code = @"
enum E
{
    {|hint:{|collapse:[Blah]
    |}$$Foo|},
    Bar
}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void WithCommentsAndAttributes()
        {
            const string code = @"
enum E
{
    {|hint:{|collapse:// Summary:
    //     This is a summary.
    [Blah]
    |}$$Foo|},
    Bar
}";

            Regions(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }
    }
}
