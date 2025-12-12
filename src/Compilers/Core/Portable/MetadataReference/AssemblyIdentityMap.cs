// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Implements a map from an assembly identity to a value. The map allows to look up the value by an identity
    /// that either exactly matches the original identity key, or corresponds to a key with the lowest version among identities
    /// with higher version than the requested identity key.
    /// </summary>
    internal sealed class AssemblyIdentityMap<TValue>
    {
        private readonly Dictionary<string, OneOrMany<KeyValuePair<AssemblyIdentity, TValue>>> _map;

        public AssemblyIdentityMap()
        {
            _map = new Dictionary<string, OneOrMany<KeyValuePair<AssemblyIdentity, TValue>>>(AssemblyIdentityComparer.SimpleNameComparer);
        }

        public bool Contains(AssemblyIdentity identity, bool allowHigherVersion = true)
        {
            TValue value;
            return TryGetValue(identity, out value, allowHigherVersion);
        }

        public bool TryGetValue(AssemblyIdentity identity, out TValue value, bool allowHigherVersion = true)
        {
            OneOrMany<KeyValuePair<AssemblyIdentity, TValue>> sameName;
            if (_map.TryGetValue(identity.Name, out sameName))
            {
                int minHigherVersionCandidate = -1;

                for (int i = 0; i < sameName.Count; i++)
                {
                    AssemblyIdentity currentIdentity = sameName[i].Key;

                    if (AssemblyIdentity.EqualIgnoringNameAndVersion(currentIdentity, identity))
                    {
                        if (currentIdentity.Version == identity.Version)
                        {
                            value = sameName[i].Value;
                            return true;
                        }

                        // only higher version candidates are considered for match:
                        if (!allowHigherVersion || currentIdentity.Version < identity.Version)
                        {
                            continue;
                        }

                        if (minHigherVersionCandidate == -1 || currentIdentity.Version < sameName[minHigherVersionCandidate].Key.Version)
                        {
                            minHigherVersionCandidate = i;
                        }
                    }
                }

                if (minHigherVersionCandidate >= 0)
                {
                    value = sameName[minHigherVersionCandidate].Value;
                    return true;
                }
            }

            value = default(TValue);
            return false;
        }

        public bool TryGetValue(AssemblyIdentity identity, out TValue value, Func<Version, Version, TValue, bool> comparer)
        {
            OneOrMany<KeyValuePair<AssemblyIdentity, TValue>> sameName;
            if (_map.TryGetValue(identity.Name, out sameName))
            {
                for (int i = 0; i < sameName.Count; i++)
                {
                    AssemblyIdentity currentIdentity = sameName[i].Key;

                    if (comparer(identity.Version, currentIdentity.Version, sameName[i].Value) &&
                        AssemblyIdentity.EqualIgnoringNameAndVersion(currentIdentity, identity))
                    {
                        value = sameName[i].Value;
                        return true;
                    }
                }
            }

            value = default(TValue);
            return false;
        }

        public void Add(AssemblyIdentity identity, TValue value)
        {
            var pair = KeyValuePair.Create(identity, value);

            OneOrMany<KeyValuePair<AssemblyIdentity, TValue>> sameName;
            _map[identity.Name] = _map.TryGetValue(identity.Name, out sameName) ? sameName.Add(pair) : OneOrMany.Create(pair);
        }
    }
}
