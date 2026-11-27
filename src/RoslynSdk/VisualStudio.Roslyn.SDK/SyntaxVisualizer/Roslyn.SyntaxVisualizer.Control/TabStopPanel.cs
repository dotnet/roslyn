// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;

namespace Roslyn.SyntaxVisualizer.Control
{
    /// <summary>
    /// Routes tab events from WinForms to WPF when the bottom or top of the form is reached,
    /// so that cyclic tab movement works correctly.
    /// </summary>
    internal class TabStopPanel : Panel
    {
        private readonly HwndHost _wpfHost;
        private PropertyGrid? _propertyGrid;
        public TabStopPanel(HwndHost wpfHost)
        {
            _wpfHost = wpfHost;
        }

        public PropertyGrid? PropertyGrid
        {
            get => _propertyGrid;
            set
            {
                if (_propertyGrid != null)
                {
                    throw new ArgumentException("Cannot initialize PropertyGrid twice", nameof(value));
                }

                _propertyGrid = value;
                Controls.Add(_propertyGrid);
            }
        }
        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (base.ProcessDialogKey(keyData))
            {
                return true;
            }
            else if ((keyData & (Keys.Alt | Keys.Control)) == Keys.None)
            {
                var keyCode = keyData & Keys.KeyCode;
                if (keyCode == Keys.Tab)
                {
                    IKeyboardInputSink sink = _wpfHost;
                    return sink.KeyboardInputSite.OnNoMoreTabStops(new TraversalRequest((keyData & Keys.Shift) == Keys.None ?
                        FocusNavigationDirection.Next : FocusNavigationDirection.Previous));
                }
            }
            return false;
        }
    }
}
