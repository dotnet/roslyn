// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using PostSharp.Backstage.Extensibility;

namespace Caravela.Compiler.Licensing
{
    // TODO: IDiagnosticsSink needs to be generalized to provide all data for DiagnosticDescriptor
    internal class DiagnosticBagSink : IDiagnosticsSink
    {
        private readonly DiagnosticBag _diagnostics;

        public DiagnosticBagSink(DiagnosticBag diagnostics)
        {
            _diagnostics = diagnostics;
        }

        public void ReportWarning(string message, IDiagnosticsLocation? location = null)
            => this.AddDiagnostic(message, ErrorCode.WRN_LicensingMessage);

        public void ReportError(string message, IDiagnosticsLocation? location = null)
            => this.AddDiagnostic(message, ErrorCode.ERR_LicensingMessage);

        private void AddDiagnostic(string message, ErrorCode errorCode) =>
            _diagnostics.Add(Diagnostic.Create(CaravelaCompilerMessageProvider.Instance, (int)errorCode, message));
    }
}
