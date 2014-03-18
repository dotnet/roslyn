// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    [Serializable]
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

        // Create a copy of this instance with a WarningAsError flag
        internal override DiagnosticInfo GetInstanceWithReportWarning(bool isWarningAsError)
        {
            return new DiagnosticInfoWithSymbols(isWarningAsError, (ErrorCode)this.Code, this.Arguments, this.Symbols);
        }

        #region Serialization

        protected DiagnosticInfoWithSymbols(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            // symbols are not exposed publicly and thus not serialized
        }

        protected override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);

            // symbols are not exposed publicly and thus not serialized
        }

        #endregion
    }
}