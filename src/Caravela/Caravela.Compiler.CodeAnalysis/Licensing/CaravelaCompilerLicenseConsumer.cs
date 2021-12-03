// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using PostSharp.Backstage.Extensibility;
using PostSharp.Backstage.Licensing.Consumption;

namespace Caravela.Compiler.Licensing
{
    /// <summary>
    /// General <see cref="ILicenseConsumer"/> for the Caravela Compiler
    /// reporting diagnotics via the give <see cref="IBackstageDiagnosticSink"/>. 
    /// </summary>
    internal class CaravelaCompilerLicenseConsumer : ILicenseConsumer
    {
        /// <inheritdoc />
        public string? TargetTypeNamespace => null;

        /// <inheritdoc />
        public string? TargetTypeName => null;

        /// <inheritdoc />
        public IBackstageDiagnosticSink Diagnostics { get; }

        /// <inheritdoc />
        public IDiagnosticsLocation? DiagnosticsLocation => null;

        /// <summary>
        /// Initializes a new instance of the <see cref="CaravelaCompilerLicenseConsumer"/> class.
        /// </summary>
        /// <param name="diagnosticsSink">Diagnostics sink.</param>
        public CaravelaCompilerLicenseConsumer(IBackstageDiagnosticSink diagnosticsSink)
        {
            Diagnostics = diagnosticsSink;
        }
    }
}
