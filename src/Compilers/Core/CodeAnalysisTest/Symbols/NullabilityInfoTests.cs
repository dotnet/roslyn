// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis
{
    [CompilerTrait(CompilerFeature.NullableReferenceTypes)]
    public class NullabilityInfoTests
    {
        [Fact]
        public void Equality()
        {
            assertEqualsAndHashCode(default(NullabilityInfo), default(NullabilityInfo), equal: true);
            assertEqualsAndHashCode(new NullabilityInfo(NullableAnnotation.Annotated, NullableFlowState.NotNull),
                                    new NullabilityInfo(NullableAnnotation.Annotated, NullableFlowState.NotNull),
                                    equal: true);

            assertEqualsAndHashCode(new NullabilityInfo(NullableAnnotation.Annotated, NullableFlowState.NotNull),
                                    new NullabilityInfo(NullableAnnotation.NotAnnotated, NullableFlowState.NotNull),
                                    equal: false);

            assertEqualsAndHashCode(new NullabilityInfo(NullableAnnotation.Annotated, NullableFlowState.MaybeNull),
                                    new NullabilityInfo(NullableAnnotation.Annotated, NullableFlowState.NotNull),
                                    equal: false);

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
