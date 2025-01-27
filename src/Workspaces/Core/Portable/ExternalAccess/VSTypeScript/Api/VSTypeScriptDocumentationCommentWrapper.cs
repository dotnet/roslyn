// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

internal readonly struct VSTypeScriptDocumentationCommentWrapper(DocumentationComment underlyingObject)
{
    public static VSTypeScriptDocumentationCommentWrapper FromXmlFragment(string xml)
        => new(DocumentationComment.FromXmlFragment(xml));

    public bool IsDefault
        => underlyingObject == null;

    public string? SummaryTextOpt
        => underlyingObject?.SummaryText;

    public string? GetParameterTextOpt(string parameterName)
        => underlyingObject?.GetParameterText(parameterName);

}
