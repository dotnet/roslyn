// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Completion;

public class CompletionListMergerTest : ToolingTestBase
{
    private readonly VSInternalCompletionItem _completionItem1;
    private readonly VSInternalCompletionItem _completionItem2;
    private readonly VSInternalCompletionItem _completionItem3;
    private readonly RazorVSInternalCompletionList _completionListWith1;
    private readonly RazorVSInternalCompletionList _completionListWith2;
    private readonly RazorVSInternalCompletionList _completionListWith13;

    public CompletionListMergerTest(ITestOutputHelper testOutput)
        : base(testOutput)
    {
        _completionItem1 = new VSInternalCompletionItem()
        {
            Label = "CompletionItem1"
        };

        _completionItem2 = new VSInternalCompletionItem()
        {
            Label = "CompletionItem2"
        };

        _completionItem3 = new VSInternalCompletionItem()
        {
            Label = "CompletionItem3"
        };

        _completionListWith1 = new RazorVSInternalCompletionList()
        {
            Items = [_completionItem1]
        };

        _completionListWith2 = new RazorVSInternalCompletionList()
        {
            Items = [_completionItem2]
        };

        _completionListWith13 = new RazorVSInternalCompletionList()
        {
            Items = [_completionItem1, _completionItem3]
        };
    }

    [Fact]
    public void Merge_FirstCompletionListNull_ReturnsSecond()
    {
        // Arrange

        // Act
        var merged = CompletionListMerger.Merge(razorCompletionList: null, _completionListWith1);

        // Assert
        Assert.Same(merged, _completionListWith1);
    }

    [Fact]
    public void Merge_SecondCompletionListNull_ReturnsFirst()
    {
        // Arrange

        // Act
        var merged = CompletionListMerger.Merge(_completionListWith1, delegatedCompletionList: null);

        // Assert
        Assert.Same(merged, _completionListWith1);
    }

    [Fact]
    public void Merge_RepresentsAllItems()
    {
        // Arrange
        var expected = new[] { _completionItem1, _completionItem2 };

        // Act
        var merged = CompletionListMerger.Merge(_completionListWith1, _completionListWith2);

        // Assert
        Assert.NotNull(merged);
        AssertCompletionItemsEqual(expected, merged.Items);
    }

    [Fact]
    public void Merge_RepresentsIsIncompleteOfBothLists()
    {
        // Arrange
        _completionListWith1.IsIncomplete = false;
        _completionListWith2.IsIncomplete = true;

        // Act
        var merged = CompletionListMerger.Merge(_completionListWith1, _completionListWith2);

        // Assert
        Assert.NotNull(merged);
        Assert.True(merged.IsIncomplete);
    }

    [Fact]
    public void Merge_RepresentsSuggestionModeOfBothLists()
    {
        // Arrange
        _completionListWith1.SuggestionMode = false;
        _completionListWith2.SuggestionMode = true;

        // Act
        var merged = CompletionListMerger.Merge(_completionListWith1, _completionListWith2);

        // Assert
        Assert.NotNull(merged);
        Assert.True(merged.SuggestionMode);
    }

    [Fact]
    public void Merge_CommitCharacters_OneInherits()
    {
        // Arrange
        var expectedCommitCharacters = new string[] { " " };
        _completionListWith1.CommitCharacters = expectedCommitCharacters;

        // Act
        var merged = CompletionListMerger.Merge(_completionListWith1, _completionListWith2);

        // Assert — merger dematerializes both lists, so merged list has no list-level CommitCharacters.
        // Items that inherited from the list get per-item chars instead.
        Assert.NotNull(merged);
        Assert.Null(merged.CommitCharacters);
        Assert.Equal(expectedCommitCharacters, _completionItem1.VsCommitCharacters);
    }

    [Fact]
    public void Merge_CommitCharacters_BothInherit_ChoosesMoreImpactfulList()
    {
        // Arrange
        var lesserCommitCharacters = new string[] { " " };
        _completionListWith2.CommitCharacters = lesserCommitCharacters;
        var expectedCommitCharacters = new string[] { ".", ">" };
        _completionListWith13.CommitCharacters = expectedCommitCharacters;

        // Act
        var merged = CompletionListMerger.Merge(_completionListWith2, _completionListWith13);

        // Assert — merger dematerializes both lists, so merged list has no list-level CommitCharacters.
        // Each item gets its own list's chars pushed to per-item.
        Assert.NotNull(merged);
        Assert.Null(merged.CommitCharacters);

        // Items from list2 got their list's chars
        Assert.Equal(lesserCommitCharacters, _completionItem2.VsCommitCharacters);
        // Items from list13 got their list's chars
        Assert.Equal(expectedCommitCharacters, _completionItem1.VsCommitCharacters);
        Assert.Equal(expectedCommitCharacters, _completionItem3.VsCommitCharacters);
    }

