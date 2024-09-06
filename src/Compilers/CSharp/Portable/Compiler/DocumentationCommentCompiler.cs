// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading;
using System.Xml;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Traverses the symbol table processing XML documentation comments and optionally writing them to
    /// a provided stream.
    /// </summary>
    internal partial class DocumentationCommentCompiler : CSharpSymbolVisitor
    {
        private readonly string _assemblyName;
        private readonly CSharpCompilation _compilation;
        private readonly TextWriter _writer; //never write directly - always use a helper
        private readonly SyntaxTree _filterTree; //if not null, limit analysis to types residing in this tree
        private readonly TextSpan? _filterSpanWithinTree; //if filterTree and filterSpanWithinTree is not null, limit analysis to types residing within this span in the filterTree.
        private readonly bool _processIncludes;
        private readonly bool _isForSingleSymbol; //minor differences in behavior between batch case and API case.
        private readonly BindingDiagnosticBag _diagnostics;
        private readonly CancellationToken _cancellationToken;

        private SyntaxNodeLocationComparer _lazyComparer;
        private DocumentationCommentIncludeCache _includedFileCache;

        private int _indentDepth;

        private Stack<TemporaryStringBuilder> _temporaryStringBuilders;

        private DocumentationCommentCompiler(
            string assemblyName,
            CSharpCompilation compilation,
            TextWriter writer,
            SyntaxTree filterTree,
            TextSpan? filterSpanWithinTree,
            bool processIncludes,
            bool isForSingleSymbol,
            BindingDiagnosticBag diagnostics,
            CancellationToken cancellationToken)
        {
            _assemblyName = assemblyName;

            _compilation = compilation;
            _writer = writer;
            _filterTree = filterTree;
            _filterSpanWithinTree = filterSpanWithinTree;
            _processIncludes = processIncludes;
            _isForSingleSymbol = isForSingleSymbol;
            _diagnostics = diagnostics;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Traverses the symbol table processing XML documentation comments and optionally writing them to
        /// a provided stream.
        /// </summary>
        /// <param name="compilation">Compilation that owns the symbol table.</param>
        /// <param name="assemblyName">Assembly name override, if specified. Otherwise the <see cref="ISymbol.Name"/> of the source assembly is used.</param>
        /// <param name="xmlDocStream">Stream to which XML will be written, if specified.</param>
        /// <param name="diagnostics">Will be supplemented with documentation comment diagnostics.</param>
        /// <param name="cancellationToken">To stop traversing the symbol table early.</param>
        /// <param name="filterTree">Only report diagnostics from this syntax tree, if non-null.</param>
        /// <param name="filterSpanWithinTree">If <paramref name="filterTree"/> and filterSpanWithinTree is non-null, report diagnostics within this span in the <paramref name="filterTree"/>.</param>
#nullable enable
        public static void WriteDocumentationCommentXml(CSharpCompilation compilation, string? assemblyName, Stream? xmlDocStream, BindingDiagnosticBag diagnostics, CancellationToken cancellationToken, SyntaxTree? filterTree = null, TextSpan? filterSpanWithinTree = null)
#nullable disable
        {
            StreamWriter writer = null;
            if (xmlDocStream != null && xmlDocStream.CanWrite)
            {
                writer = new StreamWriter(
                    stream: xmlDocStream,
                    encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false),
                    bufferSize: 0x400, // Default.
                    leaveOpen: true); // Don't close caller's stream.
            }

            try
            {
                using (writer)
                {
                    var compiler = new DocumentationCommentCompiler(assemblyName ?? compilation.SourceAssembly.Name, compilation, writer, filterTree, filterSpanWithinTree,
                        processIncludes: true, isForSingleSymbol: false, diagnostics: diagnostics, cancellationToken: cancellationToken);
                    compiler.Visit(compilation.SourceAssembly.GlobalNamespace);
                    Debug.Assert(compiler._indentDepth == 0);
                    writer?.Flush();
                }
            }
            catch (Exception e)
            {
                diagnostics.Add(ErrorCode.ERR_DocFileGen, Location.None, e.Message);
            }

            if (diagnostics.DiagnosticBag is DiagnosticBag diagnosticBag)
            {
                if (filterTree != null)
                {
                    // Will respect the DocumentationMode.
                    UnprocessedDocumentationCommentFinder.ReportUnprocessed(filterTree, filterSpanWithinTree, diagnosticBag, cancellationToken);
                }
                else
                {
                    foreach (SyntaxTree tree in compilation.SyntaxTrees)
                    {
                        // Will respect the DocumentationMode.
                        UnprocessedDocumentationCommentFinder.ReportUnprocessed(tree, null, diagnosticBag, cancellationToken);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the XML that would be written to the documentation comment file for this assembly.
        /// </summary>
        /// <param name="symbol">The symbol for which to retrieve documentation comments.</param>
        /// <param name="processIncludes">True to treat includes as semantically meaningful (pull in contents from other files and bind crefs, etc).</param>
        /// <param name="cancellationToken">To stop traversing the symbol table early.</param>
        internal static string GetDocumentationCommentXml(Symbol symbol, bool processIncludes, CancellationToken cancellationToken)
        {
            Debug.Assert(
                symbol.Kind == SymbolKind.Event ||
                symbol.Kind == SymbolKind.Field ||
                symbol.Kind == SymbolKind.Method ||
                symbol.Kind == SymbolKind.NamedType ||
                symbol.Kind == SymbolKind.Property);

            CSharpCompilation compilation = symbol.DeclaringCompilation;
            Debug.Assert(compilation != null);

            PooledStringBuilder pooled = PooledStringBuilder.GetInstance();
            StringWriter writer = new StringWriter(pooled.Builder);

            var compiler = new DocumentationCommentCompiler(
                assemblyName: null,
                compilation: compilation,
                writer: writer,
                filterTree: null,
                filterSpanWithinTree: null,
                processIncludes: processIncludes,
                isForSingleSymbol: true,
                diagnostics: BindingDiagnosticBag.Discarded,
                cancellationToken: cancellationToken);
            compiler.Visit(symbol);
            Debug.Assert(compiler._indentDepth == 0);

            writer.Dispose();
            return pooled.ToStringAndFree();
        }

        /// <summary>
        /// Write header, descend into members, and write footer.
        /// </summary>
        public override void VisitNamespace(NamespaceSymbol symbol)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (symbol.IsGlobalNamespace)
            {
                Debug.Assert(_assemblyName != null);

                WriteLine("<?xml version=\"1.0\"?>");
                WriteLine("<doc>");
                Indent();

                if (!_compilation.Options.OutputKind.IsNetModule())
                {
                    WriteLine("<assembly>");
                    Indent();
                    WriteLine("<name>{0}</name>", _assemblyName);
                    Unindent();
                    WriteLine("</assembly>");
                }

                WriteLine("<members>");
                Indent();
            }

            Debug.Assert(!_isForSingleSymbol);
            foreach (var s in symbol.GetMembers())
            {
                _cancellationToken.ThrowIfCancellationRequested();
                s.Accept(this);
            }

            if (symbol.IsGlobalNamespace)
            {
                Unindent();
                WriteLine("</members>");
                Unindent();
                WriteLine("</doc>");
            }
        }

        /// <summary>
        /// Write own documentation comments and then descend into members.
        /// </summary>
        public override void VisitNamedType(NamedTypeSymbol symbol)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (_filterTree != null && !symbol.IsDefinedInSourceTree(_filterTree, _filterSpanWithinTree))
            {
                return;
            }

            DefaultVisit(symbol);

            if (!_isForSingleSymbol)
            {
                foreach (Symbol member in symbol.GetMembers())
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    member.Accept(this);
                }
            }
        }

#nullable enable

        /// <summary>
        /// Compile documentation comments on the symbol and write them to the stream if one is provided.
        /// </summary>
        public override void DefaultVisit(Symbol symbol)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (ShouldSkip(symbol))
            {
                return;
            }

            if (_filterTree != null && !symbol.IsDefinedInSourceTree(_filterTree, _filterSpanWithinTree))
            {
                return;
            }

            bool shouldSkipPartialDefinitionComments = false;
            if (symbol.IsPartialDefinition())
            {
                Symbol? implementationPart = symbol switch
                {
                    MethodSymbol method => method.PartialImplementationPart,
                    SourcePropertySymbol property => property.PartialImplementationPart,
                    _ => null
                };

                if (implementationPart is not null)
                {
                    Visit(implementationPart);

                    foreach (var trivia in implementationPart.GetNonNullSyntaxNode().GetLeadingTrivia())
                    {
                        if (trivia.Kind() is SyntaxKind.SingleLineDocumentationCommentTrivia or SyntaxKind.MultiLineDocumentationCommentTrivia)
                        {
                            // If the partial method implementation has doc comments,
                            // we will not emit any doc comments found on the definition,
                            // regardless of whether the partial implementation doc comments are valid.
                            shouldSkipPartialDefinitionComments = true;
                            break;
                        }
                    }
                }
                else
                {
                    // The partial method has no implementation. Since it won't be present in the
                    // output assembly, it shouldn't be included in the documentation file.
                    shouldSkipPartialDefinitionComments = !_isForSingleSymbol;
                }
            }

            // synthesized record property: emit the matching param doc on containing type as the summary doc of the property.
            var symbolForDocComments = symbol is SynthesizedRecordPropertySymbol ? symbol.ContainingType : symbol;
            if (!TryGetDocumentationCommentNodes(symbolForDocComments, out var maxDocumentationMode, out var docCommentNodes))
            {
                // If the XML in any of the doc comments is invalid, skip all further processing (for this symbol) and 
                // just write a comment saying that info was lost for this symbol.
                string message = ErrorFacts.GetMessage(MessageID.IDS_XMLIGNORED, CultureInfo.CurrentUICulture);
                WriteLine(string.Format(CultureInfo.CurrentUICulture, message, symbol.GetDocumentationCommentId()));
                return;
            }

            // If there are no doc comments, then no further work is required (other than to report a diagnostic if one is required).
            if (docCommentNodes.IsEmpty)
            {
                if (maxDocumentationMode >= DocumentationMode.Diagnose
                    && RequiresDocumentationComment(symbol)
                    // We never give a missing doc comment warning on a partial method
                    // implementation, and we skip the missing doc comment warning on a partial
                    // definition whose documentation we were not going to output anyway.
                    && !symbol.IsPartialImplementation()
                    && !shouldSkipPartialDefinitionComments)
                {
                    // Report the error at a location in the tree that was parsing doc comments.
                    Location location = GetLocationInTreeReportingDocumentationCommentDiagnostics(symbol);
                    if (location != null)
                    {
                        _diagnostics.Add(ErrorCode.WRN_MissingXMLComment, location, symbol);
                    }
                }
                return;
            }

            _cancellationToken.ThrowIfCancellationRequested();

            string withUnprocessedIncludes;
            bool haveParseError;
            HashSet<TypeParameterSymbol> documentedTypeParameters;
            HashSet<ParameterSymbol> documentedParameters;
            ImmutableArray<CSharpSyntaxNode> includeElementNodes;
            if (!TryProcessDocumentationCommentTriviaNodes(
                    symbol,
                    shouldSkipPartialDefinitionComments,
                    docCommentNodes,
                    out withUnprocessedIncludes,
                    out haveParseError,
                    out documentedTypeParameters,
                    out documentedParameters,
                    out includeElementNodes))
            {
                return;
            }

            if (haveParseError)
            {
                // If the XML in any of the doc comments is invalid, skip all further processing (for this symbol) and 
                // just write a comment saying that info was lost for this symbol.
                string message = ErrorFacts.GetMessage(MessageID.IDS_XMLIGNORED, CultureInfo.CurrentUICulture);
                WriteLine(string.Format(CultureInfo.CurrentUICulture, message, symbol.GetDocumentationCommentId()));
                return;
            }

            // If there are no include elements, then there's nothing to expand.
            if (!includeElementNodes.IsDefaultOrEmpty)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                // NOTE: we are expanding include elements AFTER formatting the comment, since the included text is pure
                // XML, not XML mixed with documentation comment trivia (e.g. ///).  If we expanded them before formatting,
                // the formatting engine would have trouble determining what prefix to remove from each line.
                TextWriter? expanderWriter = shouldSkipPartialDefinitionComments ? null : _writer; // Don't actually write partial method definition parts.
                IncludeElementExpander.ProcessIncludes(withUnprocessedIncludes, symbol, includeElementNodes,
                    _compilation, ref documentedParameters, ref documentedTypeParameters, ref _includedFileCache, expanderWriter, _diagnostics, _cancellationToken);
            }
            else if (_writer != null && !shouldSkipPartialDefinitionComments)
            {
                // CONSIDER: The output would look a little different if we ran the XDocument through an XmlWriter.  In particular, 
                // formatting inside tags (e.g. <__tag___attr__=__"value"__>) would be normalized.  Whitespace in elements would
                // (or should) not be affected.  If we decide that this difference matters, we can run the XDocument through an XmlWriter.
                // Otherwise, just writing out the string saves a bunch of processing and does a better job of preserving whitespace.
                Write(withUnprocessedIncludes);
            }

            bool reportParameterOrTypeParameterDiagnostics = GetLocationInTreeReportingDocumentationCommentDiagnostics(symbol) != null;
            if (reportParameterOrTypeParameterDiagnostics)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                if (documentedParameters != null)
                {
                    foreach (ParameterSymbol parameter in GetParameters(symbol))
                    {
                        if (!documentedParameters.Contains(parameter))
                        {
                            Location location = parameter.GetFirstLocation();
                            Debug.Assert(location.SourceTree!.ReportDocumentationCommentDiagnostics()); //Should be the same tree as for the symbol.

                            // NOTE: parameter name, since the parameter would be displayed as just its type.
                            _diagnostics.Add(ErrorCode.WRN_MissingParamTag, location, parameter.Name, symbol);
                        }
                    }
                }

                if (documentedTypeParameters != null)
                {
                    foreach (TypeParameterSymbol typeParameter in GetTypeParameters(symbol))
                    {
                        if (!documentedTypeParameters.Contains(typeParameter))
                        {
                            Location location = typeParameter.GetFirstLocation();
                            Debug.Assert(location.SourceTree!.ReportDocumentationCommentDiagnostics()); //Should be the same tree as for the symbol.

                            _diagnostics.Add(ErrorCode.WRN_MissingTypeParamTag, location, typeParameter, symbol);
                        }
                    }
                }
            }
        }

        private static bool ShouldSkip(Symbol symbol)
        {
            return symbol.IsImplicitlyDeclared ||
                symbol.IsAccessor() ||
                symbol is SynthesizedSimpleProgramEntryPointSymbol;
        }

        private bool TryProcessRecordPropertyDocumentation(
            SynthesizedRecordPropertySymbol recordPropertySymbol,
            ImmutableArray<DocumentationCommentTriviaSyntax> docCommentNodes,
            [NotNullWhen(true)] out string? withUnprocessedIncludes,
            out ImmutableArray<CSharpSyntaxNode> includeElementNodes)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (getMatchingParamTags(recordPropertySymbol.Name, docCommentNodes) is not { } paramTags)
            {
                withUnprocessedIncludes = null;
                includeElementNodes = default;
                return false;
            }

            Debug.Assert(paramTags.Count > 0);

            BeginTemporaryString();
            WriteLine("<member name=\"{0}\">", recordPropertySymbol.GetDocumentationCommentId());
            Indent();
            var substitutedTextBuilder = PooledStringBuilder.GetInstance();
            var includeElementNodesBuilder = _processIncludes ? ArrayBuilder<CSharpSyntaxNode>.GetInstance() : null;
            DocumentationCommentWalker.GetSubstitutedText(_compilation, recordPropertySymbol, paramTags, includeElementNodesBuilder, substitutedTextBuilder.Builder);
            string substitutedText = substitutedTextBuilder.ToStringAndFree();
            string formattedXml = FormatComment(substitutedText);
            Write(formattedXml);
            Unindent();
            WriteLine("</member>");

            withUnprocessedIncludes = GetAndEndTemporaryString();
            includeElementNodes = includeElementNodesBuilder?.ToImmutableAndFree() ?? default;

            paramTags.Free();
            return true;

            static ArrayBuilder<XmlElementSyntax>? getMatchingParamTags(string propertyName, ImmutableArray<DocumentationCommentTriviaSyntax> docCommentNodes)
            {
                ArrayBuilder<XmlElementSyntax>? result = null;
                foreach (var trivia in docCommentNodes)
                {
                    foreach (var contentItem in trivia.Content)
                    {
                        if (contentItem is XmlElementSyntax elementSyntax)
                        {
                            foreach (var attribute in elementSyntax.StartTag.Attributes)
                            {
                                if (attribute is XmlNameAttributeSyntax nameAttribute
                                    && nameAttribute.GetElementKind() == XmlNameAttributeElementKind.Parameter
                                    && string.Equals(nameAttribute.Identifier.Identifier.ValueText, propertyName, StringComparison.Ordinal))
                                {
                                    result ??= ArrayBuilder<XmlElementSyntax>.GetInstance();
                                    result.Add(elementSyntax);
                                    break;
                                }
                            }
                        }
                    }
                }
                return result;
            }
        }

