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

    [Fact]
    public void Merge_EditRange_NeitherListHasEditRange_NoChange()
    {
        // Arrange — neither list has EditRange
        var item1 = new VSInternalCompletionItem { Label = "a", TextEdit = new TextEdit { Range = new LspRange { Start = new(0, 0), End = new(0, 1) }, NewText = "a" } };
        var item2 = new VSInternalCompletionItem { Label = "b", TextEdit = new TextEdit { Range = new LspRange { Start = new(0, 0), End = new(0, 1) }, NewText = "b" } };
        var list1 = new RazorVSInternalCompletionList { Items = [item1] };
        var list2 = new RazorVSInternalCompletionList { Items = [item2] };

        // Act
        var merged = CompletionListMerger.Merge(list1, list2);

        // Assert — items keep their TextEdits
        Assert.NotNull(merged);
        Assert.Null(merged.ItemDefaults?.EditRange);
        Assert.NotNull(item1.TextEdit);
        Assert.NotNull(item2.TextEdit);
    }

    [Fact]
    public void Merge_EditRange_OnlyOneListHasEditRange_Preserved()
    {
        // Arrange — only list1 has EditRange
        var range = new LspRange { Start = new(0, 5), End = new(0, 10) };
        var item1 = new VSInternalCompletionItem { Label = "a", TextEditText = "alpha" };
        var item2 = new VSInternalCompletionItem { Label = "b", TextEdit = new TextEdit { Range = new LspRange { Start = new(1, 0), End = new(1, 3) }, NewText = "beta" } };
        var list1 = new RazorVSInternalCompletionList { Items = [item1], ItemDefaults = new CompletionListItemDefaults { EditRange = range } };
        var list2 = new RazorVSInternalCompletionList { Items = [item2] };

        // Act
        var merged = CompletionListMerger.Merge(list1, list2);

        // Assert — EditRange preserved from list1
        Assert.NotNull(merged);
        Assert.Equal(range, merged.ItemDefaults?.EditRange?.Value);
    }

    [Fact]
    public void Merge_EditRange_BothListsSameRange_Preserved()
    {
        // Arrange — both lists share the same EditRange
        var range = new LspRange { Start = new(0, 5), End = new(0, 10) };
        var item1 = new VSInternalCompletionItem { Label = "a", TextEditText = "alpha" };
        var item2 = new VSInternalCompletionItem { Label = "b", TextEditText = "beta" };
        var list1 = new RazorVSInternalCompletionList { Items = [item1], ItemDefaults = new CompletionListItemDefaults { EditRange = range } };
        var list2 = new RazorVSInternalCompletionList { Items = [item2], ItemDefaults = new CompletionListItemDefaults { EditRange = range } };

        // Act
        var merged = CompletionListMerger.Merge(list1, list2);

        // Assert — same range preserved, items still use TextEditText
        Assert.NotNull(merged);
        Assert.Equal(range, merged.ItemDefaults?.EditRange?.Value);
        Assert.Equal("alpha", item1.TextEditText);
        Assert.Equal("beta", item2.TextEditText);
        Assert.Null(item1.TextEdit);
        Assert.Null(item2.TextEdit);
    }

    [Fact]
    public void Merge_EditRange_DifferentRanges_DematerializesBoth()
    {
        // Arrange — two lists with different EditRanges
        var range1 = new LspRange { Start = new(0, 5), End = new(0, 10) };
        var range2 = new LspRange { Start = new(1, 0), End = new(1, 4) };
        var item1 = new VSInternalCompletionItem { Label = "a", TextEditText = "alpha" };
        var item2 = new VSInternalCompletionItem { Label = "b", TextEditText = "beta" };
        var list1 = new RazorVSInternalCompletionList { Items = [item1], ItemDefaults = new CompletionListItemDefaults { EditRange = range1 } };
        var list2 = new RazorVSInternalCompletionList { Items = [item2], ItemDefaults = new CompletionListItemDefaults { EditRange = range2 } };

        // Act
        var merged = CompletionListMerger.Merge(list1, list2);

        // Assert — EditRange cleared, items get per-item TextEdits reconstructed
        Assert.NotNull(merged);
        Assert.Null(merged.ItemDefaults?.EditRange);

        var textEdit1 = Assert.IsType<TextEdit>(item1.TextEdit!.Value.Value);
        Assert.Equal(range1, textEdit1.Range);
        Assert.Equal("alpha", textEdit1.NewText);
        Assert.Null(item1.TextEditText);

        var textEdit2 = Assert.IsType<TextEdit>(item2.TextEdit!.Value.Value);
        Assert.Equal(range2, textEdit2.Range);
        Assert.Equal("beta", textEdit2.NewText);
        Assert.Null(item2.TextEditText);
    }

    [Fact]
    public void Merge_EditRange_DifferentRanges_UsesLabelWhenNoTextEditText()
    {
        // Arrange — item has no TextEditText, should fall back to Label
        var range1 = new LspRange { Start = new(0, 0), End = new(0, 3) };
        var range2 = new LspRange { Start = new(1, 0), End = new(1, 2) };
        var item1 = new VSInternalCompletionItem { Label = "foo" }; // no TextEditText
        var item2 = new VSInternalCompletionItem { Label = "bar", TextEditText = "baz" };
        var list1 = new RazorVSInternalCompletionList { Items = [item1], ItemDefaults = new CompletionListItemDefaults { EditRange = range1 } };
        var list2 = new RazorVSInternalCompletionList { Items = [item2], ItemDefaults = new CompletionListItemDefaults { EditRange = range2 } };

        // Act
        var merged = CompletionListMerger.Merge(list1, list2);

        // Assert — item1 uses Label as NewText
        Assert.NotNull(merged);
        var textEdit1 = Assert.IsType<TextEdit>(item1.TextEdit!.Value.Value);
        Assert.Equal("foo", textEdit1.NewText);
    }

    [Fact]
    public void Merge_EditRange_DifferentRanges_SkipsItemsWithExistingTextEdit()
    {
        // Arrange — item already has a TextEdit, should not be overwritten
        var range1 = new LspRange { Start = new(0, 0), End = new(0, 3) };
        var range2 = new LspRange { Start = new(1, 0), End = new(1, 2) };
        var existingRange = new LspRange { Start = new(2, 0), End = new(2, 5) };
        var item1 = new VSInternalCompletionItem { Label = "foo", TextEdit = new TextEdit { Range = existingRange, NewText = "existing" } };
        var item2 = new VSInternalCompletionItem { Label = "bar", TextEditText = "baz" };
        var list1 = new RazorVSInternalCompletionList { Items = [item1], ItemDefaults = new CompletionListItemDefaults { EditRange = range1 } };
        var list2 = new RazorVSInternalCompletionList { Items = [item2], ItemDefaults = new CompletionListItemDefaults { EditRange = range2 } };

        // Act
        var merged = CompletionListMerger.Merge(list1, list2);

        // Assert — item1's existing TextEdit is preserved
        Assert.NotNull(merged);
        var textEdit1 = Assert.IsType<TextEdit>(item1.TextEdit!.Value.Value);
        Assert.Equal(existingRange, textEdit1.Range);
        Assert.Equal("existing", textEdit1.NewText);
    }
}
