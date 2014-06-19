// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    /// answering questions like "do you have a type called Foo" in either a case sensitive or
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
        private readonly Dictionary<string, object> map = new Dictionary<string, object>(
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
            Debug.Assert(identifier != null);

            object value;
            if (!map.TryGetValue(identifier, out value))
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
            if (value is string)
            {
                if (!string.Equals(identifier, value as string, StringComparison.Ordinal))
                {
                    // We now have two spellings.  Create a collection for
                    // that and map the name to it.
                    var set = new HashSet<string>();
                    set.Add(identifier);
                    set.Add((string)value);
                    map[identifier] = set;
                }
            }
            else
            {
                // We have multiple spellings already.
                var spellings = value as HashSet<string>;

                // Note: the set will prevent duplicates.
                spellings.Add(identifier);
            }
        }

        private void AddInitialSpelling(string identifier)
        {
            // We didn't have any spellings for this word already.  Just
            // add the word as the single known spelling.
            map.Add(identifier, identifier);
        }

        public bool ContainsIdentifier(string identifier, bool caseSensitive)
        {
            Debug.Assert(identifier != null);

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
            return map.ContainsKey(identifier);
        }

        private bool CaseSensitiveContains(string identifier)
        {
            object spellings;
            if (map.TryGetValue(identifier, out spellings))
            {
                if (spellings is string)
                {
                    return string.Equals(identifier, spellings as string, StringComparison.Ordinal);
                }
                else
                {
                    var set = spellings as HashSet<string>;
                    return set.Contains(identifier);
                }
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
