// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
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
        private class IncludeElementExpander
        {
            private readonly Symbol _memberSymbol;
            private readonly ImmutableArray<CSharpSyntaxNode> _sourceIncludeElementNodes;
            private readonly CSharpCompilation _compilation;
            private readonly BindingDiagnosticBag _diagnostics;
            private readonly CancellationToken _cancellationToken;

            private int _nextSourceIncludeElementIndex;
            private HashSet<Location> _inProgressIncludeElementNodes;
            private HashSet<ParameterSymbol> _documentedParameters;
            private HashSet<TypeParameterSymbol> _documentedTypeParameters;
            private DocumentationCommentIncludeCache _includedFileCache;

            private IncludeElementExpander(
                Symbol memberSymbol,
                ImmutableArray<CSharpSyntaxNode> sourceIncludeElementNodes,
                CSharpCompilation compilation,
                HashSet<ParameterSymbol> documentedParameters,
                HashSet<TypeParameterSymbol> documentedTypeParameters,
                DocumentationCommentIncludeCache includedFileCache,
                BindingDiagnosticBag diagnostics,
                CancellationToken cancellationToken)
            {
                _memberSymbol = memberSymbol;
                _sourceIncludeElementNodes = sourceIncludeElementNodes;
                _compilation = compilation;
                _diagnostics = diagnostics;
                _cancellationToken = cancellationToken;

                _documentedParameters = documentedParameters;
                _documentedTypeParameters = documentedTypeParameters;
                _includedFileCache = includedFileCache;

                _nextSourceIncludeElementIndex = 0;
            }

            public static void ProcessIncludes(
                string unprocessed,
                Symbol memberSymbol,
                ImmutableArray<CSharpSyntaxNode> sourceIncludeElementNodes,
                CSharpCompilation compilation,
                ref HashSet<ParameterSymbol> documentedParameters,
                ref HashSet<TypeParameterSymbol> documentedTypeParameters,
                ref DocumentationCommentIncludeCache includedFileCache,
                TextWriter writer,
                BindingDiagnosticBag diagnostics,
                CancellationToken cancellationToken)
            {
                // If there are no include elements, then there's nothing to expand.
                // NOTE: By skipping parsing and re-writing, we avoid slightly
                // modifying the whitespace, as we would if we let the XmlWriter
                // do the writing.  This saves us a lot of work in the common case
                // but slightly reduces consistency when include elements are
                // present.
                if (sourceIncludeElementNodes.IsEmpty)
                {
                    if (writer != null)
                    {
                        writer.Write(unprocessed);
                    }
                    return;
                }

                XDocument doc;

                try
                {
                    // NOTE: XDocument.Parse seems to do a better job of preserving whitespace
                    // than XElement.Parse.
                    doc = XDocument.Parse(unprocessed, LoadOptions.PreserveWhitespace);
                }
                catch (XmlException e)
                {
                    // If one of the trees wasn't diagnosing doc comments, then an error might have slipped through.
                    // Otherwise, we shouldn't see exceptions from XDocument.Parse.
                    Debug.Assert(sourceIncludeElementNodes.All(syntax => syntax.SyntaxTree.Options.DocumentationMode < DocumentationMode.Diagnose),
                        "Why didn't our parser catch this exception? " + e);
                    if (writer != null)
                    {
                        writer.Write(unprocessed);
                    }
                    return;
                }

                cancellationToken.ThrowIfCancellationRequested();

                IncludeElementExpander expander = new IncludeElementExpander(
                    memberSymbol,
                    sourceIncludeElementNodes,
                    compilation,
                    documentedParameters,
                    documentedTypeParameters,
                    includedFileCache,
                    diagnostics,
                    cancellationToken);

                foreach (XNode node in expander.Rewrite(doc, currentXmlFilePath: null, originatingSyntax: null))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (writer != null)
                    {
                        writer.Write(node);
                    }
                }

                Debug.Assert(expander._nextSourceIncludeElementIndex == expander._sourceIncludeElementNodes.Length);

                documentedParameters = expander._documentedParameters;
                documentedTypeParameters = expander._documentedTypeParameters;
                includedFileCache = expander._includedFileCache;
            }

            /// <remarks>
            /// Rewrites nodes in <paramref name="nodes"/>, which is a snapshot of nodes from the original document.
            /// We're mutating the tree as we rewrite, so it's important to grab a snapshot of the
            /// nodes that we're going to reparent before we enumerate them.
            /// </remarks>
            private XNode[] RewriteMany(XNode[] nodes, string currentXmlFilePath, CSharpSyntaxNode originatingSyntax)
            {
                Debug.Assert(nodes != null);

                ArrayBuilder<XNode> builder = null;
                foreach (XNode child in nodes)
                {
                    if (builder == null)
                    {
                        builder = ArrayBuilder<XNode>.GetInstance();
                    }

                    builder.AddRange(Rewrite(child, currentXmlFilePath, originatingSyntax));
                }

                // Nodes returned by this method are going to be attached to a new parent, so it's
                // important that they don't already have parents.  If a node with a parent is
                // attached to a new parent, it is copied and its annotations are dropped.
                Debug.Assert(builder == null || builder.All(node => node.Parent == null));

                return builder == null ? Array.Empty<XNode>() : builder.ToArrayAndFree();
            }

            // CONSIDER: could add a depth count and just not rewrite below that depth.
            private XNode[] Rewrite(XNode node, string currentXmlFilePath, CSharpSyntaxNode originatingSyntax)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                string commentMessage = null;

                if (node.NodeType == XmlNodeType.Element)
                {
                    XElement element = (XElement)node;
                    if (ElementNameIs(element, DocumentationCommentXmlNames.IncludeElementName))
                    {
                        XNode[] rewritten = RewriteIncludeElement(element, currentXmlFilePath, originatingSyntax, out commentMessage);
                        if (rewritten != null)
                        {
                            return rewritten;
                        }
                    }
                }

                XContainer container = node as XContainer;
                if (container == null)
                {
                    Debug.Assert(commentMessage == null, "How did we get an error comment for a non-container?");
                    return new XNode[] { node.Copy(copyAttributeAnnotations: false) };
                }

                IEnumerable<XNode> oldNodes = container.Nodes();

                // Do this after grabbing the nodes, so we don't see copies of them.
                container = container.Copy(copyAttributeAnnotations: false);

                // WARN: don't use node after this point - use container since it's already been copied.

                if (oldNodes != null)
                {
                    XNode[] rewritten = RewriteMany(oldNodes.ToArray(), currentXmlFilePath, originatingSyntax);
                    container.ReplaceNodes(rewritten);
                }

                // NOTE: we may modify the values of cref attributes, so don't do this until AFTER we've
                // made a copy.  Also, we only care if we're included text - otherwise we've already 
                // processed the cref.
                if (container.NodeType == XmlNodeType.Element && originatingSyntax != null)
                {
                    XElement element = (XElement)container;
                    foreach (XAttribute attribute in element.Attributes())
                    {
                        if (AttributeNameIs(attribute, DocumentationCommentXmlNames.CrefAttributeName))
                        {
                            BindAndReplaceCref(attribute, originatingSyntax);
                        }
                        else if (AttributeNameIs(attribute, DocumentationCommentXmlNames.NameAttributeName))
                        {
                            if (ElementNameIs(element, DocumentationCommentXmlNames.ParameterElementName) ||
                                ElementNameIs(element, DocumentationCommentXmlNames.ParameterReferenceElementName))
                            {
                                BindName(attribute, originatingSyntax, isParameter: true, isTypeParameterRef: false);
                            }
                            else if (ElementNameIs(element, DocumentationCommentXmlNames.TypeParameterElementName))
                            {
                                BindName(attribute, originatingSyntax, isParameter: false, isTypeParameterRef: false);
                            }
                            else if (ElementNameIs(element, DocumentationCommentXmlNames.TypeParameterReferenceElementName))
                            {
                                BindName(attribute, originatingSyntax, isParameter: false, isTypeParameterRef: true);
                            }
                        }
                    }
                }

                if (commentMessage == null)
                {
                    return new XNode[] { container }; // Already copied.
                }
                else
                {
                    XComment failureComment = new XComment(commentMessage);
                    return new XNode[] { failureComment, container }; // Already copied.
                }
            }

            private static bool ElementNameIs(XElement element, string name)
            {
                return string.IsNullOrEmpty(element.Name.NamespaceName) && DocumentationCommentXmlNames.ElementEquals(element.Name.LocalName, name);
            }

            private static bool AttributeNameIs(XAttribute attribute, string name)
            {
                return string.IsNullOrEmpty(attribute.Name.NamespaceName) && DocumentationCommentXmlNames.AttributeEquals(attribute.Name.LocalName, name);
            }

            /// <remarks>
            /// This method boils down to Rewrite(XDocument.Load(fileAttrValue).XPathSelectElements(pathAttrValue)).  
            /// Everything else is error handling.
            /// </remarks>
            private XNode[] RewriteIncludeElement(XElement includeElement, string currentXmlFilePath, CSharpSyntaxNode originatingSyntax, out string commentMessage)
            {
                Location location = GetIncludeElementLocation(includeElement, ref currentXmlFilePath, ref originatingSyntax);
                Debug.Assert(originatingSyntax != null);

                bool diagnose = originatingSyntax.SyntaxTree.ReportDocumentationCommentDiagnostics();

                if (!EnterIncludeElement(location))
                {
                    // NOTE: these must exist since we're already processed this node elsewhere in the call stack.
                    XAttribute fileAttr = includeElement.Attribute(XName.Get(DocumentationCommentXmlNames.FileAttributeName));
                    XAttribute pathAttr = includeElement.Attribute(XName.Get(DocumentationCommentXmlNames.PathAttributeName));
                    string filePathValue = fileAttr.Value;
                    string xpathValue = pathAttr.Value;

                    if (diagnose)
                    {
                        _diagnostics.Add(ErrorCode.WRN_FailedInclude, location, filePathValue, xpathValue, new LocalizableErrorArgument(MessageID.IDS_OperationCausedStackOverflow));
                    }

                    commentMessage = ErrorFacts.GetMessage(MessageID.IDS_XMLNOINCLUDE, CultureInfo.CurrentUICulture);

                    // Don't inspect the children - we're already in a cycle.
                    return new XNode[] { new XComment(commentMessage), includeElement.Copy(copyAttributeAnnotations: false) };
                }

                DiagnosticBag includeDiagnostics = DiagnosticBag.GetInstance();

                try
                {
                    XAttribute fileAttr = includeElement.Attribute(XName.Get(DocumentationCommentXmlNames.FileAttributeName));
                    XAttribute pathAttr = includeElement.Attribute(XName.Get(DocumentationCommentXmlNames.PathAttributeName));

                    bool hasFileAttribute = fileAttr != null;
                    bool hasPathAttribute = pathAttr != null;
                    if (!hasFileAttribute || !hasPathAttribute)
                    {
                        var subMessage = hasFileAttribute ? MessageID.IDS_XMLMISSINGINCLUDEPATH.Localize() : MessageID.IDS_XMLMISSINGINCLUDEFILE.Localize();
                        includeDiagnostics.Add(ErrorCode.WRN_InvalidInclude, location, subMessage);
                        commentMessage = MakeCommentMessage(location, MessageID.IDS_XMLBADINCLUDE);
                        return null;
                    }

                    string xpathValue = pathAttr.Value;
                    string filePathValue = fileAttr.Value;

                    var resolver = _compilation.Options.XmlReferenceResolver;
                    if (resolver == null)
                    {
                        includeDiagnostics.Add(ErrorCode.WRN_FailedInclude, location, filePathValue, xpathValue, new CodeAnalysisResourcesLocalizableErrorArgument(nameof(CodeAnalysisResources.XmlReferencesNotSupported)));
                        commentMessage = MakeCommentMessage(location, MessageID.IDS_XMLFAILEDINCLUDE);
                        return null;
                    }

                    string resolvedFilePath = resolver.ResolveReference(filePathValue, currentXmlFilePath);

                    if (resolvedFilePath == null)
                    {
                        // NOTE: same behavior as IOException.
                        includeDiagnostics.Add(ErrorCode.WRN_FailedInclude, location, filePathValue, xpathValue, new CodeAnalysisResourcesLocalizableErrorArgument(nameof(CodeAnalysisResources.FileNotFound)));
                        commentMessage = MakeCommentMessage(location, MessageID.IDS_XMLFAILEDINCLUDE);
                        return null;
                    }

                    if (_includedFileCache == null)
                    {
                        _includedFileCache = new DocumentationCommentIncludeCache(resolver);
                    }

                    try
                    {
                        XDocument doc;

                        try
                        {
                            doc = _includedFileCache.GetOrMakeDocument(resolvedFilePath);
                        }
                        catch (IOException e)
                        {
                            // NOTE: same behavior as resolvedFilePath == null.
                            includeDiagnostics.Add(ErrorCode.WRN_FailedInclude, location, filePathValue, xpathValue, e.Message);
                            commentMessage = MakeCommentMessage(location, MessageID.IDS_XMLFAILEDINCLUDE);
                            return null;
                        }

                        Debug.Assert(doc != null);

                        string errorMessage;
                        bool invalidXPath;
                        XElement[] loadedElements = XmlUtilities.TrySelectElements(doc, xpathValue, out errorMessage, out invalidXPath);
                        if (loadedElements == null)
                        {
                            includeDiagnostics.Add(ErrorCode.WRN_FailedInclude, location, filePathValue, xpathValue, errorMessage);

                            commentMessage = MakeCommentMessage(location, MessageID.IDS_XMLFAILEDINCLUDE);
                            if (invalidXPath)
                            {
                                // leave the include node as is
                                return null;
                            }

                            if (location.IsInSource)
                            {
                                // As in Dev11, return only the comment - drop the include element.
                                return new XNode[] { new XComment(commentMessage) };
                            }
                            else
                            {
                                commentMessage = null;
                                return Array.Empty<XNode>();
                            }
                        }

                        if (loadedElements != null && loadedElements.Length > 0)
                        {
                            // change the current XML file path for nodes contained in the document:
                            XNode[] result = RewriteMany(loadedElements, resolvedFilePath, originatingSyntax);

                            // The elements could be rewritten away if they are includes that refer to invalid
                            // (but existing and accessible) XML files.  If this occurs, behave as if we
                            // had failed to find any XPath results (as in Dev11).
                            if (result.Length > 0)
                            {
                                // NOTE: in this case, we do NOT visit the children of the include element -
                                // they are dropped.
                                commentMessage = null;
                                return result;
                            }
                        }

                        commentMessage = MakeCommentMessage(location, MessageID.IDS_XMLNOINCLUDE);
                        return null;
                    }
                    catch (XmlException e)
                    {
                        // NOTE: invalid XML is handled differently from other errors - we don't include the include element
                        // in the results and the location is in the included (vs includING) file.

                        Location errorLocation = XmlLocation.Create(e, resolvedFilePath);
                        includeDiagnostics.Add(ErrorCode.WRN_XMLParseIncludeError, errorLocation, GetDescription(e)); //NOTE: location is in included file.

                        if (location.IsInSource)
                        {
                            commentMessage = string.Format(ErrorFacts.GetMessage(MessageID.IDS_XMLIGNORED2, CultureInfo.CurrentUICulture), resolvedFilePath);

                            // As in Dev11, return only the comment - drop the include element.
                            return new XNode[] { new XComment(commentMessage) };
                        }
                        else
                        {
                            commentMessage = null;
                            return Array.Empty<XNode>();
                        }
                    }
                }
                finally
                {
                    if (diagnose)
                    {
                        _diagnostics.AddRange(includeDiagnostics);
                    }

                    includeDiagnostics.Free();

                    LeaveIncludeElement(location);
                }
            }

            private static string MakeCommentMessage(Location location, MessageID messageId)
            {
                if (location.IsInSource)
                {
                    return ErrorFacts.GetMessage(messageId, CultureInfo.CurrentUICulture);
                }
                else
                {
                    return null;
                }
            }

            private bool EnterIncludeElement(Location location)
            {
                if (_inProgressIncludeElementNodes == null)
                {
                    _inProgressIncludeElementNodes = new HashSet<Location>();
                }

                return _inProgressIncludeElementNodes.Add(location);
            }

            private bool LeaveIncludeElement(Location location)
            {
                Debug.Assert(_inProgressIncludeElementNodes != null);
                bool result = _inProgressIncludeElementNodes.Remove(location);
                Debug.Assert(result);
                return result;
            }

            private Location GetIncludeElementLocation(XElement includeElement, ref string currentXmlFilePath, ref CSharpSyntaxNode originatingSyntax)
            {
                Location location = includeElement.Annotation<Location>();
                if (location != null)
                {
                    return location;
                }

                // If we are not in an XML file, then we must be in a source file.  Since we're traversing the XML tree in the same
                // order as the DocumentationCommentWalker, we can access the elements of includeElementNodes in order.
                if (currentXmlFilePath == null)
                {
                    Debug.Assert(_nextSourceIncludeElementIndex < _sourceIncludeElementNodes.Length);
                    Debug.Assert(originatingSyntax == null);
                    originatingSyntax = _sourceIncludeElementNodes[_nextSourceIncludeElementIndex];
                    location = originatingSyntax.Location;
                    _nextSourceIncludeElementIndex++;

                    // #line shall not affect the base path:
                    currentXmlFilePath = location.GetLineSpan().Path;
                }
                else
                {
                    location = XmlLocation.Create(includeElement, currentXmlFilePath);
                }

                Debug.Assert(location != null);
                includeElement.AddAnnotation(location);
                return location;
            }

            private void BindAndReplaceCref(XAttribute attribute, CSharpSyntaxNode originatingSyntax)
            {
                string attributeValue = attribute.Value;
                CrefSyntax crefSyntax = SyntaxFactory.ParseCref(attributeValue);

                if (crefSyntax == null)
                {
                    // This can happen if the cref is verbatim (e.g. "T:C").
                    return;
                }

                // CONSIDER: It would be easy to construct an XmlLocation from the XAttribute, so that
                // we could point the user at the actual problem.
                Location sourceLocation = originatingSyntax.Location;

                RecordSyntaxDiagnostics(crefSyntax, sourceLocation); // Respects DocumentationMode.

                MemberDeclarationSyntax memberDeclSyntax = BinderFactory.GetAssociatedMemberForXmlSyntax(originatingSyntax);
                Debug.Assert(memberDeclSyntax != null,
                    "Why are we processing a documentation comment that is not attached to a member declaration?");

                Binder binder = BinderFactory.MakeCrefBinder(crefSyntax, memberDeclSyntax, _compilation.GetBinderFactory(memberDeclSyntax.SyntaxTree));

                var crefDiagnostics = BindingDiagnosticBag.GetInstance(_diagnostics);
                attribute.Value = GetEscapedDocumentationCommentId(crefSyntax, binder, crefDiagnostics); // NOTE: mutation (element must be a copy)
                RecordBindingDiagnostics(crefDiagnostics, sourceLocation); // Respects DocumentationMode.
                crefDiagnostics.Free();
            }

            private void BindName(XAttribute attribute, CSharpSyntaxNode originatingSyntax, bool isParameter, bool isTypeParameterRef)
            {
                XmlNameAttributeSyntax attrSyntax = ParseNameAttribute(attribute.ToString(), attribute.Parent.Name.LocalName);

                // CONSIDER: It would be easy to construct an XmlLocation from the XAttribute, so that
                // we could point the user at the actual problem.
                Location sourceLocation = originatingSyntax.Location;

                RecordSyntaxDiagnostics(attrSyntax, sourceLocation); // Respects DocumentationMode.

                MemberDeclarationSyntax memberDeclSyntax = BinderFactory.GetAssociatedMemberForXmlSyntax(originatingSyntax);
                Debug.Assert(memberDeclSyntax != null,
                    "Why are we processing a documentation comment that is not attached to a member declaration?");

                var nameDiagnostics = BindingDiagnosticBag.GetInstance(_diagnostics);
                Binder binder = MakeNameBinder(isParameter, isTypeParameterRef, _memberSymbol, _compilation, originatingSyntax.SyntaxTree);
                DocumentationCommentCompiler.BindName(attrSyntax, binder, _memberSymbol, ref _documentedParameters, ref _documentedTypeParameters, nameDiagnostics);
                RecordBindingDiagnostics(nameDiagnostics, sourceLocation); // Respects DocumentationMode.
                nameDiagnostics.Free();
            }

            // NOTE: We're not sharing code with the BinderFactory visitor, because we already have the
            // member symbol in hand, which makes things much easier.
            private static Binder MakeNameBinder(bool isParameter, bool isTypeParameterRef, Symbol memberSymbol, CSharpCompilation compilation, SyntaxTree syntaxTree)
            {
                Binder binder = new BuckStopsHereBinder(compilation, FileIdentifier.Create(syntaxTree, compilation.Options.SourceReferenceResolver));

                // All binders should have a containing symbol.
                Symbol containingSymbol = memberSymbol.ContainingSymbol;
                Debug.Assert((object)containingSymbol != null);
                binder = binder.WithContainingMemberOrLambda(containingSymbol);

                if (isParameter)
                {
                    ImmutableArray<ParameterSymbol> parameters = ImmutableArray<ParameterSymbol>.Empty;

                    switch (memberSymbol.Kind)
                    {
                        case SymbolKind.Method:
                            parameters = ((MethodSymbol)memberSymbol).Parameters;
                            break;
                        case SymbolKind.Property:
                            parameters = ((PropertySymbol)memberSymbol).Parameters;
                            break;
                        case SymbolKind.NamedType:
                        case SymbolKind.ErrorType:
                            NamedTypeSymbol typeSymbol = (NamedTypeSymbol)memberSymbol;
                            if (typeSymbol.IsDelegateType())
                            {
                                parameters = typeSymbol.DelegateInvokeMethod.Parameters;
                            }
                            break;
                    }

                    if (parameters.Length > 0)
                    {
                        binder = new WithParametersBinder(parameters, binder);
                    }
                }
                else
                {
                    Symbol currentSymbol = memberSymbol;
                    do
                    {
                        switch (currentSymbol.Kind)
                        {
                            case SymbolKind.NamedType: // Includes delegates.
                            case SymbolKind.ErrorType:
                                NamedTypeSymbol typeSymbol = (NamedTypeSymbol)currentSymbol;
                                if (typeSymbol.Arity > 0)
                                {
                                    binder = new WithClassTypeParametersBinder(typeSymbol, binder);
                                }
                                break;
                            case SymbolKind.Method:
                                MethodSymbol methodSymbol = (MethodSymbol)currentSymbol;
                                if (methodSymbol.Arity > 0)
                                {
                                    binder = new WithMethodTypeParametersBinder(methodSymbol, binder);
                                }
                                break;
                        }
                        currentSymbol = currentSymbol.ContainingSymbol;
                    } while (isTypeParameterRef && !(currentSymbol is null));
                }

                return binder;
            }

            private static XmlNameAttributeSyntax ParseNameAttribute(string attributeText, string elementName)
            {
                // NOTE: Rather than introducing a new code path that will have to be kept in 
                // sync with other mode changes distributed throughout Lexer, SyntaxParser, and 
                // DocumentationCommentParser, we'll just wrap the text in some lexable syntax
                // and then extract the piece we want.
                string commentText = string.Format(@"/// <{0} {1}/>", elementName, attributeText);

                SyntaxTriviaList leadingTrivia = SyntaxFactory.ParseLeadingTrivia(commentText, CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.Diagnose));
                Debug.Assert(leadingTrivia.Count == 1);
                SyntaxTrivia trivia = leadingTrivia.ElementAt(0);
                DocumentationCommentTriviaSyntax structure = (DocumentationCommentTriviaSyntax)trivia.GetStructure();
                Debug.Assert(structure.Content.Count == 2);
                XmlEmptyElementSyntax elementSyntax = (XmlEmptyElementSyntax)structure.Content[1];
                Debug.Assert(elementSyntax.Attributes.Count == 1);
                return (XmlNameAttributeSyntax)elementSyntax.Attributes[0];
            }

            /// <remarks>
            /// Respects the DocumentationMode at the source location.
            /// </remarks>
            private void RecordSyntaxDiagnostics(CSharpSyntaxNode treelessSyntax, Location sourceLocation)
            {
                if (treelessSyntax.ContainsDiagnostics && sourceLocation.SourceTree.ReportDocumentationCommentDiagnostics())
                {
                    // NOTE: treelessSyntax doesn't have its own SyntaxTree, so we have to access the diagnostics
                    // via the Dummy tree.
                    foreach (Diagnostic diagnostic in CSharpSyntaxTree.Dummy.GetDiagnostics(treelessSyntax))
                    {
                        _diagnostics.Add(diagnostic.WithLocation(sourceLocation));
                    }
                }
            }

            /// <remarks>
            /// Respects the DocumentationMode at the source location.
            /// </remarks>
            private void RecordBindingDiagnostics(BindingDiagnosticBag bindingDiagnostics, Location sourceLocation)
            {
                if (sourceLocation.SourceTree.ReportDocumentationCommentDiagnostics())
                {
                    if (bindingDiagnostics.DiagnosticBag?.IsEmptyWithoutResolution == false)
                    {
                        foreach (Diagnostic diagnostic in bindingDiagnostics.DiagnosticBag.AsEnumerable())
                        {
                            // CONSIDER: Dev11 actually uses the originating location plus the offset into the cref/name
                            _diagnostics.Add(diagnostic.WithLocation(sourceLocation));
                        }
                    }
                }

                _diagnostics.AddDependencies(bindingDiagnostics);
            }
        }
    }
}
