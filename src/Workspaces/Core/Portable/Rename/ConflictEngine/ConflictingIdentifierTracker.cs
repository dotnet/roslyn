// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    internal sealed class ConflictingIdentifierTracker
    {
        /// <summary>
        /// The core data structure of the tracker. This is a dictionary of variable name to the
        /// current identifier tokens that are declaring variables. This should only ever be updated
        /// via the AddIdentifier and RemoveIdentifier helpers.
        /// </summary>
        private readonly Dictionary<string, List<SyntaxToken>> _currentIdentifiersInScope;
        private readonly HashSet<SyntaxToken> _conflictingTokensToReport;
        private readonly SyntaxToken _tokenBeingRenamed;

        public ConflictingIdentifierTracker(SyntaxToken tokenBeingRenamed, IEqualityComparer<string> identifierComparer)
        {
            _currentIdentifiersInScope = new Dictionary<string, List<SyntaxToken>>(identifierComparer);
            _conflictingTokensToReport = new HashSet<SyntaxToken>();
            _tokenBeingRenamed = tokenBeingRenamed;
        }

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
                if (conflictingTokens.Contains(_tokenBeingRenamed))
                {
                    foreach (var conflictingToken in conflictingTokens)
                    {
                        if (conflictingToken != _tokenBeingRenamed)
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
