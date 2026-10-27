// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Microsoft.CodeAnalysis.Testing
{
    public class ResourceLocationTests
    {
        [Fact]
        [WorkItem(327, "https://github.com/dotnet/roslyn-sdk/issues/327")]
        public void TestDgmlHelperResources()
        {
            Assert.NotNull(Roslyn.SyntaxVisualizer.DgmlHelper.My.Resources.Resources.SyntaxNodeLabel);
        }
    }
}
