// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Snippets;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Snippets;

using static CSharpSyntaxTokens;

[ExportLanguageService(typeof(ISnippetExpansionLanguageHelper), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpSnippetExpansionLanguageHelper(
    IThreadingContext threadingContext)
    : AbstractSnippetExpansionLanguageHelper(threadingContext)
{
    public override Guid LanguageServiceGuid => Guids.CSharpLanguageServiceId;
    public override string FallbackDefaultLiteral => "default";

    /// <returns>The tracking span of the inserted "/**/" if there is an $end$ location, null
    /// otherwise.</returns>
    public override ITrackingSpan? InsertEmptyCommentAndGetEndPositionTrackingSpan(IVsExpansionSession expansionSession, ITextView textView, ITextBuffer subjectBuffer)
    {
        RoslynDebug.AssertNotNull(expansionSession);

        var endSpanInSurfaceBuffer = new VsTextSpan[1];
        if (expansionSession.GetEndSpan(endSpanInSurfaceBuffer) != VSConstants.S_OK)
        {
            return null;
        }

        if (!TryGetSubjectBufferSpan(textView, subjectBuffer, endSpanInSurfaceBuffer[0], out var subjectBufferEndSpan))
        {
            return null;
        }

        var endPosition = subjectBufferEndSpan.Start.Position;

        var commentString = "/**/";
        subjectBuffer.Insert(endPosition, commentString);

        var commentSpan = new Span(endPosition, commentString.Length);
        return subjectBuffer.CurrentSnapshot.CreateTrackingSpan(commentSpan, SpanTrackingMode.EdgeExclusive);
    }

    public override async Task<Document> AddImportsAsync(
        Document document,
        AddImportPlacementOptions addImportOptions,
        SyntaxFormattingOptions formattingOptions,
        int position,
        XElement snippetNode,
        CancellationToken cancellationToken)
    {
        var importsNode = snippetNode.Element(XName.Get("Imports", snippetNode.Name.NamespaceName));
        if (importsNode == null || !importsNode.HasElements)
            return document;

        var root = document.GetRequiredSyntaxRootSynchronously(cancellationToken);
        var contextLocation = root.FindToken(position).GetRequiredParent();

        var newUsingDirectives = GetUsingDirectivesToAdd(contextLocation, snippetNode, importsNode);
        if (newUsingDirectives.Count == 0)
            return document;

        // In Venus/Razor, inserting imports statements into the subject buffer does not work.
        // Instead, we add the imports through the contained language host.
        if (TryAddImportsToContainedDocument(document, newUsingDirectives.SelectAsArray(u => u.Alias == null, u => u.Name!.ToString())))
            return document;

        var addImportService = document.GetRequiredLanguageService<IAddImportsService>();
        var generator = document.GetRequiredLanguageService<SyntaxGenerator>();
        var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(true);
        var newRoot = addImportService.AddImports(compilation, root, contextLocation, newUsingDirectives, generator, addImportOptions, cancellationToken);

        var newDocument = document.WithSyntaxRoot(newRoot);

        var formattedDocument = await Formatter.FormatAsync(newDocument, Formatter.Annotation, formattingOptions, cancellationToken).ConfigureAwait(true);
        await document.Project.Solution.Workspace.ApplyDocumentChangesAsync(
            this.ThreadingContext, formattedDocument, cancellationToken).ConfigureAwait(true);

        return formattedDocument;
    }

    private static List<UsingDirectiveSyntax> GetUsingDirectivesToAdd(
        SyntaxNode contextLocation, XElement snippetNode, XElement importsNode)
    {
        var namespaceXmlName = XName.Get("Namespace", snippetNode.Name.NamespaceName);
        var existingUsings = contextLocation.GetEnclosingUsingDirectives();
        var newUsings = new List<UsingDirectiveSyntax>();

        foreach (var import in importsNode.Elements(XName.Get("Import", snippetNode.Name.NamespaceName)))
        {
            var namespaceElement = import.Element(namespaceXmlName);
            if (namespaceElement == null)
            {
                continue;
            }

            var namespaceToImport = namespaceElement.Value.Trim();
            if (string.IsNullOrEmpty(namespaceToImport))
            {
                continue;
            }

            var candidateUsing = SyntaxFactory.ParseCompilationUnit("using " + namespaceToImport + ";").DescendantNodes().OfType<UsingDirectiveSyntax>().FirstOrDefault();
            if (candidateUsing == null)
            {
                continue;
            }
            else if (candidateUsing.ContainsDiagnostics && !namespaceToImport.Contains("="))
            {
                // Retry by parsing the namespace as a name and constructing a using directive from it
                candidateUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceToImport))
                    .WithUsingKeyword(UsingKeyword.WithTrailingTrivia(SyntaxFactory.Space));
            }

            if (!existingUsings.Any(u => u.IsEquivalentTo(candidateUsing, topLevel: false)))
            {
                newUsings.Add(candidateUsing.WithAdditionalAnnotations(Formatter.Annotation).WithAppendedTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed));
            }
        }

        return newUsings;
    }
}
