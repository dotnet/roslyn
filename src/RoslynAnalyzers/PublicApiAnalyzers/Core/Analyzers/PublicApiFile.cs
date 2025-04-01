// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Microsoft.CodeAnalysis.PublicApiAnalyzers
{
#pragma warning disable CA1815 // Override equals and operator equals on value types
    public readonly struct PublicApiFile
#pragma warning restore CA1815 // Override equals and operator equals on value types
    {
        public PublicApiFile(string path, bool isPublic)
        {
            var fileName = Path.GetFileName(path);

            IsShipping = IsFile(fileName, isPublic ? DeclarePublicApiAnalyzer.PublicShippedFileNamePrefix : DeclarePublicApiAnalyzer.InternalShippedFileNamePrefix);
            var isUnshippedFile = IsFile(fileName, isPublic ? DeclarePublicApiAnalyzer.PublicUnshippedFileNamePrefix : DeclarePublicApiAnalyzer.InternalUnshippedFileNamePrefix);

            IsApiFile = IsShipping || isUnshippedFile;
        }

        public bool IsShipping { get; }

        public bool IsApiFile { get; }

        private static bool IsFile(string path, string prefix)
            => path.StartsWith(prefix, StringComparison.Ordinal) && path.EndsWith(DeclarePublicApiAnalyzer.Extension, StringComparison.Ordinal);
    }
}
