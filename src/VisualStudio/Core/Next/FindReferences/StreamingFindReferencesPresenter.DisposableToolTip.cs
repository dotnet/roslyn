// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;

namespace Microsoft.VisualStudio.LanguageServices.FindReferences
{
    internal partial class StreamingFindReferencesPresenter
    {
        private class DisposableToolTip : IDisposable
        {
            public readonly ToolTip ToolTip;
            private PreviewWorkspace _workspace;

            private bool _disposed;

            public DisposableToolTip(ToolTip toolTip, PreviewWorkspace workspace)
            {
                ToolTip = toolTip;
                _workspace = workspace;
            }

            public void Dispose()
            {
                Debug.Assert(!_disposed);
                _disposed = true;
                _workspace.Dispose();
                _workspace = null;
            }
        }
    }
}