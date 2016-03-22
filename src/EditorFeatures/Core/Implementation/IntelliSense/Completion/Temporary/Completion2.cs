#if false
// Copyright (c) Microsoft Corporation
// All rights reserved
// REMOVE ONCE WE ACTUALLY REFERENCE THE REAL EDITOR DLLS.
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents a set of completions that supports a row of filter buttons displayed at the bottom on the intellisense popup.
    /// </summary>
    internal class CompletionSet2 : CompletionSet
    {
        private readonly IReadOnlyList<IIntellisenseFilter> _filters = null;
        /// <summary>
        /// Initializes a new instance of <see cref="CompletionSet2"/>.
        /// </summary>
        public CompletionSet2()
        {
        }
        /// <summary>
        /// Initializes a new instance of <see cref="CompletionSet2"/> with the specified name, text and filters.
        /// </summary>
        /// <param name="moniker">The unique, non-localized identifier for the completion set.</param>
        /// <param name="displayName">The localized name of the completion set.</param>
        /// <param name="applicableTo">The tracking span to which the completions apply.</param>
        /// <param name="completions">The list of completions.</param>
        /// <param name="completionBuilders">The list of completion builders.</param>
        /// <param name="filters">The list of <see cref="IIntellisenseFilter"/>s that will be displayed at the bottom of the completion dialog.</param>
        public CompletionSet2(string moniker,
                              string displayName,
                              ITrackingSpan applicableTo,
                              IEnumerable<Completion> completions,
                              IEnumerable<Completion> completionBuilders,
                              IReadOnlyList<IIntellisenseFilter> filters)
            : base(moniker, displayName, applicableTo, completions, completionBuilders)
        {
            _filters = filters;
        }
        public virtual IReadOnlyList<IIntellisenseFilter> Filters
        {
            get
            {
                return _filters;
            }
        }
    }
}
#endif