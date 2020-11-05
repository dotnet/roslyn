// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
using Microsoft.VisualStudio.LanguageServices.CSharp.Snippets.SnippetFunctions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Expansion;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Snippets
{
    internal sealed partial class SnippetExpansionClient : AbstractSnippetExpansionClient
    {
        public SnippetExpansionClient(IThreadingContext threadingContext, IContentType languageServiceGuid, ITextView textView, ITextBuffer subjectBuffer, IExpansionServiceProvider expansionServiceProvider)
            : base(threadingContext, languageServiceGuid, textView, subjectBuffer, expansionServiceProvider)
        {
        }

        /// <returns>The tracking span of the inserted "/**/" if there is an $end$ location, null
        /// otherwise.</returns>
        protected override ITrackingSpan InsertEmptyCommentAndGetEndPositionTrackingSpan()
        {
            var endSpanInSurfaceBuffer = ExpansionSession.EndSpan;
            if (!TryGetSubjectBufferSpan(endSpanInSurfaceBuffer, out var subjectBufferEndSpan))
            {
                return null;
            }

            var endPosition = subjectBufferEndSpan.End.Position;

            var commentString = "/**/";
            SubjectBuffer.Insert(endPosition, commentString);

            var commentSpan = new Span(endPosition, commentString.Length);
            return SubjectBuffer.CurrentSnapshot.CreateTrackingSpan(commentSpan, SpanTrackingMode.EdgeExclusive);
        }

        public override IExpansionFunction GetExpansionFunction(XElement xmlFunctionNode, string fieldName)
        {
            if (!TryGetSnippetFunctionInfo(xmlFunctionNode, out var snippetFunctionName, out var param))
            {
                throw new ArgumentException();
            }

            switch (snippetFunctionName)
            {
                case "SimpleTypeName":
                    return new SnippetFunctionSimpleTypeName(this, TextView, SubjectBuffer, fieldName, param);
                case "ClassName":
                    return new SnippetFunctionClassName(this, TextView, SubjectBuffer, fieldName);
                case "GenerateSwitchCases":
                    return new SnippetFunctionGenerateSwitchCases(this, TextView, SubjectBuffer, fieldName, param);
                default:
                    return null;
            }
        }

        internal override Document AddImports(
            Document document, int position, XElement snippetNode,
            bool placeSystemNamespaceFirst, bool allowInHiddenRegions, CancellationToken cancellationToken)
        {
            var importsNode = snippetNode.Element(XName.Get("Imports", snippetNode.Name.NamespaceName));
            if (importsNode == null ||
                !importsNode.HasElements)
            {
                return document;
            }

            var root = document.GetSyntaxRootSynchronously(cancellationToken);
            var contextLocation = root.FindToken(position).Parent;

            var newUsingDirectives = GetUsingDirectivesToAdd(contextLocation, snippetNode, importsNode, cancellationToken);
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
            var newRoot = addImportService.AddImports(compilation, root, contextLocation, newUsingDirectives, generator, placeSystemNamespaceFirst, allowInHiddenRegions, cancellationToken);

            var newDocument = document.WithSyntaxRoot(newRoot);

            var formattedDocument = Formatter.FormatAsync(newDocument, Formatter.Annotation, cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);
            document.Project.Solution.Workspace.ApplyDocumentChanges(formattedDocument, cancellationToken);

            return formattedDocument;
        }

        private static IList<UsingDirectiveSyntax> GetUsingDirectivesToAdd(
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0060 // Remove unused parameter
            SyntaxNode contextLocation, XElement snippetNode, XElement importsNode, CancellationToken cancellationToken)
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore IDE0060 // Remove unused parameter
        {
            var namespaceXmlName = XName.Get("Namespace", snippetNode.Name.NamespaceName);
            var existingUsings = (IEnumerable<UsingDirectiveSyntax>)null;// contextLocation.GetEnclosingUsingDirectives();
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

                if (!existingUsings.Any(u => u.IsEquivalentTo(candidateUsing, topLevel: false)))
                {
                    newUsings.Add(candidateUsing.WithAdditionalAnnotations(Formatter.Annotation).WithAppendedTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed));
                }
            }

            return newUsings;
        }

#pragma warning disable IDE0051 // Remove unused private members
        private static string GetAliasName(UsingDirectiveSyntax usingDirective)
#pragma warning restore IDE0051 // Remove unused private members
        {
            return (usingDirective.Alias == null || usingDirective.Alias.Name == null) ? null : usingDirective.Alias.Name.ToString();
        }
    }
}
