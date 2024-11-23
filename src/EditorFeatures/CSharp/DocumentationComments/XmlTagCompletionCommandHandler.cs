// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.DocumentationComments;

[Export(typeof(ICommandHandler))]
[ContentType(ContentTypeNames.CSharpContentType)]
[Name(nameof(XmlTagCompletionCommandHandler))]
[Order(Before = PredefinedCompletionNames.CompletionCommandHandler)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class XmlTagCompletionCommandHandler(ITextUndoHistoryRegistry undoHistory) : AbstractXmlTagCompletionCommandHandler<
    XmlNameSyntax,
    XmlTextSyntax,
    XmlElementSyntax,
    XmlElementStartTagSyntax,
    XmlElementEndTagSyntax,
    DocumentationCommentTriviaSyntax>(undoHistory)
{
    protected override XmlElementStartTagSyntax GetStartTag(XmlElementSyntax xmlElement)
        => xmlElement.StartTag;

    protected override XmlElementEndTagSyntax GetEndTag(XmlElementSyntax xmlElement)
        => xmlElement.EndTag;

    protected override XmlNameSyntax GetName(XmlElementEndTagSyntax endTag)
        => endTag.Name;

    protected override XmlNameSyntax GetName(XmlElementStartTagSyntax startTag)
        => startTag.Name;

    protected override SyntaxToken GetLocalName(XmlNameSyntax name)
        => name.LocalName;
}
