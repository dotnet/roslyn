// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;
using MaSOutliners = Microsoft.CodeAnalysis.Editor.CSharp.Outlining.MetadataAsSource;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining.MetadataAsSource
{
    public class EnumDeclarationOutlinerTests : AbstractCSharpSyntaxNodeOutlinerTests<EnumDeclarationSyntax>
    {
        protected override string WorkspaceKind => CodeAnalysis.WorkspaceKind.MetadataAsSource;
        internal override AbstractSyntaxOutliner CreateOutliner() => new MaSOutliners.EnumDeclarationOutliner();

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task NoCommentsOrAttributes()
        {
            const string code = @"
enum $$E
{
    A,
    B
}";

            await VerifyNoRegionsAsync(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task WithAttributes()
        {
            const string code = @"
{|hint:{|collapse:[Bar]
|}enum $$E|}
{
    A,
    B
}";

            await VerifyRegionsAsync(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task WithCommentsAndAttributes()
        {
            const string code = @"
{|hint:{|collapse:// Summary:
//     This is a summary.
[Bar]
|}enum $$E|}
{
    A,
    B
}";

            await VerifyRegionsAsync(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task WithCommentsAttributesAndModifiers()
        {
            const string code = @"
{|hint:{|collapse:// Summary:
//     This is a summary.
[Bar]
|}public enum $$E|}
{
    A,
    B
}";

            await VerifyRegionsAsync(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }
    }
}