#nullable disable

        /// <summary>
        /// Loop over the DocumentationCommentTriviaSyntaxes.  Gather
        ///   1) concatenated XML, as a string;
        ///   2) whether or not the XML is valid;
        ///   3) set of type parameters covered by &lt;typeparam&gt; elements;
        ///   4) set of parameters covered by &lt;param&gt; elements;
        ///   5) list of &lt;include&gt; elements, as SyntaxNodes.
        /// </summary>
        /// <returns>True, if at least one documentation comment was processed; false, otherwise.</returns>
        /// <remarks>This was factored out for clarity, not because it's reusable.</remarks>
        private bool TryProcessDocumentationCommentTriviaNodes(
            Symbol symbol,
            bool shouldSkipPartialDefinitionComments,
            ImmutableArray<DocumentationCommentTriviaSyntax> docCommentNodes,
            out string withUnprocessedIncludes,
            out bool haveParseError,
            out HashSet<TypeParameterSymbol> documentedTypeParameters,
            out HashSet<ParameterSymbol> documentedParameters,
            out ImmutableArray<CSharpSyntaxNode> includeElementNodes)
        {
            Debug.Assert(!docCommentNodes.IsDefaultOrEmpty);

            bool processedDocComment = false; // Even if there are DocumentationCommentTriviaSyntax, we may not need to process any of them.

            ArrayBuilder<CSharpSyntaxNode> includeElementNodesBuilder = null;

            documentedParameters = null;
            documentedTypeParameters = null;

            // Saw an XmlException while parsing one of the DocumentationCommentTriviaSyntax nodes.
            haveParseError = false;

            if (symbol is SynthesizedRecordPropertySymbol recordProperty)
            {
                return TryProcessRecordPropertyDocumentation(recordProperty, docCommentNodes, out withUnprocessedIncludes, out includeElementNodes);
            }

            // We're doing substitution and formatting per-trivia, rather than per-symbol,
            // because a single symbol can have both single-line and multi-line style
            // doc comments.
            foreach (DocumentationCommentTriviaSyntax trivia in docCommentNodes)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                bool reportDiagnosticsForCurrentTrivia = trivia.SyntaxTree.ReportDocumentationCommentDiagnostics();

                if (!processedDocComment)
                {
                    // Since we have to throw away all the parts if any part is bad, we need to write to an intermediate temp.
                    BeginTemporaryString();

                    if (_processIncludes)
                    {
                        includeElementNodesBuilder = ArrayBuilder<CSharpSyntaxNode>.GetInstance();
                    }

                    // We DO want to write out partial method definition parts if we're processing includes
                    // because we need to have XML to process.
                    if (!shouldSkipPartialDefinitionComments || _processIncludes)
                    {
                        WriteLine("<member name=\"{0}\">", symbol.GetDocumentationCommentId());
                        Indent();
                    }

                    processedDocComment = true;
                }

                // Will respect the DocumentationMode.
                string substitutedText = DocumentationCommentWalker.GetSubstitutedText(_compilation, _diagnostics, symbol, trivia,
                    includeElementNodesBuilder, ref documentedParameters, ref documentedTypeParameters);

                string formattedXml = FormatComment(substitutedText);

                // It would be preferable to just parse the concatenated XML at the end of the loop (we wouldn't have
                // to wrap it in a root element and we wouldn't have to reparse in the IncludeElementExpander), but
                // then we wouldn't know whether or where to report a diagnostic.
                XmlException e = XmlDocumentationCommentTextReader.ParseAndGetException(formattedXml);
                if (e != null)
                {
                    haveParseError = true;
                    if (reportDiagnosticsForCurrentTrivia)
                    {
                        Location location = new SourceLocation(trivia.SyntaxTree, new TextSpan(trivia.SpanStart, 0));
                        _diagnostics.Add(ErrorCode.WRN_XMLParseError, location, GetDescription(e));
                    }
                }

                // For partial methods, all parts are validated, but only the implementation part is written to the XML stream.
                if (!shouldSkipPartialDefinitionComments || _processIncludes)
                {
                    // This string already has indentation and line breaks, so don't call WriteLine - just write the text directly.
                    Write(formattedXml);
                }
            }

            if (!processedDocComment)
            {
                withUnprocessedIncludes = null;
                includeElementNodes = default(ImmutableArray<CSharpSyntaxNode>);

                return false;
            }

            if (!shouldSkipPartialDefinitionComments || _processIncludes)
            {
                Unindent();
                WriteLine("</member>");
            }

            // Free the temp.
            withUnprocessedIncludes = GetAndEndTemporaryString();

            // Free the builder, even if there was an error.
            includeElementNodes = _processIncludes ? includeElementNodesBuilder.ToImmutableAndFree() : default(ImmutableArray<CSharpSyntaxNode>);

            return true;
        }

        private static Location GetLocationInTreeReportingDocumentationCommentDiagnostics(Symbol symbol)
        {
            foreach (Location location in symbol.Locations)
            {
                if (location.SourceTree.ReportDocumentationCommentDiagnostics())
                {
                    return location;
                }
            }
            return null;
        }

        /// <remarks>
        /// Similar to SymbolExtensions.GetParameters, but returns empty for unsupported symbols
        /// and handles delegates.
        /// </remarks>
        private static ImmutableArray<ParameterSymbol> GetParameters(Symbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.NamedType:
                    MethodSymbol delegateInvoke = ((NamedTypeSymbol)symbol).DelegateInvokeMethod;
                    if ((object)delegateInvoke != null)
                    {
                        return delegateInvoke.Parameters;
                    }
                    break;
                case SymbolKind.Method:
                case SymbolKind.Property:
                case SymbolKind.Event:
                    return symbol.GetParameters();
            }

            return ImmutableArray<ParameterSymbol>.Empty;
        }

        /// <remarks>
        /// Similar to SymbolExtensions.GetMemberTypeParameters, but returns empty for unsupported symbols.
        /// </remarks>
        private static ImmutableArray<TypeParameterSymbol> GetTypeParameters(Symbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Method:
                case SymbolKind.NamedType:
                case SymbolKind.ErrorType:
                    return symbol.GetMemberTypeParameters();
            }

            return ImmutableArray<TypeParameterSymbol>.Empty;
        }

        /// <summary>
        /// A symbol requires a documentation comment if it was explicitly declared and
        /// will be visible outside the current assembly (ignoring InternalsVisibleTo).
        /// Exception: accessors do not require doc comments.
        /// </summary>
        private static bool RequiresDocumentationComment(Symbol symbol)
        {
            Debug.Assert((object)symbol != null);

            if (ShouldSkip(symbol))
            {
                return false;
            }

            while ((object)symbol != null)
            {
                switch (symbol.DeclaredAccessibility)
                {
                    case Accessibility.Public:
                    case Accessibility.Protected:
                    case Accessibility.ProtectedOrInternal:
                        symbol = symbol.ContainingType;
                        break;
                    default:
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get all of the DocumentationCommentTriviaSyntax associated with any declaring syntax of the
        /// given symbol (except for partial methods, which only consider the part with the body).
        /// </summary>
        /// <returns>True if the nodes are all valid XML.</returns>
        private bool TryGetDocumentationCommentNodes(Symbol symbol, out DocumentationMode maxDocumentationMode, out ImmutableArray<DocumentationCommentTriviaSyntax> nodes)
        {
            maxDocumentationMode = DocumentationMode.None;
            nodes = default(ImmutableArray<DocumentationCommentTriviaSyntax>);

            ArrayBuilder<DocumentationCommentTriviaSyntax> builder = null;
            var diagnosticBag = _diagnostics.DiagnosticBag ?? DiagnosticBag.GetInstance();

            foreach (SyntaxReference reference in symbol.DeclaringSyntaxReferences)
            {
                DocumentationMode currDocumentationMode = reference.SyntaxTree.Options.DocumentationMode;
                maxDocumentationMode = currDocumentationMode > maxDocumentationMode ? currDocumentationMode : maxDocumentationMode;

                ImmutableArray<DocumentationCommentTriviaSyntax> triviaList = SourceDocumentationCommentUtils.GetDocumentationCommentTriviaFromSyntaxNode((CSharpSyntaxNode)reference.GetSyntax(), diagnosticBag);
                foreach (var trivia in triviaList)
                {
                    if (ContainsXmlParseDiagnostic(trivia))
                    {
                        if (builder != null)
                        {
                            builder.Free();
                        }
                        return false;
                    }

                    if (builder == null)
                    {
                        builder = ArrayBuilder<DocumentationCommentTriviaSyntax>.GetInstance();
                    }
                    builder.Add(trivia);
                }
            }

            if (diagnosticBag != _diagnostics.DiagnosticBag)
            {
                diagnosticBag.Free();
            }

            if (builder == null)
            {
                nodes = ImmutableArray<DocumentationCommentTriviaSyntax>.Empty;
            }
            else
            {
                builder.Sort(Comparer);
                nodes = builder.ToImmutableAndFree();
            }

            return true;
        }

        private static bool ContainsXmlParseDiagnostic(DocumentationCommentTriviaSyntax node)
        {
            if (!node.ContainsDiagnostics)
            {
                return false;
            }

            foreach (Diagnostic diag in node.GetDiagnostics())
            {
                if ((ErrorCode)diag.Code == ErrorCode.WRN_XMLParseError)
                {
                    return true;
                }
            }

            return false;
        }

        private static readonly string[] s_newLineSequences = new[] { "\r\n", "\r", "\n" };

        /// <summary>
        /// Given the full text of a documentation comment, strip off the comment punctuation (///, /**, etc)
        /// and add appropriate indentations.
        /// </summary>
        private string FormatComment(string substitutedText)
        {
            BeginTemporaryString();

            if (TrimmedStringStartsWith(substitutedText, "///"))
            {
                //Debug.Assert(lines.Take(numLines).All(line => TrimmedStringStartsWith(line, "///")));
                WriteFormattedSingleLineComment(substitutedText);
            }
            else
            {
                string[] lines = substitutedText.Split(s_newLineSequences, StringSplitOptions.None);

                int numLines = lines.Length;
                Debug.Assert(numLines > 0);

                if (string.IsNullOrEmpty(lines[numLines - 1]))
                {
                    numLines--;
                    Debug.Assert(numLines > 0);
                }

                // We may use multi-line formatting in a "fragment" scenario.
                //     /** <summary>The record</summary>
                //     <param name="P">The parameter</param>
                //     */
                //     record Rec(int P);
                // When formatting docs for property 'Rec.P' we may have just the line with '<param ...>' as input to this method.
                WriteFormattedMultiLineComment(lines, numLines);
            }

            return GetAndEndTemporaryString();
        }

        /// <summary>
        /// Given a string, find the index of the first non-whitespace char.
        /// </summary>
        /// <param name="str">The string to search</param>
        /// <returns>The index of the first non-whitespace char in the string</returns>
        private static int GetIndexOfFirstNonWhitespaceChar(string str)
        {
            return GetIndexOfFirstNonWhitespaceChar(str, 0, str.Length);
        }

        /// <summary>
        /// Find the first non-whitespace character in a given substring.
        /// </summary>
        /// <param name="str">The string to search</param>
        /// <param name="start">The start index</param>
        /// <param name="end">The last index (non-inclusive)</param>
        /// <returns>The index of the first non-whitespace char after index start in the string up to, but not including the end index</returns>
        private static int GetIndexOfFirstNonWhitespaceChar(string str, int start, int end)
        {
            Debug.Assert(start >= 0);
            Debug.Assert(start <= str.Length);
            Debug.Assert(end >= 0);
            Debug.Assert(end <= str.Length);
            Debug.Assert(end >= start);

            for (; start < end; start++)
            {
                if (!SyntaxFacts.IsWhitespace(str[start]))
                {
                    break;
                }
            }

            return start;
        }

        /// <summary>
        /// Determine if the given string starts with the given prefix if whitespace
        /// is first trimmed from the beginning.
        /// </summary>
        /// <param name="str">The string to search</param>
        /// <param name="prefix">The prefix</param>
        /// <returns>true if str.TrimStart().StartsWith(prefix)</returns>
        private static bool TrimmedStringStartsWith(string str, string prefix)
        {
            // PERF: Avoid calling string.Trim() because that allocates a new substring
            int start = GetIndexOfFirstNonWhitespaceChar(str);
            int len = str.Length - start;
            if (len < prefix.Length)
            {
                return false;
            }

            for (int i = 0; i < prefix.Length; i++)
            {
                if (prefix[i] != str[i + start])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Given a string which may contain newline sequences, get the index of the first newline
        /// sequence beginning at the given starting index.
        /// </summary>
        /// <param name="str">The string to split.</param>
        /// <param name="start">The starting index within the string.</param>
        /// <param name="newLineLength">The length of the newline sequence discovered. 0 if the end of the string was reached, otherwise either 1 or 2 chars</param>
        /// <returns>The index of the start of the first newline sequence following the start index</returns>
        private static int IndexOfNewLine(string str, int start, out int newLineLength)
        {
            for (; start < str.Length; start++)
            {
                switch (str[start])
                {
                    case '\r':
                        if ((start + 1) < str.Length && str[start + 1] == '\n')
                        {
                            newLineLength = 2;
                        }
                        else
                        {
                            newLineLength = 1;
                        }
                        return start;

                    case '\n':
                        newLineLength = 1;
                        return start;
                }
            }

            newLineLength = 0;
            return start;
        }

        /// <summary>
        /// Given the full text of a single-line style documentation comment, for each line, strip off
        /// the comment punctuation (///) and add appropriate indentations.
        /// </summary>
        private void WriteFormattedSingleLineComment(string text)
        {
            // PERF: Avoid allocating intermediate strings e.g. via Split, Trim or Substring
            bool skipSpace = true;
            for (int start = 0; start < text.Length;)
            {
                int newLineLength;
                int end = IndexOfNewLine(text, start, out newLineLength);
                int trimStart = GetIndexOfFirstNonWhitespaceChar(text, start, end);
                int trimmedLength = end - trimStart;
                if (trimmedLength < 4 || !SyntaxFacts.IsWhitespace(text[trimStart + 3]))
                {
                    skipSpace = false;
                    break;
                }

                start = end + newLineLength;
            }

            int substringStart = skipSpace ? 4 : 3;

            for (int start = 0; start < text.Length;)
            {
                int newLineLength;
                int end = IndexOfNewLine(text, start, out newLineLength);
                int trimStart = GetIndexOfFirstNonWhitespaceChar(text, start, end) + substringStart;

                // <Metalama>
                // the following code is coming from the currently open PR https://github.com/dotnet/roslyn/pull/47360
                // if it gets merged, this could cause a merge conflict
                // this happens when text ends with new line followed by whitespace
                if (trimStart > end)
                {
                    break;
                }
                // </Metalama>

                WriteSubStringLine(text, trimStart, end - trimStart);
                start = end + newLineLength;
            }
        }

        /// <summary>
        /// Given the full text of a multi-line style documentation comment, broken into lines, strip off
        /// the comment punctuation (/**, */, etc) and add appropriate indentations.
        /// </summary>
        private void WriteFormattedMultiLineComment(string[] lines, int numLines)
        {
            bool skipFirstLine = lines[0].Trim() == "/**";
            bool skipLastLine = lines[numLines - 1].Trim() == "*/";

            if (skipLastLine)
            {
                numLines--;
                Debug.Assert(numLines > 0);
            }

            int skipLength = 0;
            if (numLines > 1)
            {
                string pattern = FindMultiLineCommentPattern(lines[1]);

                if (pattern != null)
                {
                    bool allMatch = true;

                    for (int i = 2; i < numLines; i++)
                    {
                        string currentLinePattern = LongestCommonPrefix(pattern, lines[i]);
                        if (string.IsNullOrWhiteSpace(currentLinePattern))
                        {
                            allMatch = false;
                            break;
                        }
                        Debug.Assert(pattern.StartsWith(currentLinePattern, StringComparison.Ordinal));
                        pattern = currentLinePattern;
                    }

                    if (allMatch)
                    {
                        skipLength = pattern.Length;
                    }
                }
            }

            if (!skipFirstLine)
            {
                string trimmed = lines[0].TrimStart(null);
                if (!skipLastLine && numLines == 1)
                {
                    trimmed = TrimEndOfMultiLineComment(trimmed);
                }
                WriteLine(trimmed.Substring(
                    trimmed.StartsWith("/** ") ? 4 :
                    trimmed.StartsWith("/**") ? 3 :
                    trimmed.StartsWith("* ") ? 2 :
                    trimmed.StartsWith("*") ? 1 :
                    0));
            }

            for (int i = 1; i < numLines; i++)
            {
                string trimmed = lines[i].Substring(skipLength);

                // If we've already skipped the last line, this can't happen.
                if (!skipLastLine && i == numLines - 1)
                {
                    trimmed = TrimEndOfMultiLineComment(trimmed);
                }

                WriteLine(trimmed);
            }
        }

        /// <summary>
        /// Remove "*/" and any following text, if it is present.
        /// </summary>
        private static string TrimEndOfMultiLineComment(string trimmed)
        {
            int index = trimmed.IndexOf("*/", StringComparison.Ordinal);
            if (index >= 0)
            {
                trimmed = trimmed.Substring(0, index);
            }
            return trimmed;
        }

        /// <summary>
        /// Return the longest prefix matching [whitespace]*[*][whitespace]*.
        /// </summary>
        private static string FindMultiLineCommentPattern(string line)
        {
            int length = 0;

            bool seenStar = false;
            foreach (char ch in line)
            {
                if (SyntaxFacts.IsWhitespace(ch))
                {
                    length++;
                }
                else if (!seenStar && ch == '*')
                {
                    length++;
                    seenStar = true;
                }
                else
                {
                    break;
                }
            }

            return seenStar ? line.Substring(0, length) : null;
        }

        /// <summary>
        /// Return the longest common prefix of two strings
        /// </summary>
        private static string LongestCommonPrefix(string str1, string str2)
        {
            int pos = 0;
            int minLength = Math.Min(str1.Length, str2.Length);

            for (; pos < minLength && str1[pos] == str2[pos]; pos++)
            {
            }

            return str1.Substring(0, pos);
        }

        /// <summary>
        /// Bind a CrefSyntax and unwrap the result if it's an alias.
        /// </summary>
        /// <remarks>
        /// Does not respect DocumentationMode, so use a temporary bag if diagnostics are not desired.
        /// </remarks>
        private static string GetDocumentationCommentId(CrefSyntax crefSyntax, Binder binder, BindingDiagnosticBag diagnostics)
        {
            if (crefSyntax.ContainsDiagnostics)
            {
                return ToBadCrefString(crefSyntax);
            }

            Symbol ambiguityWinner;
            ImmutableArray<Symbol> symbols = binder.BindCref(crefSyntax, out ambiguityWinner, diagnostics);

            Symbol symbol;
            switch (symbols.Length)
            {
                case 0:
                    return ToBadCrefString(crefSyntax);
                case 1:
                    symbol = symbols[0];
                    break;
                default:
                    symbol = ambiguityWinner;
                    Debug.Assert((object)symbol != null);
                    break;
            }

            if (symbol.Kind == SymbolKind.Alias)
            {
                symbol = ((AliasSymbol)symbol).GetAliasTarget(basesBeingResolved: null);
            }

            if (symbol is NamespaceSymbol ns)
            {
                Debug.Assert(!ns.IsGlobalNamespace);
                diagnostics.AddAssembliesUsedByNamespaceReference(ns);
            }
            else
            {
                diagnostics.AddDependencies(symbol as TypeSymbol ?? symbol.ContainingType);
            }

            return symbol.OriginalDefinition.GetDocumentationCommentId();
        }

        /// <summary>
        /// Given a cref syntax that cannot be resolved, get the string that will be written to
        /// the documentation file in place of a documentation comment ID.
        /// </summary>
        private static string ToBadCrefString(CrefSyntax cref)
        {
            using (StringWriter tmp = new StringWriter(CultureInfo.InvariantCulture))
            {
                cref.WriteTo(tmp);
                return "!:" + tmp.ToString().Replace("{", "&lt;").Replace("}", "&gt;");
            }
        }

        /// <summary>
        /// Bind an XmlNameAttributeSyntax and update the sets of documented parameters and type parameters.
        /// </summary>
        /// <remarks>
        /// Does not respect DocumentationMode, so do not call unless diagnostics are desired.
        /// </remarks>
        private static void BindName(
            XmlNameAttributeSyntax syntax,
            Binder binder,
            Symbol memberSymbol,
            ref HashSet<ParameterSymbol> documentedParameters,
            ref HashSet<TypeParameterSymbol> documentedTypeParameters,
            BindingDiagnosticBag diagnostics)
        {
            XmlNameAttributeElementKind elementKind = syntax.GetElementKind();

            // NOTE: We want the corresponding hash set to be non-null if we saw
            // any <param>/<typeparam> elements, even if they didn't bind (for
            // WRN_MissingParamTag and WRN_MissingTypeParamTag).
            if (elementKind == XmlNameAttributeElementKind.Parameter)
            {
                if (documentedParameters == null)
                {
                    documentedParameters = new HashSet<ParameterSymbol>();
                }
            }
            else if (elementKind == XmlNameAttributeElementKind.TypeParameter)
            {
                if (documentedTypeParameters == null)
                {
                    documentedTypeParameters = new HashSet<TypeParameterSymbol>();
                }
            }

            IdentifierNameSyntax identifier = syntax.Identifier;

            if (identifier.ContainsDiagnostics)
            {
                return;
            }

            CompoundUseSiteInfo<AssemblySymbol> useSiteInfo = binder.GetNewCompoundUseSiteInfo(diagnostics);
            ImmutableArray<Symbol> referencedSymbols = binder.BindXmlNameAttribute(syntax, ref useSiteInfo);
            diagnostics.Add(syntax, useSiteInfo);

            if (referencedSymbols.IsEmpty)
            {
                switch (elementKind)
                {
                    case XmlNameAttributeElementKind.Parameter:
                        diagnostics.Add(ErrorCode.WRN_UnmatchedParamTag, identifier.Location, identifier);
                        break;
                    case XmlNameAttributeElementKind.ParameterReference:
                        diagnostics.Add(ErrorCode.WRN_UnmatchedParamRefTag, identifier.Location, identifier, memberSymbol);
                        break;
                    case XmlNameAttributeElementKind.TypeParameter:
                        diagnostics.Add(ErrorCode.WRN_UnmatchedTypeParamTag, identifier.Location, identifier);
                        break;
                    case XmlNameAttributeElementKind.TypeParameterReference:
                        diagnostics.Add(ErrorCode.WRN_UnmatchedTypeParamRefTag, identifier.Location, identifier, memberSymbol);
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(elementKind);
                }
            }
            else
            {
                foreach (Symbol referencedSymbol in referencedSymbols)
                {
                    if (elementKind == XmlNameAttributeElementKind.Parameter)
                    {
                        Debug.Assert(referencedSymbol.Kind == SymbolKind.Parameter);
                        Debug.Assert(documentedParameters != null);

                        // Restriction preserved from dev11: don't report this for the "value" parameter.
                        // Here, we detect that case by checking the containing symbol - only "value"
                        // parameters are contained by accessors, others are on the corresponding property/event.
                        ParameterSymbol parameter = (ParameterSymbol)referencedSymbol;
                        if (!parameter.ContainingSymbol.IsAccessor() && !documentedParameters.Add(parameter))
                        {
                            diagnostics.Add(ErrorCode.WRN_DuplicateParamTag, syntax.Location, identifier);
                        }
                    }
                    else if (elementKind == XmlNameAttributeElementKind.TypeParameter)
                    {
                        Debug.Assert(referencedSymbol.Kind == SymbolKind.TypeParameter);
                        Debug.Assert(documentedTypeParameters != null);

                        if (!documentedTypeParameters.Add((TypeParameterSymbol)referencedSymbol))
                        {
                            diagnostics.Add(ErrorCode.WRN_DuplicateTypeParamTag, syntax.Location, identifier);
                        }
                    }
                }
            }
        }

        private IComparer<CSharpSyntaxNode> Comparer
        {
            get
            {
                if (_lazyComparer == null)
                {
                    _lazyComparer = new SyntaxNodeLocationComparer(_compilation);
                }
                return _lazyComparer;
            }
        }

        private void BeginTemporaryString()
        {
            if (_temporaryStringBuilders == null)
            {
                _temporaryStringBuilders = new Stack<TemporaryStringBuilder>();
            }

            _temporaryStringBuilders.Push(new TemporaryStringBuilder(_indentDepth));
        }

        private string GetAndEndTemporaryString()
        {
            TemporaryStringBuilder t = _temporaryStringBuilders.Pop();
            Debug.Assert(_indentDepth == t.InitialIndentDepth, $"Temporary strings should be indent-neutral (was {t.InitialIndentDepth}, is {_indentDepth})");
            _indentDepth = t.InitialIndentDepth;
            return t.Pooled.ToStringAndFree();
        }

        private void Indent()
        {
            _indentDepth++;
        }

        private void Unindent()
        {
            _indentDepth--;
            Debug.Assert(_indentDepth >= 0);
        }

        private void Write(string indentedAndWrappedString)
        {
            if (_temporaryStringBuilders != null && _temporaryStringBuilders.Count > 0)
            {
                StringBuilder builder = _temporaryStringBuilders.Peek().Pooled.Builder;
                builder.Append(indentedAndWrappedString);
            }
            else if (_writer != null)
            {
                _writer.Write(indentedAndWrappedString);
            }
        }

        private void WriteLine(string message)
        {
            if (_temporaryStringBuilders?.Count > 0)
            {
                StringBuilder builder = _temporaryStringBuilders.Peek().Pooled.Builder;
                builder.Append(MakeIndent(_indentDepth));
                builder.AppendLine(message);
            }
            else if (_writer != null)
            {
                _writer.Write(MakeIndent(_indentDepth));
                _writer.WriteLine(message);
            }
        }

        private void WriteSubStringLine(string message, int start, int length)
        {
            if (_temporaryStringBuilders?.Count > 0)
            {
                StringBuilder builder = _temporaryStringBuilders.Peek().Pooled.Builder;
                builder.Append(MakeIndent(_indentDepth));
                builder.Append(message, start, length);
                builder.AppendLine();
            }
            else if (_writer != null)
            {
                _writer.Write(MakeIndent(_indentDepth));
                for (int i = 0; i < length; i++)
                {
                    _writer.Write(message[start + i]);
                }
                _writer.WriteLine();
            }
        }

        private void WriteLine(string format, params object[] args)
        {
            WriteLine(string.Format(format, args));
        }

        private static string MakeIndent(int depth)
        {
            Debug.Assert(depth >= 0);

            // Since we know a lot about the structure of the output,
            // we should be able to do this without constructing any
            // new string objects.
            switch (depth)
            {
                case 0:
                    return "";
                case 1:
                    return "    ";
                case 2:
                    return "        ";
                case 3:
                    return "            ";
                default:
                    Debug.Assert(false, "Didn't expect nesting to reach depth " + depth);
                    return new string(' ', depth * 4);
            }
        }

        /// <remarks>
        /// WORKAROUND:
        /// We're taking a dependency on the location and structure of a framework assembly resource.  This is not a robust solution.
        /// 
        /// Possible alternatives:
        /// 1) Polish our XML parser until it matches MSXML.  We don't want to reinvent the wheel.
        /// 2) Build a map that lets us go from XML string positions back to source positions.  
        /// This is what the native compiler did, and it was a lot of work.  We'd also still need to modify the message.
        /// 3) Do not report a diagnostic.  This is very unhelpful.
        /// 4) Report a vague diagnostic (i.e. there's a problem somewhere in this doc comment).  This is relatively unhelpful.
        /// 5) Always report the message in English, so that we can pull it apart without needing to consume resource files.
        /// This engenders a lot of ill will.
        /// 6) Report the exception message without modification and (optionally) include the text with respect to which the
        /// position is specified.  This would not look sufficiently polished.
        /// </remarks>
        private static string GetDescription(XmlException e)
        {
            string message = e.Message;
            try
            {
                ResourceManager manager = new ResourceManager("System.Xml", typeof(XmlException).GetTypeInfo().Assembly);
                string locationTemplate = manager.GetString("Xml_MessageWithErrorPosition");
                string locationString = string.Format(locationTemplate, "", e.LineNumber, e.LinePosition); // first arg is where the problem description goes
                int position = message.IndexOf(locationString, StringComparison.Ordinal); // Expect exact match
                return position < 0
                    ? message
                    : message.Remove(position, locationString.Length);
            }
            catch
            {
                Debug.Assert(false, "If we hit this, then we might need to think about a different workaround " +
                    "for stripping the location out the message.");

                // If anything at all goes wrong, just return the message verbatim.  It probably
                // contains an invalid position, but it's better than nothing.
                return message;
            }
        }

        private readonly struct TemporaryStringBuilder
        {
            public readonly PooledStringBuilder Pooled;
            public readonly int InitialIndentDepth;

            public TemporaryStringBuilder(int indentDepth)
            {
                this.InitialIndentDepth = indentDepth;
                this.Pooled = PooledStringBuilder.GetInstance();
            }
        }
    }
}
