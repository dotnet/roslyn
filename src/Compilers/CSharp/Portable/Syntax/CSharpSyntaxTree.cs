// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using InternalSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax;

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
        /// Produces a clone of a <see cref="CSharpSyntaxNode"/> which will have current syntax tree as its parent.
        ///
        /// Caller must guarantee that if the same instance of <see cref="CSharpSyntaxNode"/> makes multiple calls
        /// to this function, only one result is observable.
        /// </summary>
        /// <typeparam name="T">Type of the syntax node.</typeparam>
        /// <param name="node">The original syntax node.</param>
        /// <returns>A clone of the original syntax node that has current <see cref="CSharpSyntaxTree"/> as its parent.</returns>
        protected T CloneNodeAsRoot<T>(T node) where T : CSharpSyntaxNode
        {
            return CSharpSyntaxNode.CloneNodeAsRoot(node, this);
        }

        /// <summary>
        /// Gets the root node of the syntax tree.
        /// </summary>
        public new abstract CSharpSyntaxNode GetRoot(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the root node of the syntax tree if it is already available.
        /// </summary>
        public abstract bool TryGetRoot([NotNullWhen(true)] out CSharpSyntaxNode? root);

        /// <summary>
        /// Gets the root node of the syntax tree asynchronously.
        /// </summary>
        /// <remarks>
        /// By default, the work associated with this method will be executed immediately on the current thread.
        /// Implementations that wish to schedule this work differently should override <see cref="GetRootAsync(CancellationToken)"/>.
        /// </remarks>
        public new virtual Task<CSharpSyntaxNode> GetRootAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.TryGetRoot(out CSharpSyntaxNode? node) ? node : this.GetRoot(cancellationToken));
        }

        /// <summary>
        /// Gets the root of the syntax tree statically typed as <see cref="CompilationUnitSyntax"/>.
        /// </summary>
        /// <remarks>
        /// Ensure that <see cref="SyntaxTree.HasCompilationUnitRoot"/> is true for this tree prior to invoking this method.
        /// </remarks>
        /// <exception cref="InvalidCastException">Throws this exception if <see cref="SyntaxTree.HasCompilationUnitRoot"/> is false.</exception>
        public CompilationUnitSyntax GetCompilationUnitRoot(CancellationToken cancellationToken = default)
        {
            return (CompilationUnitSyntax)this.GetRoot(cancellationToken);
        }

        /// <summary>
        /// Determines if two trees are the same, disregarding trivia differences.
        /// </summary>
        /// <param name="tree">The tree to compare against.</param>
        /// <param name="topLevel">
        /// If true then the trees are equivalent if the contained nodes and tokens declaring metadata visible symbolic information are equivalent,
        /// ignoring any differences of nodes inside method bodies or initializer expressions, otherwise all nodes and tokens must be equivalent.
        /// </param>
        public override bool IsEquivalentTo(SyntaxTree tree, bool topLevel = false)
        {
            return SyntaxFactory.AreEquivalent(this, tree, topLevel);
        }

        internal bool HasReferenceDirectives
        {
            get
            {
                Debug.Assert(HasCompilationUnitRoot);

                return Options.Kind == SourceCodeKind.Script && GetCompilationUnitRoot().GetReferenceDirectives().Count > 0;
            }
        }

        internal bool HasReferenceOrLoadDirectives
        {
            get
            {
                Debug.Assert(HasCompilationUnitRoot);

                if (Options.Kind == SourceCodeKind.Script)
                {
                    var compilationUnitRoot = GetCompilationUnitRoot();
                    return compilationUnitRoot.GetReferenceDirectives().Count > 0 || compilationUnitRoot.GetLoadDirectives().Count > 0;
                }

                return false;
            }
        }

        #region Preprocessor Symbols
        private bool _hasDirectives;
        private InternalSyntax.DirectiveStack _directives;

        internal void SetDirectiveStack(InternalSyntax.DirectiveStack directives)
        {
            _directives = directives;
            _hasDirectives = true;
        }

        private InternalSyntax.DirectiveStack GetDirectives()
        {
            if (!_hasDirectives)
            {
                var stack = this.GetRoot().CsGreen.ApplyDirectives(default);
                SetDirectiveStack(stack);
            }

            return _directives;
        }

        internal bool IsAnyPreprocessorSymbolDefined(ImmutableArray<string> conditionalSymbols)
        {
            Debug.Assert(conditionalSymbols != null);

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
            switch (directives.IsDefined(symbolName))
            {
                case InternalSyntax.DefineState.Defined:
                    return true;
                case InternalSyntax.DefineState.Undefined:
                    return false;
                default:
                    return this.Options.PreprocessorSymbols.Contains(symbolName);
            }
        }

        /// <summary>
        /// Stores positions where preprocessor state changes. Sorted by position.
        /// The updated state can be found in <see cref="_preprocessorStates"/> array at the same index.
        /// </summary>
        private ImmutableArray<int> _preprocessorStateChangePositions;

        /// <summary>
        /// Preprocessor states corresponding to positions in <see cref="_preprocessorStateChangePositions"/>.
        /// </summary>
        private ImmutableArray<InternalSyntax.DirectiveStack> _preprocessorStates;

        internal bool IsPreprocessorSymbolDefined(string symbolName, int position)
        {
            if (_preprocessorStateChangePositions.IsDefault)
            {
                BuildPreprocessorStateChangeMap();
            }

            int searchResult = _preprocessorStateChangePositions.BinarySearch(position);
            InternalSyntax.DirectiveStack directives;

            if (searchResult < 0)
            {
                searchResult = (~searchResult) - 1;

                if (searchResult >= 0)
                {
                    directives = _preprocessorStates[searchResult];
                }
                else
                {
                    directives = InternalSyntax.DirectiveStack.Empty;
                }
            }
            else
            {
                directives = _preprocessorStates[searchResult];
            }

            return IsPreprocessorSymbolDefined(directives, symbolName);
        }

        private void BuildPreprocessorStateChangeMap()
        {
            InternalSyntax.DirectiveStack currentState = InternalSyntax.DirectiveStack.Empty;
            var positions = ArrayBuilder<int>.GetInstance();
            var states = ArrayBuilder<InternalSyntax.DirectiveStack>.GetInstance();

            foreach (DirectiveTriviaSyntax directive in this.GetRoot().GetDirectives(d =>
                                                                        {
                                                                            switch (d.Kind())
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

                switch (directive.Kind())
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
                        throw ExceptionUtilities.UnexpectedValue(directive.Kind());
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

            ImmutableInterlocked.InterlockedInitialize(ref _preprocessorStates, states.ToImmutableAndFree());
            ImmutableInterlocked.InterlockedInitialize(ref _preprocessorStateChangePositions, positions.ToImmutableAndFree());
        }

        #endregion

        #region Factories

        // The overload that has more parameters is itself obsolete, as an intentional break to allow future
        // expansion
#pragma warning disable RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads.

        /// <summary>
        /// Creates a new syntax tree from a syntax node.
        /// </summary>
        public static SyntaxTree Create(CSharpSyntaxNode root, CSharpParseOptions? options = null, string? path = "", Encoding? encoding = null)
        {
#pragma warning disable CS0618 // We are calling into the obsolete member as that's the one that still does the real work
            return Create(root, options, path, encoding, diagnosticOptions: null);
#pragma warning restore CS0618
        }

#pragma warning restore RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads.

        /// <summary>
        /// Creates a new syntax tree from a syntax node.
        /// </summary>
        /// <param name="diagnosticOptions">An obsolete parameter. Diagnostic options should now be passed with <see cref="CompilationOptions.SyntaxTreeOptionsProvider"/></param>
        /// <param name="isGeneratedCode">An obsolete parameter. It is unused.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("The diagnosticOptions and isGeneratedCode parameters are obsolete due to performance problems, if you are using them use CompilationOptions.SyntaxTreeOptionsProvider instead", error: false)]
        public static SyntaxTree Create(
            CSharpSyntaxNode root,
            CSharpParseOptions? options,
            string? path,
            Encoding? encoding,
            // obsolete parameter -- unused
            ImmutableDictionary<string, ReportDiagnostic>? diagnosticOptions,
            // obsolete parameter -- unused
            bool? isGeneratedCode)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var directives = root.Kind() == SyntaxKind.CompilationUnit ?
                ((CompilationUnitSyntax)root).GetConditionalDirectivesStack() :
                InternalSyntax.DirectiveStack.Empty;

            return new ParsedSyntaxTree(
                textOpt: null,
                encodingOpt: encoding,
                checksumAlgorithm: SourceHashAlgorithm.Sha1,
                path: path,
                options: options ?? CSharpParseOptions.Default,
                root: root,
                directives: directives,
                diagnosticOptions,
                cloneRoot: true);
        }

        /// <summary>
        /// Creates a new syntax tree from a syntax node with text that should correspond to the syntax node.
        /// </summary>
        /// <remarks>This is used by the ExpressionEvaluator.</remarks>
        internal static SyntaxTree CreateForDebugger(CSharpSyntaxNode root, SourceText text, CSharpParseOptions options)
        {
            Debug.Assert(root != null);

            return new DebuggerSyntaxTree(root, text, options);
        }

        /// <summary>
        /// <para>
        /// Internal helper for <see cref="CSharpSyntaxNode"/> class to create a new syntax tree rooted at the given root node.
        /// This method does not create a clone of the given root, but instead preserves it's reference identity.
        /// </para>
        /// <para>NOTE: This method is only intended to be used from <see cref="CSharpSyntaxNode.SyntaxTree"/> property.</para>
        /// <para>NOTE: Do not use this method elsewhere, instead use <see cref="Create(CSharpSyntaxNode, CSharpParseOptions, string, Encoding)"/> method for creating a syntax tree.</para>
        /// </summary>
        internal static SyntaxTree CreateWithoutClone(CSharpSyntaxNode root)
        {
            Debug.Assert(root != null);

            return new ParsedSyntaxTree(
                textOpt: null,
                encodingOpt: null,
                checksumAlgorithm: SourceHashAlgorithm.Sha1,
                path: "",
                options: CSharpParseOptions.Default,
                root: root,
                directives: InternalSyntax.DirectiveStack.Empty,
                diagnosticOptions: null,
                cloneRoot: false);
        }

        /// <summary>
        /// Produces a syntax tree by parsing the source text lazily. The syntax tree is realized when
        /// <see cref="CSharpSyntaxTree.GetRoot(CancellationToken)"/> is called.
        /// </summary>
        internal static SyntaxTree ParseTextLazy(
            SourceText text,
            CSharpParseOptions? options = null,
            string path = "")
        {
            return new LazySyntaxTree(text, options ?? CSharpParseOptions.Default, path, diagnosticOptions: null);
        }

        // The overload that has more parameters is itself obsolete, as an intentional break to allow future
        // expansion
#pragma warning disable RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads.

        /// <summary>
        /// Produces a syntax tree by parsing the source text.
        /// </summary>
        public static SyntaxTree ParseText(
            string text,
            CSharpParseOptions? options = null,
            string path = "",
            Encoding? encoding = null,
            CancellationToken cancellationToken = default)
        {
#pragma warning disable CS0618 // We are calling into the obsolete member as that's the one that still does the real work
            return ParseText(text, options, path, encoding, diagnosticOptions: null, cancellationToken);
#pragma warning restore CS0618
        }

#pragma warning restore RS0027

        /// <summary>
        /// Produces a syntax tree by parsing the source text.
        /// </summary>
        /// <param name="diagnosticOptions">An obsolete parameter. Diagnostic options should now be passed with <see cref="CompilationOptions.SyntaxTreeOptionsProvider"/></param>
        /// <param name="isGeneratedCode">An obsolete parameter. It is unused.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("The diagnosticOptions and isGeneratedCode parameters are obsolete due to performance problems, if you are using them use CompilationOptions.SyntaxTreeOptionsProvider instead", error: false)]
        public static SyntaxTree ParseText(
            string text,
            CSharpParseOptions? options,
            string path,
            Encoding? encoding,
            ImmutableDictionary<string, ReportDiagnostic>? diagnosticOptions,
            bool? isGeneratedCode,
            CancellationToken cancellationToken)
        {
            return ParseText(SourceText.From(text, encoding), options, path, diagnosticOptions, isGeneratedCode, cancellationToken);
        }

        // The overload that has more parameters is itself obsolete, as an intentional break to allow future
        // expansion
#pragma warning disable RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads.

        /// <summary>
        /// Produces a syntax tree by parsing the source text.
        /// </summary>
        public static SyntaxTree ParseText(
            SourceText text,
            CSharpParseOptions? options = null,
            string path = "",
            CancellationToken cancellationToken = default)
        {
#pragma warning disable CS0618 // We are calling into the obsolete member as that's the one that still does the real work
            return ParseText(text, options, path, diagnosticOptions: null, cancellationToken);
#pragma warning restore CS0618
        }

#pragma warning restore RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads.

        /// <summary>
        /// Produces a syntax tree by parsing the source text.
        /// </summary>
        /// <param name="diagnosticOptions">An obsolete parameter. Diagnostic options should now be passed with <see cref="CompilationOptions.SyntaxTreeOptionsProvider"/></param>
        /// <param name="isGeneratedCode">An obsolete parameter. It is unused.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("The diagnosticOptions and isGeneratedCode parameters are obsolete due to performance problems, if you are using them use CompilationOptions.SyntaxTreeOptionsProvider instead", error: false)]
        public static SyntaxTree ParseText(
            SourceText text,
            CSharpParseOptions? options,
            string path,
            ImmutableDictionary<string, ReportDiagnostic>? diagnosticOptions,
            bool? isGeneratedCode,
            CancellationToken cancellationToken)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            options = options ?? CSharpParseOptions.Default;

            using var lexer = new InternalSyntax.Lexer(text, options);
            using var parser = new InternalSyntax.LanguageParser(lexer, oldTree: null, changes: null, cancellationToken: cancellationToken);
            var compilationUnit = (CompilationUnitSyntax)parser.ParseCompilationUnit().CreateRed();
            var tree = new ParsedSyntaxTree(
                text,
                text.Encoding,
                text.ChecksumAlgorithm,
                path,
                options,
                compilationUnit,
                parser.Directives,
                diagnosticOptions: diagnosticOptions,
                cloneRoot: true);
            tree.VerifySource();
            return tree;
        }

        #endregion

        #region Changes

        /// <summary>
        /// Creates a new syntax based off this tree using a new source text.
        /// </summary>
        /// <remarks>
        /// If the new source text is a minor change from the current source text an incremental parse will occur
        /// reusing most of the current syntax tree internal data.  Otherwise, a full parse will occur using the new
        /// source text.
        /// </remarks>
        public override SyntaxTree WithChangedText(SourceText newText)
        {
            // try to find the changes between the old text and the new text.
            if (this.TryGetText(out SourceText? oldText))
            {
                var changes = newText.GetChangeRanges(oldText);

                if (changes.Count == 0 && newText == oldText)
                {
                    return this;
                }

                return this.WithChanges(newText, changes);
            }

            // if we do not easily know the old text, then specify entire text as changed so we do a full reparse.
            return this.WithChanges(newText, new[] { new TextChangeRange(new TextSpan(0, this.Length), newText.Length) });
        }

        private SyntaxTree WithChanges(SourceText newText, IReadOnlyList<TextChangeRange> changes)
        {
            if (changes == null)
            {
                throw new ArgumentNullException(nameof(changes));
            }

            IReadOnlyList<TextChangeRange>? workingChanges = changes;
            CSharpSyntaxTree? oldTree = this;

            // if changes is entire text do a full reparse
            if (workingChanges.Count == 1 && workingChanges[0].Span == new TextSpan(0, this.Length) && workingChanges[0].NewLength == newText.Length)
            {
                // parser will do a full parse if we give it no changes
                workingChanges = null;
                oldTree = null;
            }

            using var lexer = new InternalSyntax.Lexer(newText, this.Options);
            using var parser = new InternalSyntax.LanguageParser(lexer, oldTree?.GetRoot(), workingChanges);

            var compilationUnit = (CompilationUnitSyntax)parser.ParseCompilationUnit().CreateRed();
            var tree = new ParsedSyntaxTree(
                newText,
                newText.Encoding,
                newText.ChecksumAlgorithm,
                FilePath,
                Options,
                compilationUnit,
                parser.Directives,
#pragma warning disable CS0618
                DiagnosticOptions,
#pragma warning restore CS0618
                cloneRoot: true);
            tree.VerifySource(changes);
            return tree;
        }

        /// <summary>
        /// Produces a pessimistic list of spans that denote the regions of text in this tree that
        /// are changed from the text of the old tree.
        /// </summary>
        /// <param name="oldTree">The old tree. Cannot be <c>null</c>.</param>
        /// <remarks>The list is pessimistic because it may claim more or larger regions than actually changed.</remarks>
        public override IList<TextSpan> GetChangedSpans(SyntaxTree oldTree)
        {
            if (oldTree == null)
            {
                throw new ArgumentNullException(nameof(oldTree));
            }

            return SyntaxDiffer.GetPossiblyDifferentTextSpans(oldTree, this);
        }

        /// <summary>
        /// Gets a list of text changes that when applied to the old tree produce this tree.
        /// </summary>
        /// <param name="oldTree">The old tree. Cannot be <c>null</c>.</param>
        /// <remarks>The list of changes may be different than the original changes that produced this tree.</remarks>
        public override IList<TextChange> GetChanges(SyntaxTree oldTree)
        {
            if (oldTree == null)
            {
                throw new ArgumentNullException(nameof(oldTree));
            }

            return SyntaxDiffer.GetTextChanges(oldTree, this);
        }

        #endregion

        #region LinePositions and Locations

        private CSharpLineDirectiveMap GetDirectiveMap()
        {
            if (_lazyLineDirectiveMap == null)
            {
                // Create the line directive map on demand.
                Interlocked.CompareExchange(ref _lazyLineDirectiveMap, new CSharpLineDirectiveMap(this), null);
            }

            return _lazyLineDirectiveMap;
        }

        /// <summary>
        /// Gets the location in terms of path, line and column for a given span.
        /// </summary>
        /// <param name="span">Span within the tree.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// <see cref="FileLinePositionSpan"/> that contains path, line and column information.
        /// </returns>
        /// <remarks>The values are not affected by line mapping directives (<c>#line</c>).</remarks>
        public override FileLinePositionSpan GetLineSpan(TextSpan span, CancellationToken cancellationToken = default)
            => new(FilePath, GetLinePosition(span.Start, cancellationToken), GetLinePosition(span.End, cancellationToken));

        /// <summary>
        /// Gets the location in terms of path, line and column after applying source line mapping directives (<c>#line</c>).
        /// </summary>
        /// <param name="span">Span within the tree.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// <para>A valid <see cref="FileLinePositionSpan"/> that contains path, line and column information.</para>
        /// <para>
        /// If the location path is mapped the resulting path is the path specified in the corresponding <c>#line</c>,
        /// otherwise it's <see cref="SyntaxTree.FilePath"/>.
        /// </para>
        /// <para>
        /// A location path is considered mapped if the first <c>#line</c> directive that precedes it and that
        /// either specifies an explicit file path or is <c>#line default</c> exists and specifies an explicit path.
        /// </para>
        /// </returns>
        public override FileLinePositionSpan GetMappedLineSpan(TextSpan span, CancellationToken cancellationToken = default)
            => GetDirectiveMap().TranslateSpan(GetText(cancellationToken), this.FilePath, span);

        /// <inheritdoc/>
        public override LineVisibility GetLineVisibility(int position, CancellationToken cancellationToken = default)
            => GetDirectiveMap().GetLineVisibility(GetText(cancellationToken), position);

        /// <inheritdoc/>
        public override IEnumerable<LineMapping> GetLineMappings(CancellationToken cancellationToken = default)
        {
            var map = GetDirectiveMap();
            Debug.Assert(map.Entries.Length >= 1);
            return (map.Entries.Length == 1) ? Array.Empty<LineMapping>() : map.GetLineMappings(GetText(cancellationToken).Lines);
        }

        /// <summary>
        /// Gets a <see cref="FileLinePositionSpan"/> for a <see cref="TextSpan"/>. FileLinePositionSpans are used
        /// primarily for diagnostics and source locations.
        /// </summary>
        /// <param name="span">The source <see cref="TextSpan" /> to convert.</param>
        /// <param name="isHiddenPosition">When the method returns, contains a boolean value indicating whether this span is considered hidden or not.</param>
        /// <returns>A resulting <see cref="FileLinePositionSpan"/>.</returns>
        internal override FileLinePositionSpan GetMappedLineSpanAndVisibility(TextSpan span, out bool isHiddenPosition)
            => GetDirectiveMap().TranslateSpanAndVisibility(GetText(), FilePath, span, out isHiddenPosition);

        /// <summary>
        /// Gets a boolean value indicating whether there are any hidden regions in the tree.
        /// </summary>
        /// <returns>True if there is at least one hidden region.</returns>
        public override bool HasHiddenRegions()
            => GetDirectiveMap().HasAnyHiddenRegions();

        /// <summary>
        /// Given the error code and the source location, get the warning state based on <c>#pragma warning</c> directives.
        /// </summary>
        /// <param name="id">Error code.</param>
        /// <param name="position">Source location.</param>
        internal PragmaWarningState GetPragmaDirectiveWarningState(string id, int position)
        {
            if (_lazyPragmaWarningStateMap == null)
            {
                // Create the warning state map on demand.
                Interlocked.CompareExchange(ref _lazyPragmaWarningStateMap, new CSharpPragmaWarningStateMap(this), null);
            }

            return _lazyPragmaWarningStateMap.GetWarningState(id, position);
        }

        private NullableContextStateMap GetNullableContextStateMap()
        {
            if (_lazyNullableContextStateMap == null)
            {
                // Create the #nullable directive map on demand.
                Interlocked.CompareExchange(
                    ref _lazyNullableContextStateMap,
                    new StrongBox<NullableContextStateMap>(NullableContextStateMap.Create(this)),
                    null);
            }
            return _lazyNullableContextStateMap.Value;
        }

        internal NullableContextState GetNullableContextState(int position)
            => GetNullableContextStateMap().GetContextState(position);

        internal bool? IsNullableAnalysisEnabled(TextSpan span) => GetNullableContextStateMap().IsNullableAnalysisEnabled(span);

        internal bool IsGeneratedCode(SyntaxTreeOptionsProvider? provider, CancellationToken cancellationToken)
        {
            return provider?.IsGenerated(this, cancellationToken) switch
            {
                null or GeneratedKind.Unknown => isGeneratedHeuristic(),
                GeneratedKind kind => kind != GeneratedKind.NotGenerated
            };

            bool isGeneratedHeuristic()
            {
                if (_lazyIsGeneratedCode == GeneratedKind.Unknown)
                {
                    // Create the generated code status on demand
                    bool isGenerated = GeneratedCodeUtilities.IsGeneratedCode(
                            this,
                            isComment: trivia => trivia.Kind() == SyntaxKind.SingleLineCommentTrivia || trivia.Kind() == SyntaxKind.MultiLineCommentTrivia,
                            cancellationToken: default);
                    _lazyIsGeneratedCode = isGenerated ? GeneratedKind.MarkedGenerated : GeneratedKind.NotGenerated;
                }

                return _lazyIsGeneratedCode == GeneratedKind.MarkedGenerated;
            }
        }

        private CSharpLineDirectiveMap? _lazyLineDirectiveMap;
        private CSharpPragmaWarningStateMap? _lazyPragmaWarningStateMap;
        private StrongBox<NullableContextStateMap>? _lazyNullableContextStateMap;

        private GeneratedKind _lazyIsGeneratedCode = GeneratedKind.Unknown;

        private LinePosition GetLinePosition(int position, CancellationToken cancellationToken)
            => GetText(cancellationToken).Lines.GetLinePosition(position);

        /// <summary>
        /// Gets a <see cref="Location"/> for the specified text <paramref name="span"/>.
        /// </summary>
        public override Location GetLocation(TextSpan span)
        {
            return new SourceLocation(this, span);
        }

        #endregion

        #region Diagnostics

        /// <summary>
        /// Gets a list of all the diagnostics in the sub tree that has the specified node as its root.
        /// </summary>
        /// <remarks>
        /// This method does not filter diagnostics based on <c>#pragma</c>s and compiler options
        /// like /nowarn, /warnaserror etc.
        /// </remarks>
        public override IEnumerable<Diagnostic> GetDiagnostics(SyntaxNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            return GetDiagnostics(node.Green, node.Position);
        }

        private IEnumerable<Diagnostic> GetDiagnostics(GreenNode greenNode, int position)
        {
            if (greenNode == null)
            {
                throw new InvalidOperationException();
            }

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
        /// </summary>
        /// <remarks>
        /// This method does not filter diagnostics based on <c>#pragma</c>s and compiler options
        /// like /nowarn, /warnaserror etc.
        /// </remarks>
        public override IEnumerable<Diagnostic> GetDiagnostics(SyntaxToken token)
        {
            if (token.Node == null)
            {
                throw new InvalidOperationException();
            }
            return GetDiagnostics(token.Node, token.Position);
        }

        /// <summary>
        /// Gets a list of all the diagnostics associated with the trivia.
        /// </summary>
        /// <remarks>
        /// This method does not filter diagnostics based on <c>#pragma</c>s and compiler options
        /// like /nowarn, /warnaserror etc.
        /// </remarks>
        public override IEnumerable<Diagnostic> GetDiagnostics(SyntaxTrivia trivia)
        {
            if (trivia.UnderlyingNode == null)
            {
                throw new InvalidOperationException();
            }
            return GetDiagnostics(trivia.UnderlyingNode, trivia.Position);
        }

        /// <summary>
        /// Gets a list of all the diagnostics in either the sub tree that has the specified node as its root or
        /// associated with the token and its related trivia.
        /// </summary>
        /// <remarks>
        /// This method does not filter diagnostics based on <c>#pragma</c>s and compiler options
        /// like /nowarn, /warnaserror etc.
        /// </remarks>
        public override IEnumerable<Diagnostic> GetDiagnostics(SyntaxNodeOrToken nodeOrToken)
        {
            if (nodeOrToken.UnderlyingNode == null)
            {
                throw new InvalidOperationException();
            }
            return GetDiagnostics(nodeOrToken.UnderlyingNode, nodeOrToken.Position);
        }

        /// <summary>
        /// Gets a list of all the diagnostics in the syntax tree.
        /// </summary>
        /// <remarks>
        /// This method does not filter diagnostics based on <c>#pragma</c>s and compiler options
        /// like /nowarn, /warnaserror etc.
        /// </remarks>
        public override IEnumerable<Diagnostic> GetDiagnostics(CancellationToken cancellationToken = default)
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

        protected override bool TryGetRootCore([NotNullWhen(true)] out SyntaxNode? root)
        {
            if (this.TryGetRoot(out CSharpSyntaxNode? node))
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

        // 3.3 BACK COMPAT OVERLOAD -- DO NOT MODIFY
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("The diagnosticOptions parameter is obsolete due to performance problems, if you are passing non-null use CompilationOptions.SyntaxTreeOptionsProvider instead", error: false)]
        public static SyntaxTree ParseText(
            SourceText text,
            CSharpParseOptions? options,
            string path,
            ImmutableDictionary<string, ReportDiagnostic>? diagnosticOptions,
            CancellationToken cancellationToken)
            => ParseText(text, options, path, diagnosticOptions, isGeneratedCode: null, cancellationToken);

        // 3.3 BACK COMPAT OVERLOAD -- DO NOT MODIFY
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("The diagnosticOptions parameter is obsolete due to performance problems, if you are passing non-null use CompilationOptions.SyntaxTreeOptionsProvider instead", error: false)]
        public static SyntaxTree ParseText(
            string text,
            CSharpParseOptions? options,
            string path,
            Encoding? encoding,
            ImmutableDictionary<string, ReportDiagnostic>? diagnosticOptions,
            CancellationToken cancellationToken)
            => ParseText(text, options, path, encoding, diagnosticOptions, isGeneratedCode: null, cancellationToken);

        // 3.3 BACK COMPAT OVERLOAD -- DO NOT MODIFY
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("The diagnosticOptions parameter is obsolete due to performance problems, if you are passing non-null use CompilationOptions.SyntaxTreeOptionsProvider instead", error: false)]
        public static SyntaxTree Create(
            CSharpSyntaxNode root,
            CSharpParseOptions? options,
            string? path,
            Encoding? encoding,
            ImmutableDictionary<string, ReportDiagnostic>? diagnosticOptions)
            => Create(root, options, path, encoding, diagnosticOptions, isGeneratedCode: null);

    }
}
