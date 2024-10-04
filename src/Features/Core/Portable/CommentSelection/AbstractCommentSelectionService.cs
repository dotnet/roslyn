// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CommentSelection;

internal abstract class AbstractCommentSelectionService : ICommentSelectionService
{
    public abstract string BlockCommentEndString { get; }
    public abstract string BlockCommentStartString { get; }
    public abstract string SingleLineCommentString { get; }
    public abstract bool SupportsBlockComment { get; }

    public CommentSelectionInfo GetInfo()
        => SupportsBlockComment
            ? new(supportsSingleLineComment: true, SupportsBlockComment, SingleLineCommentString, BlockCommentStartString, BlockCommentEndString)
            : new(supportsSingleLineComment: true, SupportsBlockComment, SingleLineCommentString, blockCommentStartString: "", blockCommentEndString: "");
}
