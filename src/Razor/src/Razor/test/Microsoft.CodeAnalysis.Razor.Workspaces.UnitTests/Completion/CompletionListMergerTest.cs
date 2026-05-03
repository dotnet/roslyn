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

        // Assert
        Assert.NotNull(merged);
        Assert.Equal(expectedCommitCharacters, merged.CommitCharacters);
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

        // Assert
        Assert.NotNull(merged);
        Assert.Equal(expectedCommitCharacters, merged.CommitCharacters);

        // Inherited commit characters got populated onto the non-chosen item.
        Assert.Equal(_completionItem2.VsCommitCharacters, lesserCommitCharacters);
        Assert.Null(_completionItem3.VsCommitCharacters);
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
}
