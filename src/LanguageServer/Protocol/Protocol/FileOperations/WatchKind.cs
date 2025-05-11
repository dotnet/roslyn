// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// File watch events of interest to a <see cref="FileSystemWatcher"/>
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#watchKind">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
[Flags]
internal enum WatchKind
{
    /// <summary>
    /// Interested in create events.
    /// </summary>
    Create = 1,

    /// <summary>
    /// Interested in change events
    /// </summary>
    Change = 2,

    /// <summary>
    /// Interested in delete events
    /// </summary>
    Delete = 4,
}
