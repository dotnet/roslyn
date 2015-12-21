// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Roslyn.Test.EditorUtilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.EditorAdapter
{
    public class TextSpanExtensionsTest
    {
        [Fact]
        public void ConvertToSpan()
        {
            Action<int, int> del = (start, length) =>
            {
                var textSpan = new TextSpan(start, length);
                var span = textSpan.ToSpan();
                Assert.Equal(start, span.Start);
                Assert.Equal(length, span.Length);
            };

            del(0, 5);
            del(15, 20);
        }

        [Fact]
        public void ConvertToSnapshotSpan1()
        {
            var snapshot = EditorFactory.CreateBuffer(TestExportProvider.ExportProviderWithCSharpAndVisualBasic, new string('a', 10)).CurrentSnapshot;
            var textSpan = new TextSpan(0, 5);
            var ss = textSpan.ToSnapshotSpan(snapshot);
            Assert.Same(snapshot, ss.Snapshot);
            Assert.Equal(0, ss.Start);
            Assert.Equal(5, ss.Length);
        }

        [Fact]
        public void ConvertToSnapshotSpan2()
        {
            var snapshot = EditorFactory.CreateBuffer(TestExportProvider.ExportProviderWithCSharpAndVisualBasic, new string('a', 10)).CurrentSnapshot;
            var textSpan = new TextSpan(0, 10);
            var ss = textSpan.ToSnapshotSpan(snapshot);
            Assert.Same(snapshot, ss.Snapshot);
            Assert.Equal(0, ss.Start);
            Assert.Equal(10, ss.Length);
        }
    }
}
