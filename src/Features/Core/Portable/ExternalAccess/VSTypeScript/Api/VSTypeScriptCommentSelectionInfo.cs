// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CommentSelection;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

[Obsolete]
internal readonly struct VSTypeScriptCommentSelectionInfo
{
    internal readonly CommentSelectionInfo UnderlyingObject;

    internal VSTypeScriptCommentSelectionInfo(CommentSelectionInfo underlyingObject)
    {
        UnderlyingObject = underlyingObject;
    }

    public VSTypeScriptCommentSelectionInfo(
        bool supportsSingleLineComment,
        bool supportsBlockComment,
        string singleLineCommentString,
        string blockCommentStartString,
        string blockCommentEndString) : this(new(
            supportsSingleLineComment,
            supportsBlockComment,
            singleLineCommentString,
            blockCommentStartString,
            blockCommentEndString))
    {
    }
}
