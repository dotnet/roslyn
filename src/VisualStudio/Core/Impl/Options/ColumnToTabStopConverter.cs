// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            return !(value is DataGridTemplateColumn);
        }
    }
}
