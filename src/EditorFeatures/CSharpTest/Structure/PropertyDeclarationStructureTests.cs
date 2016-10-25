// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure
{
    public class PropertyDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<PropertyDeclarationSyntax>
    {
        internal override AbstractSyntaxStructureProvider CreateProvider() => new PropertyDeclarationStructureProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestProperty()
        {
            const string code = @"
class C
{
    {|hint:$$public int Foo{|textspan:
    {
        get { }
        set { }
    }|}|}
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestPropertyWithLeadingComments()
        {
            const string code = @"
class C
{
    {|span1:// Foo
    // Bar|}
    {|hint2:$$public int Foo{|textspan2:
    {
        get { }
        set { }
    }|}|}
}";

            await VerifyBlockSpansAsync(code,
                Region("span1", "// Foo ...", autoCollapse: true),
                Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestPropertyWithWithExpressionBodyAndComments()
        {
            const string code = @"
class C
{
    {|span:// Foo
    // Bar|}
    $$public int Foo => 0;
}";

            await VerifyBlockSpansAsync(code,
                Region("span", "// Foo ...", autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestPropertyWithSpaceAfterIdentifier()
        {
            const string code = @"
class C
{
    {|hint:$$public int Foo    {|textspan:
    {
        get { }
        set { }
    }|}|}
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }
    }
}
