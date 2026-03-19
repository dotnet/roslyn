// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Describes a manifest resource specification stored in command line arguments.
/// </summary>
public readonly struct CommandLineResource
{
    /// <summary>
    /// Name of the manifest resource as it appears in metadata.
    /// </summary>
    public string ResourceName { get; }

    /// <summary>
    /// Full path to the resource content file.
    /// </summary>
    public string FullPath { get; }

    /// <summary>
    /// Accessibility of the resource.
    /// </summary>
    public bool IsPublic { get; }

    /// <summary>
    /// File name of a linked resource, or null if the resource is embedded.
    /// </summary>
    public string? LinkedResourceFileName { get; }

    internal CommandLineResource(string resourceName, string fullPath, string? linkedResourceFileName, bool isPublic)
    {
        Debug.Assert(!resourceName.IsEmpty());
        Debug.Assert(PathUtilities.IsAbsolute(fullPath));

        ResourceName = resourceName;
        FullPath = fullPath;
        LinkedResourceFileName = linkedResourceFileName;
        IsPublic = isPublic;
    }

    /// <summary>
    /// True if the resource is embedded.
    /// </summary>
    public bool IsEmbedded
        => LinkedResourceFileName == null;

    /// <summary>
    /// True if the resource is linked.
    /// </summary>
    [MemberNotNullWhen(true, nameof(LinkedResourceFileName))]
    public bool IsLinked
        => LinkedResourceFileName != null;

    /// <summary>
    /// Creates <see cref="ResourceDescription"/> for this resource.
    /// </summary>
    internal ResourceDescription ToDescription()
    {
        // fail fast if the method is called on default(CommandLineResource)
        var fullPath = FullPath ?? throw new NullReferenceException();

        Func<Stream> dataProvider = () =>
        {
            // Use FileShare.ReadWrite because the file could be opened by the current process.
            // For example, it is an XML doc file produced by the build.
            return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        };

        return new ResourceDescription(ResourceName, LinkedResourceFileName, dataProvider, IsPublic, isEmbedded: IsEmbedded, checkArgs: false);
    }
}
