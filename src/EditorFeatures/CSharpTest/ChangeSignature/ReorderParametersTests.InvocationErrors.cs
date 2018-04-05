// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeSignature
{
    public partial class ChangeSignatureTests : AbstractChangeSignatureTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_InvokeOnClassName_ShouldFail()
        {
            var markup = @"
using System;
class MyClass$$
{
    public void Goo(int x, string y)
    {
    }
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, expectedSuccess: false, expectedErrorText: FeaturesResources.You_can_only_change_the_signature_of_a_constructor_indexer_method_or_delegate);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_InvokeOnField_ShouldFail()
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

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, expectedSuccess: false, expectedErrorText: FeaturesResources.You_can_only_change_the_signature_of_a_constructor_indexer_method_or_delegate);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_InsufficientParameters_None()
        {
            var markup = @"class C { void $$M() { } }";
            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, expectedSuccess: false, expectedErrorText: FeaturesResources.This_signature_does_not_contain_parameters_that_can_be_changed);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public async Task ReorderMethodParameters_InvokeOnOverloadedOperator_ShouldFail()
        {
            var markup = @"
class C
{
    public static C $$operator +(C a, C b)
    {
        return null;
    }
}";

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, expectedSuccess: false, expectedErrorText: FeaturesResources.You_can_only_change_the_signature_of_a_constructor_indexer_method_or_delegate);
        }
    }
}
