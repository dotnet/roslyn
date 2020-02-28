﻿// Licensed to the .NET Foundation under one or more agreements.
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
    [ExportWorkspaceService(typeof(IPreviewPaneService), ServiceLayer.Host), Shared]
    internal class MockPreviewPaneService : IPreviewPaneService
    {
        [ImportingConstructor]
        public MockPreviewPaneService()
        {
        }

        public object GetPreviewPane(DiagnosticData diagnostic, IReadOnlyList<object> previewContents)
        {
            var contents = previewContents ?? SpecializedCollections.EmptyEnumerable<object>();

            foreach (var content in contents.OfType<IDisposable>())
            {
                content.Dispose();
            }

            // test only mock object
            return new Grid();
        }
    }
}
