// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A dictionary that maps strings to all known spellings of that string. Can be used to
    /// efficiently store the set of known type names for a module for both VB and C# while also
    /// answering questions like "do you have a type called Goo" in either a case sensitive or
    /// insensitive manner.
    /// </summary>
    internal partial class IdentifierCollection
    {
        // Maps an identifier to all spellings of that identifier in this module.  The value type is
        // typed as object so that it can store either an individual element (the common case), or a 
        // collection.
        //
        // Note: we use a case insensitive comparer so that we can quickly lookup if we know a name
        // regardless of its case.
        private readonly Dictionary<string, object> _map = new Dictionary<string, object>(
            StringComparer.OrdinalIgnoreCase);

        public IdentifierCollection()
        {
        }

        public IdentifierCollection(IEnumerable<string> identifiers)
        {
            this.AddIdentifiers(identifiers);
        }

        public void AddIdentifiers(IEnumerable<string> identifiers)
        {
            foreach (var identifier in identifiers)
            {
                AddIdentifier(identifier);
            }
        }

        public void AddIdentifier(string identifier)
        {
            RoslynDebug.Assert(identifier != null);

            object? value;
            if (!_map.TryGetValue(identifier, out value))
            {
                AddInitialSpelling(identifier);
            }
            else
            {
                AddAdditionalSpelling(identifier, value);
            }
        }

        private void AddAdditionalSpelling(string identifier, object value)
        {
            // Had a mapping for it.  It will either map to a single 
            // spelling, or to a set of spellings.
            var strValue = value as string;
            if (strValue != null)
            {
                if (!string.Equals(identifier, strValue, StringComparison.Ordinal))
                {
                    // We now have two spellings.  Create a collection for
                    // that and map the name to it.
                    _map[identifier] = new HashSet<string> { identifier, strValue };
                }
            }
            else
            {
                // We have multiple spellings already.
                var spellings = (HashSet<string>)value;

                // Note: the set will prevent duplicates.
                spellings.Add(identifier);
            }
        }

        private void AddInitialSpelling(string identifier)
        {
            // We didn't have any spellings for this word already.  Just
            // add the word as the single known spelling.
            _map.Add(identifier, identifier);
        }

        public bool ContainsIdentifier(string identifier, bool caseSensitive)
        {
            RoslynDebug.Assert(identifier != null);

            if (caseSensitive)
            {
                return CaseSensitiveContains(identifier);
            }
            else
            {
                return CaseInsensitiveContains(identifier);
            }
        }

        private bool CaseInsensitiveContains(string identifier)
        {
            // Simple case.  Just check if we've mapped this word to 
            // anything.  The map will take care of the case insensitive
            // lookup for us.
            return _map.ContainsKey(identifier);
        }

        private bool CaseSensitiveContains(string identifier)
        {
            object? spellings;
            if (_map.TryGetValue(identifier, out spellings))
            {
                var spelling = spellings as string;
                if (spelling != null)
                {
                    return string.Equals(identifier, spelling, StringComparison.Ordinal);
                }

                var set = (HashSet<string>)spellings;
                return set.Contains(identifier);
            }

            return false;
        }

        public ICollection<string> AsCaseSensitiveCollection()
        {
            return new CaseSensitiveCollection(this);
        }

        public ICollection<string> AsCaseInsensitiveCollection()
        {
            return new CaseInsensitiveCollection(this);
        }
    }
}
