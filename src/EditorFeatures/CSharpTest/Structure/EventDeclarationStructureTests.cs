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

[Trait(Traits.Feature, Traits.Features.Outlining)]
public sealed class EventDeclarationStructureTests : AbstractCSharpSyntaxNodeStructureTests<EventDeclarationSyntax>
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new EventDeclarationStructureProvider();

    [Fact]
    public Task TestEvent1()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|hint:$$event EventHandler E{|textspan:
                    {
                        add { }
                        remove { }
                    }|}|}
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestEvent2()
        => VerifyBlockSpansAsync("""
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
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestEvent3()
        => VerifyBlockSpansAsync("""
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
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestEvent4()
        => VerifyBlockSpansAsync("""
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
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestEvent5()
        => VerifyBlockSpansAsync("""
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
                }
                """,
            Region("textspan", "hint", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestEventWithComments()
        => VerifyBlockSpansAsync("""
                class C
                {
                    {|span1:// Goo
                    // Bar|}
                    {|hint2:$$event EventHandler E{|textspan2:
                    {
                        add { }
                        remove { }
                    }|}|}
                }
                """,
            Region("span1", "// Goo ...", autoCollapse: true),
            Region("textspan2", "hint2", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
}
