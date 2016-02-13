// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal abstract partial class AbstractEditorInlineRenameService : IEditorInlineRenameService
    {
        private readonly IEnumerable<IRefactorNotifyService> _refactorNotifyServices;

        protected AbstractEditorInlineRenameService(IEnumerable<IRefactorNotifyService> refactorNotifyServices)
        {
            _refactorNotifyServices = refactorNotifyServices;
        }

        public Task<IInlineRenameInfo> GetRenameInfoAsync(Document document, int position, CancellationToken cancellationToken)
        {
            // This is unpleasant, but we do everything synchronously.  That's because we end up
            // needing to make calls on the UI thread to determine if the locations of the symbol
            // are in readonly buffer sections or not.  If we go pure async we have the following
            // problem:
            //   1) if we call ConfigureAwait(false), then we might call into the text buffer on 
            //      the wrong thread.
            //   2) if we try to call those pieces of code on the UI thread, then we will deadlock
            //      as our caller often is doing a 'Wait' on us, and our UI calling code won't run.
            var info = this.GetRenameInfo(document, position, cancellationToken);
            return Task.FromResult(info);
        }

        private IInlineRenameInfo GetRenameInfo(Document document, int position, CancellationToken cancellationToken)
        {
            var triggerToken = GetTriggerToken(document, position, cancellationToken);
            if (triggerToken == default(SyntaxToken))
            {
                return new FailureInlineRenameInfo(EditorFeaturesResources.YouMustRenameAnIdentifier);
            }

            return GetRenameInfo(_refactorNotifyServices, document, triggerToken, cancellationToken);
        }

        internal static IInlineRenameInfo GetRenameInfo(
            IEnumerable<IRefactorNotifyService> refactorNotifyServices,
            Document document, SyntaxToken triggerToken, CancellationToken cancellationToken)
        {
            var syntaxFactsService = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
            if (syntaxFactsService.IsKeyword(triggerToken))
            {
                return new FailureInlineRenameInfo(EditorFeaturesResources.YouMustRenameAnIdentifier);
            }

            var semanticModel = document.GetSemanticModelAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var semanticFacts = document.GetLanguageService<ISemanticFactsService>();

            var tokenRenameInfo = RenameUtilities.GetTokenRenameInfo(semanticFacts, semanticModel, triggerToken, cancellationToken);

            // Rename was invoked on a member group reference in a nameof expression.
            // Trigger the rename on any of the candidate symbols but force the 
            // RenameOverloads option to be on.
            var triggerSymbol = tokenRenameInfo.HasSymbols ? tokenRenameInfo.Symbols.First() : null;
            if (triggerSymbol == null)
            {
                return new FailureInlineRenameInfo(EditorFeaturesResources.YouCannotRenameThisElement);
            }

            // If rename is invoked on a member group reference in a nameof expression, then the
            // RenameOverloads option should be forced on.
            var forceRenameOverloads = tokenRenameInfo.IsMemberGroup;

            if (syntaxFactsService.IsTypeNamedVarInVariableOrFieldDeclaration(triggerToken, triggerToken.Parent))
            {
                // To check if var in this context is a real type, or the keyword, we need to 
                // speculatively bind the identifier "var". If it returns a symbol, it's a real type,
                // if not, it's the keyword.
                // see bugs 659683 (compiler API) and 659705 (rename/workspace api) for examples
                var symbolForVar = semanticModel.GetSpeculativeSymbolInfo(
                    triggerToken.SpanStart,
                    triggerToken.Parent,
                    SpeculativeBindingOption.BindAsTypeOrNamespace).Symbol;

                if (symbolForVar == null)
                {
                    return new FailureInlineRenameInfo(EditorFeaturesResources.YouCannotRenameThisElement);
                }
            }

            var symbol = RenameLocations.ReferenceProcessing.GetRenamableSymbolAsync(document, triggerToken.SpanStart, cancellationToken: cancellationToken).WaitAndGetResult(cancellationToken);
            if (symbol == null)
            {
                return new FailureInlineRenameInfo(EditorFeaturesResources.YouCannotRenameThisElement);
            }

            if (symbol.Kind == SymbolKind.Alias && symbol.IsExtern)
            {
                return new FailureInlineRenameInfo(EditorFeaturesResources.YouCannotRenameThisElement);
            }

            // Cannot rename constructors in VB.  TODO: this logic should be in the VB subclass of this type.
            var workspace = document.Project.Solution.Workspace;
            if (symbol != null &&
                symbol.Kind == SymbolKind.NamedType &&
                symbol.Language == LanguageNames.VisualBasic &&
                triggerToken.ToString().Equals("New", StringComparison.OrdinalIgnoreCase))
            {
                var originalSymbol = SymbolFinder.FindSymbolAtPosition(semanticModel, triggerToken.SpanStart, workspace, cancellationToken: cancellationToken);

                if (originalSymbol != null && originalSymbol.IsConstructor())
                {
                    return new FailureInlineRenameInfo(EditorFeaturesResources.YouCannotRenameThisElement);
                }
            }

            if (syntaxFactsService.IsTypeNamedDynamic(triggerToken, triggerToken.Parent))
            {
                if (symbol.Kind == SymbolKind.DynamicType)
                {
                    return new FailureInlineRenameInfo(EditorFeaturesResources.YouCannotRenameThisElement);
                }
            }

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
                return new FailureInlineRenameInfo(EditorFeaturesResources.YouCannotRenameThisElement);
            }

            if (symbol.Kind == SymbolKind.Property && symbol.ContainingType.IsAnonymousType)
            {
                return new FailureInlineRenameInfo(EditorFeaturesResources.RenamingAnonymousTypeMemberNotSupported);
            }

            if (symbol.IsErrorType())
            {
                return new FailureInlineRenameInfo(EditorFeaturesResources.PleaseResolveErrorsInYourCodeBeforeRenaming);
            }

            if (symbol.Kind == SymbolKind.Method && ((IMethodSymbol)symbol).MethodKind == MethodKind.UserDefinedOperator)
            {
                return new FailureInlineRenameInfo(EditorFeaturesResources.YouCannotRenameOperators);
            }

            var symbolLocations = symbol.Locations;

            // Does our symbol exist in an unchangeable location?
            var navigationService = workspace.Services.GetService<IDocumentNavigationService>();
            foreach (var location in symbolLocations)
            {
                if (location.IsInMetadata)
                {
                    return new FailureInlineRenameInfo(EditorFeaturesResources.YouCannotRenameElementsInMetadata);
                }
                else if (location.IsInSource)
                {
                    if (document.Project.IsSubmission)
                    {
                        var solution = document.Project.Solution;
                        var projectIdOfLocation = solution.GetDocument(location.SourceTree).Project.Id;

                        if (solution.Projects.Any(p => p.IsSubmission && p.ProjectReferences.Any(r => r.ProjectId == projectIdOfLocation)))
                        {
                            return new FailureInlineRenameInfo(EditorFeaturesResources.YouCannotRenameElementsFromPrevSubmissions);
                        }
                    }
                    else
                    {
                        var sourceText = location.SourceTree.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken);
                        var textSnapshot = sourceText.FindCorrespondingEditorTextSnapshot();

                        if (textSnapshot != null)
                        {
                            var buffer = textSnapshot.TextBuffer;
                            var originalSpan = location.SourceSpan.ToSnapshotSpan(textSnapshot).TranslateTo(buffer.CurrentSnapshot, SpanTrackingMode.EdgeInclusive);

                            if (buffer.IsReadOnly(originalSpan) || !navigationService.CanNavigateToSpan(workspace, document.Id, location.SourceSpan))
                            {
                                return new FailureInlineRenameInfo(EditorFeaturesResources.YouCannotRenameThisElement);
                            }
                        }
                    }
                }
                else
                {
                    return new FailureInlineRenameInfo(EditorFeaturesResources.YouCannotRenameThisElement);
                }
            }

            return new SymbolInlineRenameInfo(refactorNotifyServices, document, triggerToken.Span, symbol, forceRenameOverloads, cancellationToken);
        }

        private SyntaxToken GetTriggerToken(Document document, int position, CancellationToken cancellationToken)
        {
            var syntaxTree = document.GetSyntaxTreeAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
            var token = syntaxTree.GetTouchingWordAsync(position, syntaxFacts, cancellationToken, findInsideTrivia: true).WaitAndGetResult(cancellationToken);

            return token;
        }
    }
}
