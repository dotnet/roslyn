﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Represents a result of lookup operation over a 0 or 1 symbol (as opposed to a scope). The
    /// typical use is to represent that a particular symbol is good/bad/unavailable.
    /// 
    /// For more explanation of Kind, Symbol, Error - see LookupResult.
    /// </summary>
    internal struct SingleLookupResult
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
