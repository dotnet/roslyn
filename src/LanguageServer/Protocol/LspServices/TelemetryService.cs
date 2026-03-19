// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class TelemetryService : AbstractTelemetryService, ILspService
{
    private readonly RequestTelemetryLogger _requestTelemetryLogger;

    public TelemetryService(ILspServices lspServices)
    {
        var requestTelemetryLogger = lspServices.GetRequiredService<RequestTelemetryLogger>();

        _requestTelemetryLogger = requestTelemetryLogger;
    }

    public override AbstractRequestScope CreateRequestScope(string lspMethodName)
    {
        return new RequestTelemetryScope(lspMethodName, _requestTelemetryLogger);
    }
}
