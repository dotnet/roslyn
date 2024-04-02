// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

namespace Microsoft.CommonLanguageServerProtocol.Framework;

#if BINARY_COMPAT // TODO - Remove with https://github.com/dotnet/roslyn/issues/72251
public abstract class AbstractTelemetryService
#else
internal abstract class AbstractTelemetryService
#endif
{
    public abstract AbstractRequestScope CreateRequestScope(string lspMethodName);
}
