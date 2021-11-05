// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using PostSharp.Backstage.Extensibility;

namespace Caravela.Compiler.Licensing
{
    internal class LicensingDiagnosticsSink : IDiagnosticsSink
    {
        private readonly List<Diagnostic> _diagnostics = new();
        
        public void ReportWarning(string message, IDiagnosticsLocation? location = null)
            => this.AddDiagnostic(message, ErrorCode.WRN_LicensingMessage);

        public void ReportError(string message, IDiagnosticsLocation? location = null)
            => this.AddDiagnostic(message, ErrorCode.ERR_LicensingMessage);

        private void AddDiagnostic(string message, ErrorCode errorCode) =>
            _diagnostics.Add(Diagnostic.Create(CaravelaCompilerMessageProvider.Instance, (int)errorCode, message));

        public IEnumerable<Diagnostic> GetDiagnostics() => _diagnostics;
    }
}
