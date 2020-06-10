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
    public class OperatorDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<OperatorDeclarationSyntax>
    {
        internal override AbstractSyntaxStructureProvider CreateProvider() => new OperatorDeclarationStructureProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestOperator1()
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
        public async Task TestOperator2()
        {
            const string code = @"
class C
{
    {|hint:$$public static int operator +(int i){|textspan:
    {
    }|}|}
    public static int operator -(int i)
    {
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestOperator3()
        {
            const string code = @"
class C
{
    {|hint:$$public static int operator +(int i){|textspan:
    {
    }|}|}

    public static int operator -(int i)
    {
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestOperator4()
        {
            const string code = @"
class C
{
    {|hint:$$public static int operator +(int i){|textspan:
    {
    }|}|}
    public static explicit operator C(int i)
    {
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestOperator5()
        {
            const string code = @"
class C
{
    {|hint:$$public static int operator +(int i){|textspan:
    {
    }|}|}
    public static explicit operator C(int i)
    {
    }
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
