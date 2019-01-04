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
            // Determine the constituent strings for the display value in column, which should be used for applying the filter.
            // For example, if value "Read, Write" is displayed in column for an entry, we will return "Read" and "Write" here,
            // so filtering based on individual filter terms can be done.
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

        public abstract string GetDisplayStringForColumnValues(ImmutableArray<string> values);
        protected abstract ImmutableArray<string> SplitColumnDisplayValue(string displayValue);

        protected static string JoinValues(ImmutableArray<string> values) => string.Join(", ", values);
        protected static ImmutableArray<string> SplitAndTrimValue(string displayValue) => displayValue.Split(',').Select(v => v.Trim()).ToImmutableArray();
    }
}
