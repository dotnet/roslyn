// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Formatting;
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
        public SnippetExpansionClient(Guid languageServiceGuid, ITextView textView, ITextBuffer subjectBuffer, IVsEditorAdaptersFactoryService editorAdaptersFactoryService)
            : base(languageServiceGuid, textView, subjectBuffer, editorAdaptersFactoryService)
        {
        }

        /// <returns>The tracking span of the inserted "/**/" if there is an $end$ location, null
        /// otherwise.</returns>
        protected override ITrackingSpan InsertEmptyCommentAndGetEndPositionTrackingSpan()
        {
            VsTextSpan[] endSpanInSurfaceBuffer = new VsTextSpan[1];
            if (ExpansionSession.GetEndSpan(endSpanInSurfaceBuffer) != VSConstants.S_OK)
            {
                return null;
            }

            SnapshotSpan subjectBufferEndSpan;
            if (!TryGetSubjectBufferSpan(endSpanInSurfaceBuffer[0], out subjectBufferEndSpan))
            {
                return null;
            }

            var endPosition = subjectBufferEndSpan.Start.Position;

            string commentString = "/**/";
            SubjectBuffer.Insert(endPosition, commentString);

            var commentSpan = new Span(endPosition, commentString.Length);
            return SubjectBuffer.CurrentSnapshot.CreateTrackingSpan(commentSpan, SpanTrackingMode.EdgeExclusive);
        }

        public override int GetExpansionFunction(IXMLDOMNode xmlFunctionNode, string bstrFieldName, out IVsExpansionFunction pFunc)
        {
            string snippetFunctionName, param;
            if (!TryGetSnippetFunctionInfo(xmlFunctionNode, out snippetFunctionName, out param))
            {
                pFunc = null;
                return VSConstants.E_INVALIDARG;
            }

            switch (snippetFunctionName)
            {
                case "SimpleTypeName":
                    pFunc = new SnippetFunctionSimpleTypeName(this, TextView, SubjectBuffer, bstrFieldName, param);
                    return VSConstants.S_OK;
                case "ClassName":
                    pFunc = new SnippetFunctionClassName(this, TextView, SubjectBuffer, bstrFieldName);
                    return VSConstants.S_OK;
                case "GenerateSwitchCases":
                    pFunc = new SnippetFunctionGenerateSwitchCases(this, TextView, SubjectBuffer, bstrFieldName, param);
                    return VSConstants.S_OK;
                default:
                    pFunc = null;
                    return VSConstants.E_INVALIDARG;
            }
        }

        internal override Document AddImports(Document document, XElement snippetNode, bool placeSystemNamespaceFirst, CancellationToken cancellationToken)
        {
            var importsNode = snippetNode.Element(XName.Get("Imports", snippetNode.Name.NamespaceName));
            if (importsNode == null ||
                !importsNode.HasElements)
            {
                return document;
            }

            var newUsingDirectives = GetUsingDirectivesToAdd(document, snippetNode, importsNode, cancellationToken);
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

            var root = document.GetSyntaxRootSynchronously(cancellationToken);

            var newRoot = ((CompilationUnitSyntax)root).AddUsingDirectives(newUsingDirectives, placeSystemNamespaceFirst);
            var newDocument = document.WithSyntaxRoot(newRoot);

            var formattedDocument = Formatter.FormatAsync(newDocument, Formatter.Annotation, cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);
            document.Project.Solution.Workspace.ApplyDocumentChanges(formattedDocument, cancellationToken);

            return formattedDocument;
        }

        private static IList<UsingDirectiveSyntax> GetUsingDirectivesToAdd(Document document, XElement snippetNode, XElement importsNode, CancellationToken cancellationToken)
        {
            var namespaceXmlName = XName.Get("Namespace", snippetNode.Name.NamespaceName);
            var root = document.GetSyntaxRootSynchronously(cancellationToken);
            var existingUsings = ((CompilationUnitSyntax)root).Usings;
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

                if (!existingUsings.Any(u => UsingsMatch(u, candidateUsing)))
                {
                    newUsings.Add(candidateUsing.WithAdditionalAnnotations(Formatter.Annotation).WithAppendedTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed));
                }
            }

            return newUsings;
        }

        private static bool UsingsMatch(UsingDirectiveSyntax usingDirective1, UsingDirectiveSyntax usingDirective2)
        {
            return usingDirective1.Name.ToString() == usingDirective2.Name.ToString() && GetAliasName(usingDirective1) == GetAliasName(usingDirective2);
        }

        private static string GetAliasName(UsingDirectiveSyntax usingDirective)
        {
            return (usingDirective.Alias == null || usingDirective.Alias.Name == null) ? null : usingDirective.Alias.Name.ToString();
        }
    }
}
