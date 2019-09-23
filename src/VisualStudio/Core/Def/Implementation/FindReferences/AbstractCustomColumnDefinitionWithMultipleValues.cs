// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.LanguageServices.FindUsages;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindReferences
{
    internal abstract class AbstractCustomColumnDefinitionWithMultipleValues : AbstractCustomColumnDefinition
    {
        public abstract string GetDisplayStringForColumnValues(ImmutableArray<string> values);

        protected static string JoinValues(ImmutableArray<string> values) => string.Join(", ", values);

        protected static ImmutableArray<string> SplitAndTrimValue(string displayValue) => displayValue.Split(',').Select(v => v.Trim()).ToImmutableArray();
    }
}
