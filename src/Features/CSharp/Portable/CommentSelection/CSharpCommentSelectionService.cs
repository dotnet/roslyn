// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CommentSelection;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.CSharp.CommentSelection;

[ExportLanguageService(typeof(ICommentSelectionService), LanguageNames.CSharp), Shared]
internal class CSharpCommentSelectionService : AbstractCommentSelectionService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpCommentSelectionService()
    {
    }

    public override string SingleLineCommentString => "//";
    public override bool SupportsBlockComment => true;
    public override string BlockCommentStartString => "/*";
    public override string BlockCommentEndString => "*/";
}
