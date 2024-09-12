// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Host;

/// <summary>
/// TemporaryStorage can be used to read and write text to a temporary storage location.
/// </summary>
internal interface ITemporaryStorageWithName
{
    // TODO: clean up https://github.com/dotnet/roslyn/issues/43037
    // Name shouldn't be nullable.

    /// <summary>
    /// Get name of the temporary storage
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// Get offset of the temporary storage
    /// </summary>
    long Offset { get; }

    /// <summary>
    /// Get size of the temporary storage
    /// </summary>
    long Size { get; }
}
