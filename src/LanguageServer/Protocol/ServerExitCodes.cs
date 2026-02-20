// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Exit codes used by the language server to indicate different termination reasons.
/// </summary>
internal static class ServerExitCodes
{
    /// <summary>
    /// The server is exiting because the client process has exited.
    /// This occurs when the server is monitoring a client process (via the processId 
    /// provided in the initialize request) and that process exits, or when we encounter
    /// issues while trying to determine if the client process is still running.
    /// </summary>
    public const int ClientProcessExited = 1;
}
