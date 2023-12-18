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
public class DisabledTextStructureTests : AbstractCSharpSyntaxTriviaStructureTests
{
    internal override AbstractSyntaxStructureProvider CreateProvider() => new DisabledTextTriviaStructureProvider();

    [Fact]
    public async Task TestDisabledIf()
    {
        var code = """
                #if false
                {|span:$$Blah
                Blah
                Blah|}
                #endif
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestDisabledElse()
    {
        var code = """
                #if true
                #else
                {|span:$$Blah
                Blah
                Blah|}
                #endif
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact]
    public async Task TestDisabledElIf()
    {
        var code = """
                #if true
                #elif false
                {|span:$$Blah
                Blah
                Blah|}
                #endif
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531360")]
    public async Task DisabledCodeWithEmbeddedPreprocessorDirectivesShouldCollapseEntireDisabledRegion()
    {
        var code = """
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
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531360")]
    public async Task DisabledCodeShouldNotCollapseUnlessItFollowsADirective()
    {
        var code = """
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
                """;

        await VerifyNoBlockSpansAsync(code);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070677")]
    public async Task NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion()
    {
        var code = """
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
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=459257")]
    public async Task NestedDisabledCodePreProcessorDirectivesWithElseShouldCollapseEntireDisabledRegion()
    {
        var code = """
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
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=459257")]
    public async Task NestedDisabledCodePreProcessorDirectivesWithElifShouldCollapseEntireDisabledRegion()
    {
        var code = """
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
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=459257")]
    public async Task NestedDisabledCodePreProcessorDirectivesWithElseAndElifShouldCollapseEntireDisabledRegion()
    {
        var code = """
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
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070677")]
    public async Task NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion2()
    {
        var code = """
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
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070677")]
    public async Task NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion3()
    {
        var code = """
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
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070677")]
    public async Task NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion4()
    {
        var code = """
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
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1100600")]
    public async Task PreprocessorDirectivesInTrailingTrivia()
    {
        var code = """
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
                """;

        await VerifyBlockSpansAsync(code,
            Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
    }
}
