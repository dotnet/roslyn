// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Roslyn.Hosting.Diagnostics.PerfMargin
{
    public sealed class PerfMarginPanel : UserControl
    {
        private static readonly DataModel s_model = new();
        private static readonly PerfEventActivityLogger s_logger = new(s_model);

        private readonly ListView _mainListView;
        private readonly Grid _mainGrid;

        private readonly DispatcherTimer _timer;
        private readonly List<StatusIndicator> _indicators = new();

        private ListView _detailsListView;
        private bool _stopTimer;

        public PerfMarginPanel()
        {
            Logger.SetLogger(AggregateLogger.AddOrReplace(s_logger, Logger.GetLogger(), l => l is PerfEventActivityLogger));

            // grid
            _mainGrid = new Grid();
            _mainGrid.ColumnDefinitions.Add(new ColumnDefinition());
            _mainGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            _mainGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            // set diagnostic list
            _mainListView = CreateContent(new ActivityLevel[] { s_model.RootNode }.Concat(s_model.RootNode.Children), useWrapPanel: true);
            _mainListView.SelectionChanged += OnPerfItemsListSelectionChanged;
            Grid.SetRow(_mainListView, 0);

            _mainGrid.Children.Add(_mainListView);

            Content = _mainGrid;

            _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(500), DispatcherPriority.Background, UpdateUI, Dispatcher);
            StartTimer();

            s_model.RootNode.IsActiveChanged += (s, e) =>
            {
                if (_stopTimer)
                {
                    StartTimer();
                }
            };
        }

        private void StartTimer()
        {
            _timer.Start();
            _stopTimer = false;
        }

        private void UpdateUI(object sender, EventArgs e)
        {
            foreach (var item in _indicators)
            {
                item.UpdateOnUIThread();
            }

            if (_stopTimer)
            {
                _timer.Stop();
            }
            else
            {
                // Stop it next time if there was no activity.
                _stopTimer = true;
            }
        }

        private ListView CreateContent(IEnumerable<ActivityLevel> items, bool useWrapPanel)
        {
            var listView = new ListView();

            foreach (var item in items)
            {
                var s = new StackPanel() { Orientation = Orientation.Horizontal };

                var indicator = new StatusIndicator(item);
                indicator.Subscribe();
                indicator.Width = 30;
                indicator.Height = 10;
                s.Children.Add(indicator);
                _indicators.Add(indicator);

                var label = new TextBlock();
                label.Text = item.Name;
                Grid.SetColumn(label, 1);
                s.Children.Add(label);

                s.ToolTip = item.Name;
                s.Tag = item;

                listView.Items.Add(s);
            }

            if (useWrapPanel)
            {
                listView.SelectionMode = SelectionMode.Single;
                listView.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);

                var wrapPanelFactory = new FrameworkElementFactory(typeof(WrapPanel));
                wrapPanelFactory.SetValue(WrapPanel.ItemWidthProperty, 120d);
                listView.ItemsPanel = new ItemsPanelTemplate(wrapPanelFactory);
            }

            return listView;
        }

        private void OnPerfItemsListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_detailsListView != null)
            {
                _mainGrid.ColumnDefinitions.RemoveAt(1);
                _mainGrid.Children.Remove(_detailsListView);
                foreach (StackPanel item in _detailsListView.Items)
                {
                    var indicator = item.Children[0] as StatusIndicator;
                    _indicators.Remove(indicator);
                    indicator.Unsubscribe();
                }

                _detailsListView = null;
            }

            if (_mainListView.SelectedItem is not StackPanel selectedItem)
            {
                return;
            }

            if (selectedItem.Tag is ActivityLevel context && context.Children != null && context.Children.Any())
            {
                _detailsListView = CreateContent(context.Children, useWrapPanel: false);
                _mainGrid.Children.Add(_detailsListView);
                _mainGrid.ColumnDefinitions.Add(new ColumnDefinition());
                Grid.SetColumn(_detailsListView, 1);

                // Update the UI
                StartTimer();
            }
        }
    }
}
