// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public abstract class FixedSizeValueTypeKeywordRecommenderTests : SpecialTypeKeywordRecommenderTests
    {
        [Fact]
        public async Task TestAfterStackAlloc()
        {
            await VerifyKeywordAsync("""
                class C {
                     int* goo = stackalloc $$
                """);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInFixedStatement(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                "fixed ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, WorkItem(538804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        [CombinatorialData]
        public async Task TestInSizeOf(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                "sizeof($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }
    }
}
