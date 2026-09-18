// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Storage;

/// <summary>
/// Hands out the persistent storage of a solution: the one Roslyn's own indices are kept in.
/// </summary>
internal interface IFSharpChecksummedPersistentStorageService
{
    /// <summary>
    /// The storage of <paramref name="solution"/>. It keeps nothing when the solution has no file path, or its storage
    /// cannot be opened.
    /// </summary>
    ValueTask<IFSharpChecksummedPersistentStorage> GetStorageAsync(Solution solution, CancellationToken cancellationToken);
}
