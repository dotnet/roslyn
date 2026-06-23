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
}