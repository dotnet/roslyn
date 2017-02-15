// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;
using static System.Linq.ImmutableArrayExtensions;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class DiagnosticInfoWithSymbols : DiagnosticInfo
    {
        // not serialized:
        internal readonly ImmutableArray<Symbol> Symbols;

        internal DiagnosticInfoWithSymbols(ErrorCode errorCode, object[] arguments, ImmutableArray<Symbol> symbols)
            : base(CSharp.MessageProvider.Instance, (int)errorCode, arguments)
        {
            this.Symbols = symbols;
        }

        internal DiagnosticInfoWithSymbols(bool isWarningAsError, ErrorCode errorCode, object[] arguments, ImmutableArray<Symbol> symbols)
            : base(CSharp.MessageProvider.Instance, isWarningAsError, (int)errorCode, arguments)
        {
            this.Symbols = symbols;
        }

        public override bool Equals(object obj)
        {
            return obj is DiagnosticInfoWithSymbols diws && 
                   this.Symbols.SequenceEqual(diws.Symbols) &&
                   base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(Hash.CombineValues(Symbols), base.GetHashCode());
        }
    }
}
