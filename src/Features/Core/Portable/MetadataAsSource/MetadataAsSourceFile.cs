// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.MetadataAsSource;

internal sealed class MetadataAsSourceFile
{
    internal MetadataAsSourceFile(string filePath, Location identifierLocation, string documentTitle, string documentTooltip)
    {
        FilePath = filePath;
        IdentifierLocation = identifierLocation;
        DocumentTitle = documentTitle;
        DocumentTooltip = documentTooltip;
    }

    public string FilePath { get; }
    public Location IdentifierLocation { get; }
    public string DocumentTitle { get; }
    public string DocumentTooltip { get; }
}
