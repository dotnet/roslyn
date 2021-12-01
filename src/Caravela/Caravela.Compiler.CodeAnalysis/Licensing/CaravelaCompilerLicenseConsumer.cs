// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using PostSharp.Backstage.Extensibility;
using PostSharp.Backstage.Licensing.Consumption;

namespace Caravela.Compiler.Licensing
{
    internal class CaravelaCompilerLicenseConsumer : ILicenseConsumer
    {
        public string? TargetTypeNamespace => null;

        /// <summary>
        /// Gets the name of the type requiring a licensed feature.
        /// </summary>
        public string? TargetTypeName => null;

        public IBackstageDiagnosticSink Diagnostics { get; }

        /// <summary>
        /// Gets <see cref="IDiagnosticsLocation" /> of the licensed feature request.
        /// </summary>
        public IDiagnosticsLocation? DiagnosticsLocation => null;

        public CaravelaCompilerLicenseConsumer(IBackstageDiagnosticSink diagnosticsSink)
        {
            Diagnostics = diagnosticsSink;
        }
    }
}
