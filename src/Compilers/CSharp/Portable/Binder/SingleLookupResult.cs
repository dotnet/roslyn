// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents a result of lookup operation over a 0 or 1 symbol (as opposed to a scope). The
    /// typical use is to represent that a particular symbol is good/bad/unavailable.
    /// 
    /// For more explanation of Kind, Symbol, Error - see LookupResult.
    /// </summary>
    internal readonly struct SingleLookupResult
    {
        // the kind of result.
        internal readonly LookupResultKind Kind;

        // the symbol or null.
        internal readonly Symbol Symbol;

        // the error of the result, if it is NonViable or Inaccessible
        internal readonly DiagnosticInfo Error;

        internal SingleLookupResult(LookupResultKind kind, Symbol symbol, DiagnosticInfo error)
        {
            this.Kind = kind;
            this.Symbol = symbol;
            this.Error = error;
        }
    }
}
