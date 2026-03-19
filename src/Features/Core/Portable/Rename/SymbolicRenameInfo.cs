// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename;

internal sealed class SymbolicRenameInfo
{
    private const string AttributeSuffix = "Attribute";

    [MemberNotNullWhen(true, nameof(LocalizedErrorMessage))]
    [MemberNotNullWhen(false, nameof(Document))]
    [MemberNotNullWhen(false, nameof(TriggerText))]
    [MemberNotNullWhen(false, nameof(Symbol))]
    public bool IsError => LocalizedErrorMessage != null;

    public string? LocalizedErrorMessage { get; }

    public Document? Document { get; }
    public SyntaxToken TriggerToken { get; }
    public string? TriggerText { get; }
    public ISymbol? Symbol { get; }
    public bool ForceRenameOverloads { get; }
    public ImmutableArray<DocumentSpan> DocumentSpans { get; }

    public bool IsRenamingAttributePrefix { get; }

    private SymbolicRenameInfo(string localizedErrorMessage)
        => this.LocalizedErrorMessage = localizedErrorMessage;

    private SymbolicRenameInfo(
        Document document,
        SyntaxToken triggerToken,
        string triggerText,
        ISymbol symbol,
        bool forceRenameOverloads,
        ImmutableArray<DocumentSpan> documentSpans)
    {
        Document = document;
        TriggerToken = triggerToken;
        TriggerText = triggerText;
        Symbol = symbol;
        ForceRenameOverloads = forceRenameOverloads;
        DocumentSpans = documentSpans;

        this.IsRenamingAttributePrefix = CanRenameAttributePrefix(triggerText);
    }

    private bool CanRenameAttributePrefix(string triggerText)
    {
        Contract.ThrowIfTrue(this.IsError);

        // if this isn't an attribute, or it doesn't have the 'Attribute' suffix, then clearly
        // we can't rename just the attribute prefix.
        if (!IsRenamingAttributeTypeWithAttributeSuffix())
            return false;

        // Ok, the symbol is good.  Now, make sure that the trigger text starts with the prefix
        // of the attribute.  If it does, then we can rename just the attribute prefix (otherwise
        // we need to rename the entire attribute).
        var nameWithoutAttribute = GetWithoutAttributeSuffix(this.Symbol.Name);
        return triggerText.StartsWith(nameWithoutAttribute);

        bool IsRenamingAttributeTypeWithAttributeSuffix()
        {
            if (this.Symbol.IsAttribute() || (this.Symbol is IAliasSymbol alias && alias.Target.IsAttribute()))
            {
                if (HasAttributeSuffix(this.Symbol.Name))
                    return true;
            }

            return false;
        }
    }

    public string GetWithoutAttributeSuffix(string value)
    {
        Contract.ThrowIfTrue(this.IsError);
        var isCaseSensitive = this.Document.GetRequiredLanguageService<ISyntaxFactsService>().IsCaseSensitive;
        return value.GetWithoutAttributeSuffix(isCaseSensitive)!;
    }

    private bool HasAttributeSuffix(string value)
    {
        Contract.ThrowIfTrue(this.IsError);

        var isCaseSensitive = this.Document.GetRequiredLanguageService<ISyntaxFactsService>().IsCaseSensitive;
        return value.TryGetWithoutAttributeSuffix(isCaseSensitive, result: out var _);
    }

    public string GetFinalSymbolName(string replacementText)
    {
        if (this.IsRenamingAttributePrefix && !HasAttributeSuffix(replacementText))
        {
            return replacementText + AttributeSuffix;
        }

        return replacementText;
    }