    [Fact]
    public void Merge_Data_OneInherits()
    {
        // Arrange
        var expectedData = new object();
        _completionListWith1.Data = expectedData;

        // Act
        var merged = CompletionListMerger.Merge(_completionListWith1, _completionListWith2);

        // Assert
        Assert.NotNull(merged);
        Assert.Same(expectedData, merged.Data);
        Assert.NotNull(_completionItem2.Data);
        Assert.NotSame(expectedData, _completionItem2.Data);
    }

    [Fact]
    public void Merge_Data_BothInherit_ChoosesMoreImpactfulList()
    {
        // Arrange
        var data1 = new object();
        _completionListWith1.Data = data1;
        var data2 = new object();
        _completionListWith2.Data = data2;

        // Act
        var merged = CompletionListMerger.Merge(_completionListWith1, _completionListWith2);

        // Assert
        Assert.NotNull(merged);
        Assert.NotSame(data1, merged.Data);
        Assert.NotSame(data2, merged.Data);
    }

    private static void AssertCompletionItemsEqual(VSInternalCompletionItem[] expected, CompletionItem[] actual)
    {
        var sortedExpected = expected.OrderByAsArray(item => item.Label);
        var sortedActual = actual.OrderByAsArray(item => item.Label);

        Assert.Equal(sortedExpected.Length, sortedActual.Length);

        for (var i = 0; i < sortedExpected.Length; i++)
        {
            Assert.Same(sortedExpected[i], sortedActual[i]);
        }
    }

    #region EnsureMergeableEditRange

    [Fact]
    public void Merge_EditRange_NeitherListHasEditRange_NoChange()
    {
        // Arrange — neither list has EditRange
        var listA = new RazorVSInternalCompletionList
        {
            Items = [new VSInternalCompletionItem { Label = "item1", TextEdit = new TextEdit { Range = new LspRange { Start = new Position(0, 0), End = new Position(0, 5) }, NewText = "item1" } }]
        };
        var listB = new RazorVSInternalCompletionList
        {
            Items = [new VSInternalCompletionItem { Label = "item2", TextEdit = new TextEdit { Range = new LspRange { Start = new Position(0, 0), End = new Position(0, 5) }, NewText = "item2" } }]
        };

        // Act
        var merged = CompletionListMerger.Merge(listA, listB);

        // Assert — items retain their TextEdits unchanged
        Assert.NotNull(merged);
        Assert.NotNull(merged.Items[0].TextEdit);
        Assert.NotNull(merged.Items[1].TextEdit);
        Assert.Null(merged.ItemDefaults?.EditRange);
    }

    [Fact]
    public void Merge_EditRange_OnlyOneListHasEditRange_Preserved()
    {
        // Arrange — only listA has an EditRange
        var editRange = new LspRange { Start = new Position(1, 0), End = new Position(1, 10) };
        var listA = new RazorVSInternalCompletionList
        {
            ItemDefaults = new CompletionListItemDefaults { EditRange = editRange },
            Items = [new VSInternalCompletionItem { Label = "item1", TextEditText = "item1" }]
        };
        var listB = new RazorVSInternalCompletionList
        {
            Items = [new VSInternalCompletionItem { Label = "item2", TextEdit = new TextEdit { Range = new LspRange { Start = new Position(2, 0), End = new Position(2, 5) }, NewText = "item2" } }]
        };

        // Act
        var merged = CompletionListMerger.Merge(listA, listB);

        // Assert — listA's item still uses TextEditText (EditRange not dematerialized)
        Assert.NotNull(merged);
        var item1 = Assert.Single(merged.Items, i => i.Label == "item1");
        Assert.Equal("item1", ((VSInternalCompletionItem)item1).TextEditText);
        Assert.Null(item1.TextEdit);
    }

    [Fact]
    public void Merge_EditRange_BothListsSameRange_Preserved()
    {
        // Arrange — both lists have the same EditRange
        var editRange = new LspRange { Start = new Position(3, 5), End = new Position(3, 15) };
        var listA = new RazorVSInternalCompletionList
        {
            ItemDefaults = new CompletionListItemDefaults { EditRange = editRange },
            Items = [new VSInternalCompletionItem { Label = "item1", TextEditText = "item1" }]
        };
        var listB = new RazorVSInternalCompletionList
        {
            ItemDefaults = new CompletionListItemDefaults { EditRange = editRange },
            Items = [new VSInternalCompletionItem { Label = "item2", TextEditText = "item2" }]
        };

        // Act
        var merged = CompletionListMerger.Merge(listA, listB);

        // Assert — both items still use TextEditText (EditRange preserved, not dematerialized)
        Assert.NotNull(merged);
        var item1 = Assert.Single(merged.Items, i => i.Label == "item1");
        var item2 = Assert.Single(merged.Items, i => i.Label == "item2");
        Assert.Equal("item1", ((VSInternalCompletionItem)item1).TextEditText);
        Assert.Equal("item2", ((VSInternalCompletionItem)item2).TextEditText);
        Assert.Null(item1.TextEdit);
        Assert.Null(item2.TextEdit);
    }

