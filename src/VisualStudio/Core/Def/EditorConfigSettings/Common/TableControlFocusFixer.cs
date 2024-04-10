// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.LanguageServices;

internal static class TableControlFocusFixer
{
    /// <summary>
    /// A Workaround for a focus issue in the tabular data control.
    /// When buckets are collapsed, depending on which element has focus at the time,
    /// focus is shifted to the outer control making keyboard navigation difficult.
    /// In some cases this behavior is fine (like the find-all-references tool window)
    /// because we already navigate the user (and therefore change focus) to the symbol definition
    /// on focus. Use this workaround in cases where we must not change keyboard focus.
    /// </summary>
    public static void DoNotLoseFocusOnBucketExpandOrCollapse(this IWpfTableControl tableControl)
    {
        tableControl.Control.PreviewLostKeyboardFocus += (object sender, KeyboardFocusChangedEventArgs e) =>
        {
            // The tabular data control is a list view, the new focus changing to a different control tells us we've hit this case.
            // This workaround will break if the underlying implementation of the tabular data control is changed someday.
            if (e.NewFocus is not ListView && (e.KeyboardDevice.IsKeyDown(Key.Left) || e.KeyboardDevice.IsKeyDown(Key.Right)))
            {
                // Set handled to true to indicate that we want to not do this focus change.
                e.Handled = true;
            }
        };
    }
}
