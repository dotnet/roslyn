// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CommentSelection;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.CSharp.CommentSelection
{
    [ExportLanguageService(typeof(ICommentSelectionService), LanguageNames.CSharp), Shared]
    internal class CSharpCommentSelectionService : AbstractCommentSelectionService
    {
        [ImportingConstructor]
        public CSharpCommentSelectionService()
        {
        }

        public override string SingleLineCommentString => "//";
        public override bool SupportsBlockComment => true;
        public override string BlockCommentStartString => "/*";
        public override string BlockCommentEndString => "*/";
    }
}
