// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class DiagnosticInfo
    {
        private class DiagnosticInfoWithOverridenSeverity : DiagnosticInfo
        {
            private readonly DiagnosticSeverity overriddenSeverity;

            // Only the compiler creates instances.
            internal DiagnosticInfoWithOverridenSeverity(DiagnosticInfo original, DiagnosticSeverity overriddenSeverity) :
                base (original.messageProvider, original.isWarningAsError, original.errorCode, original.Arguments)
            {
                this.overriddenSeverity = overriddenSeverity;
            }

            public override DiagnosticSeverity Severity
            {
                get
                {
                    return this.overriddenSeverity;
                }
            }
        }
    }
}
