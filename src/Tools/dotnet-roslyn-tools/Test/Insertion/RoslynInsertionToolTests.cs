// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using Microsoft.RoslynTools.Insertion;

namespace Microsoft.RoslynTools.UnitTests.Insertion;

public class RoslynInsertionToolTests
{
    [Fact]
    public void AzDO_UrlConstructionShouldBeValidCompareLink()
    {
        // Arrange

        // URLs and commits are generated for testing purposes only.
        string repoUrl = "https://someserver.example.com/myorg/myproject/_git/myrepo";
        string beforeCommit = "159abc3527def456cba852fed321654987654321";
        string afterCommit = "fed456cba123987852147369abc258def4567893";
        string expected = "https://someserver.example.com/myorg/myproject/_git/myrepo/branchCompare?baseVersion=GC159abc3527def456cba852fed321654987654321&targetVersion=GCfed456cba123987852147369abc258def4567893";

        // Act
        string actual = RoslynInsertionTool.ConstructAzDOCompareUrl(repoUrl, beforeCommit, afterCommit);

        // Assert
        Assert.Equal(expected, actual);
    }
}
