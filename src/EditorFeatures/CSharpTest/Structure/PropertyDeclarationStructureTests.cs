﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure
{
    public class PropertyDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<PropertyDeclarationSyntax>
    {
        internal override AbstractSyntaxStructureProvider CreateProvider() => new PropertyDeclarationStructureProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestProperty1()
        {
            const string code = @"
class C
{
    {|hint:$$public int Goo{|textspan:
    {
        get { }
        set { }
    }|}|}
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestProperty2()
        {
            const string code = @"
class C
{
    {|hint:$$public int Goo{|textspan:
    {
        get { }
        set { }
    }|}|}
    public int Goo2
    {
        get { }
        set { }
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestProperty3()
        {
            const string code = @"
class C
{
    {|hint:$$public int Goo{|textspan:
    {
        get { }
        set { }
    }|}|}

    public int Goo2
    {
        get { }
        set { }
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestProperty4()
        {
            const string code = @"
class C
{
    {|hint:$$public int Goo{|textspan:
    {
        get { }
        set { }
    }|}|}

    public int this[int value]
    {
        get { }
        set { }
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestProperty5()
        {
            const string code = @"
class C
{
    {|hint:$$public int Goo{|textspan:
    {
        get { }
        set { }
    }|}|}

    public event EventHandler Event;
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestProperty6()
        {
            const string code = @"
class C
{
    {|hint:$$public int Goo{|textspan:
    {
        get { }
        set { }
    }|}|}

    public event EventHandler Event
    {
        add { }
        remove { }
    }
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
    {|span1:// Goo
    // Bar|}
    {|hint2:$$public int Goo{|textspan2:
    {
        get { }
        set { }
    }|}|}
}";

            await VerifyBlockSpansAsync(code,
                Region("span1", "// Goo ...", autoCollapse: true),
                Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestPropertyWithWithExpressionBodyAndComments()
        {
            const string code = @"
class C
{
    {|span:// Goo
    // Bar|}
    $$public int Goo => 0;
}";

            await VerifyBlockSpansAsync(code,
                Region("span", "// Goo ...", autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestPropertyWithSpaceAfterIdentifier()
        {
            const string code = @"
class C
{
    {|hint:$$public int Goo    {|textspan:
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
