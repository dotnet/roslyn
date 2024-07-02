// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression;

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
        => SuppressionFixAllProvider.Instance;

    public bool IsFixableDiagnostic(Diagnostic diagnostic)
        => SuppressionHelpers.CanBeSuppressed(diagnostic) || SuppressionHelpers.CanBeUnsuppressed(diagnostic);

    protected abstract SyntaxTriviaList CreatePragmaDisableDirectiveTrivia(Diagnostic diagnostic, Func<SyntaxNode, CancellationToken, SyntaxNode> formatNode, bool needsLeadingEndOfLine, bool needsTrailingEndOfLine, CancellationToken cancellationToken);
    protected abstract SyntaxTriviaList CreatePragmaRestoreDirectiveTrivia(Diagnostic diagnostic, Func<SyntaxNode, CancellationToken, SyntaxNode> formatNode, bool needsLeadingEndOfLine, bool needsTrailingEndOfLine, CancellationToken cancellationToken);

    protected abstract SyntaxNode AddGlobalSuppressMessageAttribute(
        SyntaxNode newRoot,
        ISymbol targetSymbol,
        INamedTypeSymbol suppressMessageAttribute,
        Diagnostic diagnostic,
        SolutionServices services,
        SyntaxFormattingOptions options,
        IAddImportsService addImportsService,
        CancellationToken cancellationToken);

    protected abstract SyntaxNode AddLocalSuppressMessageAttribute(
        SyntaxNode targetNode, ISymbol targetSymbol, INamedTypeSymbol suppressMessageAttribute, Diagnostic diagnostic);

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

    protected static string GetOrMapDiagnosticId(Diagnostic diagnostic, out bool includeTitle)
    {
        if (diagnostic.Id == IDEDiagnosticIds.FormattingDiagnosticId)
        {
            includeTitle = false;
            return FormattingDiagnosticIds.FormatDocumentControlDiagnosticId;
        }

        includeTitle = true;
        return diagnostic.Id;
    }

    protected abstract SyntaxNode GetContainingStatement(SyntaxToken token);
    protected abstract bool TokenHasTrailingLineContinuationChar(SyntaxToken token);

    protected SyntaxToken GetAdjustedTokenForPragmaDisable(SyntaxToken token, SyntaxNode root, TextLineCollection lines)
    {
        var containingStatement = GetContainingStatement(token);

        // The containing statement might not start on the same line as the token, but we don't want to split
        // a statement in the middle, so we actually want to use the first token on the line that has the first token
        // of the statement.
        //
        // eg, given: public void M() { int x = 1; }
        //
        // When trying to suppress an "unused local" for x, token would be "x", the first token
        // of the containing statement is "int", but we want the pragma before "public".
        if (containingStatement is not null && containingStatement.GetFirstToken() != token)
        {
            var indexOfLine = lines.IndexOf(containingStatement.GetFirstToken().SpanStart);
            var line = lines[indexOfLine];
            token = root.FindToken(line.Start);
        }

        return token;
    }

    private SyntaxToken GetAdjustedTokenForPragmaRestore(SyntaxToken token, SyntaxNode root, TextLineCollection lines, int indexOfLine)
    {
        var containingStatement = GetContainingStatement(token);

        // As per above, the last token of the statement might not be the last token on the line
        if (containingStatement is not null && containingStatement.GetLastToken() != token)
        {
            indexOfLine = lines.IndexOf(containingStatement.GetLastToken().SpanStart);
        }

        var line = lines[indexOfLine];
        token = root.FindToken(line.End);

        // VB has line continuation characters that can explicitly extend the line beyond the last
        // token, so allow for that by just skipping over them
        while (TokenHasTrailingLineContinuationChar(token))
        {
            indexOfLine = indexOfLine + 1;
            token = root.FindToken(lines[indexOfLine].End, findInsideTrivia: true);
        }

        return token;
    }

    public Task<ImmutableArray<CodeFix>> GetFixesAsync(
        TextDocument textDocument, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
    {
        if (textDocument is not Document document)
            return Task.FromResult(ImmutableArray<CodeFix>.Empty);

        return GetSuppressionsAsync(document, span, diagnostics, skipSuppressMessage: false, skipUnsuppress: false, cancellationToken: cancellationToken);
    }

    internal async Task<ImmutableArray<PragmaWarningCodeAction>> GetPragmaSuppressionsAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
    {
        var codeFixes = await GetSuppressionsAsync(document, span, diagnostics, skipSuppressMessage: true, skipUnsuppress: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        return codeFixes.SelectMany(fix => fix.Action.NestedActions)
                        .OfType<PragmaWarningCodeAction>()
                        .ToImmutableArray();
    }

    private async Task<ImmutableArray<CodeFix>> GetSuppressionsAsync(
        Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, bool skipSuppressMessage, bool skipUnsuppress, CancellationToken cancellationToken)
    {
        var suppressionTargetInfo = await GetSuppressionTargetInfoAsync(document, span, cancellationToken).ConfigureAwait(false);
        if (suppressionTargetInfo == null)
        {
            return [];
        }

        return await GetSuppressionsAsync(
            document, document.Project, diagnostics, suppressionTargetInfo, skipSuppressMessage, skipUnsuppress, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ImmutableArray<CodeFix>> GetFixesAsync(
        Project project, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
    {
        if (!project.SupportsCompilation)
        {
            return [];
        }

        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        var suppressionTargetInfo = new SuppressionTargetInfo() { TargetSymbol = compilation.Assembly };
        return await GetSuppressionsAsync(
            documentOpt: null, project, diagnostics, suppressionTargetInfo,
            skipSuppressMessage: false, skipUnsuppress: false,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<ImmutableArray<CodeFix>> GetSuppressionsAsync(
        Document documentOpt, Project project, IEnumerable<Diagnostic> diagnostics, SuppressionTargetInfo suppressionTargetInfo, bool skipSuppressMessage, bool skipUnsuppress, CancellationToken cancellationToken)
    {
        // We only care about diagnostics that can be suppressed/unsuppressed.
        diagnostics = diagnostics.Where(IsFixableDiagnostic);
        if (diagnostics.IsEmpty())
        {
            return [];
        }

        INamedTypeSymbol suppressMessageAttribute = null;
        if (!skipSuppressMessage)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            suppressMessageAttribute = compilation.SuppressMessageAttributeType();
            skipSuppressMessage = suppressMessageAttribute == null || !suppressMessageAttribute.IsAttribute();
        }

        var lazyFormattingOptions = (SyntaxFormattingOptions)null;
        var result = ArrayBuilder<CodeFix>.GetInstance();
        foreach (var diagnostic in diagnostics)
        {
            if (!diagnostic.IsSuppressed)
            {
                var nestedActions = ArrayBuilder<NestedSuppressionCodeAction>.GetInstance();
                if (diagnostic.Location.IsInSource && documentOpt != null)
                {
                    // pragma warning disable.
                    lazyFormattingOptions ??= await documentOpt.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);
                    nestedActions.Add(PragmaWarningCodeAction.Create(suppressionTargetInfo, documentOpt, lazyFormattingOptions, diagnostic, this));
                }

                // SuppressMessageAttribute suppression is not supported for compiler diagnostics.
                if (!skipSuppressMessage && SuppressionHelpers.CanBeSuppressedWithAttribute(diagnostic))
                {
                    // global assembly-level suppress message attribute.
                    nestedActions.Add(new GlobalSuppressMessageCodeAction(
                        suppressionTargetInfo.TargetSymbol, suppressMessageAttribute, project, diagnostic, this));

                    // local suppress message attribute
                    // please note that in order to avoid issues with existing unit tests referencing the code fix
                    // by their index this needs to be the last added to nestedActions
                    if (suppressionTargetInfo.TargetMemberNode != null && suppressionTargetInfo.TargetSymbol.Kind != SymbolKind.Namespace)
                    {
                        nestedActions.Add(new LocalSuppressMessageCodeAction(
                            this, suppressionTargetInfo.TargetSymbol, suppressMessageAttribute, suppressionTargetInfo.TargetMemberNode, documentOpt, diagnostic));
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
        startToken = GetAdjustedTokenForPragmaDisable(startToken, root, lines);

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

        // Outside of a member declaration, suppress diagnostic for the entire assembly.
        targetSymbol ??= semanticModel.Compilation.Assembly;

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

    protected static string GetScopeString(SymbolKind targetSymbolKind)
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

    protected static string GetTargetString(ISymbol targetSymbol)
        => "~" + DocumentationCommentId.CreateDeclarationId(targetSymbol);
}
