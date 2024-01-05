// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

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

        protected DiagnosticInfoWithSymbols(DiagnosticInfoWithSymbols original, DiagnosticSeverity severity) : base(original, severity)
        {
            this.Symbols = original.Symbols;
        }

        protected override DiagnosticInfo GetInstanceWithSeverityCore(DiagnosticSeverity severity)
        {
            return new DiagnosticInfoWithSymbols(this, severity);
        }
    }
}
