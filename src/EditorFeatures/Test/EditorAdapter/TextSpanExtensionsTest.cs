// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Roslyn.Test.EditorUtilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.EditorAdapter
{
    [UseExportProvider]
    public class TextSpanExtensionsTest
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
            var snapshot = EditorFactory.CreateBuffer(EditorServicesUtil.ExportProvider, new string('a', 10)).CurrentSnapshot;
            var textSpan = new TextSpan(0, 5);
            var ss = textSpan.ToSnapshotSpan(snapshot);
            Assert.Same(snapshot, ss.Snapshot);
            Assert.Equal(0, ss.Start);
            Assert.Equal(5, ss.Length);
        }

        [Fact]
        public void ConvertToSnapshotSpan2()
        {
            var snapshot = EditorFactory.CreateBuffer(EditorServicesUtil.ExportProvider, new string('a', 10)).CurrentSnapshot;
            var textSpan = new TextSpan(0, 10);
            var ss = textSpan.ToSnapshotSpan(snapshot);
            Assert.Same(snapshot, ss.Snapshot);
            Assert.Equal(0, ss.Start);
            Assert.Equal(10, ss.Length);
        }
    }
}
