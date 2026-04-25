// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Completion;

public class CompletionListOptimizerTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [Fact]
    public void Convert_CommitCharactersTrue_RemovesCommitCharactersFromItems()
    {
        // Arrange
        var commitCharacters = new[] { "<" };
        var completionList = new RazorVSInternalCompletionList()
        {
            Items = new[]
            {
                new VSInternalCompletionItem()
                {
                    Label = "Test",
                    VsCommitCharacters = commitCharacters
                }
            }
        };
        var capabilities = new VSInternalCompletionSetting()
        {
            CompletionList = new VSInternalCompletionListSetting()
            {
                CommitCharacters = true,
            }
        };

        // Act
        var vsCompletionList = CompletionListOptimizer.Optimize(completionList, capabilities);

        // Assert
        var item = Assert.Single(vsCompletionList.Items);
        Assert.Null(item.CommitCharacters);

        Assert.NotNull(vsCompletionList.CommitCharacters);
        var commitCharacter = Assert.Single(vsCompletionList.CommitCharacters.Value.First);
        Assert.Equal("<", commitCharacter);
    }

    [Fact]
    public void Convert_CommitCharactersFalse_DoesNotTouchCommitCharacters()
    {
        // Arrange
        var commitCharacters = new[] { "<" };
        var completionList = new RazorVSInternalCompletionList()
        {
            Items = [
                new VSInternalCompletionItem()
                {
                    Label = "Test",
                    VsCommitCharacters = commitCharacters
                }
            ]
        };
        var capabilities = new VSInternalCompletionSetting()
        {
            CompletionList = new VSInternalCompletionListSetting()
            {
                CommitCharacters = false,
            }
        };

        // Act
        var vsCompletionList = CompletionListOptimizer.Optimize(completionList, capabilities);

        // Assert
        var item = Assert.Single(vsCompletionList.Items);
        Assert.Equal(commitCharacters, ((VSInternalCompletionItem)item).VsCommitCharacters);
        Assert.Null(vsCompletionList.CommitCharacters);
    }
}
