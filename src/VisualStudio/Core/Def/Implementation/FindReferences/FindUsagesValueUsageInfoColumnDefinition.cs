// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
    internal sealed class FindUsagesValueUsageInfoColumnDefinition : AbstractFindUsagesCustomColumnDefinition
    {
        public const string ColumnName = nameof(ValueUsageInfo);

        // Allow filtering of the column by each ValueUsageInfo kind.
        private static readonly ImmutableArray<string> s_defaultFilters = Enum.GetValues(typeof(ValueUsageInfo))
                                                                .Cast<ValueUsageInfo>()
                                                                .Where(value => value != ValueUsageInfo.None && (value & (value - 1)) == 0) // Single bit set
                                                                .Select(v => v.ToString())
                                                                .ToImmutableArray();
        public override IEnumerable<string> FilterPresets => s_defaultFilters;
        public override bool IsFilterable => true;

        public override string Name => ColumnName;
        public override string DisplayName => ServicesVSResources.Kind;
        public override double DefaultWidth => 100.0;
    }
}
