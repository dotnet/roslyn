// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure
{
    public class IndexerDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<IndexerDeclarationSyntax>
    {
        internal override AbstractSyntaxStructureProvider CreateProvider() => new IndexerDeclarationStructureProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestIndexer1()
        {
            const string code = @"
class C
{
    {|hint:$$public string this[int index]{|textspan:
    {
        get { }
    }|}|}
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestIndexer2()
        {
            const string code = @"
class C
{
    {|hint:$$public string this[int index]{|textspan:
    {
        get { }
    }|}|}
    int Value => 0;
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestIndexer3()
        {
            const string code = @"
class C
{
    {|hint:$$public string this[int index]{|textspan:
    {
        get { }
    }|}|}

    int Value => 0;
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestIndexerWithComments()
        {
            const string code = @"
class C
{
    {|span1:// Goo
    // Bar|}
    {|hint2:$$public string this[int index]{|textspan2:
    {
        get { }
    }|}|}
}";

            await VerifyBlockSpansAsync(code,
                Region("span1", "// Goo ...", autoCollapse: true),
                Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestIndexerWithWithExpressionBodyAndComments()
        {
            const string code = @"
class C
{
    {|span:// Goo
    // Bar|}
    $$public string this[int index] => 0;
}";

            await VerifyBlockSpansAsync(code,
                Region("span", "// Goo ...", autoCollapse: true));
        }
    }
}
