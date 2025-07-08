// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure;

[Trait(Traits.Feature, Traits.Features.Outlining)]
public sealed class DisabledTextStructureTests : AbstractCSharpSyntaxTriviaStructureTests
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new DisabledTextTriviaStructureProvider();

    [Fact]
    public async Task TestDisabledIf()
    {
        await VerifyBlockSpansAsync("""
                #if false
                {|span:$$Blah
                Blah
                Blah|}
                #endif
                """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestDisabledElse()
    {
        await VerifyBlockSpansAsync("""
                #if true
                #else
                {|span:$$Blah
                Blah
                Blah|}
                #endif
                """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestDisabledElIf()
    {
        await VerifyBlockSpansAsync("""
                #if true
                #elif false
                {|span:$$Blah
                Blah
                Blah|}
                #endif
                """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531360")]
    public async Task DisabledCodeWithEmbeddedPreprocessorDirectivesShouldCollapseEntireDisabledRegion()
    {
        await VerifyBlockSpansAsync("""
                class P {
                #if false
                {|span:    void $$M()
                    {
                #region "R"
                       M();
                #endregion
                        }|}
                #endif
                    }
                """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531360")]
    public async Task DisabledCodeShouldNotCollapseUnlessItFollowsADirective()
    {
        await VerifyNoBlockSpansAsync("""
                class P {
                #if false
                {|span:    void M()
                    {
                #region "R"
                       $$M();
                #endregion
                        }|}
                #endif
                    }
                """);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070677")]
    public async Task NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion()
    {
        await VerifyBlockSpansAsync("""
                class P {
                #if Goo
                {|span:    void $$M()
                    {
                #if Bar
                       M();
                #endif
                        }|}
                #endif
                    }
                """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=459257")]
    public async Task NestedDisabledCodePreProcessorDirectivesWithElseShouldCollapseEntireDisabledRegion()
    {
        await VerifyBlockSpansAsync("""
                class P {
                #if Goo
                {|span:    void $$M()
                    {
                #if Bar
                       M();
                #else

                #endif
                        }|}
                #endif
                    }
                """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=459257")]
    public async Task NestedDisabledCodePreProcessorDirectivesWithElifShouldCollapseEntireDisabledRegion()
    {
        await VerifyBlockSpansAsync("""
                class P {
                #if Goo
                {|span:    void $$M()
                    {
                #if Bar
                       M();
                #elif Baz

                #endif
                        }|}
                #endif
                    }
                """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=459257")]
    public async Task NestedDisabledCodePreProcessorDirectivesWithElseAndElifShouldCollapseEntireDisabledRegion()
    {
        await VerifyBlockSpansAsync("""
                class P {
                #if Goo
                {|span:    void $$M()
                    {
                #if Bar
                       M();
                #else

                #elif Baz

                #endif
                        }|}
                #endif
                    }
                """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070677")]
    public async Task NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion2()
    {
        await VerifyBlockSpansAsync("""
                class P {
                #if Goo
                    void M()
                    {
                #if Bar
                {|span:       $$M();
                       M();|}
                #endif
                        }
                #endif
                    }
                """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070677")]
    public async Task NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion3()
    {
        await VerifyBlockSpansAsync("""
                class P {
                #if Goo
                {|span:    void $$M()
                    {|}
                #if Bar
                       M();
                       M();
                        }
                #endif
                    }
                """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070677")]
    public async Task NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion4()
    {
        await VerifyBlockSpansAsync("""
                class P {
                #if Goo
                {|span:    void $$M()
                    {
                #if Bar
                       M();
                #line 10
                        //some more text...
                        //text
                #if Car
                        //random text
                        //text
                #endif
                        // more text
                        // text
                #endif
                    }|}
                #endif
                    }
                """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1100600")]
    public async Task PreprocessorDirectivesInTrailingTrivia()
    {
        await VerifyBlockSpansAsync("""
                class P {
                #if Goo
                {|span:    void $$M()
                    {
                #if Bar
                       M();
                #line 10
                        //some more text...
                        //text
                #if Car
                        //random text
                        //text
                #endif
                        // more text
                        // text
                #endif
                    }|}
                #endif
                    }
                """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }
}
