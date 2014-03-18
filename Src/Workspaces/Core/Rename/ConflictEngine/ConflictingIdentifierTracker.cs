// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly Dictionary<string, List<SyntaxToken>> currentIdentifiersInScope;
        private readonly HashSet<SyntaxToken> conflictingTokensToReport;
        private readonly SyntaxToken tokenBeingRenamed;

        public ConflictingIdentifierTracker(SyntaxToken tokenBeingRenamed, IEqualityComparer<string> identifierComparer)
        {
            this.currentIdentifiersInScope = new Dictionary<string, List<SyntaxToken>>(identifierComparer);
            this.conflictingTokensToReport = new HashSet<SyntaxToken>();
            this.tokenBeingRenamed = tokenBeingRenamed;
        }

        public IEnumerable<SyntaxToken> ConflictingTokens
        {
            get
            {
                return conflictingTokensToReport;
            }
        }

        public void AddIdentifier(SyntaxToken token)
        {
            if (token.IsMissing || token.ValueText == null)
            {
                return;
            }

            string name = token.ValueText;

            if (currentIdentifiersInScope.ContainsKey(name))
            {
                var conflictingTokens = currentIdentifiersInScope[name];
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
                            conflictingTokensToReport.Add(conflictingToken);
                        }
                    }
                }
            }
            else
            {
                // No identifiers yet, so record the first one
                currentIdentifiersInScope.Add(name, new List<SyntaxToken> { token });
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

            string name = token.ValueText;

            var currentIdentifiers = currentIdentifiersInScope[name];
            currentIdentifiers.Remove(token);

            if (currentIdentifiers.Count == 0)
            {
                currentIdentifiersInScope.Remove(name);
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
