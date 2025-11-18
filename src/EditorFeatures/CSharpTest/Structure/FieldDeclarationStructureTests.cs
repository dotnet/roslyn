// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure;

public sealed class FieldDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<FieldDeclarationSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new FieldDeclarationStructureProvider();

    [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
    public Task TestFieldWithComments()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|span:// Goo
                    // Bar|}
                    $$int F;
                }
                """,
            Region("span", "// Goo ...", autoCollapse: true));
}
