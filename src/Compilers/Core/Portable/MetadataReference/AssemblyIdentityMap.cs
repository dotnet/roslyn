// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Implements a map from an assembly identity to a value. The map allows to look up the value by an identity
    /// that either exactly matches the original identity key or whose version is lower (that is the resulting value 
    /// corresponds to an identity with a higher version).
    /// </summary>
    internal sealed class AssemblyIdentityMap<TValue>
    {
        private readonly Dictionary<string, OneOrMany<KeyValuePair<AssemblyIdentity, TValue>>> _map;

        public AssemblyIdentityMap(Func<AssemblyIdentity, AssemblyIdentity, bool> secondaryComparer = null)
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

                    if (EqualModuloNameAndVersion(currentIdentity, identity))
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

        private static bool EqualModuloNameAndVersion(AssemblyIdentity x, AssemblyIdentity y)
        {
            return
                x.IsRetargetable == y.IsRetargetable &&
                x.ContentType == y.ContentType &&
                AssemblyIdentityComparer.CultureComparer.Equals(x.CultureName, y.CultureName) &&
                AssemblyIdentity.KeysEqual(x, y);
        }

        public void Add(AssemblyIdentity identity, TValue value)
        {
            var pair = KeyValuePair.Create(identity, value);

            OneOrMany<KeyValuePair<AssemblyIdentity, TValue>> sameName;
            _map[identity.Name] = _map.TryGetValue(identity.Name, out sameName) ? sameName.Add(pair) : OneOrMany.Create(pair);
        }
    }
}
