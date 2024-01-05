// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    internal sealed class ConflictingIdentifierTracker(SyntaxToken tokenBeingRenamed, IEqualityComparer<string> identifierComparer)
    {
        /// <summary>
        /// The core data structure of the tracker. This is a dictionary of variable name to the
        /// current identifier tokens that are declaring variables. This should only ever be updated
        /// via the AddIdentifier and RemoveIdentifier helpers.
        /// </summary>
        private readonly Dictionary<string, List<SyntaxToken>> _currentIdentifiersInScope = new Dictionary<string, List<SyntaxToken>>(identifierComparer);
        private readonly HashSet<SyntaxToken> _conflictingTokensToReport = new();

        public IEnumerable<SyntaxToken> ConflictingTokens => _conflictingTokensToReport;

        public void AddIdentifier(SyntaxToken token)
        {
            if (token.IsMissing || token.ValueText == null)
            {
                return;
            }

            var name = token.ValueText;

            if (_currentIdentifiersInScope.TryGetValue(name, out var conflictingTokens))
            {
                conflictingTokens.Add(token);

                // If at least one of the identifiers is the one we're renaming,
                // track it. This means that conflicts unrelated to our rename (that
                // were there when we started) we won't flag.
                if (conflictingTokens.Contains(tokenBeingRenamed))
                {
                    foreach (var conflictingToken in conflictingTokens)
                    {
                        if (conflictingToken != tokenBeingRenamed)
                        {
                            // conflictingTokensToReport is a set, so we won't get duplicates
                            _conflictingTokensToReport.Add(conflictingToken);
                        }
                    }
                }
            }
            else
            {
                // No identifiers yet, so record the first one
                _currentIdentifiersInScope.Add(name, new List<SyntaxToken> { token });
            }
        }

        public void AddIdentifiers(IEnumerable<SyntaxToken> tokens)
        {
            foreach (var token in tokens)
            {
                AddIdentifier(token);
            }
        }

        public void RemoveIdentifier(SyntaxToken token)
        {
            if (token.IsMissing || token.ValueText == null)
            {
                return;
            }

            var name = token.ValueText;

            var currentIdentifiers = _currentIdentifiersInScope[name];
            currentIdentifiers.Remove(token);

            if (currentIdentifiers.Count == 0)
            {
                _currentIdentifiersInScope.Remove(name);
            }
        }

        public void RemoveIdentifiers(IEnumerable<SyntaxToken> tokens)
        {
            foreach (var token in tokens)
            {
                RemoveIdentifier(token);
            }
        }
    }
}
