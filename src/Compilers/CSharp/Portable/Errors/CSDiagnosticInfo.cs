// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class CSDiagnosticInfo : DiagnosticInfoWithSymbols
    {
        public static readonly DiagnosticInfo EmptyErrorInfo = new CSDiagnosticInfo(0);
        public static readonly DiagnosticInfo VoidDiagnosticInfo = new CSDiagnosticInfo(ErrorCode.Void);

        private readonly ImmutableArray<Location> _additionalLocations;

        internal CSDiagnosticInfo(ErrorCode code)
            : this(code, Array.Empty<object>(), ImmutableArray<Symbol>.Empty, ImmutableArray<Location>.Empty)
        {
        }

        internal CSDiagnosticInfo(ErrorCode code, params object[] args)
            : this(code, args, ImmutableArray<Symbol>.Empty, ImmutableArray<Location>.Empty)
        {
        }

        internal CSDiagnosticInfo(ErrorCode code, ImmutableArray<Symbol> symbols, object[] args)
            : this(code, args, symbols, ImmutableArray<Location>.Empty)
        {
        }

        internal CSDiagnosticInfo(ErrorCode code, object[] args, ImmutableArray<Symbol> symbols, ImmutableArray<Location> additionalLocations)
            : base(code, args, symbols)
        {
            // Internal errors are abnormal and should not occur except where there are bugs in the compiler.
            Debug.Assert(code != ErrorCode.ERR_InternalError);
            _additionalLocations = additionalLocations;
        }

        public override IReadOnlyList<Location> AdditionalLocations => _additionalLocations;

        internal new ErrorCode Code => (ErrorCode)base.Code;

        internal static bool IsEmpty(DiagnosticInfo info) => (object)info == EmptyErrorInfo;
    }
}
