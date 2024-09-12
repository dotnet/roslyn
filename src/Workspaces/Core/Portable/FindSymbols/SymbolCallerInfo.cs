// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.FindSymbols;

/// <summary>
/// Contains information about a call from one symbol to another.  The symbol making the call is
/// stored in CallingSymbol and the symbol that the call was made to is stored in CalledSymbol.
/// Whether or not the call is direct or indirect is also stored.  A direct call is a call that
/// does not go through any other symbols in the inheritance hierarchy of CalledSymbol, while an
/// indirect call does go through the inheritance hierarchy.  For example, calls through a base
/// member that this symbol overrides, or through an interface member that this symbol
/// implements will be considered 'indirect'. 
/// </summary>
public readonly struct SymbolCallerInfo
{
    /// <summary>
    /// The symbol that is calling the symbol being called.
    /// </summary>
    public ISymbol CallingSymbol { get; }

    /// <summary>
    /// The locations inside the calling symbol where the called symbol is referenced.
    /// </summary>
    public IEnumerable<Location> Locations { get; }

    /// <summary>
    /// The symbol being called.
    /// </summary>
    public ISymbol CalledSymbol { get; }

    /// <summary>
    /// True if the CallingSymbol is directly calling CalledSymbol.  False if it is calling a
    /// symbol in the inheritance hierarchy of the CalledSymbol.  For example, if the called
    /// symbol is a class method, then an indirect call might be through an interface method that
    /// the class method implements.
    /// </summary>
    public bool IsDirect { get; }

    internal SymbolCallerInfo(
        ISymbol callingSymbol,
        ISymbol calledSymbol,
        IEnumerable<Location> locations,
        bool isDirect)
    {
        CallingSymbol = callingSymbol;
        CalledSymbol = calledSymbol;
        this.IsDirect = isDirect;
        this.Locations = locations;
    }
}
