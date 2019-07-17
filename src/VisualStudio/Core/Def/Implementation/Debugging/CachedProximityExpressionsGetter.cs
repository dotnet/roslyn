// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Debugging
{
    internal partial class CachedProximityExpressionsGetter
    {
        // { ITextSnapshot -> { LocationName -> [ [e1, e2, e3], .... [e_x, e_y, e_z] ] } }
        private ConditionalWeakTable<ITextSnapshot, IDictionary<string, LinkedList<IList<string>>>> _snapshotToExpressions;

        private readonly IProximityExpressionsService _proximityExpressionsGetter;
        private const int MaxCacheLength = 2;

        public CachedProximityExpressionsGetter(IProximityExpressionsService proximityExpressionsGetter)
        {
            _proximityExpressionsGetter = proximityExpressionsGetter;
            ClearTable();
        }

        private void ClearTable()
        {
            _snapshotToExpressions = new ConditionalWeakTable<ITextSnapshot, IDictionary<string, LinkedList<IList<string>>>>();
        }

        public async Task<IList<string>> DoAsync(
            Document document,
            int position,
            string locationName,
            CancellationToken cancellationToken)
        {
            var result = SpecializedCollections.EmptyList<string>();

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var snapshot = text.FindCorrespondingEditorTextSnapshot();
            locationName ??= string.Empty;

            // Get the prior values we've computed.
            var locationToExpressionsMap = snapshot != null
                                         ? _snapshotToExpressions.GetValue(snapshot, _ => new Dictionary<string, LinkedList<IList<string>>>())
                                         : new Dictionary<string, LinkedList<IList<string>>>();

            var cachedExpressionLists = locationToExpressionsMap.GetOrAdd(locationName,
                _ => new LinkedList<IList<string>>());

            // Determine the right expressions for this position.
            var expressions = await _proximityExpressionsGetter.GetProximityExpressionsAsync(document, position, cancellationToken).ConfigureAwait(false);

            // If we get a new set of expressions, then add it to the list and evict any values we
            // no longer need.
            if (expressions != null)
            {
                if (cachedExpressionLists.Count == 0 ||
                    !cachedExpressionLists.First.Value.SetEquals(expressions))
                {
                    cachedExpressionLists.AddFirst(expressions);
                    while (cachedExpressionLists.Count > MaxCacheLength)
                    {
                        cachedExpressionLists.RemoveLast();
                    }
                }
            }

            // Return all the unique values from the previous and current invocation.  However, if
            // these are not the current values, then pull out any expressions that are not valid in
            // our current context.
            if (cachedExpressionLists.Any())
            {
                var list = new List<string>();
                foreach (var expr in cachedExpressionLists.Flatten())
                {
                    if (await _proximityExpressionsGetter.IsValidAsync(document, position, expr, cancellationToken).ConfigureAwait(false))
                    {
                        list.Add(expr);
                    }
                }

                result = list.Distinct().ToList();
            }

            return result;
        }

        internal void OnDebugModeChanged(DebugMode debugMode)
        {
            if (debugMode == DebugMode.Design)
            {
                // When we switch back to design mode, clear the cached values we've kept around.
                ClearTable();
            }
        }
    }
}
