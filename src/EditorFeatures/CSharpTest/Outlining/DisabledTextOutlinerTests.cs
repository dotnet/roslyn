// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.CSharp.Outlining;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Outlining
{
    public class DisabledTextOutlinerTests : AbstractCSharpSyntaxTriviaOutlinerTests
    {
        internal override AbstractSyntaxOutliner CreateOutliner() => new DisabledTextTriviaOutliner();

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestDisabledIf()
        {
            const string code = @"
#if false
{|span:$$Blah
Blah
Blah|}
#endif
";

            Regions(code,
                Region("span", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestDisabledElse()
        {
            const string code = @"
#if true
#else
{|span:$$Blah
Blah
Blah|}
#endif
";

            Regions(code,
                Region("span", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void TestDisabledElIf()
        {
            const string code = @"
#if true
#elif false
{|span:$$Blah
Blah
Blah|}
#endif
";

            Regions(code,
                Region("span", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WorkItem(531360)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void DisabledCodeWithEmbeddedPreprocessorDirectivesShouldCollapseEntireDisabledRegion()
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

            Regions(code,
                Region("span", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WorkItem(531360)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void DisabledCodeShouldNotCollapseUnlessItFollowsADirective()
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

            NoRegions(code);
        }

        [WorkItem(1070677)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion()
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

            Regions(code,
                Region("span", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WorkItem(1070677)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion2()
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

            Regions(code,
                Region("span", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WorkItem(1070677)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion3()
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

            Regions(code,
                Region("span", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WorkItem(1070677)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void NestedDisabledCodePreProcessorDirectivesShouldCollapseEntireDisabledRegion4()
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

            Regions(code,
                Region("span", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }

        [WorkItem(1100600)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)]
        public void PreprocessorDirectivesInTrailingTrivia()
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

            Regions(code,
                Region("span", CSharpOutliningHelpers.Ellipsis, autoCollapse: true));
        }
    }
}
