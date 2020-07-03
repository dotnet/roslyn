// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class FileLinePositionSpanExtensions
    {
        /// <summary>
        /// Get mapped file path if exist, otherwise return null.
        /// </summary>
        public static string? GetMappedFilePathIfExist(this FileLinePositionSpan fileLinePositionSpan)
            => fileLinePositionSpan.HasMappedPath ? fileLinePositionSpan.Path : null;
    }
}
