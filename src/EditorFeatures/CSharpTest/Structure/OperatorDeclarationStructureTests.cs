// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure
{
    public class OperatorDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<OperatorDeclarationSyntax>
    {
        internal override AbstractSyntaxStructureProvider CreateProvider() => new OperatorDeclarationStructureProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestOperator()
        {
            const string code = @"
class C
{
    {|hint:$$public static int operator +(int i){|textspan:
    {
    }|}|}
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestOperatorWithLeadingComments()
        {
            const string code = @"
class C
{
    {|span1:// Goo
    // Bar|}
    {|hint2:$$public static int operator +(int i){|textspan2:
    {
    }|}|}
}";

            await VerifyBlockSpansAsync(code,
                Region("span1", "// Goo ...", autoCollapse: true),
                Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }
    }
}
