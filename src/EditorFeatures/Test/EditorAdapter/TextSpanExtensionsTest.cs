// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Roslyn.Test.EditorUtilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.EditorAdapter;

[UseExportProvider]
public sealed class TextSpanExtensionsTest
{
    [Fact]
    public void ConvertToSpan()
    {
        static void del(int start, int length)
        {
            var textSpan = new TextSpan(start, length);
            var span = textSpan.ToSpan();
            Assert.Equal(start, span.Start);
            Assert.Equal(length, span.Length);
        }

        del(0, 5);
        del(15, 20);
    }

    [Fact]
    public void ConvertToSnapshotSpan1()
    {
        var exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider();
        var snapshot = EditorFactory.CreateBuffer(exportProvider, new string('a', 10)).CurrentSnapshot;
        var textSpan = new TextSpan(0, 5);
        var ss = textSpan.ToSnapshotSpan(snapshot);
        Assert.Same(snapshot, ss.Snapshot);
        Assert.Equal(0, ss.Start);
        Assert.Equal(5, ss.Length);
    }

    [Fact]
    public void ConvertToSnapshotSpan2()
    {
        var exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider();
        var snapshot = EditorFactory.CreateBuffer(exportProvider, new string('a', 10)).CurrentSnapshot;
        var textSpan = new TextSpan(0, 10);
        var ss = textSpan.ToSnapshotSpan(snapshot);
        Assert.Same(snapshot, ss.Snapshot);
        Assert.Equal(0, ss.Start);
        Assert.Equal(10, ss.Length);
    }
}
