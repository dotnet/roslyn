// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is consumed as 'generated' code in a source package and therefore requires an explicit nullable enable
#nullable enable

using System;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

/// <summary>
/// Thrown when an operation requires the language server to have been asked to shut down,
/// but shutdown has not yet been initiated or completed.
/// </summary>
internal sealed class ServerNotShutDownException : InvalidOperationException
{
    public ServerNotShutDownException(string message)
        : base(message)
    {
    }
}
