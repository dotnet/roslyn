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
            assertEqualsAndHashCode(default(NullabilityInfo), default(NullabilityInfo), equal: true);
            assertEqualsAndHashCode(new NullabilityInfo(NullableAnnotation.Annotated, NullableFlowState.NotNull),
                                    new NullabilityInfo(NullableAnnotation.Annotated, NullableFlowState.NotNull),
                                    equal: true);

#pragma warning disable IDE0055 // Fix formatting: spacing is intentional to allow for visual field comparison
            assertEqualsAndHashCode(new NullabilityInfo(NullableAnnotation.Annotated,    NullableFlowState.NotNull),
                                    new NullabilityInfo(NullableAnnotation.NotAnnotated, NullableFlowState.NotNull),
                                    equal: false);

            assertEqualsAndHashCode(new NullabilityInfo(NullableAnnotation.Annotated, NullableFlowState.MaybeNull),
                                    new NullabilityInfo(NullableAnnotation.Annotated, NullableFlowState.NotNull),
                                    equal: false);
#pragma warning restore IDE0055 // Fix formatting

            void assertEqualsAndHashCode(NullabilityInfo n1, NullabilityInfo n2, bool equal)
            {
                if (equal)
                {
                    Assert.Equal(n1, n2);
                    Assert.Equal(n1.GetHashCode(), n2.GetHashCode());
                }
                else
                {
                    Assert.NotEqual(n1, n2);
                    Assert.NotEqual(n1.GetHashCode(), n2.GetHashCode());
                }
            }
        }
    }
}
