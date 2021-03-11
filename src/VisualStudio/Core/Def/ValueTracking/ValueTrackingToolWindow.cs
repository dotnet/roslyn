// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.ValueTracking
{
    [Guid(Guids.ValueTrackingToolWindowIdString)]
    internal class ValueTrackingToolWindow : ToolWindowPane
    {
        public static ValueTrackingToolWindow? Instance { get; set; }
        private readonly ValueTrackingTreeViewModel _viewModel;

        // Needed for VSSDK003
        // See https://github.com/Microsoft/VSSDK-Analyzers/blob/main/doc/VSSDK003.md
        public ValueTrackingToolWindow(object o)
            : base(null)
        {
            if (Instance is not null)
            {
                throw new Exception("Cannot initialize the window more than once");
            }

            this.Caption = "Value Tracking";

            if (o is ValueTrackingTreeItemViewModel root)
            {
                _viewModel = new ValueTrackingTreeViewModel(root);
                Content = new ValueTrackingTree(_viewModel);
            }
            else
            {
                throw new Exception("This shouldn't happen");
            }
        }

        public ValueTrackingTreeItemViewModel Root
        {
            get => _viewModel.Roots.Single();
            set
            {
                _viewModel.Roots.Clear();
                _viewModel.Roots.Add(value);
            }
        }
    }
}
