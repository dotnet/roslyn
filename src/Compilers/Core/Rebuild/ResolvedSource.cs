// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace BuildValidator
{
    public record ResolvedSource(
        string? OnDiskPath,
        SourceText SourceText,
        SourceFileInfo SourceFileInfo)
    {
        public string DisplayPath => OnDiskPath ?? ("[embedded]" + SourceFileInfo.SourceFilePath);
    }
}