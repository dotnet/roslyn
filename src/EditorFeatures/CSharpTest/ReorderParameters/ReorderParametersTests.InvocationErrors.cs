// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ReorderParameters
{
    public partial class ReorderParametersTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParameters_InvokeOnClassName_ShouldFail()
        {
            var markup = @"
using System;
class MyClass$$
{
    public void Goo(int x, string y)
    {
    }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, expectedSuccess: false, expectedErrorText: FeaturesResources.You_can_only_change_the_signature_of_a_constructor_indexer_method_or_delegate);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParameters_InvokeOnField_ShouldFail()
        {
            var markup = @"
using System;
class MyClass
{
    int t$$ = 2;

    public void Goo(int x, string y)
    {
    }
}";

            TestReorderParameters(LanguageNames.CSharp, markup, expectedSuccess: false, expectedErrorText: FeaturesResources.You_can_only_change_the_signature_of_a_constructor_indexer_method_or_delegate);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParameters_InsufficientParameters_None()
        {
            var markup = @"class C { void $$M() { } }";
            TestReorderParameters(LanguageNames.CSharp, markup, expectedSuccess: false, expectedErrorText: FeaturesResources.InsufficientParametersToReorder);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParameters_InsufficientParameters_OneRegular()
        {
            var markup = @"class C { void $$M(int x) { } }";
            TestReorderParameters(LanguageNames.CSharp, markup, expectedSuccess: false, expectedErrorText: FeaturesResources.InsufficientParametersToReorder);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParameters_InsufficientParameters_OneDefault()
        {
            var markup = @"class C { void $$M(int x = 7) { } }";
            TestReorderParameters(LanguageNames.CSharp, markup, expectedSuccess: false, expectedErrorText: FeaturesResources.InsufficientParametersToReorder);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParameters_InsufficientParameters_OneRegularOneDefault()
        {
            var markup = @"class C { void $$M(int x, int y = 7) { } }";
            TestReorderParameters(LanguageNames.CSharp, markup, expectedSuccess: false, expectedErrorText: FeaturesResources.InsufficientParametersToReorder);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParameters_InsufficientParameters_OneRegularOneDefaultOneParams()
        {
            var markup = @"class C { void $$M(int x, int y = 7, params int[] z) { } }";
            TestReorderParameters(LanguageNames.CSharp, markup, expectedSuccess: false, expectedErrorText: FeaturesResources.InsufficientParametersToReorder);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ReorderParameters)]
        public void ReorderMethodParameters_InsufficientParameters_OneThisOneRegularOneDefaultOneParams()
        {
            var markup = @"
static class C
{
    static void $$M(this object o, int x, int y = 7, params int[] z)
    {
    }
}";
            TestReorderParameters(LanguageNames.CSharp, markup, expectedSuccess: false, expectedErrorText: FeaturesResources.InsufficientParametersToReorder);
        }
    }
}
