// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Windows.Controls;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.ValueTracking
{
    [Guid(Guids.ValueTrackingToolWindowIdString)]
    internal class ValueTrackingToolWindow : ToolWindowPane
    {
        private readonly Grid _rootGrid = new();

        public static ValueTrackingToolWindow? Instance { get; set; }

        private ValueTrackingTreeViewModel? _viewModel;
        public ValueTrackingTreeViewModel? ViewModel
        {
            get => _viewModel;
            set
            {
                if (value is null)
                {
                    throw new ArgumentNullException(nameof(ViewModel));
                }

                _viewModel = value;
                _rootGrid.Children.Clear();
                _rootGrid.Children.Add(new ValueTrackingTree(_viewModel));
            }
        }

        /// <summary>
        /// This paramterless constructor is used when
        /// the tool window is initialized on open without any
        /// context. If the tool window is left open across shutdown/restart
        /// of VS for example, then this gets called. 
        /// </summary>
        public ValueTrackingToolWindow() : base(null)
        {
            Caption = ServicesVSResources.Value_Tracking;

            _rootGrid.Children.Add(new TextBlock()
            {
                Text = "Select an appropriate symbol to start value tracking"
            });

            Content = _rootGrid;
        }

        public ValueTrackingToolWindow(ValueTrackingTreeViewModel viewModel)
            : base(null)
        {
            Content = _rootGrid;
            ViewModel = viewModel;
        }

        public TreeItemViewModel? Root
        {
            get => ViewModel?.Roots.Single();
            set
            {
                if (value is null)
                {
                    return;
                }

                Contract.ThrowIfNull(ViewModel);

                ViewModel.Roots.Clear();
                ViewModel.Roots.Add(value);
            }
        }
    }
}
