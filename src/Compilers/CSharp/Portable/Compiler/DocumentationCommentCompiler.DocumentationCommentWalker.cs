// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    // Traverses the symbol table processing XML documentation comments and optionally writing them to
    // a provided stream.
    internal partial class DocumentationCommentCompiler : CSharpSymbolVisitor
    {
        /// <summary>
        /// Walks a DocumentationCommentTriviaSyntax, binding the semantically meaningful parts 
        /// to produce diagnostics and to replace source crefs with documentation comment IDs.
        /// </summary>
        [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
        private class DocumentationCommentWalker : CSharpSyntaxWalker
        {
            private readonly CSharpCompilation _compilation;
            private readonly BindingDiagnosticBag _diagnostics;
            private readonly Symbol _memberSymbol;
            private readonly StringWriter _writer;
            private readonly ArrayBuilder<CSharpSyntaxNode> _includeElementNodes;

            private HashSet<ParameterSymbol> _documentedParameters;
            private HashSet<TypeParameterSymbol> _documentedTypeParameters;

            private DocumentationCommentWalker(
                CSharpCompilation compilation,
                BindingDiagnosticBag diagnostics,
                Symbol memberSymbol,
                StringWriter writer,
                ArrayBuilder<CSharpSyntaxNode> includeElementNodes,
                HashSet<ParameterSymbol> documentedParameters,
                HashSet<TypeParameterSymbol> documentedTypeParameters)
                : base(SyntaxWalkerDepth.StructuredTrivia)
            {
                _compilation = compilation;
                _diagnostics = diagnostics;
                _memberSymbol = memberSymbol;
                _writer = writer;
                _includeElementNodes = includeElementNodes;

                _documentedParameters = documentedParameters;
                _documentedTypeParameters = documentedTypeParameters;
            }

            /// <summary>
            /// Writes the matching 'param' tags on a primary constructor as 'summary' tags for a synthesized record property.
            /// </summary>
            /// <remarks>
            /// Still has all of the comment punctuation (///, /**, etc). associated with the 'param' tag.
            /// </remarks>
            public static void GetSubstitutedText(
                CSharpCompilation compilation,
                SynthesizedRecordPropertySymbol symbol,
                ArrayBuilder<XmlElementSyntax> paramElements,
                ArrayBuilder<CSharpSyntaxNode> includeElementNodes,
                StringBuilder builder)
            {
                StringWriter writer = new StringWriter(builder, CultureInfo.InvariantCulture);
                DocumentationCommentWalker walker = new DocumentationCommentWalker(compilation, BindingDiagnosticBag.Discarded, symbol, writer, includeElementNodes, documentedParameters: null, documentedTypeParameters: null);

                // Before: <param name="NAME">CONTENT</param>
                // After: <summary>CONTENT</summary>
                foreach (var paramElement in paramElements)
                {
                    // '///<param': '<' owns the '///' trivia
                    // '/// <param': ' ' token preceding '<' owns '///' trivia
                    var startLessThanToken = paramElement.StartTag.LessThanToken;
                    if (!startLessThanToken.LeadingTrivia.Any(SyntaxKind.DocumentationCommentExteriorTrivia))
                    {
                        walker.VisitToken(startLessThanToken.GetPreviousToken());
                    }
                    walker.VisitToken(startLessThanToken);
                    writer.Write("summary");
                    walker.VisitToken(paramElement.StartTag.GreaterThanToken);

                    foreach (var item in paramElement.Content)
                    {
                        walker.Visit(item);
                    }

                    walker.VisitToken(paramElement.EndTag.LessThanSlashToken);
                    writer.Write("summary");

                    var endGreaterThanToken = paramElement.EndTag.GreaterThanToken;
                    walker.VisitToken(endGreaterThanToken);

                    // The '>' token doesn't own the following new line. Instead, it is directly followed by an 'XmlTextLiteralNewLineToken'.
                    if (endGreaterThanToken.GetNextToken() is SyntaxToken newLineToken && newLineToken.IsKind(SyntaxKind.XmlTextLiteralNewLineToken))
                    {
                        walker.VisitToken(newLineToken);
                    }
                }
            }

            /// <summary>
            /// Given a DocumentationCommentTriviaSyntax, return the full text, but with
            /// documentation comment IDs substituted into crefs.
            /// </summary>
            /// <remarks>
            /// Still has all of the comment punctuation (///, /**, etc).
            /// </remarks>
            public static string GetSubstitutedText(
                CSharpCompilation compilation,
                BindingDiagnosticBag diagnostics,
                Symbol symbol,
                DocumentationCommentTriviaSyntax trivia,
                ArrayBuilder<CSharpSyntaxNode> includeElementNodes,
                ref HashSet<ParameterSymbol> documentedParameters,
                ref HashSet<TypeParameterSymbol> documentedTypeParameters)
            {
                PooledStringBuilder pooled = PooledStringBuilder.GetInstance();
                using (StringWriter writer = new StringWriter(pooled.Builder, CultureInfo.InvariantCulture))
                {
                    DocumentationCommentWalker walker = new DocumentationCommentWalker(compilation, diagnostics, symbol, writer, includeElementNodes, documentedParameters, documentedTypeParameters);
                    walker.Visit(trivia);

                    // Copy back out in case they have been initialized.
                    documentedParameters = walker._documentedParameters;
                    documentedTypeParameters = walker._documentedTypeParameters;
                }
                return pooled.ToStringAndFree();
            }

            public override void DefaultVisit(SyntaxNode node)
            {
                SyntaxKind nodeKind = node.Kind();
                bool diagnose = node.SyntaxTree.ReportDocumentationCommentDiagnostics();

                if (nodeKind == SyntaxKind.XmlCrefAttribute)
                {
                    XmlCrefAttributeSyntax crefAttr = (XmlCrefAttributeSyntax)node;
                    CrefSyntax cref = crefAttr.Cref;

                    BinderFactory factory = _compilation.GetBinderFactory(cref.SyntaxTree);
                    Binder binder = factory.GetBinder(cref);

                    // Do this for the diagnostics, even if it won't be written.
                    BindingDiagnosticBag diagnostics = diagnose ? _diagnostics : BindingDiagnosticBag.GetInstance(withDiagnostics: false, withDependencies: _diagnostics.AccumulatesDependencies);
                    string docCommentId = GetEscapedDocumentationCommentId(cref, binder, diagnostics);

                    if (!diagnose)
                    {
                        _diagnostics.AddRangeAndFree(diagnostics);
                    }

                    if (_writer != null)
                    {
                        Visit(crefAttr.Name);
                        VisitToken(crefAttr.EqualsToken);

                        // Not going to visit normally, because we want to skip trivia within
                        // the attribute value.
                        crefAttr.StartQuoteToken.WriteTo(_writer, leading: true, trailing: false);

                        // We're not going to visit the cref because we want to bind it
                        // and write a doc comment ID in its place.
                        _writer.Write(docCommentId);

                        // Not going to visit normally, because we want to skip trivia within
                        // the attribute value.
                        crefAttr.EndQuoteToken.WriteTo(_writer, leading: false, trailing: true);
                    }

                    // Don't descend - we've already written out everything necessary.
                    return;
                }
                else if (diagnose && nodeKind == SyntaxKind.XmlNameAttribute)
                {
                    XmlNameAttributeSyntax nameAttr = (XmlNameAttributeSyntax)node;

                    BinderFactory factory = _compilation.GetBinderFactory(nameAttr.SyntaxTree);
                    Binder binder = factory.GetBinder(nameAttr, nameAttr.Identifier.SpanStart);

                    // Do this for diagnostics, even if we aren't writing.
                    BindName(nameAttr, binder, _memberSymbol, ref _documentedParameters, ref _documentedTypeParameters, _diagnostics);

                    // Do descend - we still need to write out the tokens of the attribute.
                }

                // NOTE: if we're recording any include element nodes (i.e. if includeElementsNodes is non-null),
                // then we want to record all of them, because we won't be able to distinguish in the XML DOM.
                if (_includeElementNodes != null)
                {
                    XmlNameSyntax nameSyntax = null;
                    if (nodeKind == SyntaxKind.XmlEmptyElement)
                    {
                        nameSyntax = ((XmlEmptyElementSyntax)node).Name;
                    }
                    else if (nodeKind == SyntaxKind.XmlElementStartTag)
                    {
                        nameSyntax = ((XmlElementStartTagSyntax)node).Name;
                    }

                    if (nameSyntax != null && nameSyntax.Prefix == null &&
                        DocumentationCommentXmlNames.ElementEquals(nameSyntax.LocalName.ValueText, DocumentationCommentXmlNames.IncludeElementName))
                    {
                        _includeElementNodes.Add((CSharpSyntaxNode)node);
                    }
                }

                base.DefaultVisit(node);
            }

            public override void VisitToken(SyntaxToken token)
            {
                if (_writer != null)
                {
                    token.WriteTo(_writer);
                }

                base.VisitToken(token);
            }

            private string GetDebuggerDisplay()
            {
                return _writer.GetStringBuilder().ToString();
            }
        }
    }
}
