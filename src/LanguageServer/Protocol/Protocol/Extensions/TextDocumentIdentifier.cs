// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

internal partial class TextDocumentIdentifier
{
    public override string ToString()
    {
        var result = Uri?.UriString ?? string.Empty;
        if (ProjectContext is not null)
            result += "|" + ProjectContext.Label + "|" + ProjectContext.Id;

        return result;
    }
}
