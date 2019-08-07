// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    /// <summary>
    /// Custom column to display the reference kind/usage info for the Find All References window.
    /// </summary>
    [Export(typeof(ITableColumnDefinition))]
    [Name(ColumnName)]
    internal sealed class FindUsagesValueUsageInfoColumnDefinition : AbstractCustomColumnDefinition
    {
        // We can have only a handful of different values for ValueUsageInfo flags enum, so the maximum size of the below dictionaries are capped.
        // So, we store these as static dictionarys which will be held in memory for the lifetime of the process.
        private static readonly ConcurrentDictionary<ImmutableArray<string>, string> s_constituentValuesToDisplayValuesMap
            = new ConcurrentDictionary<ImmutableArray<string>, string>();
        private static readonly ConcurrentDictionary<string, ImmutableArray<string>> s_displayValueToConstituentValuesMap
            = new ConcurrentDictionary<string, ImmutableArray<string>>();

        public const string ColumnName = nameof(SymbolUsageInfo);

        [ImportingConstructor]
        public FindUsagesValueUsageInfoColumnDefinition()
        {
        }

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


        // Allow filtering of the column by each allowed SymbolUsageInfo kind.
        public override IEnumerable<string> FilterPresets => SymbolUsageInfo.LocalizableStringsForAllAllowedValues;
        public override bool IsFilterable => true;

        public override string Name => ColumnName;
        public override string DisplayName => ServicesVSResources.Kind;
        public override double DefaultWidth => 100.0;

        public override string GetDisplayStringForColumnValues(ImmutableArray<string> values)
            => s_constituentValuesToDisplayValuesMap.GetOrAdd(values, JoinValues);
        internal ImmutableArray<string> SplitColumnDisplayValue(string displayValue)
            => s_displayValueToConstituentValuesMap.GetOrAdd(displayValue, SplitAndTrimValue);
    }
}
