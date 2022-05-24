// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices
{
    [Guid(Guids.SampleToolWindowIdString)]
    internal class SampleToolWindow : ToolWindowPane
    {
        private bool _initialized;

        [MemberNotNullWhen(true, nameof(_initialized))]
        public SampleToolboxUserControl? SampleUserControl { get; private set; }

        public SampleToolWindow() : base(null)
        {
            Caption = "Sample Tool Window";
            SampleUserControl = new SampleToolboxUserControl();
            Content = SampleUserControl;
        }

        internal void InitializeIfNeeded(RoslynPackage package)
        {
            if (_initialized)
            {
                return;
            }

            // Do any initialization logic here
            _initialized = true;
        }
    }
}
