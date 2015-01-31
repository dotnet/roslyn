// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.ErrorListDiagnosticsPackage
{
    /// <summary>
    /// Interaction logic for MyControl.xaml
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1309:FieldNamesMustNotBeginWithUnderscore", Justification = "This is OK here.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1119:StatementMustNotUseUnnecessaryParenthesis", Justification = "Reviewed.")]
    public partial class DiagnosticControl : UserControl
    {
        public DiagnosticControl()
        {
            InitializeComponent();
        }

        private IVsTaskList GetService()
        {
            if (this.SVsTaskList.IsChecked == true)
            {
                return Common.GetService<IVsTaskList, SVsTaskList>(Common.GlobalServiceProvider);
            }

            return Common.GetService<IVsTaskList, SVsErrorList>(Common.GlobalServiceProvider);
        }

        private IVsTaskList _service;
        private MyTaskProvider _provider;
        private uint _providerCookie = VSConstants.VSCOOKIE_NIL;
        private Random _random = new Random();
        private static readonly string[] s_documents =
                                            {
                                                @"d:\test\a.txt",
                                                @"d:\test\b.txt",
                                                @"d:\test\c.txt",
                                                @"d:\test\c.txt",
                                                @"d:\test\e.txt",
                                                @"d:\test\f.txt",
                                                @"d:\test\g.txt",
                                                @"d:\test\h.txt",
                                            };

        private void CreateProvider_Click(object sender, RoutedEventArgs e)
        {
            _service = this.GetService();
            if (_service != null)
            {
                _provider = new MyTaskProvider();
                _provider.AddItems(new MyTaskItem(_provider, document: @"d:\test\a.txt", text: "test 1 2 3", category: VSTASKCATEGORY.CAT_BUILDCOMPILE),
                                   new MyTaskItem(_provider, document: @"d:\test\b.txt", text: "test 4 5 6", subcategoryIndex: 1));

                int hr = _service.RegisterTaskProvider(_provider, out _providerCookie);

                if (ErrorHandler.Succeeded(hr))
                {
                    this.CreateProvider.Foreground = Brushes.Green;
                    this.RemoveProvider.IsEnabled = true;
                    this.Refresh.IsEnabled = true;
                    this.Add1000.IsEnabled = true;
                    this.RemoveHalf.IsEnabled = true;
                    this.CreateProvider.IsEnabled = false;

                    return;
                }
            }

            this.CreateProvider.Foreground = Brushes.Red;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            if ((_service != null) && (_providerCookie != VSConstants.VSCOOKIE_NIL))
            {
                int hr = _service.RefreshTasks(_providerCookie);

                if (ErrorHandler.Succeeded(hr))
                {
                    this.Refresh.Foreground = Brushes.Green;

                    return;
                }
            }

            this.Refresh.Foreground = Brushes.Red;
        }

        private void Add1000_Click(object sender, RoutedEventArgs e)
        {
            if ((_service != null) && (_providerCookie != VSConstants.VSCOOKIE_NIL))
            {
                IVsTaskItem[] items = new IVsTaskItem[1000];
                for (int i = 0; (i < 1000); ++i)
                {
                    var name = s_documents[_random.Next(s_documents.Length)];
                    var line = _random.Next(1000);
                    var col = _random.Next(100);
                    var mti = new MyTaskItem(_provider, document: name, line: line, column: col, text: "Random " + i.ToString(), category: VSTASKCATEGORY.CAT_BUILDCOMPILE);
                    items[i] = mti;
                }

                _provider.AddItems(items);

                int hr = ((IVsTaskList2)_service).RefreshOrAddTasks(_providerCookie, items.Length, items);

                if (ErrorHandler.Succeeded(hr))
                {
                    this.Add1000.Foreground = Brushes.Green;

                    return;
                }
            }

            this.Add1000.Foreground = Brushes.Red;
        }

        private void RemoveHalf_Click(object sender, RoutedEventArgs e)
        {
            if ((_service != null) && (_providerCookie != VSConstants.VSCOOKIE_NIL))
            {
                var items = new List<IVsTaskItem>();
                for (int i = _provider.Items.Count - 1; (i >= 0); i -= 2)
                {
                    items.Add(_provider.Items[i]);
                    _provider.Items.RemoveAt(i);
                }

                int hr = ((IVsTaskList2)_service).RemoveTasks(_providerCookie, items.Count, items.ToArray());

                if (ErrorHandler.Succeeded(hr))
                {
                    this.RemoveHalf.Foreground = Brushes.Green;

                    return;
                }
            }

            this.RemoveHalf.Foreground = Brushes.Red;
        }

        private void RemoveProvider_Click(object sender, RoutedEventArgs e)
        {
            if ((_service != null) && (_providerCookie != VSConstants.VSCOOKIE_NIL))
            {
                _provider.ClearItems();
                _service.RefreshTasks(_providerCookie);
                int hr = _service.UnregisterTaskProvider(_providerCookie);

                if (ErrorHandler.Succeeded(hr))
                {
                    _providerCookie = VSConstants.VSCOOKIE_NIL;
                    _provider = null;
                    _service = null;

                    this.RemoveProvider.Foreground = Brushes.Green;
                    this.CreateProvider.IsEnabled = true;
                    this.Refresh.IsEnabled = false;
                    this.Add1000.IsEnabled = false;
                    this.RemoveHalf.IsEnabled = false;
                    this.RemoveProvider.IsEnabled = false;
                    this.RemoveProvider.IsEnabled = false;

                    return;
                }
            }

            this.RemoveProvider.Foreground = Brushes.Red;
        }
    }
}
