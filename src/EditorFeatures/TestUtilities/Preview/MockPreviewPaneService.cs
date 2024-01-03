// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Preview
{
    [ExportWorkspaceService(typeof(IPreviewPaneService), ServiceLayer.Test), Shared, PartNotDiscoverable]
    internal class MockPreviewPaneService : IPreviewPaneService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public MockPreviewPaneService()
        {
        }

        public IDisposable GetPreviewPane(DiagnosticData? diagnostic, IReadOnlyList<PreviewWrapper>? previewContents)
        {
            var contents = previewContents ?? SpecializedCollections.EmptyEnumerable<PreviewWrapper>();

            foreach (var content in contents)
            {
                content.Dispose();
            }

            // test only mock object
            return new DisposableGrid();
        }

        private sealed class DisposableGrid : Grid, IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
