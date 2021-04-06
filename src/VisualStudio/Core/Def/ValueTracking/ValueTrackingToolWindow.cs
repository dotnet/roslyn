// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.ValueTracking
{
    [Guid(Guids.ValueTrackingToolWindowIdString)]
    internal class ValueTrackingToolWindow : ToolWindowPane
    {
        public static ValueTrackingToolWindow? Instance { get; set; }
        private readonly ValueTrackingTreeViewModel _viewModel;

        public ValueTrackingToolWindow() : base(null)
        {
            this.Caption = "Value Tracking";

            _viewModel = new();
            Content = new ValueTrackingTree(_viewModel);
        }

        public ValueTrackingToolWindow(ValueTrackingTreeItemViewModel root)
            : base(null)
        {
            Caption = "Value Tracking";

            _viewModel = new(root);
            Content = new ValueTrackingTree(_viewModel);
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
