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
        public ValueTrackingTreeViewModel? ViewModel { get; }

        public ValueTrackingToolWindow() : base(null)
        {
            Caption = ServicesVSResources.Value_Tracking;
            Content = new BindableTextBlock()
            {
                Text = "Select an appropriate symbol to start value tracking"
            };
        }

        public ValueTrackingToolWindow(ValueTrackingTreeViewModel viewModel)
            : base(null)
        {
            Caption = ServicesVSResources.Value_Tracking;

            ViewModel = viewModel;
            Content = new ValueTrackingTree(ViewModel);
        }

        public ValueTrackingTreeItemViewModel? Root
        {
            get => ViewModel?.Roots.Single();
            set
            {
                if (ViewModel is null || value is null)
                {
                    return;
                }

                ViewModel.Roots.Clear();
                ViewModel.Roots.Add(value);
            }
        }
    }
}