    public static async Task<SymbolicRenameInfo> GetRenameInfoAsync(
        Document document, int position, CancellationToken cancellationToken)
    {
        var triggerToken = await GetTriggerTokenAsync(document, position, cancellationToken).ConfigureAwait(false);
        if (triggerToken == default)
            return new SymbolicRenameInfo(FeaturesResources.You_must_rename_an_identifier);

        return await GetRenameInfoAsync(document, triggerToken, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<SyntaxToken> GetTriggerTokenAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var token = await syntaxTree.GetTouchingWordAsync(position, syntaxFacts, cancellationToken, findInsideTrivia: true).ConfigureAwait(false);
        return token;
    }

    private static async Task<SymbolicRenameInfo> GetRenameInfoAsync(
        Document document,
        SyntaxToken triggerToken,
        CancellationToken cancellationToken)
    {
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        if (syntaxFacts.IsReservedOrContextualKeyword(triggerToken))
            return new SymbolicRenameInfo(FeaturesResources.You_must_rename_an_identifier);

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();

        var tokenRenameInfo = RenameUtilities.GetTokenRenameInfo(semanticFacts, semanticModel, triggerToken, cancellationToken);

        // Rename was invoked on a member group reference in a nameof expression.
        // Trigger the rename on any of the candidate symbols but force the 
        // RenameOverloads option to be on.
        var triggerSymbol = tokenRenameInfo.HasSymbols ? tokenRenameInfo.Symbols.First() : null;
        if (triggerSymbol == null)
            return new SymbolicRenameInfo(FeaturesResources.You_cannot_rename_this_element);

        // see https://github.com/dotnet/roslyn/issues/10898
        // we are disabling rename for tuple fields for now
        // 1) compiler does not return correct location information in these symbols
        // 2) renaming tuple fields seems a complex enough thing to require some design
        if (triggerSymbol.ContainingType?.IsTupleType == true)
            return new SymbolicRenameInfo(FeaturesResources.You_cannot_rename_this_element);

        // If rename is invoked on a member group reference in a nameof expression, then the
        // RenameOverloads option should be forced on.
        var forceRenameOverloads = tokenRenameInfo.IsMemberGroup;
        var symbol = await RenameUtilities.TryGetRenamableSymbolAsync(document, triggerToken.SpanStart, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (symbol == null)
            return new SymbolicRenameInfo(FeaturesResources.You_cannot_rename_this_element);

        if (symbol.Kind == SymbolKind.Alias && symbol.IsExtern)
            return new SymbolicRenameInfo(FeaturesResources.You_cannot_rename_this_element);

        // Cannot rename constructors in VB.  TODO: this logic should be in the VB subclass of this type.
        if (symbol.Kind == SymbolKind.NamedType &&
            symbol.Language == LanguageNames.VisualBasic &&
            triggerToken.ToString().Equals("New", StringComparison.OrdinalIgnoreCase))
        {
            var originalSymbol = await SymbolFinder.FindSymbolAtPositionAsync(
                semanticModel, triggerToken.SpanStart, document.Project.Solution.Services, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (originalSymbol != null && originalSymbol.IsConstructor())
                return new SymbolicRenameInfo(FeaturesResources.You_cannot_rename_this_element);
        }

        var issuesService = document.GetRequiredLanguageService<IRenameIssuesService>();
        if (issuesService.CheckLanguageSpecificIssues(semanticModel, symbol, triggerToken, out var langError))
            return new SymbolicRenameInfo(langError);

        // we allow implicit locals and parameters of Event handlers
        if (symbol.IsImplicitlyDeclared &&
            symbol.Kind != SymbolKind.Local &&
            !(symbol.Kind == SymbolKind.Parameter &&
              symbol.ContainingSymbol.Kind == SymbolKind.Method &&
              symbol.ContainingType != null &&
              symbol.ContainingType.IsDelegateType() &&
              symbol.ContainingType.AssociatedSymbol != null))
        {
            // We enable the parameter in RaiseEvent, if the Event is declared with a signature. If the Event is declared as a 
            // delegate type, we do not have a connection between the delegate type and the event.
            // this prevents a rename in this case :(.
            return new SymbolicRenameInfo(FeaturesResources.You_cannot_rename_this_element);
        }

        if (symbol.IsErrorType())
            return new SymbolicRenameInfo(FeaturesResources.Please_resolve_errors_in_your_code_before_renaming_this_element);

        if (symbol.Kind == SymbolKind.Method && ((IMethodSymbol)symbol).MethodKind == MethodKind.UserDefinedOperator)
            return new SymbolicRenameInfo(FeaturesResources.You_cannot_rename_operators);

        var symbolLocations = symbol.Locations;

        // Does our symbol exist in an unchangeable location?
        using var _ = ArrayBuilder<DocumentSpan>.GetInstance(out var documentSpans);
        foreach (var location in symbolLocations)
        {
            if (location.IsInMetadata)
            {
                return new SymbolicRenameInfo(FeaturesResources.You_cannot_rename_elements_that_are_defined_in_metadata);
            }
            else if (location.IsInSource)
            {
                var solution = document.Project.Solution;
                var sourceDocument = solution.GetRequiredDocument(location.SourceTree);

                if (sourceDocument is SourceGeneratedDocument && !sourceDocument.IsRazorSourceGeneratedDocument())
                {
                    // The file is generated so doesn't count towards valid spans 
                    // we can edit.
                    continue;
                }

                if (document.Project.IsSubmission)
                {
                    var projectIdOfLocation = sourceDocument.Project.Id;

                    if (solution.Projects.Any(p => p.IsSubmission && p.ProjectReferences.Any(r => r.ProjectId == projectIdOfLocation)))
                        return new SymbolicRenameInfo(FeaturesResources.You_cannot_rename_elements_from_previous_submissions);
                }
                else
                {
                    // We eventually need to return the symbol locations, so we must convert each location to a DocumentSpan since our return type is language-agnostic.
                    documentSpans.Add(new DocumentSpan(sourceDocument, location.SourceSpan));
                }
            }
            else
            {
                return new SymbolicRenameInfo(FeaturesResources.You_cannot_rename_this_element);
            }
        }

        // No valid spans available in source we can edit
        if (documentSpans.Count == 0)
        {
            return new SymbolicRenameInfo(FeaturesResources.You_cannot_rename_this_element);
        }

        var sourceText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var triggerText = sourceText.ToString(triggerToken.Span);

        return new SymbolicRenameInfo(
            document, triggerToken, triggerText, symbol, forceRenameOverloads, documentSpans.ToImmutable());
    }
}
