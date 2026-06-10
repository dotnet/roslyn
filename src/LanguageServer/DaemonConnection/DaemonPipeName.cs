// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace Microsoft.CodeAnalysis.LanguageServer.Daemon;

/// <summary>
/// Computes the named-pipe and mutex names used to discover and connect to a shared language
/// server daemon. Only identity/version-compatible clients connect to a given daemon.
/// <para>
/// This file is source-shared (linked) into both the thin client and the language server, so
/// they must remain dependency-light and AOT/trim-safe (no reflection).
/// </para>
/// </summary>
internal static class DaemonPipeName
{
    private const string GlobalMutexPrefix = "Global\\";

    /// <summary>
    /// Optional environment variable that, when set, is used verbatim as the daemon pipe name instead of the
    /// value derived from the tool identifier. This lets independent instances run isolated daemons that don't
    /// share state (primarily so end-to-end tests can scope a daemon to a single test, but also usable for
    /// advanced scenarios that deliberately want a separate daemon). Normal clients leave it unset so that only
    /// version-compatible clients share a daemon.
    /// </summary>
    public const string PipeNameOverrideEnvironmentVariable = "ROSLYN_LANGUAGE_SERVER_DAEMON_PIPE_NAME";

    /// <summary>
    /// Computes the pipe name for the current user, scoped by <paramref name="toolIdentifier"/>.
    /// </summary>
    public static string GetPipeName(string toolIdentifier)
    {
        // Prefix with username and elevation so different users / elevation levels don't share a daemon.
        var isAdmin = false;
        if (OperatingSystem.IsWindows())
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        return GetPipeName(Environment.UserName, isAdmin, toolIdentifier);
    }

    /// <summary>
    /// Computes the pipe name from the user identity and a tool identifier. The
    /// <paramref name="toolIdentifier"/> ensures only compatible clients connect to a compatible
    /// server; we use the full path to the server executable (in a versioned location).
    /// </summary>
    public static string GetPipeName(string userName, bool isAdmin, string toolIdentifier)
    {
        // Normalize away trailing separators and casing so we don't spin up multiple daemons for
        // identifiers that differ only cosmetically.
        toolIdentifier = toolIdentifier.TrimEnd(Path.DirectorySeparatorChar);
        toolIdentifier = toolIdentifier.ToLowerInvariant();

        var pipeNameInput = $"{userName}.{isAdmin}.{toolIdentifier}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(pipeNameInput));
        return Convert.ToBase64String(bytes)
            .Replace("/", "_")
            .Replace("=", string.Empty);
    }

    /// <summary>
    /// Name of the mutex held by the daemon for its entire lifetime; its existence means a daemon
    /// is running for this pipe.
    /// </summary>
    public static string GetServerMutexName(string pipeName)
        => $"{GlobalMutexPrefix}{pipeName}.server";

    /// <summary>
    /// Name of the mutex briefly acquired by a connecting client to serialize the
    /// check-server-then-launch sequence so two clients can't race to start two daemons.
    /// </summary>
    public static string GetClientMutexName(string pipeName)
        => $"{GlobalMutexPrefix}{pipeName}.client";
}
