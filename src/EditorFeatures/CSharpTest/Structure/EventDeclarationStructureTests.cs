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
    public class EventDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<EventDeclarationSyntax>
    {
        internal override AbstractSyntaxStructureProvider CreateProvider() => new EventDeclarationStructureProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestEvent1()
        {
            const string code = @"
class C
{
    {|hint:$$event EventHandler E{|textspan:
    {
        add { }
        remove { }
    }|}|}
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestEvent2()
        {
            const string code = @"
class C
{
    {|hint:$$event EventHandler E{|textspan:
    {
        add { }
        remove { }
    }|}|}
    event EventHandler E2
    {
        add { }
        remove { }
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestEvent3()
        {
            const string code = @"
class C
{
    {|hint:$$event EventHandler E{|textspan:
    {
        add { }
        remove { }
    }|}|}

    event EventHandler E2
    {
        add { }
        remove { }
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestEvent4()
        {
            const string code = @"
class C
{
    {|hint:$$event EventHandler E{|textspan:
    {
        add { }
        remove { }
    }|}|}

    EventHandler E2
    {
        get;
        set;
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestEvent5()
        {
            const string code = @"
class C
{
    {|hint:$$event EventHandler E{|textspan:
    {
        add { }
        remove { }
    }|}|}

    EventHandler this[int index]
    {
        get => throw null;
        set => throw null;
    }
}";

            await VerifyBlockSpansAsync(code,
                Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestEventWithComments()
        {
            const string code = @"
class C
{
    {|span1:// Goo
    // Bar|}
    {|hint2:$$event EventHandler E{|textspan2:
    {
        add { }
        remove { }
    }|}|}
}";

            await VerifyBlockSpansAsync(code,
                Region("span1", "// Goo ...", autoCollapse: true),
                Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }
    }
}
