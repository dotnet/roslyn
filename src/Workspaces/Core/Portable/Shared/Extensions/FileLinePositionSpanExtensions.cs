// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class FileLinePositionSpanExtensions
    {
        /// <summary>
        /// Get mapped file path if exist, otherwise return null.
        /// </summary>
        public static string? GetMappedFilePathIfExist(this FileLinePositionSpan fileLinePositionSpan)
        {
            return fileLinePositionSpan.HasMappedPath ? fileLinePositionSpan.Path : null;
        }
    }
}
