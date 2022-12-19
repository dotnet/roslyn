// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public abstract class WholeNumberTypeKeywordRecommenderTests : FixedSizeValueTypeKeywordRecommenderTests
    {
        [Fact]
        public async Task TestEnumBaseType()
        {
            await VerifyKeywordAsync("enum E : $$");
        }
    }
}
