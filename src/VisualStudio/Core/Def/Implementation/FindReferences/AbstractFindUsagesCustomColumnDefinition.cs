// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    /// <summary>
    /// Implementation of a custom, dynamic column for the Find All References window.
    /// </summary>
    internal abstract class AbstractFindUsagesCustomColumnDefinition : TableColumnDefinitionBase
    {
        protected AbstractFindUsagesCustomColumnDefinition()
        {
            DefaultColumnState = new ColumnState2(Name, isVisible: false, DefaultWidth);
        }

        public ColumnState2 DefaultColumnState { get; }

        public override bool TryGetFilterItems(ITableEntryHandle entry, out IEnumerable<string> filterItems)
        {
            if (IsFilterable &&
                entry.TryGetValue(Name, out var value) &&
                value is string displayString &&
                !string.IsNullOrEmpty(displayString))
            {
                filterItems = SplitColumnDisplayValue(displayString);
                return true;
            }

            return base.TryGetFilterItems(entry, out filterItems);
        }

        // Default implementation for column display value.
        public virtual string GetDisplayStringForColumnValues(ImmutableArray<string> values) => string.Join(", ", values);
        protected virtual IEnumerable<string> SplitColumnDisplayValue(string displayValue) => displayValue.Split(',').Select(v => v.Trim());
    }
}
