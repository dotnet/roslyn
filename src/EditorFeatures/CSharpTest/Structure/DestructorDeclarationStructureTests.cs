// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure
{
    public class DestructorDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<DestructorDeclarationSyntax>
    {
        internal override AbstractSyntaxStructureProvider CreateProvider() => new DestructorDeclarationStructureProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestDestructor()
        {
            const string code = @"
class C
{
    {|hint:$$~C(){|collapse:
    {
    }|}|}
}";

            await VerifyBlockSpansAsync(code,
                Region("collapse", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestDestructorWithComments()
        {
            const string code = @"
class C
{
    {|span1:// Foo
    // Bar|}
    {|hint2:$$~C(){|collapse2:
    {
    }|}|}
}";

            await VerifyBlockSpansAsync(code,
                Region("span1", "// Foo ...", autoCollapse: true),
                Region("collapse2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestDestructorMissingCloseParenAndBody()
        {
            // Expected behavior is that the class should be outlined, but the destructor should not.

            const string code = @"
class C
{
    $$~C(
}";

            await VerifyNoBlockSpansAsync(code);
        }
    }
}
