// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    /// <summary>
    /// Error list column for Suppression state of a diagnostic.
    /// </summary>
    /// <remarks>
    /// TODO: Move this column down to the shell as it is shared by multiple issue sources (Roslyn and FxCop).
    /// </remarks>
    [Export(typeof(ITableColumnDefinition))]
    [Name(ColumnName)]
    internal class SuppressionStateColumnDefinition : TableColumnDefinitionBase
    {
        public const string ColumnName = "suppressionstate";
        private static readonly string[] s_defaultFilters = new[] { ServicesVSResources.Active, ServicesVSResources.NotApplicable, ServicesVSResources.Suppressed };

        [ImportingConstructor]
        public SuppressionStateColumnDefinition()
        {
        }

        public override string Name => ColumnName;
        public override string DisplayName => ServicesVSResources.Suppression_State;
        public override string HeaderName => ServicesVSResources.Suppression_State;
        public override double MinWidth => 50.0;
        public override bool DefaultVisible => true;
        public override bool IsFilterable => true;
        public override IEnumerable<string> FilterPresets => s_defaultFilters;

        public static void SetDefaultFilter(IWpfTableControl tableControl)
        {
            // We want only the active diagnostics to show up in the error list by default.
            if (tableControl.ColumnDefinitionManager.GetColumnDefinition(ColumnName) is SuppressionStateColumnDefinition suppressionStateColumn)
            {
                tableControl.SetFilter(ColumnName, new ColumnHashSetFilter(suppressionStateColumn, excluded: ServicesVSResources.Suppressed));
            }
        }

        public override bool TryCreateToolTip(ITableEntryHandle entry, out object toolTip)
        {
            if (entry.TryGetValue(ColumnName, out var content) &&
                content as string == ServicesVSResources.NotApplicable)
            {
                toolTip = ServicesVSResources.SuppressionNotSupportedToolTip;
                return true;
            }

            toolTip = null;
            return false;
        }
    }
}

