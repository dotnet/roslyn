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

        public ImmutableDictionary<(ISymbol Symbol, IOperation Definition), bool> DefinitionUsageMap { get; }
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

        public bool GetInitialDefinitionUsageForParameter(IParameterSymbol parameter)
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
