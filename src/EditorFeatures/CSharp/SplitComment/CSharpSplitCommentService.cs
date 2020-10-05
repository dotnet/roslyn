// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.SplitComment;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.CSharp.SplitComment
{
    [ExportLanguageService(typeof(ISplitCommentService), LanguageNames.CSharp), Shared]
    internal class CSharpSplitCommentService : ISplitCommentService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpSplitCommentService()
        {
        }

        public string CommentStart => "//";

        public bool IsAllowed(SyntaxNode root, SyntaxTrivia trivia)
            => true;
    }
}
