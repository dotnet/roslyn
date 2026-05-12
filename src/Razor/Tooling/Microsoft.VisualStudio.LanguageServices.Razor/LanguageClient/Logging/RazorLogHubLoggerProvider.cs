// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.VisualStudio.Razor.Logging;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Logging;

[ExportLoggerProvider]
[method: ImportingConstructor]
internal sealed class RazorLogHubLoggerProvider(RazorLogHubTraceProvider traceProvider) : ILoggerProvider
{
    private readonly RazorLogHubTraceProvider _traceProvider = traceProvider;

    public ILogger CreateLogger(string categoryName)
    {
        return new RazorLogHubLogger(categoryName, _traceProvider);
    }
}
