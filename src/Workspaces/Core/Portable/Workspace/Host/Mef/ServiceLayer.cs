// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Host.Mef;

/// <summary>
/// The layer of an exported service.  
/// 
/// If there are multiple definitions of a service, the <see cref="ServiceLayer"/> is used to determine which is used.
/// </summary>
public static class ServiceLayer
{
    /// <summary>
    /// Service layer that overrides <see cref="Editor"/>, <see cref="Desktop"/> and <see cref="Default"/>.
    /// </summary>
    internal const string Test = nameof(Test);

    /// <summary>
    /// Service layer that overrides <see cref="Editor"/>, <see cref="Desktop"/> and <see cref="Default"/>.
    /// </summary>
    public const string Host = nameof(Host);

    /// <summary>
    /// Service layer that overrides <see cref="Desktop" /> and <see cref="Default"/>.
    /// </summary>
    public const string Editor = nameof(Editor);

    /// <summary>
    /// Service layer that overrides <see cref="Default"/>.
    /// </summary>
    public const string Desktop = nameof(Desktop);

    /// <summary>
    /// The base service layer.
    /// </summary>
    public const string Default = nameof(Default);
}
