// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Structure;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Structure
{
    public class DisabledTextStructureTests : AbstractCSharpSyntaxTriviaStructureTests
    {
        internal override AbstractSyntaxStructureProvider CreateProvider() => new DisabledTextTriviaStructureProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestDisabledIf()
        {
            const string code = @"
#if false
{|span:$$Blah
Blah
Blah|}
#endif
";

            await VerifyBlockSpansAsync(code,
                Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestDisabledElse()
        {
            const string code = @"
#if true
#else
{|span:$$Blah
Blah
Blah|}
#endif
";

            await VerifyBlockSpansAsync(code,
                Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task TestDisabledElIf()
        {
            const string code = @"
#if true
#elif false
{|span:$$Blah
Blah
Blah|}
#endif
";

            await VerifyBlockSpansAsync(code,
                Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [WorkItem(531360, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531360")]
        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task DisabledCodeWithEmbeddedPreprocessorDirectivesShouldCollapseEntireDisabledRegion()
        {
            const string code = @"
class P {
#if false
{|span:    void $$M()
    {
#region ""R""
       M();
#endregion
        }|}
#endif
    }
";

            await VerifyBlockSpansAsync(code,
                Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [WorkItem(531360, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531360")]
        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task DisabledCodeShouldNotCollapseUnlessItFollowsADirective()
        {
            const string code = @"
class P {
#if false
{|span:    void M()
    {
#region ""R""
       $$M();
#endregion
        }|}
#endif
    }
";

            await VerifyNoBlockSpansAsync(code);
        }

        [WorkItem(1070677, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070677")]
        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion()
        {
            const string code = @"
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
";

            await VerifyBlockSpansAsync(code,
                Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [WorkItem(459257, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=459257")]
        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task NestedDisabledCodePreProcessorDirectivesWithElseShouldCollapseEntireDisabledRegion()
        {
            const string code = @"
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
";

            await VerifyBlockSpansAsync(code,
                Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [WorkItem(459257, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=459257")]
        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task NestedDisabledCodePreProcessorDirectivesWithElifShouldCollapseEntireDisabledRegion()
        {
            const string code = @"
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
";

            await VerifyBlockSpansAsync(code,
                Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [WorkItem(459257, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=459257")]
        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task NestedDisabledCodePreProcessorDirectivesWithElseAndElifShouldCollapseEntireDisabledRegion()
        {
            const string code = @"
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
";

            await VerifyBlockSpansAsync(code,
                Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [WorkItem(1070677, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070677")]
        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion2()
        {
            const string code = @"
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
";

            await VerifyBlockSpansAsync(code,
                Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [WorkItem(1070677, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070677")]
        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion3()
        {
            const string code = @"
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
";

            await VerifyBlockSpansAsync(code,
                Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [WorkItem(1070677, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070677")]
        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion4()
        {
            const string code = @"
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
";

            await VerifyBlockSpansAsync(code,
                Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }

        [WorkItem(1100600, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1100600")]
        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task PreprocessorDirectivesInTrailingTrivia()
        {
            const string code = @"
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
";

            await VerifyBlockSpansAsync(code,
                Region("span", CSharpStructureHelpers.Ellipsis, autoCollapse: true));
        }
    }
}
