// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    [Serializable]
    internal sealed class CSDiagnosticInfo : DiagnosticInfoWithSymbols
    {
        public static readonly DiagnosticInfo EmptyErrorInfo = new CSDiagnosticInfo(0);
        public static readonly DiagnosticInfo VoidDiagnosticInfo = new CSDiagnosticInfo(ErrorCode.Void);

        private readonly ImmutableArray<Location> additionalLocations;

        internal CSDiagnosticInfo(ErrorCode code)
            : this(code, SpecializedCollections.EmptyArray<object>(), ImmutableArray<Symbol>.Empty, ImmutableArray<Location>.Empty)
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
            this.additionalLocations = additionalLocations;
        }

        private CSDiagnosticInfo(bool isWarningAsError, ErrorCode code, object[] args, ImmutableArray<Symbol> symbols, ImmutableArray<Location> additionalLocations)
            : base(isWarningAsError, code, args, symbols)
        {
            this.additionalLocations = additionalLocations;
        }

        // Create a copy of this instance with a WarningAsError flag
        internal override DiagnosticInfo GetInstanceWithReportWarning(bool isWarningAsError)
        {
            return new CSDiagnosticInfo(isWarningAsError, this.Code, this.Arguments, this.Symbols, this.additionalLocations);
        }

        #region Serialization

        private CSDiagnosticInfo(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            additionalLocations = info.GetArray<Location>("additionalLocations");
        }

        protected override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddArray("additionalLocations", additionalLocations);
        }

        #endregion

        public override IReadOnlyList<Location> AdditionalLocations
        {
            get
            {
                return additionalLocations;
            }
        }

        internal new ErrorCode Code
        {
            get
            {
                return (ErrorCode)base.Code;
            }
        }
    }
}
