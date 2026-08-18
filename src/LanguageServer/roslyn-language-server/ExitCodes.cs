// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.Client;

internal static class ExitCodes
{
    /// <summary>The thin client and its language server session shut down cleanly.</summary>
    public const int Success = 0;

    /// <summary>The server or daemon connection closed before the editor connection.</summary>
    public const int ServerConnectionLost = 1;

    /// <summary>The editor connection closed, or the monitored editor process exited.</summary>
    public const int EditorConnectionLost = 2;

    /// <summary>The thin client failed to resolve, launch, or connect to the server or daemon.</summary>
    public const int ServerLaunchOrConnectFailure = 3;

    /// <summary>The thin client received invalid command-line arguments.</summary>
    public const int BadArguments = 4;

    /// <summary>The bootstrap launched a daemon, but it did not become ready before the startup timeout.</summary>
    public const int DaemonReadyTimeout = 5;
}
