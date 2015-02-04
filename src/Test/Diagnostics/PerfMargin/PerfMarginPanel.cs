using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Roslyn.Hosting.Diagnostics.PerfMargin
{
    public class PerfMarginPanel : UserControl
    {
        private static readonly DataModel model = new DataModel();
        private static readonly PerfEventActivityLogger logger = new PerfEventActivityLogger(model);

        private readonly ListView mainListView;
        private readonly Grid mainGrid;

        private readonly DispatcherTimer timer;
        private readonly List<StatusIndicator> indicators = new List<StatusIndicator>();

        private ListView detailsListView;
        private bool stopTimer;

        public PerfMarginPanel()
        {
            Logger.SetLogger(AggregateLogger.AddOrReplace(logger, Logger.GetLogger(), l => l is PerfEventActivityLogger));

            // grid
            mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition());
            mainGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            // set diagnostic list
            this.mainListView = CreateContent(new ActivityLevel[] { model.RootNode }.Concat(model.RootNode.Children), useWrapPanel: true);
            this.mainListView.SelectionChanged += OnPerfItemsListSelectionChanged;
            Grid.SetRow(this.mainListView, 0);

            mainGrid.Children.Add(this.mainListView);

            this.Content = mainGrid;

            this.timer = new DispatcherTimer(TimeSpan.FromMilliseconds(500), DispatcherPriority.Background, UpdateUI, this.Dispatcher);
            StartTimer();

            model.RootNode.IsActiveChanged += (s, e) =>
            {
                if (this.stopTimer)
                {
                    StartTimer();
                }
            };
        }

        private void StartTimer()
        {
            this.timer.Start();
            this.stopTimer = false;
        }

        private void UpdateUI(object sender, EventArgs e)
        {
            foreach (var item in this.indicators)
            {
                item.UpdateOnUIThread();
            }

            if (this.stopTimer)
            {
                this.timer.Stop();
            }
            else
            {
                // Stop it next time if there was no activity.
                this.stopTimer = true;
            }
        }

        private ListView CreateContent(IEnumerable<ActivityLevel> items, bool useWrapPanel)
        {
            var listView = new ListView();

            foreach (var item in items)
            {
                StackPanel s = new StackPanel() { Orientation = Orientation.Horizontal };
                ////g.HorizontalAlignment = HorizontalAlignment.Stretch;

                StatusIndicator indicator = new StatusIndicator(item);
                indicator.Subscribe();
                indicator.Width = 30;
                indicator.Height = 10;
                s.Children.Add(indicator);
                this.indicators.Add(indicator);

                TextBlock label = new TextBlock();
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
            if (this.detailsListView != null)
            {
                mainGrid.ColumnDefinitions.RemoveAt(1);
                mainGrid.Children.Remove(this.detailsListView);
                foreach (StackPanel item in this.detailsListView.Items)
                {
                    var indicator = item.Children[0] as StatusIndicator;
                    this.indicators.Remove(indicator);
                    indicator.Unsubscribe();
                }

                this.detailsListView = null;
            }

            var selectedItem = this.mainListView.SelectedItem as StackPanel;
            if (selectedItem == null)
            {
                return;
            }

            var context = selectedItem.Tag as ActivityLevel;
            if (context != null && context.Children != null && context.Children.Any())
            {
                this.detailsListView = CreateContent(context.Children, useWrapPanel: false);
                mainGrid.Children.Add(this.detailsListView);
                mainGrid.ColumnDefinitions.Add(new ColumnDefinition());
                Grid.SetColumn(this.detailsListView, 1);

                // Update the UI
                StartTimer();
            }
        }
    }
}
