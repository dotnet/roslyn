// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class DisabledTextOutlinerTests : AbstractCSharpSyntaxTriviaOutlinerTests
    {
        internal override AbstractSyntaxOutliner CreateOutliner() => new DisabledTextTriviaOutliner();

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

            await VerifyRegionsAsync(code,
                Region("span", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
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

            await VerifyRegionsAsync(code,
                Region("span", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
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

            await VerifyRegionsAsync(code,
                Region("span", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WorkItem(531360)]
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

            await VerifyRegionsAsync(code,
                Region("span", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WorkItem(531360)]
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

            await VerifyNoRegionsAsync(code);
        }

        [WorkItem(1070677)]
        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion()
        {
            const string code = @"
class P {
#if Foo
{|span:    void $$M()
    {
#if Bar
       M();
#endif
        }|}
#endif
    }
";

            await VerifyRegionsAsync(code,
                Region("span", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WorkItem(1070677)]
        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion2()
        {
            const string code = @"
class P {
#if Foo
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

            await VerifyRegionsAsync(code,
                Region("span", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WorkItem(1070677)]
        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion3()
        {
            const string code = @"
class P {
#if Foo
{|span:    void $$M()
    {|}
#if Bar
       M();
       M();
        }
#endif
    }
";

            await VerifyRegionsAsync(code,
                Region("span", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WorkItem(1070677)]
        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion4()
        {
            const string code = @"
class P {
#if Foo
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

            await VerifyRegionsAsync(code,
                Region("span", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WorkItem(1100600)]
        [Fact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public async Task PreprocessorDirectivesInTrailingTrivia()
        {
            const string code = @"
class P {
#if Foo
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

            await VerifyRegionsAsync(code,
                Region("span", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }
    }
}
