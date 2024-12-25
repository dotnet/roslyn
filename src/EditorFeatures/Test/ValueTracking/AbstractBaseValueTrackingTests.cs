// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ValueTracking;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.ValueTracking;

public abstract class AbstractBaseValueTrackingTests
{
    protected TestWorkspace CreateWorkspace(string code, TestHost testHost)
        => CreateWorkspace(code, EditorTestCompositions.EditorFeatures.WithTestHostParts(testHost));

    protected abstract TestWorkspace CreateWorkspace(string code, TestComposition composition);

    internal static async Task<ImmutableArray<ValueTrackedItem>> GetTrackedItemsAsync(TestWorkspace testWorkspace, CancellationToken cancellationToken = default)
    {
        var cursorDocument = testWorkspace.DocumentWithCursor;
        var document = testWorkspace.CurrentSolution.GetRequiredDocument(cursorDocument.Id);
        var textSpan = new TextSpan(cursorDocument.CursorPosition!.Value, 0);
        var service = testWorkspace.Services.GetRequiredService<IValueTrackingService>();
        return await service.TrackValueSourceAsync(textSpan, document, cancellationToken);

    }

    internal static async Task<ImmutableArray<ValueTrackedItem>> GetTrackedItemsAsync(TestWorkspace testWorkspace, ValueTrackedItem item, CancellationToken cancellationToken = default)
    {
        var service = testWorkspace.Services.GetRequiredService<IValueTrackingService>();
        return await service.TrackValueSourceAsync(testWorkspace.CurrentSolution, item, cancellationToken);
    }

    internal static async Task<ImmutableArray<ValueTrackedItem>> ValidateItemsAsync(TestWorkspace testWorkspace, (int line, string text)[] itemInfo, CancellationToken cancellationToken = default)
    {
        var items = await GetTrackedItemsAsync(testWorkspace, cancellationToken);
        Assert.True(itemInfo.Length == items.Length, $"GetTrackedItemsAsync\n\texpected: [{string.Join(",", itemInfo.Select(p => p.text))}]\n\t  actual: [{string.Join(",", items)}]");

        for (var i = 0; i < items.Length; i++)
        {
            ValidateItem(items[i], itemInfo[i].line, itemInfo[i].text);
        }

        return items;
    }

    internal static async Task<ImmutableArray<ValueTrackedItem>> ValidateChildrenAsync(TestWorkspace testWorkspace, ValueTrackedItem item, (int line, string text)[] childInfo, CancellationToken cancellationToken = default)
    {
        var children = await GetTrackedItemsAsync(testWorkspace, item, cancellationToken);
        Assert.True(childInfo.Length == children.Length, $"GetTrackedItemsAsync on [{item}]\n\texpected: [{string.Join(",", childInfo.Select(p => p.text))}]\n\t  actual: [{string.Join(",", children)}]");

        for (var i = 0; i < childInfo.Length; i++)
        {
            ValidateItem(children[i], childInfo[i].line, childInfo[i].text);
        }

        return children;
    }

    internal static async Task ValidateChildrenEmptyAsync(TestWorkspace testWorkspace, ValueTrackedItem item, CancellationToken cancellationToken = default)
    {
        var children = await GetTrackedItemsAsync(testWorkspace, item, cancellationToken);
        Assert.Empty(children);
    }

    internal static async Task ValidateChildrenEmptyAsync(TestWorkspace testWorkspace, IEnumerable<ValueTrackedItem> items, CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
        {
            await ValidateChildrenEmptyAsync(testWorkspace, item, cancellationToken);
        }
    }

    internal static void ValidateItem(ValueTrackedItem item, int line, string? text = null)
    {
        item.SourceText.GetLineAndOffset(item.Span.Start, out var lineStart, out var _);
        Assert.Equal(line, lineStart);

        if (text is not null)
        {
            Assert.Equal(text, item.ToString());
        }
    }
}
