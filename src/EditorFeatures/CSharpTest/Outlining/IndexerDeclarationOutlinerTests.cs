// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class IndexerDeclarationOutlinerTests : AbstractCSharpSyntaxNodeOutlinerTests<IndexerDeclarationSyntax>
    {
        internal override AbstractSyntaxOutliner CreateOutliner() => new IndexerDeclarationOutliner();

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestIndexer()
        {
            const string code = @"
class C
{
    {|hint:$$public string this[int index]{|collapse:
    {
        get { }
    }|}|}
}";

            await VerifyRegionsAsync(code,
                Region("collapse", "hint", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestIndexerWithComments()
        {
            const string code = @"
class C
{
    {|span1:// Foo
    // Bar|}
    {|hint2:$$public string this[int index]{|collapse2:
    {
        get { }
    }|}|}
}";

            await VerifyRegionsAsync(code,
                Region("span1", "// Foo ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestIndexerWithWithExpressionBodyAndComments()
        {
            const string code = @"
class C
{
    {|span:// Foo
    // Bar|}
    $$public string this[int index] => 0;
}";

            await VerifyRegionsAsync(code,
                Region("span", "// Foo ...", autoCollapse: true));
        }
    }
}
