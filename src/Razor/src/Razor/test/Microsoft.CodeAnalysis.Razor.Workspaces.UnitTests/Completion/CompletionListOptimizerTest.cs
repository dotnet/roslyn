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
            Items =
            [
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
                CommitCharacters = true,
            }
        };

        // Act
        var vsCompletionList = CompletionListOptimizer.Optimize(completionList, capabilities);

        // Assert
        var item = Assert.Single(vsCompletionList.Items);
        Assert.Null(item.CommitCharacters);

        Assert.NotNull(vsCompletionList.CommitCharacters);
        var promotedChar = Assert.Single(vsCompletionList.CommitCharacters.Value.First);
        Assert.Equal("<", promotedChar);
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
    public void PromoteEditRange_TextEditTextEqualsLabel_OmitsTextEditText()
    {
        // Arrange — when NewText equals Label, TextEditText should be omitted
        // because the client falls back to Label automatically.
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
                    Label = "span",
                    TextEdit = new TextEdit { Range = sharedRange, NewText = "span" }
                },
                new VSInternalCompletionItem()
                {
                    Label = "div",
                    TextEdit = new TextEdit { Range = sharedRange, NewText = "different-text" }
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

        // Assert — "span" item has TextEditText omitted (equals Label), "div" item retains it
        Assert.Null(result.Items[0].TextEditText);
        Assert.Equal("different-text", result.Items[1].TextEditText);
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

    [Fact]
    public void PromoteCommitCharacters_ItemDefaultsPath_PromotesToItemDefaultsCommitCharacters()
    {
        // Arrange — standard LSP client that supports commitCharacters in ItemDefaults
        var commitChars = new[] { "=", " " };
        var completionList = new RazorVSInternalCompletionList()
        {
            Items = [
                new VSInternalCompletionItem()
                {
                    Label = "Item1",
                    VsCommitCharacters = commitChars
                },
                new VSInternalCompletionItem()
                {
                    Label = "Item2",
                    VsCommitCharacters = commitChars
                },
                new VSInternalCompletionItem()
                {
                    Label = "Item3",
                    VsCommitCharacters = new[] { ">" }
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

        // Assert — the most common group (["=", " "]) gets promoted to ItemDefaults.CommitCharacters
        Assert.NotNull(result.ItemDefaults);
        Assert.NotNull(result.ItemDefaults.CommitCharacters);
        Assert.Equal(commitChars, result.ItemDefaults.CommitCharacters);

        // Items that matched the promoted set have their per-item chars cleared
        Assert.Null(((VSInternalCompletionItem)result.Items[0]).VsCommitCharacters);
        Assert.Null(((VSInternalCompletionItem)result.Items[1]).VsCommitCharacters);

        // Item with different chars retains them
        var item2VsChars = ((VSInternalCompletionItem)result.Items[2]).VsCommitCharacters;
        Assert.NotNull(item2VsChars);
        Assert.Equal([">"], (string[])item2VsChars.Value.Value!);
    }

    [Fact]
    public void PromoteCommitCharacters_ItemDefaultsPath_FiltersInsertFalseCharacters()
    {
        // Arrange — VSInternalCommitCharacter[] with Insert=false should be filtered
        // when promoting to standard LSP ItemDefaults (which only supports string[])
        var vsChars = new VSInternalCommitCharacter[]
        {
            new() { Character = "=", Insert = false },
            new() { Character = " ", Insert = true }
        };
        var completionList = new RazorVSInternalCompletionList()
        {
            Items = [
                new VSInternalCompletionItem()
                {
                    Label = "Item1",
                    VsCommitCharacters = vsChars
                },
                new VSInternalCompletionItem()
                {
                    Label = "Item2",
                    VsCommitCharacters = vsChars
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

        // Assert — only characters with Insert != false are promoted
        Assert.NotNull(result.ItemDefaults);
        Assert.NotNull(result.ItemDefaults.CommitCharacters);
        var promoted = Assert.Single(result.ItemDefaults.CommitCharacters);
        Assert.Equal(" ", promoted);
    }

    [Fact]
    public void PromoteCommitCharacters_ItemDefaultsPath_ItemsWithNoCommitChars_GetExplicitEmpty()
    {
        // Arrange — items without commit chars should get explicit empty so they don't inherit
        var commitChars = new[] { "<" };
        var completionList = new RazorVSInternalCompletionList()
        {
            Items = [
                new VSInternalCompletionItem()
                {
                    Label = "ItemWithChars",
                    VsCommitCharacters = commitChars
                },
                new VSInternalCompletionItem()
                {
                    Label = "ItemWithoutChars"
                    // No CommitCharacters or VsCommitCharacters
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

        // Assert — item without chars gets explicit empty to opt out of inheritance
        Assert.NotNull(result.ItemDefaults);
        var itemWithout = (VSInternalCompletionItem)result.Items[1];
        Assert.NotNull(itemWithout.CommitCharacters);
        Assert.Empty(itemWithout.CommitCharacters);
    }
}
