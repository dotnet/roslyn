// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ValueTracking;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.ValueTracking
{
    public abstract class AbstractBaseValueTrackingTests
    {
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
            return await service.TrackValueSourceAsync(item, cancellationToken);
        }

        internal static async Task<ImmutableArray<ValueTrackedItem>> ValidateChildrenAsync(TestWorkspace testWorkspace, ValueTrackedItem item, int[] lines, CancellationToken cancellationToken = default)
        {
            var children = await GetTrackedItemsAsync(testWorkspace, item, cancellationToken);

            Assert.Equal(lines.Length, children.Length);

            for (var i = 0; i < lines.Length; i++)
            {
                ValidateItem(children[i], lines[i]);
            }

            return children;
        }

        internal static async Task<ImmutableArray<ValueTrackedItem>> ValidateChildrenAsync(TestWorkspace testWorkspace, ValueTrackedItem item, (int line, string text)[] childInfo, CancellationToken cancellationToken = default)
        {
            var children = await GetTrackedItemsAsync(testWorkspace, item, cancellationToken);

            Assert.Equal(childInfo.Length, children.Length);

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
            Assert.Equal(line, item.LineSpan.Start);

            if (text is not null)
            {
                Assert.Equal(text, item.ToString());
            }
        }
    }
}
