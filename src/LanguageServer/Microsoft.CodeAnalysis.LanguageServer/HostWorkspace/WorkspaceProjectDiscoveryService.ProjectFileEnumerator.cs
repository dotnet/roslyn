// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO.Enumeration;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal sealed partial class WorkspaceProjectDiscoveryService
{
    private sealed class ProjectFileEnumerator(string directory, ImmutableArray<string> supportedExtensions)
        : FileSystemEnumerator<string>(directory, new EnumerationOptions { RecurseSubdirectories = false, IgnoreInaccessible = true })
    {
        protected override bool ShouldIncludeEntry(ref FileSystemEntry entry)
            => !entry.IsDirectory && IsSupportedExtension(Path.GetExtension(entry.FileName));

        protected override string TransformEntry(ref FileSystemEntry entry)
            => entry.ToFullPath();

        protected override bool ShouldRecurseIntoEntry(ref FileSystemEntry entry)
            => throw ExceptionUtilities.Unreachable();

        private bool IsSupportedExtension(ReadOnlySpan<char> extensionWithDot)
        {
            if (extensionWithDot is not ['.', .. var extension])
                return false;

            foreach (var supported in supportedExtensions)
            {
                if (extension.Equals(supported, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
