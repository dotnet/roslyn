// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Completion.Log;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal abstract class AbstractImportCompletionProvider : LSPCompletionProvider, INotifyCommittingItemCompletionProvider
{
    protected abstract bool ShouldProvideCompletion(CompletionContext completionContext, SyntaxContext syntaxContext);
    protected abstract void WarmUpCacheInBackground(Document document);
    protected abstract Task AddCompletionItemsAsync(CompletionContext completionContext, SyntaxContext syntaxContext, HashSet<string> namespacesInScope, CancellationToken cancellationToken);
    protected abstract bool IsFinalSemicolonOfUsingOrExtern(SyntaxNode directive, SyntaxToken token);
    protected abstract Task<bool> ShouldProvideParenthesisCompletionAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken);
    protected abstract void LogCommit();

    public Task NotifyCommittingItemAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
    {
        LogCommit();
        return Task.CompletedTask;
    }

    internal override bool IsExpandItemProvider => true;

    public override async Task ProvideCompletionsAsync(CompletionContext completionContext)
    {
        if (!completionContext.CompletionOptions.ShouldShowItemsFromUnimportedNamespaces)
            return;

        var cancellationToken = completionContext.CancellationToken;
        var document = completionContext.Document;

        var syntaxContext = await completionContext.GetSyntaxContextWithExistingSpeculativeModelAsync(document, cancellationToken).ConfigureAwait(false);

        if (!ShouldProvideCompletion(completionContext, syntaxContext))
        {
            // Queue a background task to warm up cache and return immediately if this is not the context to trigger this provider.
            // `ForceExpandedCompletionIndexCreation` and `UpdateImportCompletionCacheInBackground` are both test only options to
            // make test behavior deterministic.
            var options = completionContext.CompletionOptions;
            if (options.UpdateImportCompletionCacheInBackground && !options.ForceExpandedCompletionIndexCreation)
                WarmUpCacheInBackground(document);

            return;
        }

        // Find all namespaces in scope at current cursor location, 
        // which will be used to filter so the provider only returns out-of-scope types.
        var namespacesInScope = GetNamespacesInScope(syntaxContext, cancellationToken);
        await AddCompletionItemsAsync(completionContext, syntaxContext, namespacesInScope, cancellationToken).ConfigureAwait(false);
    }

    private static HashSet<string> GetNamespacesInScope(SyntaxContext syntaxContext, CancellationToken cancellationToken)
    {
        var semanticModel = syntaxContext.SemanticModel;
        var document = syntaxContext.Document;

        var importedNamespaces = GetImportedNamespaces(syntaxContext, cancellationToken);

        // This hashset will be used to match namespace names, so it must have the same case-sensitivity as the source language.
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var namespacesInScope = new HashSet<string>(importedNamespaces, syntaxFacts.StringComparer);

        // Get containing namespaces.
        var namespaceSymbol = semanticModel.GetEnclosingNamespace(syntaxContext.Position, cancellationToken);
        while (namespaceSymbol != null)
        {
            namespacesInScope.Add(namespaceSymbol.ToDisplayString(SymbolDisplayFormats.NameFormat));
            namespaceSymbol = namespaceSymbol.ContainingNamespace;
        }

        return namespacesInScope;
    }

    private static ImmutableArray<string> GetImportedNamespaces(SyntaxContext context, CancellationToken cancellationToken)
    {
        var position = context.Position;
        var targetToken = context.TargetToken;

        // If we are immediately after `using` directive adjust position to the start of the next token.
        // This is a workaround for an issue, when immediately after a `using` directive it is not included into the import scope.
        // See https://github.com/dotnet/roslyn/issues/67447 for more info.
        if (context.IsRightAfterUsingOrImportDirective)
            position = targetToken.GetNextToken(includeZeroWidth: true).SpanStart;

        var scopes = context.SemanticModel.GetImportScopes(position, cancellationToken);

        using var _ = ArrayBuilder<string>.GetInstance(out var usingsBuilder);

        foreach (var scope in scopes)
        {
            foreach (var import in scope.Imports)
            {
                if (import.NamespaceOrType is INamespaceSymbol @namespace)
                {
                    usingsBuilder.Add(@namespace.ToDisplayString(SymbolDisplayFormats.NameFormat));
                }
            }
        }

        return usingsBuilder.ToImmutableAndClear();
    }

    public override async Task<CompletionChange> GetChangeAsync(
        Document document, CompletionItem completionItem, char? commitKey, CancellationToken cancellationToken)
    {
        var containingNamespace = ImportCompletionItem.GetContainingNamespace(completionItem);
        var provideParenthesisCompletion = await ShouldProvideParenthesisCompletionAsync(
            document,
            completionItem,
            commitKey,
            cancellationToken).ConfigureAwait(false);

        var insertText = completionItem.DisplayText;
        if (provideParenthesisCompletion)
        {
            insertText += "()";
            CompletionProvidersLogger.LogCustomizedCommitToAddParenthesis(commitKey);
        }

        if (await ShouldCompleteWithFullyQualifyTypeNameAsync().ConfigureAwait(false))
        {
            var completionText = $"{containingNamespace}.{insertText}";
            return CompletionChange.Create(new TextChange(completionItem.Span, completionText));
        }

        // Find context node so we can use it to decide where to insert using/imports.
        var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        var addImportContextNode = root.FindToken(completionItem.Span.Start, findInsideTrivia: true).Parent;

        // Add required using/imports directive.
        var addImportService = document.GetRequiredLanguageService<IAddImportsService>();
        var generator = document.GetRequiredLanguageService<SyntaxGenerator>();

        var addImportsOptions = await document.GetAddImportPlacementOptionsAsync(cancellationToken).ConfigureAwait(false);
        var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);

        var importNode = CreateImport(document, containingNamespace);

        var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
        var rootWithImport = addImportService.AddImport(compilation, root, addImportContextNode!, importNode, generator, addImportsOptions, cancellationToken);
        var documentWithImport = document.WithSyntaxRoot(rootWithImport);
        // This only formats the annotated import we just added, not the entire document.
        var formattedDocumentWithImport = await Formatter.FormatAsync(documentWithImport, Formatter.Annotation, formattingOptions, cancellationToken).ConfigureAwait(false);

        using var _ = ArrayBuilder<TextChange>.GetInstance(out var builder);

        // Get text change for add import
        var importChanges = await formattedDocumentWithImport.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);
        builder.AddRange(importChanges);

        // Create text change for complete type name.
        //
        // Note: Don't try to obtain TextChange for completed type name by replacing the text directly, 
        //       then use Document.GetTextChangesAsync on document created from the changed text. This is
        //       because it will do a diff and return TextChanges with minimum span instead of actual 
        //       replacement span.
        //
        //       For example: If I'm typing "asd", the completion provider could be triggered after "a"
        //       is typed. Then if I selected type "AsnEncodedData" to commit, by using the approach described 
        //       above, we will get a TextChange of "AsnEncodedDat" with 0 length span, instead of a change of 
        //       the full display text with a span of length 1. This will later mess up span-tracking and end up 
        //       with "AsnEncodedDatasd" in the code.
        builder.Add(new TextChange(completionItem.Span, insertText));

        // Then get the combined change
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var newText = text.WithChanges(builder);

        var changes = builder.ToImmutable();
        var change = Utilities.Collapse(newText, changes);
        return CompletionChange.Create(change, changes);

        async Task<bool> ShouldCompleteWithFullyQualifyTypeNameAsync()
        {
            if (ImportCompletionItem.ShouldAlwaysFullyQualify(completionItem))
                return true;

            if (!IsAddingImportsSupported(document, completionOptions: null))
                return true;

            // We might need to qualify unimported types to use them in an import directive, because they only affect members of the containing
            // import container (e.g. namespace/class/etc. declarations).
            //
            // For example, `List` and `StringBuilder` both need to be fully qualified below: 
            // 
            //      using CollectionOfStringBuilders = System.Collections.Generic.List<System.Text.StringBuilder>;
            //
            // However, if we are typing in an C# using directive that is inside a nested import container (i.e. inside a namespace declaration block), 
            // then we can add an using in the outer import container instead (this is not allowed in VB). 
            //
            // For example:
            //
            //      using System.Collections.Generic;
            //      using System.Text;
            //
            //      namespace Foo
            //      {
            //          using CollectionOfStringBuilders = List<StringBuilder>;
            //      }
            //
            // Here we will always choose to qualify the unimported type, just to be consistent and keeps things simple.
            return await IsInImportsDirectiveAsync(document, completionItem.Span.Start, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<bool> IsInImportsDirectiveAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var leftToken = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDirectives: true);
        return leftToken.GetAncestor(syntaxFacts.IsUsingOrExternOrImport) is { } node
            && !IsFinalSemicolonOfUsingOrExtern(node, leftToken);
    }

    protected static bool IsAddingImportsSupported(Document document, CompletionOptions? completionOptions)
    {
        // Certain documents, e.g. Razor document, don't support adding imports
        return completionOptions?.CanAddImportStatement != false &&
            document.Project.Solution.Services.GetRequiredService<IDocumentSupportsFeatureService>().SupportsRefactorings(document);
    }

    private static SyntaxNode CreateImport(Document document, string namespaceName)
    {
        var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
        return syntaxGenerator.NamespaceImportDeclaration(namespaceName).WithAdditionalAnnotations(Formatter.Annotation);
    }

    internal override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
        => ImportCompletionItem.GetCompletionDescriptionAsync(document, item, displayOptions, cancellationToken);
}
