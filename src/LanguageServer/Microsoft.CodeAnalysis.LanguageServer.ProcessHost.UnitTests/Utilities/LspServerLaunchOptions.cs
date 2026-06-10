// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.ProcessHost.UnitTests;

internal sealed record LspServerLaunchOptions
{
    public static LspServerLaunchOptions Default { get; } = new();

    public bool AutoLoadProjects { get; init; }
    public bool IncludeDevKitComponents { get; init; }
    public bool DebugLsp { get; init; }

    /// <summary>
    /// Whether the editor transport is a named pipe (the default) or stdio. In single-server mode the thin client
    /// forwards this choice to the dedicated server it launches.
    /// </summary>
    public bool UseNamedPipe { get; init; } = true;

    /// <summary>
    /// The process id forwarded as the editor (<c>--clientProcessId</c>) that the thin client and server monitor.
    /// Defaults to the current test process when not specified.
    /// </summary>
    public int? ClientProcessId { get; init; }

    /// <summary>
    /// Whether the thin client should run in daemon mode (<c>--daemon-mode</c>), connecting the editor transport to
    /// a shared daemon instead of launching a dedicated per-session server.
    /// </summary>
    public bool DaemonMode { get; init; }

    /// <summary>
    /// Overrides the daemon pipe name (and therefore which daemon a thin client discovers/launches). Clients that
    /// share a value share a daemon; distinct values use distinct daemons. Tests set this so each test scopes its
    /// own isolated daemon; it is ignored unless <see cref="DaemonMode"/> is set.
    /// </summary>
    public string? DaemonPipeName { get; init; }

    /// <summary>
    /// The keepalive window the launched daemon uses after its last client disconnects. Forwarded to the daemon via
    /// the keepalive environment variable; ignored unless <see cref="DaemonMode"/> is set. When unset the daemon
    /// uses its default.
    /// </summary>
    public TimeSpan? DaemonKeepAlive { get; init; }

    /// <summary>
    /// The process id sent in the LSP <c>initialize</c> request's <c>processId</c> field. In daemon mode the
    /// per-client server monitors this process and tears that client's server down (only) when it exits. Defaults
    /// to unset (no process monitored for that client).
    /// </summary>
    public int? InitializeProcessId { get; init; }
}
