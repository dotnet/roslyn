// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CommentSelection;

internal readonly struct CommentSelectionInfo
{
    public CommentSelectionInfo(bool supportsSingleLineComment, bool supportsBlockComment, string singleLineCommentString, string blockCommentStartString, string blockCommentEndString) : this()
    {
        SupportsSingleLineComment = supportsSingleLineComment;
        SupportsBlockComment = supportsBlockComment;
        SingleLineCommentString = singleLineCommentString;
        BlockCommentStartString = blockCommentStartString;
        BlockCommentEndString = blockCommentEndString;
    }

    public bool SupportsSingleLineComment { get; }
    public bool SupportsBlockComment { get; }

    public string SingleLineCommentString { get; }

    public string BlockCommentStartString { get; }
    public string BlockCommentEndString { get; }
}
