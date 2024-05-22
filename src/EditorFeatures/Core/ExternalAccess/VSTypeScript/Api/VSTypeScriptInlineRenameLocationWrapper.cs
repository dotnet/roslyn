// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

internal readonly struct VSTypeScriptInlineRenameLocationWrapper(InlineRenameLocation underlyingObject)
{
    private readonly InlineRenameLocation _underlyingObject = underlyingObject;

    public Document Document => _underlyingObject.Document;
    public TextSpan TextSpan => _underlyingObject.TextSpan;
}
