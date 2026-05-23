// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.VisualStudio.Razor.Telemetry;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.CodeAnalysis.Remote.Razor.Telemetry;

[Export(typeof(ITelemetryReporter)), Shared]
internal class OutOfProcessTelemetryReporter() : TelemetryReporter(TelemetryService.DefaultSession)
{
}
