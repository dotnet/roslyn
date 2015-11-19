// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeSignature
{
    public partial class ChangeSignatureTests : AbstractChangeSignatureTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void ReorderMethodParameters_InvokeOnClassName_ShouldFail()
        {
            var markup = @"
using System;
class MyClass$$
{
    public void Foo(int x, string y)
    {
    }
}";

            TestChangeSignatureViaCommand(LanguageNames.CSharp, markup, expectedSuccess: false, expectedErrorText: FeaturesResources.YouCanOnlyChangeTheSignatureOfAConstructorIndexerMethodOrDelegate);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void ReorderMethodParameters_InvokeOnField_ShouldFail()
        {
            var markup = @"
using System;
class MyClass
{
    int t$$ = 2;

    public void Foo(int x, string y)
    {
    }
}";

            TestChangeSignatureViaCommand(LanguageNames.CSharp, markup, expectedSuccess: false, expectedErrorText: FeaturesResources.YouCanOnlyChangeTheSignatureOfAConstructorIndexerMethodOrDelegate);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void ReorderMethodParameters_InsufficientParameters_None()
        {
            var markup = @"class C { void $$M() { } }";
            TestChangeSignatureViaCommand(LanguageNames.CSharp, markup, expectedSuccess: false, expectedErrorText: FeaturesResources.ThisSignatureDoesNotContainParametersThatCanBeChanged);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.ChangeSignature)]
        public void ReorderMethodParameters_InvokeOnOverloadedOperator_ShouldFail()
        {
            var markup = @"
class C
{
    public static C $$operator +(C a, C b)
    {
        return null;
    }
}";

            TestChangeSignatureViaCommand(LanguageNames.CSharp, markup, expectedSuccess: false, expectedErrorText: FeaturesResources.YouCanOnlyChangeTheSignatureOfAConstructorIndexerMethodOrDelegate);
        }
    }
}
