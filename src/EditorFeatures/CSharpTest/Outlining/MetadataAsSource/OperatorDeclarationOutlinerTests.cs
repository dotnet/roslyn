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
    public class OperatorDeclarationOutlinerTests : AbstractCSharpSyntaxNodeOutlinerTests<OperatorDeclarationSyntax>
    {
        protected override string WorkspaceKind => CodeAnalysis.WorkspaceKind.MetadataAsSource;
        internal override AbstractSyntaxOutliner CreateOutliner() => new MaSOutliners.OperatorDeclarationOutliner();

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task NoCommentsOrAttributes()
        {
            const string code = @"
class Foo
{
    public static bool operator $$==(Foo a, Foo b);
}";

            await VerifyNoRegionsAsync(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task WithAttributes()
        {
            const string code = @"
class Foo
{
    {|hint:{|collapse:[Blah]
    |}public static bool operator $$==(Foo a, Foo b);|}
}";

            await VerifyRegionsAsync(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task WithCommentsAndAttributes()
        {
            const string code = @"
class Foo
{
    {|hint:{|collapse:// Summary:
    //     This is a summary.
    [Blah]
    |}bool operator $$==(Foo a, Foo b);|}
}";

            await VerifyRegionsAsync(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task WithCommentsAttributesAndModifiers()
        {
            const string code = @"
class Foo
{
    {|hint:{|collapse:// Summary:
    //     This is a summary.
    [Blah]
    |}public static bool operator $$==(Foo a, Foo b);|}
}";

            await VerifyRegionsAsync(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }
    }
}
