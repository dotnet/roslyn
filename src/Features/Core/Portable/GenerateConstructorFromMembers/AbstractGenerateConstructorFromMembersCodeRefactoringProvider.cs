﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Features.Intents;
using Microsoft.CodeAnalysis.GenerateFromMembers;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateConstructorFromMembers
{
    /// <summary>
    /// This <see cref="CodeRefactoringProvider"/> is responsible for allowing a user to pick a 
    /// set of members from a class or struct, and then generate a constructor for that takes in
    /// matching parameters and assigns them to those members.  The members can be picked using 
    /// a actual selection in the editor, or they can be picked using a picker control that will
    /// then display all the viable members and allow the user to pick which ones they want to
    /// use.
    /// 
    /// Importantly, this type is not responsible for generating constructors when the user types
    /// something like "new MyType(x, y, z)", nor is it responsible for generating constructors
    /// in a derived type that delegate to a base type. Both of those are handled by other services.
    /// </summary>
    internal abstract partial class AbstractGenerateConstructorFromMembersCodeRefactoringProvider : AbstractGenerateFromMembersCodeRefactoringProvider, IIntentProvider
    {
        private const string AddNullChecksId = nameof(AddNullChecksId);

        private readonly IPickMembersService? _pickMembersService_forTesting;

        protected AbstractGenerateConstructorFromMembersCodeRefactoringProvider() : this(null)
        {
        }

        /// <summary>
        /// For testing purposes only.
        /// </summary>
        protected AbstractGenerateConstructorFromMembersCodeRefactoringProvider(IPickMembersService? pickMembersService_forTesting)
            => _pickMembersService_forTesting = pickMembersService_forTesting;

        protected abstract bool ContainingTypesOrSelfHasUnsafeKeyword(INamedTypeSymbol containingType);
        protected abstract string ToDisplayString(IParameterSymbol parameter, SymbolDisplayFormat format);
        protected abstract bool PrefersThrowExpression(DocumentOptionSet options);

        public override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            return ComputeRefactoringsAsync(context.Document, context.Span,
                (action, applicableToSpan) => context.RegisterRefactoring(action, applicableToSpan),
                (actions) => context.RegisterRefactorings(actions), context.CancellationToken);
        }

        public async Task<ImmutableArray<IntentProcessorResult>> ComputeIntentAsync(
            Document priorDocument,
            TextSpan priorSelection,
            Document currentDocument,
            string? serializedIntentData,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<CodeAction>.GetInstance(out var actions);
            await ComputeRefactoringsAsync(
                priorDocument,
                priorSelection,
                (singleAction, applicableToSpan) => actions.Add(singleAction),
                (multipleActions) => actions.AddRange(multipleActions),
                cancellationToken).ConfigureAwait(false);

            if (actions.IsEmpty())
            {
                return ImmutableArray<IntentProcessorResult>.Empty;
            }

            // The refactorings returned will be in the following order (if available)
            // FieldDelegatingCodeAction, ConstructorDelegatingCodeAction, GenerateConstructorWithDialogCodeAction
            using var resultsBuilder = ArrayBuilder<IntentProcessorResult>.GetInstance(out var results);
            foreach (var action in actions)
            {
                var intentResult = await GetIntentProcessorResultAsync(action, cancellationToken).ConfigureAwait(false);
                results.AddIfNotNull(intentResult);
            }

            return results.ToImmutable();

            static async Task<IntentProcessorResult?> GetIntentProcessorResultAsync(CodeAction codeAction, CancellationToken cancellationToken)
            {
                var operations = await GetCodeActionOperationsAsync(codeAction, cancellationToken).ConfigureAwait(false);

                // Generate ctor will only return an ApplyChangesOperation or potentially document navigation actions.
                // We can only return edits, so we only care about the ApplyChangesOperation.
                var applyChangesOperation = operations.OfType<ApplyChangesOperation>().SingleOrDefault();
                if (applyChangesOperation == null)
                {
                    return null;
                }

                var type = codeAction.GetType();
                return new IntentProcessorResult(applyChangesOperation.ChangedSolution, codeAction.Title, type.Name);
            }

            static async Task<ImmutableArray<CodeActionOperation>> GetCodeActionOperationsAsync(
                CodeAction action,
                CancellationToken cancellationToken)
            {
                if (action is GenerateConstructorWithDialogCodeAction dialogAction)
                {
                    // Usually applying this code action pops up a dialog allowing the user to choose which options.
                    // We can't do that here, so instead we just take the defaults until we have more intent data.
                    var options = new PickMembersResult(
                        dialogAction.ViableMembers,
                        dialogAction.PickMembersOptions,
                        selectedAll: true);
                    var operations = await dialogAction.GetOperationsAsync(options: options, cancellationToken).ConfigureAwait(false);
                    return operations == null ? ImmutableArray<CodeActionOperation>.Empty : operations.ToImmutableArray();
                }
                else
                {
                    return await action.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task ComputeRefactoringsAsync(
            Document document,
            TextSpan textSpan,
            Action<CodeAction, TextSpan> registerSingleAction,
            Action<ImmutableArray<CodeAction>> registerMultipleActions,
            CancellationToken cancellationToken)
        {
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var actions = await GenerateConstructorFromMembersAsync(
                document, textSpan, addNullChecks: false, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!actions.IsDefault)
            {
                registerMultipleActions(actions);
            }

            if (actions.IsDefaultOrEmpty && textSpan.IsEmpty)
            {
                var nonSelectionAction = await HandleNonSelectionAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
                if (nonSelectionAction != null)
                {
                    registerSingleAction(nonSelectionAction.Value.CodeAction, nonSelectionAction.Value.ApplicableToSpan);
                }
            }
        }

        private async Task<(CodeAction CodeAction, TextSpan ApplicableToSpan)?> HandleNonSelectionAsync(
            Document document,
            TextSpan textSpan,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // We offer the refactoring when the user is either on the header of a class/struct,
            // or if they're between any members of a class/struct and are on a blank line.
            if (!syntaxFacts.IsOnTypeHeader(root, textSpan.Start, out var typeDeclaration) &&
                !syntaxFacts.IsBetweenTypeMembers(sourceText, root, textSpan.Start, out typeDeclaration))
            {
                return null;
            }

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // Only supported on classes/structs.
            var containingType = semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken: cancellationToken) as INamedTypeSymbol;
            if (containingType?.TypeKind is not TypeKind.Class and not TypeKind.Struct)
            {
                return null;
            }

            // No constructors for static classes.
            if (containingType.IsStatic)
            {
                return null;
            }

            // Find all the possible writable instance fields/properties.  If there are any, then
            // show a dialog to the user to select the ones they want.  Otherwise, if there are none
            // don't offer to generate anything.
            var viableMembers = containingType.GetMembers().WhereAsArray(IsWritableInstanceFieldOrProperty);
            if (viableMembers.Length == 0)
            {
                return null;
            }

            // We shouldn't offer a refactoring if the compilation doesn't contain the ArgumentNullException type,
            // as we use it later on in our computations.
            var argumentNullExceptionType = typeof(ArgumentNullException).FullName;
            if (argumentNullExceptionType is null || semanticModel.Compilation.GetTypeByMetadataName(argumentNullExceptionType) is null)
            {
                return null;
            }

            using var _ = ArrayBuilder<PickMembersOption>.GetInstance(out var pickMemberOptions);
            var canAddNullCheck = viableMembers.Any(
                m => m.GetSymbolType().CanAddNullCheck());

            if (canAddNullCheck)
            {
                var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                var optionValue = options.GetOption(GenerateConstructorFromMembersOptions.AddNullChecks);

                pickMemberOptions.Add(new PickMembersOption(
                    AddNullChecksId,
                    FeaturesResources.Add_null_checks,
                    optionValue));
            }

            return (new GenerateConstructorWithDialogCodeAction(
                    this, document, textSpan, containingType, viableMembers,
                    pickMemberOptions.ToImmutable()), typeDeclaration.Span);
        }

        public async Task<ImmutableArray<CodeAction>> GenerateConstructorFromMembersAsync(
            Document document, TextSpan textSpan, bool addNullChecks, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_GenerateFromMembers_GenerateConstructorFromMembers, cancellationToken))
            {
                var info = await GetSelectedMemberInfoAsync(document, textSpan, allowPartialSelection: true, cancellationToken).ConfigureAwait(false);
                if (info != null)
                {
                    var state = await State.TryGenerateAsync(this, document, textSpan, info.ContainingType, info.SelectedMembers, cancellationToken).ConfigureAwait(false);
                    if (state != null && state.MatchingConstructor == null)
                    {
                        return GetCodeActions(document, state, addNullChecks);
                    }
                }

                return default;
            }
        }

        private ImmutableArray<CodeAction> GetCodeActions(Document document, State state, bool addNullChecks)
        {
            using var _ = ArrayBuilder<CodeAction>.GetInstance(out var result);

            result.Add(new FieldDelegatingCodeAction(this, document, state, addNullChecks));
            if (state.DelegatedConstructor != null)
                result.Add(new ConstructorDelegatingCodeAction(this, document, state, addNullChecks));

            return result.ToImmutable();
        }

        private static async Task<Document> AddNavigationAnnotationAsync(Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var nodes = root.GetAnnotatedNodes(CodeGenerator.Annotation);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            foreach (var node in nodes)
            {
                var parameterList = syntaxFacts.GetParameterList(node);
                if (parameterList != null)
                {
                    var closeParen = parameterList.GetLastToken();
                    var newRoot = root.ReplaceToken(closeParen, closeParen.WithAdditionalAnnotations(NavigationAnnotation.Create()));
                    return document.WithSyntaxRoot(newRoot);
                }
            }

            return document;
        }
    }
}
