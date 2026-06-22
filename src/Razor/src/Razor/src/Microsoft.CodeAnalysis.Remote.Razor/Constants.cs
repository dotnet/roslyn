// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Remote.Razor;

internal static class Constants
{
    /// <summary>
    /// The name we use for the "server" in cohosting, which is not really an LSP server, but we use it for telemetry to distinguish events
    /// </summary>
    public const string ExternalAccessServerName = "Razor.ExternalAccess";
}
