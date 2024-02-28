// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host.Mef;

/// <summary>
/// MEF metadata class used to find exports declared for a specific file extensions.
/// </summary>
internal class FileExtensionsMetadata
{
    public IEnumerable<string> Extensions { get; }

    public FileExtensionsMetadata(IDictionary<string, object> data)
        => this.Extensions = ((IReadOnlyDictionary<string, object>)data).GetEnumerableMetadata<string>("Extensions");

    public FileExtensionsMetadata(params string[] extensions)
    {
        if (extensions?.Length == 0)
        {
            throw new ArgumentException(nameof(extensions));
        }

        this.Extensions = extensions;
    }
}
