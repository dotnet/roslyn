// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract partial class AbstractSuppressionCodeFixProvider : IConfigurationFixProvider
    {
        public const string SuppressMessageAttributeName = "System.Diagnostics.CodeAnalysis.SuppressMessage";
        private const string s_globalSuppressionsFileName = "GlobalSuppressions";
        private const string s_suppressionsFileCommentTemplate =
@"{0} This file is used by Code Analysis to maintain SuppressMessage
{0} attributes that are applied to this project.
{0} Project-level suppressions either have no target or are given
{0} a specific target and scoped to a namespace, type, member, etc.

";
        protected AbstractSuppressionCodeFixProvider()
        {
        }

        public FixAllProvider GetFixAllProvider()
        {
            return SuppressionFixAllProvider.Instance;
        }

        public bool IsFixableDiagnostic(Diagnostic diagnostic)
        {
            return SuppressionHelpers.CanBeSuppressed(diagnostic) || SuppressionHelpers.CanBeUnsuppressed(diagnostic);
        }

        protected abstract SyntaxTriviaList CreatePragmaDisableDirectiveTrivia(Diagnostic diagnostic, Func<SyntaxNode, SyntaxNode> formatNode, bool needsLeadingEndOfLine, bool needsTrailingEndOfLine);
        protected abstract SyntaxTriviaList CreatePragmaRestoreDirectiveTrivia(Diagnostic diagnostic, Func<SyntaxNode, SyntaxNode> formatNode, bool needsLeadingEndOfLine, bool needsTrailingEndOfLine);

        protected abstract SyntaxNode AddGlobalSuppressMessageAttribute(SyntaxNode newRoot, ISymbol targetSymbol, Diagnostic diagnostic, Workspace workspace, CancellationToken cancellationToken);
        protected abstract SyntaxNode AddLocalSuppressMessageAttribute(SyntaxNode targetNode, ISymbol targetSymbol, Diagnostic diagnostic);

        protected abstract string DefaultFileExtension { get; }
        protected abstract string SingleLineCommentStart { get; }
        protected abstract bool IsAttributeListWithAssemblyAttributes(SyntaxNode node);
        protected abstract bool IsEndOfLine(SyntaxTrivia trivia);
        protected abstract bool IsEndOfFileToken(SyntaxToken token);
        protected abstract bool IsSingleAttributeInAttributeList(SyntaxNode attribute);
        protected abstract bool IsAnyPragmaDirectiveForId(SyntaxTrivia trivia, string id, out bool enableDirective, out bool hasMultipleIds);
        protected abstract SyntaxTrivia TogglePragmaDirective(SyntaxTrivia trivia);

        protected string GlobalSuppressionsFileHeaderComment
        {
            get
            {
                return string.Format(s_suppressionsFileCommentTemplate, SingleLineCommentStart);
            }
        }

        protected string GetOrMapDiagnosticId(Diagnostic diagnostic, out bool includeTitle)
        {
            if (diagnostic.Id == IDEDiagnosticIds.FormattingDiagnosticId)
            {
                includeTitle = false;
                return FormattingDiagnosticIds.FormatDocumentControlDiagnosticId;
            }

            includeTitle = true;
            return diagnostic.Id;
        }

        protected virtual SyntaxToken GetAdjustedTokenForPragmaDisable(SyntaxToken token, SyntaxNode root, TextLineCollection lines, int indexOfLine)
        {
            return token;
        }

        protected virtual SyntaxToken GetAdjustedTokenForPragmaRestore(SyntaxToken token, SyntaxNode root, TextLineCollection lines, int indexOfLine)
        {
            return token;
        }

        public Task<ImmutableArray<CodeFix>> GetFixesAsync(
            Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            return GetSuppressionsAsync(document, span, diagnostics, skipSuppressMessage: false, skipUnsuppress: false, cancellationToken: cancellationToken);
        }

        internal async Task<ImmutableArray<PragmaWarningCodeAction>> GetPragmaSuppressionsAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var codeFixes = await GetSuppressionsAsync(document, span, diagnostics, skipSuppressMessage: true, skipUnsuppress: true, cancellationToken: cancellationToken).ConfigureAwait(false);
            return codeFixes.SelectMany(fix => fix.Action.NestedCodeActions)
                            .OfType<PragmaWarningCodeAction>()
                            .ToImmutableArray();
        }

        private async Task<ImmutableArray<CodeFix>> GetSuppressionsAsync(
            Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, bool skipSuppressMessage, bool skipUnsuppress, CancellationToken cancellationToken)
        {
            var suppressionTargetInfo = await GetSuppressionTargetInfoAsync(document, span, cancellationToken).ConfigureAwait(false);
            if (suppressionTargetInfo == null)
            {
                return ImmutableArray<CodeFix>.Empty;
            }

            return await GetSuppressionsAsync(
                documentOpt: document, project: document.Project, diagnostics: diagnostics,
                suppressionTargetInfo: suppressionTargetInfo, skipSuppressMessage: skipSuppressMessage,
                skipUnsuppress: skipUnsuppress, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task<ImmutableArray<CodeFix>> GetFixesAsync(
            Project project, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            if (!project.SupportsCompilation)
            {
                return ImmutableArray<CodeFix>.Empty;
            }

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var suppressionTargetInfo = new SuppressionTargetInfo() { TargetSymbol = compilation.Assembly };
            return await GetSuppressionsAsync(
                documentOpt: null, project: project, diagnostics: diagnostics,
                suppressionTargetInfo: suppressionTargetInfo, skipSuppressMessage: false,
                skipUnsuppress: false, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private async Task<ImmutableArray<CodeFix>> GetSuppressionsAsync(
            Document documentOpt, Project project, IEnumerable<Diagnostic> diagnostics, SuppressionTargetInfo suppressionTargetInfo, bool skipSuppressMessage, bool skipUnsuppress, CancellationToken cancellationToken)
        {
            // We only care about diagnostics that can be suppressed/unsuppressed.
            diagnostics = diagnostics.Where(IsFixableDiagnostic);
            if (diagnostics.IsEmpty())
            {
                return ImmutableArray<CodeFix>.Empty;
            }

            if (!skipSuppressMessage)
            {
                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var suppressMessageAttribute = compilation.SuppressMessageAttributeType();
                skipSuppressMessage = suppressMessageAttribute == null || !suppressMessageAttribute.IsAttribute();
            }

            var result = ArrayBuilder<CodeFix>.GetInstance();
            foreach (var diagnostic in diagnostics)
            {
                if (!diagnostic.IsSuppressed)
                {
                    var nestedActions = ArrayBuilder<NestedSuppressionCodeAction>.GetInstance();
                    if (diagnostic.Location.IsInSource && documentOpt != null)
                    {
                        // pragma warning disable.
                        nestedActions.Add(PragmaWarningCodeAction.Create(suppressionTargetInfo, documentOpt, diagnostic, this));
                    }

                    // SuppressMessageAttribute suppression is not supported for compiler diagnostics.
                    if (!skipSuppressMessage && SuppressionHelpers.CanBeSuppressedWithAttribute(diagnostic))
                    {
                        // global assembly-level suppress message attribute.
                        nestedActions.Add(new GlobalSuppressMessageCodeAction(suppressionTargetInfo.TargetSymbol, project, diagnostic, this));

                        // local suppress message attribute
                        // please note that in order to avoid issues with exising unit tests referencing the code fix
                        // by their index this needs to be the last added to nestedActions
                        if (suppressionTargetInfo.TargetMemberNode != null && suppressionTargetInfo.TargetSymbol.Kind != SymbolKind.Namespace)
                        {
                            nestedActions.Add(new LocalSuppressMessageCodeAction(this, suppressionTargetInfo.TargetSymbol, suppressionTargetInfo.TargetMemberNode, documentOpt, diagnostic));
                        }
                    }

                    if (nestedActions.Count > 0)
                    {
                        var codeAction = new TopLevelSuppressionCodeAction(
                            diagnostic, nestedActions.ToImmutableAndFree());
                        result.Add(new CodeFix(project, codeAction, diagnostic));
                    }
                }
                else if (!skipUnsuppress)
                {
                    var codeAction = await RemoveSuppressionCodeAction.CreateAsync(suppressionTargetInfo, documentOpt, project, diagnostic, this, cancellationToken).ConfigureAwait(false);
                    if (codeAction != null)
                    {
                        result.Add(new CodeFix(project, codeAction, diagnostic));
                    }
                }
            }

            return result.ToImmutableAndFree();
        }

        internal class SuppressionTargetInfo
        {
            public ISymbol TargetSymbol { get; set; }
            public SyntaxToken StartToken { get; set; }
            public SyntaxToken EndToken { get; set; }
            public SyntaxNode NodeWithTokens { get; set; }
            public SyntaxNode TargetMemberNode { get; set; }
        }

        private async Task<SuppressionTargetInfo> GetSuppressionTargetInfoAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxTree.GetLineVisibility(span.Start, cancellationToken) == LineVisibility.Hidden)
            {
                return null;
            }

            // Find the start token to attach leading pragma disable warning directive.
            var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var lines = syntaxTree.GetText(cancellationToken).Lines;
            var indexOfLine = lines.IndexOf(span.Start);
            var lineAtPos = lines[indexOfLine];
            var startToken = root.FindToken(lineAtPos.Start);
            startToken = GetAdjustedTokenForPragmaDisable(startToken, root, lines, indexOfLine);

            // Find the end token to attach pragma restore warning directive.
            var spanEnd = Math.Max(startToken.Span.End, span.End);
            indexOfLine = lines.IndexOf(spanEnd);
            lineAtPos = lines[indexOfLine];
            var endToken = root.FindToken(lineAtPos.End);
            endToken = GetAdjustedTokenForPragmaRestore(endToken, root, lines, indexOfLine);

            var nodeWithTokens = GetNodeWithTokens(startToken, endToken, root);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            ISymbol targetSymbol = null;
            var targetMemberNode = syntaxFacts.GetContainingMemberDeclaration(root, nodeWithTokens.SpanStart);
            if (targetMemberNode != null)
            {
                targetSymbol = semanticModel.GetDeclaredSymbol(targetMemberNode, cancellationToken);

                if (targetSymbol == null)
                {
                    var analyzerDriverService = document.GetLanguageService<IAnalyzerDriverService>();

                    // targetMemberNode could be a declaration node with multiple decls (e.g. field declaration defining multiple variables).
                    // Let us compute all the declarations intersecting the span.
                    var declsBuilder = ArrayBuilder<DeclarationInfo>.GetInstance();
                    analyzerDriverService.ComputeDeclarationsInSpan(semanticModel, span, true, declsBuilder, cancellationToken);
                    var decls = declsBuilder.ToImmutableAndFree();

                    if (!decls.IsEmpty)
                    {
                        var containedDecls = decls.Where(d => span.Contains(d.DeclaredNode.Span));
                        if (containedDecls.Count() == 1)
                        {
                            // Single containing declaration, use this symbol.
                            var decl = containedDecls.Single();
                            targetSymbol = decl.DeclaredSymbol;
                        }
                        else
                        {
                            // Otherwise, use the most enclosing declaration.
                            TextSpan? minContainingSpan = null;
                            foreach (var decl in decls)
                            {
                                var declSpan = decl.DeclaredNode.Span;
                                if (declSpan.Contains(span) &&
                                    (!minContainingSpan.HasValue || minContainingSpan.Value.Contains(declSpan)))
                                {
                                    minContainingSpan = declSpan;
                                    targetSymbol = decl.DeclaredSymbol;
                                }
                            }
                        }
                    }
                }
            }

            if (targetSymbol == null)
            {
                // Outside of a member declaration, suppress diagnostic for the entire assembly.
                targetSymbol = semanticModel.Compilation.Assembly;
            }

            return new SuppressionTargetInfo() { TargetSymbol = targetSymbol, NodeWithTokens = nodeWithTokens, StartToken = startToken, EndToken = endToken, TargetMemberNode = targetMemberNode };
        }

        internal SyntaxNode GetNodeWithTokens(SyntaxToken startToken, SyntaxToken endToken, SyntaxNode root)
        {
            if (IsEndOfFileToken(endToken))
            {
                return root;
            }
            else
            {
                return startToken.GetCommonRoot(endToken);
            }
        }

        protected string GetScopeString(SymbolKind targetSymbolKind)
        {
            switch (targetSymbolKind)
            {
                case SymbolKind.Event:
                case SymbolKind.Field:
                case SymbolKind.Method:
                case SymbolKind.Property:
                    return "member";

                case SymbolKind.NamedType:
                    return "type";

                case SymbolKind.Namespace:
                    return "namespace";

                default:
                    return null;
            }
        }

        protected string GetTargetString(ISymbol targetSymbol)
        {
            return "~" + DocumentationCommentId.CreateDeclarationId(targetSymbol);
        }
    }
}
