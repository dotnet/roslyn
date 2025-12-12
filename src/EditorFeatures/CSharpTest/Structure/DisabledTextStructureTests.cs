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
    public Task TestDisabledIf()
        => VerifyBlockSpansAsync("""
                #if false
                {|span:$$Blah
                Blah
                Blah|}
                #endif
                """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestDisabledElse()
        => VerifyBlockSpansAsync("""
                #if true
                #else
                {|span:$$Blah
                Blah
                Blah|}
                #endif
                """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact]
    public Task TestDisabledElIf()
        => VerifyBlockSpansAsync("""
                #if true
                #elif false
                {|span:$$Blah
                Blah
                Blah|}
                #endif
                """,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531360")]
    public Task DisabledCodeWithEmbeddedPreprocessorDirectivesShouldCollapseEntireDisabledRegion()
        => VerifyBlockSpansAsync("""
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531360")]
    public Task DisabledCodeShouldNotCollapseUnlessItFollowsADirective()
        => VerifyNoBlockSpansAsync("""
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070677")]
    public Task NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion()
        => VerifyBlockSpansAsync("""
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

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=459257")]
    public Task NestedDisabledCodePreProcessorDirectivesWithElseShouldCollapseEntireDisabledRegion()
        => VerifyBlockSpansAsync("""
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

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=459257")]
    public Task NestedDisabledCodePreProcessorDirectivesWithElifShouldCollapseEntireDisabledRegion()
        => VerifyBlockSpansAsync("""
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

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=459257")]
    public Task NestedDisabledCodePreProcessorDirectivesWithElseAndElifShouldCollapseEntireDisabledRegion()
        => VerifyBlockSpansAsync("""
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070677")]
    public Task NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion2()
        => VerifyBlockSpansAsync("""
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070677")]
    public Task NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion3()
        => VerifyBlockSpansAsync("""
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070677")]
    public Task NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion4()
        => VerifyBlockSpansAsync("""
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

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1100600")]
    public Task PreprocessorDirectivesInTrailingTrivia()
        => VerifyBlockSpansAsync("""
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
