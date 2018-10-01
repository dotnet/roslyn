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
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    /// <summary>
    /// Custom column to display the reference kind/usage info for the Find All References window.
    /// </summary>
    [Export(typeof(ITableColumnDefinition))]
    [Name(ColumnName)]
    internal sealed class FindUsagesValueUsageInfoColumnDefinition : AbstractFindUsagesCustomColumnDefinition
    {
        // We can have only a handful of different values for ValueUsageInfo flags enum, so the maximum size of the below dictionaries are capped.
        // So, we store these as static dictionarys which will be held in memory for the lifetime of the process.
        private static readonly ConcurrentDictionary<MultiDictionary<string, string>.ValueSet, string> s_constituentValuesToDisplayValuesMap
            = new ConcurrentDictionary<MultiDictionary<string, string>.ValueSet, string>();
        private static readonly ConcurrentDictionary<string, IEnumerable<string>> s_displayValueToConstituentValuesMap
            = new ConcurrentDictionary<string, IEnumerable<string>>();

        public const string ColumnName = nameof(ValueUsageInfo);

        // Allow filtering of the column by each ValueUsageInfo kind.
        private static readonly ImmutableArray<string> s_defaultFilters = Enum.GetValues(typeof(ValueUsageInfo))
                                                                .Cast<ValueUsageInfo>()
                                                                .Where(value => value != ValueUsageInfo.None && value.IsSingleBitSet())
                                                                .Select(v => v.ToLocalizableString())
                                                                .ToImmutableArray();
        public override IEnumerable<string> FilterPresets => s_defaultFilters;
        public override bool IsFilterable => true;

        public override string Name => ColumnName;
        public override string DisplayName => ServicesVSResources.Kind;
        public override double DefaultWidth => 100.0;

        public override string GetDisplayStringForColumnValues(MultiDictionary<string, string>.ValueSet values)
            => s_constituentValuesToDisplayValuesMap.GetOrAdd(values, JoinValues);
        protected override IEnumerable<string> SplitColumnDisplayValue(string displayValue)
            => s_displayValueToConstituentValuesMap.GetOrAdd(displayValue, SplitAndTrimValue);
    }
}
