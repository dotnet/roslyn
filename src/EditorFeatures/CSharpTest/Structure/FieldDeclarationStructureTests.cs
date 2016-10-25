// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure
{
    public class FieldDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<FieldDeclarationSyntax>
    {
        internal override AbstractSyntaxStructureProvider CreateProvider() => new FieldDeclarationStructureProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestFieldWithComments()
        {
            const string code = @"
class C
{
    {|span:// Foo
    // Bar|}
    $$int F;
}";

            await VerifyBlockSpansAsync(code,
                Region("span", "// Foo ...", autoCollapse: true));
        }
    }
}
