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

        internal static void ValidateItem(ValueTrackedItem item, int line)
        {
            Assert.Equal(line, item.LineSpan.Start);
        }
    }
}
