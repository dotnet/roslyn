// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.MetadataAsSource;

internal sealed class MetadataAsSourceFile
{
    private readonly string _filePath;
    private readonly Location _identifierLocation;
    private readonly string _documentTitle;
    private readonly string _documentTooltip;

    internal MetadataAsSourceFile(string filePath, Location identifierLocation, string documentTitle, string documentTooltip)
    {
        _filePath = filePath;
        _identifierLocation = identifierLocation;
        _documentTitle = documentTitle;
        _documentTooltip = documentTooltip;
    }

    public string FilePath => _filePath;
    public Location IdentifierLocation => _identifierLocation;
    public string DocumentTitle => _documentTitle;
    public string DocumentTooltip => _documentTooltip;
}
