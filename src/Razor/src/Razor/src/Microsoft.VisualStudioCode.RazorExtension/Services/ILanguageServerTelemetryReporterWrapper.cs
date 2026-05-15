// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.VisualStudioCode.RazorExtension.Services;

internal interface ILanguageServerTelemetryReporterWrapper
{
    void ReportEvent(string name, List<KeyValuePair<string, object?>> properties);
}
