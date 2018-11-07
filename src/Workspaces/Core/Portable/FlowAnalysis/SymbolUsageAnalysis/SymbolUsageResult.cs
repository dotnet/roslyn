// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis.SymbolUsageAnalysis
{
    internal sealed class SymbolUsageResult
    {
        public SymbolUsageResult(
            ImmutableDictionary<(ISymbol symbol, IOperation write), bool> symbolWritesMap,
            ImmutableHashSet<ISymbol> symbolsRead)
        {
            SymbolWritesMap = symbolWritesMap;
            SymbolsRead = symbolsRead;
        }

        /// <summary>
        /// Map from each symbol write to a boolean indicating if the value assinged
        /// at write is used/read on some control flow path.
        /// </summary>
        public ImmutableDictionary<(ISymbol symbol, IOperation write), bool> SymbolWritesMap { get; }

        /// <summary>
        /// Set of locals/parameters that are read at least once.
        /// </summary>
        public ImmutableHashSet<ISymbol> SymbolsRead { get; }

        public bool HasUnreadSymbolWrites()
        {
            foreach (var kvp in SymbolWritesMap)
            {
                if (!kvp.Value)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets symbol writes that have are never read.
        /// </summary>
        public IEnumerable<(ISymbol Symbol, IOperation WriteOperation)> GetUnreadSymbolWrites()
        {
            foreach (var kvp in SymbolWritesMap)
            {
                if (!kvp.Value)
                {
                    yield return kvp.Key;
                }
            }
        }

        /// <summary>
        /// Returns true if the initial value of the parameter from the caller is used.
        /// </summary>
        public bool IsInitialParameterValueUsed(IParameterSymbol parameter)
        {
            foreach (var kvp in SymbolWritesMap)
            {
                if (kvp.Key.write == null && kvp.Key.symbol == parameter)
                {
                    return kvp.Value;
                }
            }

            throw ExceptionUtilities.Unreachable;
        }

        /// <summary>
        /// Gets the write count for a given local/parameter symbol.
        /// </summary>
        public int GetSymbolWriteCount(ISymbol symbol)
        {
            int count = 0;
            foreach (var kvp in SymbolWritesMap)
            {
                if (kvp.Key.symbol == symbol)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
