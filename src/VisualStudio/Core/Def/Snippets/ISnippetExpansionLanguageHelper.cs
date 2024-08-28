// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Snippets;

internal interface ISnippetExpansionLanguageHelper : ILanguageService
{
    Guid LanguageServiceGuid { get; }
    string FallbackDefaultLiteral { get; }

    bool TryGetSubjectBufferSpan(ITextView textView, ITextBuffer subjectBuffer, VsTextSpan surfaceBufferTextSpan, out SnapshotSpan subjectBufferSpan);
    ITrackingSpan? InsertEmptyCommentAndGetEndPositionTrackingSpan(IVsExpansionSession expansionSession, ITextView textView, ITextBuffer subjectBuffer);
    Document AddImports(Document document, AddImportPlacementOptions addImportOptions, SyntaxFormattingOptions formattingOptions, int position, XElement snippetNode, CancellationToken cancellationToken);
}
