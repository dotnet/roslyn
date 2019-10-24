// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.GenerateFromMembers;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

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
    internal abstract partial class AbstractGenerateConstructorFromMembersCodeRefactoringProvider : AbstractGenerateFromMembersCodeRefactoringProvider
    {
        private const string AddNullChecksId = nameof(AddNullChecksId);

        private readonly IPickMembersService _pickMembersService_forTesting;

        protected AbstractGenerateConstructorFromMembersCodeRefactoringProvider() : this(null)
        {
        }

        /// <summary>
        /// For testing purposes only.
        /// </summary>
        protected AbstractGenerateConstructorFromMembersCodeRefactoringProvider(IPickMembersService pickMembersService_forTesting)
        {
            _pickMembersService_forTesting = pickMembersService_forTesting;
        }

        protected abstract bool PrefersThrowExpression(DocumentOptionSet options);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var actions = await GenerateConstructorFromMembersAsync(
                document, textSpan, addNullChecks: false, cancellationToken: cancellationToken).ConfigureAwait(false);
            context.RegisterRefactorings(actions);

            if (actions.IsDefaultOrEmpty && textSpan.IsEmpty)
            {
                await HandleNonSelectionAsync(context).ConfigureAwait(false);
            }
        }

        private async Task HandleNonSelectionAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // We offer the refactoring when the user is either on the header of a class/struct,
            // or if they're between any members of a class/struct and are on a blank line.
            if (!syntaxFacts.IsOnTypeHeader(root, textSpan.Start, out var typeDeclaration) &&
                !syntaxFacts.IsBetweenTypeMembers(sourceText, root, textSpan.Start, out typeDeclaration))
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // Only supported on classes/structs.
            var containingType = semanticModel.GetDeclaredSymbol(typeDeclaration) as INamedTypeSymbol;
            if (containingType?.TypeKind != TypeKind.Class && containingType?.TypeKind != TypeKind.Struct)
            {
                return;
            }

            // No constructors for static classes.
            if (containingType.IsStatic)
            {
                return;
            }

            // Find all the possible writable instance fields/properties.  If there are any, then
            // show a dialog to the user to select the ones they want.  Otherwise, if there are none
            // don't offer to generate anything.
            var viableMembers = containingType.GetMembers().WhereAsArray(IsWritableInstanceFieldOrProperty);
            if (viableMembers.Length == 0)
            {
                return;
            }

            var pickMemberOptions = ArrayBuilder<PickMembersOption>.GetInstance();
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

            context.RegisterRefactoring(
                new GenerateConstructorWithDialogCodeAction(
                    this, document, textSpan, containingType, viableMembers,
                    pickMemberOptions.ToImmutableAndFree()),
                typeDeclaration.Span);
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
            var result = ArrayBuilder<CodeAction>.GetInstance();

            result.Add(new FieldDelegatingCodeAction(this, document, state, addNullChecks));
            if (state.DelegatedConstructor != null)
            {
                result.Add(new ConstructorDelegatingCodeAction(this, document, state, addNullChecks));
            }

            return result.ToImmutableAndFree();
        }

        private static async Task<Document> AddNavigationAnnotationAsync(Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var nodes = root.GetAnnotatedNodes(CodeGenerator.Annotation);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

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