    [Fact]
    public void Merge_EditRange_DifferentRanges_DematerializesBoth()
    {
        // Arrange — lists have different EditRanges
        var editRangeA = new LspRange { Start = new Position(1, 0), End = new Position(1, 10) };
        var editRangeB = new LspRange { Start = new Position(1, 0), End = new Position(1, 15) };
        var listA = new RazorVSInternalCompletionList
        {
            ItemDefaults = new CompletionListItemDefaults { EditRange = editRangeA },
            Items = [new VSInternalCompletionItem { Label = "item1", TextEditText = "replaced1" }]
        };
        var listB = new RazorVSInternalCompletionList
        {
            ItemDefaults = new CompletionListItemDefaults { EditRange = editRangeB },
            Items = [new VSInternalCompletionItem { Label = "item2", TextEditText = "replaced2" }]
        };

        // Act
        var merged = CompletionListMerger.Merge(listA, listB);

        // Assert — both items now have per-item TextEdits, EditRange cleared
        Assert.NotNull(merged);
        var item1 = Assert.Single(merged.Items, i => i.Label == "item1");
        var item2 = Assert.Single(merged.Items, i => i.Label == "item2");

        Assert.NotNull(item1.TextEdit);
        var textEdit1 = (TextEdit)item1.TextEdit.Value;
        Assert.Equal("replaced1", textEdit1.NewText);
        Assert.Equal(editRangeA, textEdit1.Range);
        Assert.Null(((VSInternalCompletionItem)item1).TextEditText);

        Assert.NotNull(item2.TextEdit);
        var textEdit2 = (TextEdit)item2.TextEdit.Value;
        Assert.Equal("replaced2", textEdit2.NewText);
        Assert.Equal(editRangeB, textEdit2.Range);
        Assert.Null(((VSInternalCompletionItem)item2).TextEditText);
    }

    [Fact]
    public void Merge_EditRange_DifferentRanges_UsesLabelWhenNoTextEditText()
    {
        // Arrange — item has no TextEditText, should fall back to Label
        var editRangeA = new LspRange { Start = new Position(0, 0), End = new Position(0, 5) };
        var editRangeB = new LspRange { Start = new Position(0, 0), End = new Position(0, 8) };
        var listA = new RazorVSInternalCompletionList
        {
            ItemDefaults = new CompletionListItemDefaults { EditRange = editRangeA },
            Items = [new VSInternalCompletionItem { Label = "myLabel" }] // no TextEditText
        };
        var listB = new RazorVSInternalCompletionList
        {
            ItemDefaults = new CompletionListItemDefaults { EditRange = editRangeB },
            Items = [new VSInternalCompletionItem { Label = "other", TextEditText = "otherText" }]
        };

        // Act
        var merged = CompletionListMerger.Merge(listA, listB);

        // Assert — item1 uses Label as NewText fallback
        Assert.NotNull(merged);
        var item1 = Assert.Single(merged.Items, i => i.Label == "myLabel");
        Assert.NotNull(item1.TextEdit);
        var textEdit1 = (TextEdit)item1.TextEdit.Value;
        Assert.Equal("myLabel", textEdit1.NewText);
        Assert.Equal(editRangeA, textEdit1.Range);
    }

    [Fact]
    public void Merge_EditRange_DifferentRanges_SkipsItemsWithExistingTextEdit()
    {
        // Arrange — item already has a TextEdit; dematerialization should skip it
        var editRangeA = new LspRange { Start = new Position(0, 0), End = new Position(0, 5) };
        var editRangeB = new LspRange { Start = new Position(0, 0), End = new Position(0, 8) };
        var existingTextEdit = new TextEdit { Range = new LspRange { Start = new Position(0, 0), End = new Position(0, 3) }, NewText = "custom" };
        var listA = new RazorVSInternalCompletionList
        {
            ItemDefaults = new CompletionListItemDefaults { EditRange = editRangeA },
            Items = [new VSInternalCompletionItem { Label = "item1", TextEdit = existingTextEdit }]
        };
        var listB = new RazorVSInternalCompletionList
        {
            ItemDefaults = new CompletionListItemDefaults { EditRange = editRangeB },
            Items = [new VSInternalCompletionItem { Label = "item2", TextEditText = "item2" }]
        };

        // Act
        var merged = CompletionListMerger.Merge(listA, listB);

        // Assert — item1 keeps its original TextEdit unchanged
        Assert.NotNull(merged);
        var item1 = Assert.Single(merged.Items, i => i.Label == "item1");
        Assert.NotNull(item1.TextEdit);
        Assert.Same(existingTextEdit, (TextEdit)item1.TextEdit.Value);
    }

    #endregion
}
