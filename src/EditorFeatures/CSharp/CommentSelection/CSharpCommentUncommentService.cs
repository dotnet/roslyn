// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.CSharp.CommentSelection
{
    [ExportLanguageService(typeof(ICommentUncommentService), LanguageNames.CSharp), Shared]
    internal class CSharpCommentUncommentService : AbstractCommentUncommentService
    {
        public override string SingleLineCommentString
        {
            get { return "//"; }
        }

        public override bool SupportsBlockComment
        {
            get { return true; }
        }

        public override string BlockCommentStartString
        {
            get { return "/*"; }
        }

        public override string BlockCommentEndString
        {
            get { return "*/"; }
        }
    }
}
