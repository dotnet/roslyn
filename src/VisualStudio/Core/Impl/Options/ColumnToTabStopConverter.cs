// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Globalization;
using System.Windows.Controls;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    /// <summary>
    /// DataGridTemplateColumns have custom controls that should be focused instead of the cell.
    /// </summary>
    internal class ColumnToTabStopConverter : ValueConverter<DataGridColumn, bool>
    {
        protected override bool Convert(DataGridColumn value, object parameter, CultureInfo culture)
        {
            // We use DataGridTemplateColumns in our options grids to contain controls (as opposed
            // to plain text in DataGridTextColumns). We want the tab stop to be on the contained
            // control and not the cell itself, so don't have DataGridTemplateColumns be tab stops.
            return value is not DataGridTemplateColumn;
        }
    }
}
