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

    [Fact]
    public void PromoteEditRange_AllItemsShareSameRange_PromotesToItemDefaults()
    {
        // Arrange
        var sharedRange = new LspRange
        {
            Start = new Position { Line = 0, Character = 5 },
            End = new Position { Line = 0, Character = 15 }
        };
        var completionList = new RazorVSInternalCompletionList()
        {
            Items = [
                new VSInternalCompletionItem()
                {
                    Label = "Item1",
                    TextEdit = new TextEdit { Range = sharedRange, NewText = "NewItem1" }
                },
                new VSInternalCompletionItem()
                {
                    Label = "Item2",
                    TextEdit = new TextEdit { Range = sharedRange, NewText = "NewItem2" }
                }
            ]
        };
        var capabilities = new VSInternalCompletionSetting()
        {
            CompletionListSetting = new CompletionListSetting()
            {
                ItemDefaults = ["editRange"]
            }
        };

        // Act
        var result = CompletionListOptimizer.Optimize(completionList, capabilities);

        // Assert
        Assert.NotNull(result.ItemDefaults);
        Assert.True(result.ItemDefaults.EditRange!.Value.TryGetFirst(out var promotedRange));
        Assert.Equal(sharedRange, promotedRange);

        Assert.All(result.Items, item =>
        {
            Assert.Null(item.TextEdit);
            Assert.NotNull(item.TextEditText);
        });
        Assert.Equal("NewItem1", result.Items[0].TextEditText);
        Assert.Equal("NewItem2", result.Items[1].TextEditText);
    }

    [Fact]
    public void PromoteEditRange_ItemsHaveDifferentRanges_DoesNotPromote()
    {
        // Arrange
        var range1 = new LspRange
        {
            Start = new Position { Line = 0, Character = 5 },
            End = new Position { Line = 0, Character = 10 }
        };
        var range2 = new LspRange
        {
            Start = new Position { Line = 0, Character = 5 },
            End = new Position { Line = 0, Character = 15 }
        };
        var completionList = new RazorVSInternalCompletionList()
        {
            Items = [
                new VSInternalCompletionItem()
                {
                    Label = "Item1",
                    TextEdit = new TextEdit { Range = range1, NewText = "NewItem1" }
                },
                new VSInternalCompletionItem()
                {
                    Label = "Item2",
                    TextEdit = new TextEdit { Range = range2, NewText = "NewItem2" }
                }
            ]
        };
        var capabilities = new VSInternalCompletionSetting()
        {
            CompletionListSetting = new CompletionListSetting()
            {
                ItemDefaults = ["editRange"]
            }
        };

        // Act
        var result = CompletionListOptimizer.Optimize(completionList, capabilities);

        // Assert
        Assert.Null(result.ItemDefaults);
        Assert.All(result.Items, item => Assert.NotNull(item.TextEdit));
    }

    [Fact]
    public void PromoteEditRange_SomeItemsLackTextEdit_DoesNotPromote()
    {
        // Arrange
        var sharedRange = new LspRange
        {
            Start = new Position { Line = 0, Character = 5 },
            End = new Position { Line = 0, Character = 15 }
        };
        var completionList = new RazorVSInternalCompletionList()
        {
            Items = [
                new VSInternalCompletionItem()
                {
                    Label = "Item1",
                    TextEdit = new TextEdit { Range = sharedRange, NewText = "NewItem1" }
                },
                new VSInternalCompletionItem()
                {
                    Label = "Item2"
                }
            ]
        };
        var capabilities = new VSInternalCompletionSetting()
        {
            CompletionListSetting = new CompletionListSetting()
            {
                ItemDefaults = ["editRange"]
            }
        };

        // Act
        var result = CompletionListOptimizer.Optimize(completionList, capabilities);

        // Assert
        Assert.Null(result.ItemDefaults);
    }

    [Fact]
    public void PromoteEditRange_ClientDoesNotSupportEditRange_DoesNotPromote()
    {
        // Arrange
        var sharedRange = new LspRange
        {
            Start = new Position { Line = 0, Character = 5 },
            End = new Position { Line = 0, Character = 15 }
        };
        var completionList = new RazorVSInternalCompletionList()
        {
            Items = [
                new VSInternalCompletionItem()
                {
                    Label = "Item1",
                    TextEdit = new TextEdit { Range = sharedRange, NewText = "NewItem1" }
                }
            ]
        };
        var capabilities = new VSInternalCompletionSetting()
        {
            CompletionListSetting = new CompletionListSetting()
            {
                ItemDefaults = ["commitCharacters"]
            }
        };

        // Act
        var result = CompletionListOptimizer.Optimize(completionList, capabilities);

        // Assert
        Assert.Null(result.ItemDefaults);
        Assert.NotNull(result.Items[0].TextEdit);
    }
}
