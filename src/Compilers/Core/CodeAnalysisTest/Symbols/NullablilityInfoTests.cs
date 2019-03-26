// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis
{
    [CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    public class NullablilityInfoTests
    {
        [Fact]
        public void Equality()
        {
            Assert.Equal(default(NullabilityInfo), default(NullabilityInfo));
            Assert.Equal(new NullabilityInfo(NullableAnnotation.Annotated, NullableFlowState.NotNull), new NullabilityInfo(NullableAnnotation.Annotated, NullableFlowState.NotNull));

#pragma warning disable IDE0055 // Fix formatting: spacing is intentional to allow for visual field comparison
            Assert.NotEqual(new NullabilityInfo(NullableAnnotation.Annotated,    NullableFlowState.NotNull),
                            new NullabilityInfo(NullableAnnotation.NotAnnotated, NullableFlowState.NotNull));

            Assert.NotEqual(new NullabilityInfo(NullableAnnotation.Annotated, NullableFlowState.MaybeNull),
                            new NullabilityInfo(NullableAnnotation.Annotated, NullableFlowState.NotNull));
#pragma warning restore IDE0055 // Fix formatting
        }
    }
}
