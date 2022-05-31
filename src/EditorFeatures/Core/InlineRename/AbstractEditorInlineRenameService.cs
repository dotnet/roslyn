// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal abstract partial class AbstractEditorInlineRenameService : IEditorInlineRenameService
    {
        private readonly IEnumerable<IRefactorNotifyService> _refactorNotifyServices;
        private readonly IGlobalOptionService _globalOptions;

        protected AbstractEditorInlineRenameService(IEnumerable<IRefactorNotifyService> refactorNotifyServices, IGlobalOptionService globalOptions)
        {
            _refactorNotifyServices = refactorNotifyServices;
            _globalOptions = globalOptions;
        }

        protected abstract bool CheckLanguageSpecificIssues(
            SemanticModel semantic, ISymbol symbol, SyntaxToken triggerToken, [NotNullWhen(true)] out string? langError);

        public async Task<IInlineRenameInfo> GetRenameInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var triggerToken = await GetTriggerTokenAsync(document, position, cancellationToken).ConfigureAwait(false);

            if (triggerToken == default)
            {
                return new FailureInlineRenameInfo(EditorFeaturesResources.You_must_rename_an_identifier);
            }

            return await GetRenameInfoAsync(_refactorNotifyServices, document, triggerToken, cancellationToken).ConfigureAwait(false);
        }

        private async Task<IInlineRenameInfo> GetRenameInfoAsync(
            IEnumerable<IRefactorNotifyService> refactorNotifyServices,
            Document document, SyntaxToken triggerToken,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            if (syntaxFacts.IsReservedOrContextualKeyword(triggerToken))
                return new FailureInlineRenameInfo(EditorFeaturesResources.You_must_rename_an_identifier);

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var semanticFacts = document.GetLanguageService<ISemanticFactsService>();

            var tokenRenameInfo = RenameUtilities.GetTokenRenameInfo(semanticFacts, semanticModel, triggerToken, cancellationToken);

            // Rename was invoked on a member group reference in a nameof expression.
            // Trigger the rename on any of the candidate symbols but force the 
            // RenameOverloads option to be on.
            var triggerSymbol = tokenRenameInfo.HasSymbols ? tokenRenameInfo.Symbols.First() : null;
            if (triggerSymbol == null)
                return new FailureInlineRenameInfo(EditorFeaturesResources.You_cannot_rename_this_element);

            // see https://github.com/dotnet/roslyn/issues/10898
            // we are disabling rename for tuple fields for now
            // 1) compiler does not return correct location information in these symbols
            // 2) renaming tuple fields seems a complex enough thing to require some design
            if (triggerSymbol.ContainingType?.IsTupleType == true)
                return new FailureInlineRenameInfo(EditorFeaturesResources.You_cannot_rename_this_element);

            // If rename is invoked on a member group reference in a nameof expression, then the
            // RenameOverloads option should be forced on.
            var forceRenameOverloads = tokenRenameInfo.IsMemberGroup;
            var symbol = await RenameLocations.ReferenceProcessing.TryGetRenamableSymbolAsync(document, triggerToken.SpanStart, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (symbol == null)
                return new FailureInlineRenameInfo(EditorFeaturesResources.You_cannot_rename_this_element);

            if (symbol.Kind == SymbolKind.Alias && symbol.IsExtern)
                return new FailureInlineRenameInfo(EditorFeaturesResources.You_cannot_rename_this_element);

            // Cannot rename constructors in VB.  TODO: this logic should be in the VB subclass of this type.
            var workspace = document.Project.Solution.Workspace;
            if (symbol.Kind == SymbolKind.NamedType &&
                symbol.Language == LanguageNames.VisualBasic &&
                triggerToken.ToString().Equals("New", StringComparison.OrdinalIgnoreCase))
            {
                var originalSymbol = await SymbolFinder.FindSymbolAtPositionAsync(
                    semanticModel, triggerToken.SpanStart, workspace, cancellationToken: cancellationToken).ConfigureAwait(false);

                if (originalSymbol != null && originalSymbol.IsConstructor())
                    return new FailureInlineRenameInfo(EditorFeaturesResources.You_cannot_rename_this_element);
            }

            if (CheckLanguageSpecificIssues(semanticModel, symbol, triggerToken, out var langError))
                return new FailureInlineRenameInfo(langError);

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
                return new FailureInlineRenameInfo(EditorFeaturesResources.You_cannot_rename_this_element);
            }

            if (symbol.Kind == SymbolKind.Property && symbol.ContainingType.IsAnonymousType)
                return new FailureInlineRenameInfo(EditorFeaturesResources.Renaming_anonymous_type_members_is_not_yet_supported);

            if (symbol.IsErrorType())
                return new FailureInlineRenameInfo(EditorFeaturesResources.Please_resolve_errors_in_your_code_before_renaming_this_element);

            if (symbol.Kind == SymbolKind.Method && ((IMethodSymbol)symbol).MethodKind == MethodKind.UserDefinedOperator)
                return new FailureInlineRenameInfo(EditorFeaturesResources.You_cannot_rename_operators);

            var symbolLocations = symbol.Locations;

            // Does our symbol exist in an unchangeable location?
            var documentSpans = ArrayBuilder<DocumentSpan>.GetInstance();
            foreach (var location in symbolLocations)
            {
                if (location.IsInMetadata)
                {
                    return new FailureInlineRenameInfo(EditorFeaturesResources.You_cannot_rename_elements_that_are_defined_in_metadata);
                }
                else if (location.IsInSource)
                {
                    var solution = document.Project.Solution;
                    var sourceDocument = solution.GetRequiredDocument(location.SourceTree);

                    if (sourceDocument is SourceGeneratedDocument)
                    {
                        // The file is generated so we can't go editing it (for now)
                        return new FailureInlineRenameInfo(EditorFeaturesResources.You_cannot_rename_this_element);
                    }

                    if (document.Project.IsSubmission)
                    {
                        var projectIdOfLocation = sourceDocument.Project.Id;

                        if (solution.Projects.Any(p => p.IsSubmission && p.ProjectReferences.Any(r => r.ProjectId == projectIdOfLocation)))
                            return new FailureInlineRenameInfo(EditorFeaturesResources.You_cannot_rename_elements_from_previous_submissions);
                    }
                    else
                    {
                        // We eventually need to return the symbol locations, so we must convert each location to a DocumentSpan since our return type is language-agnostic.
                        documentSpans.Add(new DocumentSpan(sourceDocument, location.SourceSpan));
                    }
                }
                else
                {
                    return new FailureInlineRenameInfo(EditorFeaturesResources.You_cannot_rename_this_element);
                }
            }

            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var triggerText = sourceText.ToString(triggerToken.Span);
            var fallbackOptions = _globalOptions.CreateProvider();

            return new SymbolInlineRenameInfo(
                refactorNotifyServices, document, triggerToken.Span, triggerText,
                symbol, forceRenameOverloads, documentSpans.ToImmutableAndFree(),
                fallbackOptions, cancellationToken);
        }

        private static async Task<SyntaxToken> GetTriggerTokenAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var token = await syntaxTree.GetTouchingWordAsync(position, syntaxFacts, cancellationToken, findInsideTrivia: true).ConfigureAwait(false);
            return token;
        }
    }
}
