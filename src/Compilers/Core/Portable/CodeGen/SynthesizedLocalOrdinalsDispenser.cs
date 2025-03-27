// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// Dispenser of unique ordinals for synthesized variable names that have the same kind and syntax offset.
    /// </summary>
    internal sealed class SynthesizedLocalOrdinalsDispenser
    {
        // The key is (local.SyntaxOffset << 8) | local.SynthesizedKind.
        private PooledDictionary<long, int>? _lazyMap;

        private static long MakeKey(SynthesizedLocalKind localKind, int syntaxOffset)
        {
            return (long)syntaxOffset << 8 | (long)localKind;
        }

        public void Free()
        {
            if (_lazyMap != null)
            {
                _lazyMap.Free();
                _lazyMap = null;
            }
        }

        public int AssignLocalOrdinal(SynthesizedLocalKind localKind, int syntaxOffset)
        {
#if !DEBUG
            // Optimization (avoid growing the dictionary below): 
            // User-defined locals have to have a distinct syntax offset, thus ordinal is always 0.
            if (localKind == SynthesizedLocalKind.UserDefined)
            {
                return 0;
            }
#endif
            int ordinal;
            long key = MakeKey(localKind, syntaxOffset);

            // Group by syntax offset and kind.
            // Variables associated with the same syntax and kind will be assigned different ordinals.
            if (_lazyMap == null)
            {
                _lazyMap = PooledDictionary<long, int>.GetInstance();
                ordinal = 0;
            }
            else if (!_lazyMap.TryGetValue(key, out ordinal))
            {
                ordinal = 0;
            }

            _lazyMap[key] = ordinal + 1;
            Debug.Assert(ordinal == 0 || localKind != SynthesizedLocalKind.UserDefined);
            return ordinal;
        }
    }
}
