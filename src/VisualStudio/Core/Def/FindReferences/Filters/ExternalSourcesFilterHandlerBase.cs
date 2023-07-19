// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Internal.VisualStudio.Shell.ErrorList;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindReferences.Filters
{
    internal abstract class ExternalSourcesFilterHandlerBase : FilterHandlerBase
    {
        /// <summary>
        /// Whether to include items that have an ItemOrigin ("Source") column value of Exact ("Local")
        /// </summary>
        public abstract bool IncludeExact { get; }

        /// <summary>
        /// Whether to include items that have an ItemOrigin ("Source") column value of ExactMetadata ("Local (from metadata)")
        /// </summary>
        public abstract bool IncludeExactMetadata { get; }

        /// <summary>
        /// Whether to include items that have an ItemOrigin ("Source") column value of Other
        /// </summary>
        public abstract bool IncludeOther { get; }

        public override IEntryFilter GetFilter(out string displayText)
        {
            displayText = FilterDisplayName;
            return new EntryFilter(IncludeExact, IncludeExactMetadata, IncludeOther);
        }

        private class EntryFilter : IEntryFilter
        {
            private readonly bool _includeExact;
            private readonly bool _includeExactMetadata;
            private readonly bool _includeOther;

            public EntryFilter(bool includeExact, bool includeExactMetadata, bool includeOther)
            {
                _includeExact = includeExact;
                _includeExactMetadata = includeExactMetadata;
                _includeOther = includeOther;
            }

            public bool Match(ITableEntryHandle entry)
            {
                if (!entry.TryGetValue(StandardTableKeyNames.ItemOrigin, out ItemOrigin origin))
                    origin = ItemOrigin.Exact;

                return _includeExact && origin == ItemOrigin.Exact
                    || _includeExactMetadata && origin == ItemOrigin.ExactMetadata
                    || _includeOther && origin == ItemOrigin.Other;
            }
        }
    }
}
