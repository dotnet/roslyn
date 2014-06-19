// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Globalization;

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
        private class DocumentationCommentWalker : CSharpSyntaxWalker
        {
            private readonly CSharpCompilation compilation;
            private readonly DiagnosticBag diagnostics;
            private readonly Symbol memberSymbol;
            private readonly TextWriter writer;
            private readonly ArrayBuilder<CSharpSyntaxNode> includeElementNodes;

            private HashSet<ParameterSymbol> documentedParameters;
            private HashSet<TypeParameterSymbol> documentedTypeParameters;

            private DocumentationCommentWalker(
                CSharpCompilation compilation,
                DiagnosticBag diagnostics,
                Symbol memberSymbol,
                TextWriter writer,
                ArrayBuilder<CSharpSyntaxNode> includeElementNodes,
                HashSet<ParameterSymbol> documentedParameters,
                HashSet<TypeParameterSymbol> documentedTypeParameters)
                : base(SyntaxWalkerDepth.StructuredTrivia)
            {
                this.compilation = compilation;
                this.diagnostics = diagnostics;
                this.memberSymbol = memberSymbol;
                this.writer = writer;
                this.includeElementNodes = includeElementNodes;

                this.documentedParameters = documentedParameters;
                this.documentedTypeParameters = documentedTypeParameters;
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
                DiagnosticBag diagnostics,
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
                    documentedParameters = walker.documentedParameters;
                    documentedTypeParameters = walker.documentedTypeParameters;
                }
                return pooled.ToStringAndFree();
            }

            public override void DefaultVisit(SyntaxNode node)
            {
                SyntaxKind nodeKind = node.CSharpKind();
                bool diagnose = ((SyntaxTree)node.SyntaxTree).ReportDocumentationCommentDiagnostics();

                if (nodeKind == SyntaxKind.XmlCrefAttribute)
                {
                    XmlCrefAttributeSyntax crefAttr = (XmlCrefAttributeSyntax)node;
                    CrefSyntax cref = crefAttr.Cref;

                    BinderFactory factory = compilation.GetBinderFactory(cref.SyntaxTree);
                    Binder binder = factory.GetBinder(cref);

                    // Do this for the diagnostics, even if it won't be written.
                    DiagnosticBag crefDiagnostics = DiagnosticBag.GetInstance();
                    string docCommentId = GetDocumentationCommentId(cref, binder, crefDiagnostics);
                    if (diagnose)
                    {
                        diagnostics.AddRange(crefDiagnostics);
                    }
                    crefDiagnostics.Free();

                    if ((object)writer != null)
                    {
                        Visit(crefAttr.Name);
                        VisitToken(crefAttr.EqualsToken);

                        // Not going to visit normally, because we want to skip trivia within
                        // the attribute value.
                        crefAttr.StartQuoteToken.WriteTo(writer, leading: true, trailing: false);

                        // We're not going to visit the cref because we want to bind it
                        // and write a doc comment ID in its place.
                        writer.Write(docCommentId);

                        // Not going to visit normally, because we want to skip trivia within
                        // the attribute value.
                        crefAttr.EndQuoteToken.WriteTo(writer, leading: false, trailing: true);
                    }

                    // Don't descend - we've already written out everything necessary.
                    return;
                }
                else if (nodeKind == SyntaxKind.XmlNameAttribute && diagnose)
                {
                    XmlNameAttributeSyntax nameAttr = (XmlNameAttributeSyntax)node;

                    BinderFactory factory = compilation.GetBinderFactory(nameAttr.SyntaxTree);
                    Binder binder = factory.GetBinder(nameAttr, nameAttr.Identifier.SpanStart);

                    // Do this for diagnostics, even if we aren't writing.
                    DocumentationCommentCompiler.BindName(nameAttr, binder, memberSymbol, ref documentedParameters, ref documentedTypeParameters, diagnostics);

                    // Do descend - we still need to write out the tokens of the attribute.
                }

                // NOTE: if we're recording any include element nodes (i.e. if includeElementsNodes is non-null),
                // then we want to record all of them, because we won't be able to distinguish in the XML DOM.
                if ((object)includeElementNodes != null)
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

                    if ((object)nameSyntax != null && (object)nameSyntax.Prefix == null &&
                        DocumentationCommentXmlNames.ElementEquals(nameSyntax.LocalName.ValueText, DocumentationCommentXmlNames.IncludeElementName))
                    {
                        includeElementNodes.Add((CSharpSyntaxNode)node);
                    }
                }

                base.DefaultVisit(node);
            }

            public override void VisitToken(SyntaxToken token)
            {
                if ((object)writer != null)
                {
                    token.WriteTo(writer);
                }

                base.VisitToken(token);
            }
        }
    }
}