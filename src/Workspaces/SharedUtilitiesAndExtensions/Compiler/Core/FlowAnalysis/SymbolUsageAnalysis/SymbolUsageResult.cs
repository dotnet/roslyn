// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis.SymbolUsageAnalysis;

internal readonly struct SymbolUsageResult(
    ImmutableDictionary<(ISymbol symbol, IOperation write), bool> symbolWritesMap,
    ImmutableHashSet<ISymbol> symbolsRead)
{

    /// <summary>
    /// Map from each symbol write to a boolean indicating if the value assinged
    /// at write is used/read on some control flow path.
    /// For example, consider the following code:
    /// <code>
    ///     int x = 0;
    ///     x = 1;
    ///     Console.WriteLine(x);
    /// </code>
    /// This map will have two entries for 'x':
    ///     1. Key = (symbol: x, write: 'int x = 0')
    ///        Value = 'false', because value assigned to 'x' here **is never** read. 
    ///     2. Key = (symbol: x, write: 'x = 1')
    ///        Value = 'true', because value assigned to 'x' here **may be** read on
    ///        some control flow path.
    /// </summary>
    public ImmutableDictionary<(ISymbol symbol, IOperation write), bool> SymbolWritesMap { get; } = symbolWritesMap;

    /// <summary>
    /// Set of locals/parameters that are read at least once.
    /// </summary>
    public ImmutableHashSet<ISymbol> SymbolsRead { get; } = symbolsRead;

    public bool HasUnreadSymbolWrites()
        => SymbolWritesMap.Values.Any(value => !value);

    /// <summary>
    /// Gets symbol writes that have are never read.
    /// WriteOperation will be null for the initial value write to parameter symbols from the callsite.
    /// </summary>
    public IEnumerable<(ISymbol Symbol, IOperation WriteOperation)> GetUnreadSymbolWrites()
        => SymbolWritesMap.Where(kvp => !kvp.Value).Select(kvp => kvp.Key);

    /// <summary>
    /// Returns true if the initial value of the parameter from the caller is used.
    /// </summary>
    public bool IsInitialParameterValueUsed(IParameterSymbol parameter)
    {
        foreach (var kvp in SymbolWritesMap)
        {
            // 'write' operation is 'null' for the initial write of the parameter,
            // which may be from the caller's argument or parameter initializer.
            if (kvp.Key.write == null && Equals(kvp.Key.symbol, parameter))
            {
                return kvp.Value;
            }
        }

        throw ExceptionUtilities.Unreachable();
    }

    /// <summary>
    /// Gets the write count for a given local/parameter symbol.
    /// </summary>
    public int GetSymbolWriteCount(ISymbol symbol)
    {
        var count = 0;
        foreach (var kvp in SymbolWritesMap)
        {
            if (Equals(kvp.Key.symbol, symbol))
            {
                count++;
            }
        }

        return count;
    }
}
