// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.Client;

internal static class ExitCodes
{
    public const int Success = 0;

    // Exit code scheme:
    // 1: the server/daemon connection closed before the editor connection.
    // 2: the editor connection closed, or the monitored editor process exited.
    // 3: failed to resolve, launch, or connect to the server/daemon.
    // 4: invalid thin-client command-line arguments.
    public const int ServerConnectionLost = 1;
    public const int EditorConnectionLost = 2;
    public const int ServerLaunchOrConnectFailure = 3;
    public const int BadArguments = 4;
}
