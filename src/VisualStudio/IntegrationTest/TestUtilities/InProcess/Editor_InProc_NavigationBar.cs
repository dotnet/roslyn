// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess.ReflectionExtensions;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using IObjectWithSite = Microsoft.VisualStudio.OLE.Interop.IObjectWithSite;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal partial class Editor_InProc
    {
        public string GetSelectedNavBarItem(int comboBoxIndex)
            => ExecuteOnActiveView(v => GetNavigationBarComboBoxes(v)[comboBoxIndex].SelectedItem?.ToString());

        public string[] GetNavBarItems(int comboBoxIndex)
            => ExecuteOnActiveView(v =>
                GetNavigationBarComboBoxes(v)[comboBoxIndex]
                .Items
                .OfType<object>()
                .Select(i => i?.ToString() ?? "")
                .ToArray());

        public int GetNavbarItemIndex(int index, string itemText)
        {
            int FindItem(ComboBox comboBox)
            {
                for (var i = 0; i < comboBox.Items.Count; i++)
                {
                    if (comboBox.Items[i].ToString() == itemText)
                    {
                        return i;
                    }
                }

                return -1;
            }

            return ExecuteOnActiveView(v => FindItem(GetNavigationBarComboBoxes(v)[index]));
        }

        public void ExpandNavigationBar(int index)
        {
            ExecuteOnActiveView(v =>
            {
                var combobox = GetNavigationBarComboBoxes(v)[index];
                FocusManager.SetFocusedElement(FocusManager.GetFocusScope(combobox), combobox);
                combobox.IsDropDownOpen = true;
            });
        }

        public void SelectNavBarItem(int comboboxIndex, string selection)
        {
            var itemIndex = GetNavbarItemIndex(comboboxIndex, selection);
            if (itemIndex < 0)
            {
                throw new ArgumentException($"Could not find {selection} in combobox");
            }

            ExpandNavigationBar(comboboxIndex);
            _sendKeys.Send(VirtualKey.Home);
            for (var i = 0; i < itemIndex; i++)
            {
                _sendKeys.Send(VirtualKey.Down);
            }

            _sendKeys.Send(VirtualKey.Enter);
        }

        public bool IsNavBarEnabled()
            => ExecuteOnActiveView(v => GetNavbar(v) != null);

        private List<ComboBox> GetNavigationBarComboBoxes(IWpfTextView textView)
        {
            var margin = GetNavbar(textView);
            var combos = margin.GetFieldValue<List<ComboBox>>("_combos");
            return combos;
        }

        private static UIElement GetNavbar(IWpfTextView textView)
        {
            // Visual Studio 2019
            var editorAdaptersFactoryService = GetComponentModelService<IVsEditorAdaptersFactoryService>();
            var viewAdapter = editorAdaptersFactoryService.GetViewAdapter(textView);

            // Make sure we have the top pane
            //
            // The docs are wrong. When a secondary view exists, it is the secondary view which is on top. The primary
            // view is only on top when there is no secondary view.
            var codeWindow = TryGetCodeWindow(viewAdapter);
            if (ErrorHandler.Succeeded(codeWindow.GetSecondaryView(out var secondaryViewAdapter)))
            {
                viewAdapter = secondaryViewAdapter;
            }

            var textViewHost = editorAdaptersFactoryService.GetWpfTextViewHost(viewAdapter);
            var dropDownMargin = textViewHost.GetTextViewMargin("DropDownMargin");
            if (dropDownMargin != null)
            {
                return ((Decorator)dropDownMargin.VisualElement).Child;
            }

            // Visual Studio 2017
            var control = textView.VisualElement;
            while (control != null)
            {
                if (control.GetType().Name == "WpfMultiViewHost")
                {
                    break;
                }

                control = VisualTreeHelper.GetParent(control) as FrameworkElement;
            }

            var topMarginControl = control.GetPropertyValue<ContentControl>("TopMarginControl");
            var vsDropDownBarAdapterMargin = topMarginControl.Content as UIElement;
            return vsDropDownBarAdapterMargin;
        }

        private static IVsCodeWindow TryGetCodeWindow(IVsTextView textView)
        {
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            if (!(textView is IObjectWithSite objectWithSite))
            {
                return null;
            }

            var riid = typeof(IOleServiceProvider).GUID;
            objectWithSite.GetSite(ref riid, out var ppvSite);
            if (ppvSite == IntPtr.Zero)
            {
                return null;
            }

            IOleServiceProvider oleServiceProvider = null;
            try
            {
                oleServiceProvider = Marshal.GetObjectForIUnknown(ppvSite) as IOleServiceProvider;
            }
            finally
            {
                Marshal.Release(ppvSite);
            }

            if (oleServiceProvider == null)
            {
                return null;
            }

            var guidService = typeof(SVsWindowFrame).GUID;
            riid = typeof(IVsWindowFrame).GUID;
            if (ErrorHandler.Failed(oleServiceProvider.QueryService(ref guidService, ref riid, out var ppvObject)) || ppvObject == IntPtr.Zero)
            {
                return null;
            }

            IVsWindowFrame frame = null;
            try
            {
                frame = Marshal.GetObjectForIUnknown(ppvObject) as IVsWindowFrame;
            }
            finally
            {
                Marshal.Release(ppvObject);
            }

            riid = typeof(IVsCodeWindow).GUID;
            if (ErrorHandler.Failed(frame.QueryViewInterface(ref riid, out ppvObject)) || ppvObject == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                return Marshal.GetObjectForIUnknown(ppvObject) as IVsCodeWindow;
            }
            finally
            {
                Marshal.Release(ppvObject);
            }
        }
    }
}
