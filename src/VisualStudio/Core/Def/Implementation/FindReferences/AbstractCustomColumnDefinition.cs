// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.Shell.TableControl;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
    /// <summary>
    /// Column definition for additional properties to be displayed in the Find All References window.
    /// </summary>
    internal abstract class AbstractCustomColumnDefinition : TableColumnDefinitionBase
    {
        protected AbstractCustomColumnDefinition()
        {
            DefaultColumnState = new ColumnState2(Name, isVisible: false, DefaultWidth);
        }

        public ColumnState2 DefaultColumnState { get; }

        public abstract string GetDisplayStringForColumnValues(ImmutableArray<string> values);

        protected static string JoinValues(ImmutableArray<string> values) => string.Join(", ", values);

        protected static ImmutableArray<string> SplitAndTrimValue(string displayValue) => displayValue.Split(',').Select(v => v.Trim()).ToImmutableArray();
    }
}
