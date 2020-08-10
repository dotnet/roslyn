// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.CSharp.Snippets.SnippetFunctions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using MSXML;
using Roslyn.Utilities;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Snippets
{
    internal sealed partial class SnippetExpansionClient : AbstractSnippetExpansionClient
    {
        public SnippetExpansionClient(IThreadingContext threadingContext, Guid languageServiceGuid, ITextView textView, ITextBuffer subjectBuffer, IVsEditorAdaptersFactoryService editorAdaptersFactoryService)
            : base(threadingContext, languageServiceGuid, textView, subjectBuffer, editorAdaptersFactoryService)
        {
        }

        /// <returns>The tracking span of the inserted "/**/" if there is an $end$ location, null
        /// otherwise.</returns>
        protected override ITrackingSpan InsertEmptyCommentAndGetEndPositionTrackingSpan()
        {
            var endSpanInSurfaceBuffer = new VsTextSpan[1];
            if (ExpansionSession.GetEndSpan(endSpanInSurfaceBuffer) != VSConstants.S_OK)
            {
                return null;
            }

            if (!TryGetSubjectBufferSpan(endSpanInSurfaceBuffer[0], out var subjectBufferEndSpan))
            {
                return null;
            }

            var endPosition = subjectBufferEndSpan.Start.Position;

            var commentString = "/**/";
            SubjectBuffer.Insert(endPosition, commentString);

            var commentSpan = new Span(endPosition, commentString.Length);
            return SubjectBuffer.CurrentSnapshot.CreateTrackingSpan(commentSpan, SpanTrackingMode.EdgeExclusive);
        }

        public override int GetExpansionFunction(IXMLDOMNode xmlFunctionNode, string bstrFieldName, out IVsExpansionFunction pFunc)
        {
            if (!TryGetSnippetFunctionInfo(xmlFunctionNode, out var snippetFunctionName, out var param))
            {
                pFunc = null;
                return VSConstants.E_INVALIDARG;
            }

            switch (snippetFunctionName)
            {
                case "SimpleTypeName":
                    pFunc = new SnippetFunctionSimpleTypeName(this, SubjectBuffer, bstrFieldName, param);
                    return VSConstants.S_OK;
                case "ClassName":
                    pFunc = new SnippetFunctionClassName(this, SubjectBuffer, bstrFieldName);
                    return VSConstants.S_OK;
                case "GenerateSwitchCases":
                    pFunc = new SnippetFunctionGenerateSwitchCases(this, SubjectBuffer, bstrFieldName, param);
                    return VSConstants.S_OK;
                default:
                    pFunc = null;
                    return VSConstants.E_INVALIDARG;
            }
        }

        internal override Document AddImports(
            Document document, int position, XElement snippetNode,
            bool placeSystemNamespaceFirst, CancellationToken cancellationToken)
        {
            var importsNode = snippetNode.Element(XName.Get("Imports", snippetNode.Name.NamespaceName));
            if (importsNode == null ||
                !importsNode.HasElements)
            {
                return document;
            }

            var root = document.GetSyntaxRootSynchronously(cancellationToken);
            var contextLocation = root.FindToken(position).Parent;

            var newUsingDirectives = GetUsingDirectivesToAdd(contextLocation, snippetNode, importsNode);
            if (!newUsingDirectives.Any())
            {
                return document;
            }

            // In Venus/Razor, inserting imports statements into the subject buffer does not work.
            // Instead, we add the imports through the contained language host.
            if (TryAddImportsToContainedDocument(document, newUsingDirectives.Where(u => u.Alias == null).Select(u => u.Name.ToString())))
            {
                return document;
            }

            var addImportService = document.GetLanguageService<IAddImportsService>();
            var generator = document.GetLanguageService<SyntaxGenerator>();
            var compilation = document.Project.GetCompilationAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var newRoot = addImportService.AddImports(compilation, root, contextLocation, newUsingDirectives, generator, placeSystemNamespaceFirst, cancellationToken);

            var newDocument = document.WithSyntaxRoot(newRoot);

            var formattedDocument = Formatter.FormatAsync(newDocument, Formatter.Annotation, cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);
            document.Project.Solution.Workspace.ApplyDocumentChanges(formattedDocument, cancellationToken);

            return formattedDocument;
        }

        private static IList<UsingDirectiveSyntax> GetUsingDirectivesToAdd(
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
                        .WithUsingKeyword(SyntaxFactory.Token(SyntaxKind.UsingKeyword).WithTrailingTrivia(SyntaxFactory.Space));
                }

                if (!existingUsings.Any(u => u.IsEquivalentTo(candidateUsing, topLevel: false)))
                {
                    newUsings.Add(candidateUsing.WithAdditionalAnnotations(Formatter.Annotation).WithAppendedTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed));
                }
            }

            return newUsings;
        }
    }
}
