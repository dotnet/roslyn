// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.CommentSelection
{
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
}
