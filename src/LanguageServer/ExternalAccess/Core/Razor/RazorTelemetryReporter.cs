// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;

internal abstract class RazorTelemetryReporter : AbstractRazorLspService
{
    private ILanguageServerTelemetryReporterWrapper? _wrapper;

    internal void Initialize(ILanguageServerTelemetryReporterWrapper wrapper)
    {
        _wrapper = wrapper;
    }

    public void ReportEvent(string name, List<KeyValuePair<string, object?>> properties)
    {
        _wrapper?.ReportEvent(name, properties);
    }
}
