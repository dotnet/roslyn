// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class DiagnosticInfoWithSymbols : DiagnosticInfo
    {
        // not serialized:
        internal readonly ImmutableArray<Symbol> Symbols;

        internal DiagnosticInfoWithSymbols(ErrorCode errorCode, object[] arguments, ImmutableArray<Symbol> symbols)
            : base(CSharp.MessageProvider.Instance, (int)errorCode, arguments)
        {
#if DEBUG
            AssertArguments(arguments);
#endif
            this.Symbols = symbols;
        }

        internal DiagnosticInfoWithSymbols(bool isWarningAsError, ErrorCode errorCode, object[] arguments, ImmutableArray<Symbol> symbols)
            : base(CSharp.MessageProvider.Instance, isWarningAsError, (int)errorCode, arguments)
        {
#if DEBUG
            AssertArguments(arguments);
#endif
            this.Symbols = symbols;
        }

#if DEBUG
        private static void AssertArguments(object[] arguments)
        {
            if (arguments != null)
            {
                foreach (var argument in arguments)
                {
                    Debug.Assert(!(argument is SymbolWithAnnotations));
                }
            }
        }
#endif 
    }
}
