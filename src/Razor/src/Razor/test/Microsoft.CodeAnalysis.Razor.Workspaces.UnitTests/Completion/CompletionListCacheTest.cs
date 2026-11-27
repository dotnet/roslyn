// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Completion;

public class CompletionListCacheTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    private readonly CompletionListCache _completionListCache = new CompletionListCache();
    private readonly ICompletionResolveContext _context = StrictMock.Of<ICompletionResolveContext>();

    [Fact]
    public void TryGet_SetCompletionList_ReturnsTrue()
    {
        // Arrange
        var completionList = new RazorVSInternalCompletionList()
        {
            Items = [new VSInternalCompletionItem()]
        };
        var resultId = _completionListCache.Add(completionList, _context);
        completionList.SetResultId(resultId, clientCapabilities: new());

        // Act
        var result = _completionListCache.TryGetOriginalRequestData((VSInternalCompletionItem)completionList.Items[0], out var cachedCompletionList, out var context);

        // Assert
        Assert.True(result);
        Assert.Same(completionList, cachedCompletionList);
        Assert.Same(_context, context);
    }

    [Fact]
    public void TryGet_SetCompletionListOnFullCache_ReturnsTrue()
    {
        // Arrange

        // Fill the completion list cache up until its cache max so the next entry causes eviction.
        for (var i = 0; i < CompletionListCache.MaxCacheSize; i++)
        {
            _completionListCache.Add(new VSInternalCompletionList(), _context);
        }

        var completionList = new RazorVSInternalCompletionList()
        {
            Items = [new VSInternalCompletionItem()]
        };
        var resultId = _completionListCache.Add(completionList, _context);
        completionList.SetResultId(resultId, clientCapabilities: new());

        // Act
        var result = _completionListCache.TryGetOriginalRequestData((VSInternalCompletionItem)completionList.Items[0], out var cachedCompletionList, out var context);

        // Assert
        Assert.True(result);
        Assert.Same(completionList, cachedCompletionList);
        Assert.Same(_context, context);
    }

    [Fact]
    public void TryGet_UnknownCompletionList_ReturnsTrue()
    {
        // Act
        var result = _completionListCache.TryGetOriginalRequestData(new VSInternalCompletionItem(), out var cachedCompletionList, out var context);

        // Assert
        Assert.False(result);
        Assert.Null(cachedCompletionList);
        Assert.Null(context);
    }

    [Fact]
    public void TryGet_LastCompletionList_ReturnsTrue()
    {
        // Arrange
        var initialCompletionList = new RazorVSInternalCompletionList()
        {
            Items = [new VSInternalCompletionItem()]
        };
        var initialCompletionListResultId = _completionListCache.Add(initialCompletionList, _context);
        initialCompletionList.SetResultId(initialCompletionListResultId, clientCapabilities: new());

        for (var i = 0; i < CompletionListCache.MaxCacheSize - 1; i++)
        {
            // We now fill the completion list cache up to its last slot.
            _completionListCache.Add(new VSInternalCompletionList(), _context);
        }

        // Act
        var result = _completionListCache.TryGetOriginalRequestData((VSInternalCompletionItem)initialCompletionList.Items[0], out var cachedCompletionList, out var context);

        // Assert
        Assert.True(result);
        Assert.Same(initialCompletionList, cachedCompletionList);
        Assert.Same(_context, context);
    }

    [Fact]
    public void TryGet_EvictedCompletionList_ReturnsFalse()
    {
        // Arrange
        var initialCompletionList = new RazorVSInternalCompletionList()
        {
            Items = [new VSInternalCompletionItem()]
        };
        var initialCompletionListResultId = _completionListCache.Add(initialCompletionList, _context);
        initialCompletionList.SetResultId(initialCompletionListResultId, clientCapabilities: new());

        // We now fill the completion list cache up until its cache max so that the initial completion list we set gets evicted.
        for (var i = 0; i < CompletionListCache.MaxCacheSize; i++)
        {
            _completionListCache.Add(new VSInternalCompletionList(), _context);
        }

        // Act
        var result = _completionListCache.TryGetOriginalRequestData((VSInternalCompletionItem)initialCompletionList.Items[0], out var cachedCompletionList, out var context);

        // Assert
        Assert.False(result);
        Assert.Null(cachedCompletionList);
        Assert.Null(context);
    }
}
