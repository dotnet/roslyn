// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis.ReachingDefinitions
{
    internal sealed class DefinitionUsageResult
    {
        public DefinitionUsageResult(
            ImmutableDictionary<(ISymbol Symbol, IOperation Definition), bool> definitionUsageMap,
            ImmutableHashSet<ISymbol> symbolsRead)
        {
            DefinitionUsageMap = definitionUsageMap;
            SymbolsRead = symbolsRead;
        }

        /// <summary>
        /// Map from each symbol definition to a boolean indicating if the value assinged
        /// at definition is used/read on some control flow path.
        /// </summary>
        public ImmutableDictionary<(ISymbol Symbol, IOperation Definition), bool> DefinitionUsageMap { get; }

        /// <summary>
        /// Set of locals/parameters that have at least one use/read for one of its definitions.
        /// </summary>
        public ImmutableHashSet<ISymbol> SymbolsRead { get; }

        public bool HasUnusedDefinitions()
        {
            if (DefinitionUsageMap.IsEmpty)
            {
                return false;
            }

            foreach (var kvp in DefinitionUsageMap)
            {
                if (!kvp.Value)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets symbol definitions (writes) that have are never read.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<(ISymbol Symbol, IOperation Definition)> GetUnusedDefinitions()
        {
            foreach (var kvp in DefinitionUsageMap)
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
            foreach (var kvp in DefinitionUsageMap)
            {
                if (kvp.Key.Definition == null && kvp.Key.Symbol == parameter)
                {
                    return kvp.Value;
                }
            }

            throw ExceptionUtilities.Unreachable;
        }

        /// <summary>
        /// Gets the definition (write) count for a given local/parameter symbol.
        /// </summary>
        public int GetDefinitionCount(ISymbol symbol)
        {
            int count = 0;
            foreach (var kvp in DefinitionUsageMap)
            {
                if (kvp.Key.Symbol == symbol)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
