// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.MSBuild;

internal interface IBinLogPathProvider
{
    /// <summary>
    /// Returns a new log path. Each call will return a new name, so that way we don't have collisions if multiple processes are writing to different logs.
    /// </summary>
    /// <returns>A new path, or null if no logging is currently wanted. An instance is allowed to switch between return null and returning non-null if
    /// the user changes configuration.</returns>
    string? GetNewLogPath();
}
