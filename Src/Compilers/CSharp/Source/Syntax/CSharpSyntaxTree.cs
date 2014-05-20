// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Instrumentation;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using InternalSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax;
using System.Collections.Immutable;
using System.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The parsed representation of a C# source document.
    /// </summary>
    public abstract partial class CSharpSyntaxTree : SyntaxTree
    {
        internal static readonly SyntaxTree Dummy = new DummySyntaxTree();

        /// <summary>
        /// The options used by the parser to produce the syntax tree.
        /// </summary>
        public new abstract CSharpParseOptions Options { get; }

        // REVIEW: I would prefer to not expose CloneAsRoot and make the functionality 
        // internal to CaaS layer, to ensure that for a given SyntaxTree there can not 
        // be multiple trees claiming to be its children. 
        // 
        // However, as long as we provide GetRoot extensibility point on SyntaxTree 
        // the guarantee above cannot be implemented and we have to provide some way for 
        // creating root nodes.
        //
        // Therefore I place CloneAsRoot API on SyntaxTree and make it protected to 
        // at least limit its visibility to SyntaxTree extenders.

        /// <summary>
        /// Produces a clone of a CSharpSyntaxNode which will have current syntax tree as its parent.
        /// 
        /// Caller must guarantee that if the same instance of CSharpSyntaxNode makes multiple calls 
        /// to this function, only one result is observable.
        /// </summary>
        /// <typeparam name="T">Type of the syntax node.</typeparam>
        /// <param name="node">The original syntax node.</param>
        /// <returns>A clone of the original syntax node that has current SyntaxTree as its parent.</returns>
        protected T CloneNodeAsRoot<T>(T node) where T : CSharpSyntaxNode
        {
            return CSharpSyntaxNode.CloneNodeAsRoot(node, this);
        }

        /// <summary>
        /// Gets the root node of the syntax tree.
        /// </summary>
        public new abstract CSharpSyntaxNode GetRoot(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the root node of the syntax tree if it is available.
        /// </summary>
        public abstract bool TryGetRoot(out CSharpSyntaxNode root);

        /// <summary>
        /// Gets the root node of the syntax tree asynchronously.
        /// </summary>
        public new virtual Task<CSharpSyntaxNode> GetRootAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            CSharpSyntaxNode node;
            if (this.TryGetRoot(out node))
            {
                return Task.FromResult(node);
            }
            else
            {
                return Task.Factory.StartNew(() => this.GetRoot(cancellationToken), cancellationToken); // TODO: Should we use ExceptionFilter.ExecuteWithErrorReporting here?
            }
        }

        /// <summary>
        /// Returns the root of the syntax tree strongly typed to <see cref="CompilationUnitSyntax"/>.
        /// </summary>
        /// <remarks>
        /// Ensure that <see cref="P:HasCompilationUnitRoot"/> is true for this tree prior to invoking this method.
        /// </remarks>
        /// <exception cref="InvalidCastException">Throws this exception if <see cref="P:HasCompilationUnitRoot"/> is false.</exception>
        public CompilationUnitSyntax GetCompilationUnitRoot(CancellationToken cancellationToken = default(CancellationToken))
        {
            return (CompilationUnitSyntax)this.GetRoot(cancellationToken);
        }

        /// <summary>
        /// Determines if two trees are the same, disregarding trivia differences.
        /// </summary>
        /// <param name="tree">The tree to compare against.</param>
        /// <param name="topLevel"> If true then the trees are equivalent if the contained nodes and tokens declaring
        /// metadata visible symbolic information are equivalent, ignoring any differences of nodes inside method bodies
        /// or initializer expressions, otherwise all nodes and tokens must be equivalent. 
        /// </param>
        public override bool IsEquivalentTo(SyntaxTree tree, bool topLevel = false)
        {
            return SyntaxFactory.AreEquivalent(this, tree, topLevel);
        }

        internal bool HasReferenceDirectives
        {
            get
            {
                Debug.Assert(this.HasCompilationUnitRoot);

                if (Options.Kind == SourceCodeKind.Interactive || Options.Kind == SourceCodeKind.Script)
                {
                    return this.GetCompilationUnitRoot().GetReferenceDirectives().Count > 0;
                }

                return false;
            }
        }

        #region Preprocessor Symbols
        private bool hasDirectives;
        private Syntax.InternalSyntax.DirectiveStack directives;

        internal void SetDirectiveStack(Syntax.InternalSyntax.DirectiveStack directives)
        {
            this.directives = directives;
            this.hasDirectives = true;
        }

        private Syntax.InternalSyntax.DirectiveStack GetDirectives()
        {
            if (!this.hasDirectives)
            {
                var root = this.GetRoot(CancellationToken.None);
                var stack = (root.CsGreen).ApplyDirectives(default(InternalSyntax.DirectiveStack));
                SetDirectiveStack(stack);
            }

            return this.directives;
        }

        internal bool IsAnyPreprocessorSymbolDefined(ImmutableArray<string> conditionalSymbols)
        {
            System.Diagnostics.Debug.Assert(conditionalSymbols != null);

            foreach (string conditionalSymbol in conditionalSymbols)
            {
                if (IsPreprocessorSymbolDefined(conditionalSymbol))
                {
                    return true;
                }
            }

            return false;
        }

        internal bool IsPreprocessorSymbolDefined(string symbolName)
        {
            return IsPreprocessorSymbolDefined(GetDirectives(), symbolName);
        }

        private bool IsPreprocessorSymbolDefined(InternalSyntax.DirectiveStack directives, string symbolName)
        {
            var defState = directives.IsDefined(symbolName);
            switch (defState)
            {
                default:
                case InternalSyntax.DefineState.Unspecified:
                    return this.Options.PreprocessorSymbols.Contains(symbolName);
                case InternalSyntax.DefineState.Defined:
                    return true;
                case InternalSyntax.DefineState.Undefined:
                    return false;
            }
        }

        /// <summary>
        /// Stores positions where preprocessor state change. Sorted by position.
        /// The updated state can be found in <see cref="preprocessorStates"/> array at the same index.
        /// </summary>
        private ImmutableArray<int> preprocessorStateChangePositions;

        /// <summary>
        /// Preprocessor states corresponding to positions in <see cref="preprocessorStateChangePositions"/>.
        /// </summary>
        private ImmutableArray<Syntax.InternalSyntax.DirectiveStack> preprocessorStates;

        internal bool IsPreprocessorSymbolDefined(string symbolName, int position)
        {
            if (preprocessorStateChangePositions.IsDefault)
            {
                BuildPreprocessorStateChangeMap();
            }

            int searchResult = preprocessorStateChangePositions.BinarySearch(position);
            Syntax.InternalSyntax.DirectiveStack directives;

            if (searchResult < 0)
            {
                searchResult = (~searchResult) - 1;

                if (searchResult >= 0)
                {
                    directives = preprocessorStates[searchResult];
                }
                else
                {
                    directives = Syntax.InternalSyntax.DirectiveStack.Empty;
                }
            }
            else
            {
                directives = preprocessorStates[searchResult];
            }

            return IsPreprocessorSymbolDefined(directives, symbolName);
        }

        private void BuildPreprocessorStateChangeMap()
        {
            Syntax.InternalSyntax.DirectiveStack currentState = Syntax.InternalSyntax.DirectiveStack.Empty;
            var positions = ArrayBuilder<int>.GetInstance();
            var states = ArrayBuilder<Syntax.InternalSyntax.DirectiveStack>.GetInstance();

            foreach (DirectiveTriviaSyntax directive in this.GetRoot().GetDirectives(d =>
                                                                        {
                                                                            switch (d.CSharpKind())
                                                                            {
                                                                                case SyntaxKind.IfDirectiveTrivia:
                                                                                case SyntaxKind.ElifDirectiveTrivia:
                                                                                case SyntaxKind.ElseDirectiveTrivia:
                                                                                case SyntaxKind.EndIfDirectiveTrivia:
                                                                                case SyntaxKind.DefineDirectiveTrivia:
                                                                                case SyntaxKind.UndefDirectiveTrivia:
                                                                                    return true;
                                                                                default:
                                                                                    return false;
                                                                            }
                                                                        }))
            {
                currentState = directive.ApplyDirectives(currentState);

                switch (directive.CSharpKind())
                {
                    case SyntaxKind.IfDirectiveTrivia:
                        // #if directive doesn't affect the set of defined/undefined symbols
                        break;

                    case SyntaxKind.ElifDirectiveTrivia:
                        states.Add(currentState);
                        positions.Add(((ElifDirectiveTriviaSyntax)directive).ElifKeyword.SpanStart);
                        break;

                    case SyntaxKind.ElseDirectiveTrivia:
                        states.Add(currentState);
                        positions.Add(((ElseDirectiveTriviaSyntax)directive).ElseKeyword.SpanStart);
                        break;

                    case SyntaxKind.EndIfDirectiveTrivia:
                        states.Add(currentState);
                        positions.Add(((EndIfDirectiveTriviaSyntax)directive).EndIfKeyword.SpanStart);
                        break;

                    case SyntaxKind.DefineDirectiveTrivia:
                        states.Add(currentState);
                        positions.Add(((DefineDirectiveTriviaSyntax)directive).Name.SpanStart);
                        break;

                    case SyntaxKind.UndefDirectiveTrivia:
                        states.Add(currentState);
                        positions.Add(((UndefDirectiveTriviaSyntax)directive).Name.SpanStart);
                        break;

                    default:
                        throw ExceptionUtilities.Unreachable;
                }
            }

#if DEBUG
            int currentPos = -1;
            foreach (int pos in positions)
            {
                Debug.Assert(currentPos < pos);
                currentPos = pos;
            }
#endif

            ImmutableInterlocked.InterlockedInitialize(ref preprocessorStates, states.ToImmutableAndFree());
            ImmutableInterlocked.InterlockedInitialize(ref preprocessorStateChangePositions, positions.ToImmutableAndFree());
        }
        #endregion

        #region Factories

        /// <summary>
        /// Create a new syntax tree from a syntax node.
        /// </summary>
        public static SyntaxTree Create(CSharpSyntaxNode root, CSharpParseOptions options = null, string path = "", Encoding encoding = null)
        {
            if (root == null)
            {
                throw new ArgumentNullException("root");
            }

            var directives = root.Kind == SyntaxKind.CompilationUnit ?
                ((CompilationUnitSyntax)root).GetConditionalDirectivesStack() :
                InternalSyntax.DirectiveStack.Empty;

            return new ParsedSyntaxTree(
                textOpt: null,
                encodingOpt: encoding, 
                path: path,
                root: root,
                options: options ?? CSharpParseOptions.Default, 
                directives: directives);
        }

        /// <summary>
        /// Internal helper for <see cref="CSharpSyntaxNode"/> class to create a new syntax tree rooted at the given root node.
        /// This method does not create a clone of the given root, but instead preserves it's reference identity.
        /// 
        /// NOTE: This method is only intended to be used from <see cref="P:CSharpSyntaxNode.SyntaxTree"/> property.
        /// NOTE: Do not use this method elsewhere, instead use <see cref="M:SyntaxTree.Create"/> method for creating a syntax tree.
        /// </summary>
        internal static SyntaxTree CreateWithoutClone(CSharpSyntaxNode root)
        {
            Debug.Assert(root != null);

            return new ParsedSyntaxTree(
                textOpt: null,
                path: "", 
                encodingOpt: null,
                options: CSharpParseOptions.Default,
                root: root, 
                directives: InternalSyntax.DirectiveStack.Empty, 
                cloneRoot: false);
        }

        /// <summary>
        /// Produces a syntax tree by parsing the source text.
        /// </summary>
        public static SyntaxTree ParseText(
            string text,
            CSharpParseOptions options = null,
            string path = "",
            Encoding encoding = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return ParseText(SourceText.From(text, encoding), options, path, cancellationToken);
        }

        /// <summary>
        /// Produces a syntax tree by parsing the source text.
        /// </summary>
        public static SyntaxTree ParseText(
            SourceText text,
            CSharpParseOptions options = null,
            string path = "",
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            using (Logger.LogBlock(FunctionId.CSharp_SyntaxTree_FullParse, path, text.Length, cancellationToken))
            {
                options = options ?? CSharpParseOptions.Default;

                using (var lexer = new InternalSyntax.Lexer(text, options))
                {
                    using (var parser = new InternalSyntax.LanguageParser(lexer, oldTree: null, changes: null, cancellationToken: cancellationToken))
                    {
                        var compilationUnit = (CompilationUnitSyntax)parser.ParseCompilationUnit().CreateRed();
                        var tree = new ParsedSyntaxTree(text, text.Encoding, path, options, compilationUnit, parser.Directives);
                        tree.VerifySource();
                        return tree;
                    }
                }
            }
        }

        /// <summary>
        /// Produces a syntax tree by parsing the source file.
        /// </summary>
        public static SyntaxTree ParseFile(
            string path,
            CSharpParseOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("path");

            using (var data = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return ParseText(EncodedStringText.Create(data), options, path, cancellationToken);
            }
        }

        #endregion

        #region Changes

        /// <summary>
        /// Create a new syntax based off this tree using a new source text. 
        /// 
        /// If the new source text is a minor change from the current source text an incremental parse will occur
        /// reusing most of the current syntax tree internal data.  Otherwise, a full parse will using the new
        /// source text.
        /// </summary>
        public override SyntaxTree WithChangedText(SourceText newText)
        {
            using (Logger.LogBlock(FunctionId.CSharp_SyntaxTree_IncrementalParse, message: this.FilePath))
            {
                // try to find the changes between the old text and the new text.
                SourceText oldText;
                if (this.TryGetText(out oldText))
                {
                    var changes = newText.GetChangeRanges(oldText);

                    if (changes.Count == 0 && newText == oldText)
                    {
                        return this;
                    }

                    return this.WithChanges(newText, changes);
                }
                else
                {
                    // if we do not easily know the old text, then specify entire text as changed so we do a full reparse.
                    return this.WithChanges(newText, new TextChangeRange[] { new TextChangeRange(new TextSpan(0, this.Length), newText.Length) });
                }
            }
        }

        private SyntaxTree WithChanges(SourceText newText, IReadOnlyList<TextChangeRange> changes)
        {
            if (changes == null)
            {
                throw new ArgumentNullException("changes");
            }

            var oldTree = this;

            // if changes is entire text do a full reparse
            if (changes.Count == 1 && changes[0].Span == new TextSpan(0, this.Length) && changes[0].NewLength == newText.Length)
            {
                // parser will do a full parse if we give it no changes
                changes = null;
                oldTree = null;
            }

            using (var lexer = new InternalSyntax.Lexer(newText, this.Options))
            {
                CSharpSyntaxNode oldRoot = oldTree != null ? oldTree.GetRoot() : null;
                using (var parser = new InternalSyntax.LanguageParser(lexer, oldRoot, changes))
                {
                    var compilationUnit = (CompilationUnitSyntax)parser.ParseCompilationUnit().CreateRed();
                    var tree = new ParsedSyntaxTree(newText, newText.Encoding, this.FilePath, this.Options, compilationUnit, parser.Directives);
                    tree.VerifySource(changes);
                    return tree;
                }
            }
        }

        /// <summary>
        /// Produces a pessimistic list of spans that denote the regions of text in this tree that
        /// are changed from the text of the old tree.
        /// </summary>
        /// <param name="oldTree">The old tree.</param>
        /// <remarks>The list is pessimistic because it may claim more or larger regions than actually changed.</remarks>
        public override IList<TextSpan> GetChangedSpans(SyntaxTree oldTree)
        {
            return SyntaxDiffer.GetPossiblyDifferentTextSpans(oldTree, this);
        }

        /// <summary>
        /// Gets a list of text changes that when applied to the old tree produce this tree.
        /// </summary>
        /// <param name="oldTree">The old tree.</param>
        /// <remarks>The list of changes may be different than the original changes that produced this tree.</remarks>
        public override IList<TextChange> GetChanges(SyntaxTree oldTree)
        {
            return SyntaxDiffer.GetTextChanges(oldTree, this);
        }

        #endregion

        #region LinePositions and Locations

        /// <summary>
        /// Gets the location in terms of path, line and column for a given span.
        /// </summary>
        /// <param name="span">Span within the tree.</param>
        /// <param name="cancellationToken">Cancallation token.</param>
        /// <returns>
        /// <see cref="FileLinePositionSpan"/> that contains path, line and column information.
        /// The values are not affected by line mapping directives (<code>#line</code>).
        /// </returns>
        public override FileLinePositionSpan GetLineSpan(TextSpan span, CancellationToken cancellationToken = default(CancellationToken))
        {
            return new FileLinePositionSpan(this.FilePath, GetLinePosition(span.Start), GetLinePosition(span.End));
        }

        /// <summary>
        /// Gets the location in terms of path, line and column after applying source line mapping directives (<code>#line</code>). 
        /// </summary>
        /// <param name="span">Span within the tree.</param>
        /// <param name="cancellationToken">Cancallation token.</param>
        /// <returns>
        /// A valid <see cref="FileLinePositionSpan"/> that contains path, line and column information.
        /// 
        /// If the location path is mapped the resulting path is the path specified in the corresponding <code>#line</code>,
        /// otherwise it's <see cref="SyntaxTree.FilePath"/>.
        /// 
        /// A location path is considered mapped if the first <code>#line</code> directive that preceeds it and that 
        /// either specifies an explicit file path or is <code>#line default</code> exists and specifies an explicit path.
        /// </returns>
        public override FileLinePositionSpan GetMappedLineSpan(TextSpan span, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (lazyLineDirectiveMap == null)
            {
                // Create the line directive map on demand.
                Interlocked.CompareExchange(ref lazyLineDirectiveMap, new CSharpLineDirectiveMap(this), null);
            }

            return lazyLineDirectiveMap.TranslateSpan(this.GetText(cancellationToken), this.FilePath, span);
        }

        public override LineVisibility GetLineVisibility(int position, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (lazyLineDirectiveMap == null)
            {
                // Create the line directive map on demand.
                Interlocked.CompareExchange(ref lazyLineDirectiveMap, new CSharpLineDirectiveMap(this), null);
            }

            return lazyLineDirectiveMap.GetLineVisibility(this.GetText(cancellationToken), position);
        }

        /// <summary>
        /// Gets a <see cref="FileLinePositionSpan"/> for a <see cref="TextSpan"/>. FileLinePositionSpans are used
        /// primarily for diagnostics and source locations.
        /// </summary>
        /// <param name="span">The source <see cref="TextSpan" /> to convert.</param>
        /// <param name="isHiddenPosition">Returns a boolean indicating whether this span is considered hidden or not.</param>
        /// <returns>A resulting <see cref="FileLinePositionSpan"/>.</returns>
        internal override FileLinePositionSpan GetMappedLineSpanAndVisibility(TextSpan span, out bool isHiddenPosition)
        {
            if (lazyLineDirectiveMap == null)
            {
                // Create the line directive map on demand.
                Interlocked.CompareExchange(ref lazyLineDirectiveMap, new CSharpLineDirectiveMap(this), null);
            }

            return lazyLineDirectiveMap.TranslateSpanAndVisibility(this.GetText(), this.FilePath, span, out isHiddenPosition);
        }

        /// <summary>
        /// Are there any hidden regions in the tree?
        /// </summary>
        /// <returns>True if there is at least one hidden region.</returns>
        public override bool HasHiddenRegions()
        {
            if (lazyLineDirectiveMap == null)
            {
                // Create the line directive map on demand.
                Interlocked.CompareExchange(ref lazyLineDirectiveMap, new CSharpLineDirectiveMap(this), null);
            }

            return lazyLineDirectiveMap.HasAnyHiddenRegions();
        }

        // Given the error code and the source location, get the warning state based on pragma warning directives.
        internal ReportDiagnostic GetPragmaDirectiveWarningState(string id, int position)
        {
            if (lazyPragmaWarningStateMap == null)
            {
                // Create the warning state map on demand.
                Interlocked.CompareExchange(ref lazyPragmaWarningStateMap, new CSharpPragmaWarningStateMap(this), null);
            }

            return lazyPragmaWarningStateMap.GetWarningState(id, position);
        }

        private CSharpLineDirectiveMap lazyLineDirectiveMap;
        private CSharpPragmaWarningStateMap lazyPragmaWarningStateMap;

        private LinePosition GetLinePosition(int position)
        {
            return this.GetText().Lines.GetLinePosition(position);
        }

        /// <summary>
        /// Gets a <see cref="Location"/> for the specified text span.
        /// </summary>
        public override Location GetLocation(TextSpan span)
        {
            return new SourceLocation(this, span);
        }

        #endregion

        #region Diagnostics

        /// <summary>
        /// Gets a list of all the diagnostics in the sub tree that has the specified node as its root.
        /// This method does not filter diagnostics based on #pragmas and compiler options
        /// like nowarn, warnaserror etc.
        /// </summary>
        public override IEnumerable<Diagnostic> GetDiagnostics(SyntaxNode node)
        {
            if (node == null)
                throw new ArgumentNullException("node");

            return GetDiagnostics(node.Green, node.Position);
        }

        private IEnumerable<Diagnostic> GetDiagnostics(GreenNode greenNode, int position)
        {
            if (greenNode == null)
                throw new InvalidOperationException();

            if (greenNode.ContainsDiagnostics)
            {
                return EnumerateDiagnostics(greenNode, position);
            }

            return SpecializedCollections.EmptyEnumerable<Diagnostic>();
        }

        private IEnumerable<Diagnostic> EnumerateDiagnostics(GreenNode node, int position)
        {
            var enumerator = new SyntaxTreeDiagnosticEnumerator(this, node, position);
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }

        /// <summary>
        /// Gets a list of all the diagnostics associated with the token and any related trivia.
        /// This method does not filter diagnostics based on #pragmas and compiler options
        /// like nowarn, warnaserror etc.
        /// </summary>
        public override IEnumerable<Diagnostic> GetDiagnostics(SyntaxToken token)
        {
            return GetDiagnostics((InternalSyntax.CSharpSyntaxNode)token.Node, token.Position);
        }

        /// <summary>
        /// Gets a list of all the diagnostics associated with the trivia.
        /// This method does not filter diagnostics based on #pragmas and compiler options
        /// like nowarn, warnaserror etc.
        /// </summary>
        public override IEnumerable<Diagnostic> GetDiagnostics(SyntaxTrivia trivia)
        {
            return GetDiagnostics((InternalSyntax.CSharpSyntaxNode)trivia.UnderlyingNode, trivia.Position);
        }

        /// <summary>
        /// Gets a list of all the diagnostics in either the sub tree that has the specified node as its root or
        /// associated with the token and its related trivia. 
        /// This method does not filter diagnostics based on #pragmas and compiler options
        /// like nowarn, warnaserror etc.
        /// </summary>
        public override IEnumerable<Diagnostic> GetDiagnostics(SyntaxNodeOrToken nodeOrToken)
        {
            return GetDiagnostics((InternalSyntax.CSharpSyntaxNode)nodeOrToken.UnderlyingNode, nodeOrToken.Position);
        }

        /// <summary>
        /// Gets a list of all the diagnostics in the syntax tree.
        /// This method does not filter diagnostics based on #pragmas and compiler options
        /// like nowarn, warnaserror etc.
        /// </summary>
        public override IEnumerable<Diagnostic> GetDiagnostics(CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.GetDiagnostics(this.GetRoot(cancellationToken));
        }

        #endregion

        #region SyntaxTree

        protected override SyntaxNode GetRootCore(CancellationToken cancellationToken)
        {
            return this.GetRoot(cancellationToken);
        }

        protected override async Task<SyntaxNode> GetRootAsyncCore(CancellationToken cancellationToken)
        {
            return await this.GetRootAsync(cancellationToken).ConfigureAwait(false);
        }

        protected override bool TryGetRootCore(out SyntaxNode root)
        {
            CSharpSyntaxNode node;
            if (this.TryGetRoot(out node))
            {
                root = node;
                return true;
            }
            else
            {
                root = null;
                return false;
            }
        }

        protected override ParseOptions OptionsCore
        {
            get
            {
                return this.Options;
            }
        }

        #endregion
    }
}