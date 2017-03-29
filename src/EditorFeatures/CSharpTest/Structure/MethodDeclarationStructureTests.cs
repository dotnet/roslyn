// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure
{
    public class MethodDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<MethodDeclarationSyntax>
    {
        internal override AbstractSyntaxStructureProvider CreateProvider() => new MethodDeclarationStructureProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestMethod()
        {
            const string code = @"
class C
{
    {|hint:$$public string Foo(){|textspan:
    {
    }|}|}
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestMethodWithTrailingSpaces()
        {
            const string code = @"
class C
{
    {|hint:$$public string Foo()    {|textspan:
    {
    }|}|}
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestMethodWithLeadingComments()
        {
            const string code = @"
class C
{
    {|span1:// Foo
    // Bar|}
    {|hint2:$$public string Foo(){|textspan2:
    {
    }|}|}
}";

            await VerifyBlockSpansAsync(code,
                Region("span1", "// Foo ...", autoCollapse: true),
                Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestMethodWithWithExpressionBodyAndComments()
        {
            const string code = @"
class C
{
    {|span:// Foo
    // Bar|}
    $$public string Foo() => ""Foo"";
}";

            await VerifyBlockSpansAsync(code,
                Region("span", "// Foo ...", autoCollapse: true));
        }
    }
}
