// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.GenerateFromMembers;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, 
        Name = PredefinedCodeRefactoringProviderNames.GenerateEqualsAndGetHashCodeFromMembers), Shared]
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.GenerateConstructorFromMembers,
                    Before = PredefinedCodeRefactoringProviderNames.AddConstructorParametersFromMembers)]
    internal partial class GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider : AbstractGenerateFromMembersCodeRefactoringProvider
    {
        private const string EqualsName = nameof(object.Equals);
        private const string GetHashCodeName = nameof(object.GetHashCode);

        private readonly IPickMembersService _pickMembersService_forTestingPurposes;

        public GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider() : this(null)
        {
        }

        public GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider(IPickMembersService pickMembersService)
        {
            _pickMembersService_forTestingPurposes = pickMembersService;
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;

            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var actions = await this.GenerateEqualsAndGetHashCodeFromMembersAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            context.RegisterRefactorings(actions);

            if (actions.IsDefaultOrEmpty && textSpan.IsEmpty)
            {
                await HandleNonSelectionAsync(context).ConfigureAwait(false);
            }
        }

        private async Task HandleNonSelectionAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // We offer the refactoring when the user is either on the header of a class/struct,
            // or if they're between any members of a class/struct and are on a blank line.
            if (!syntaxFacts.IsOnTypeHeader(root, textSpan.Start) &&
                !syntaxFacts.IsBetweenTypeMembers(sourceText, root, textSpan.Start))
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // Only supported on classes/structs.
            var containingType = GetEnclosingNamedType(semanticModel, root, textSpan.Start, cancellationToken);
            if (containingType?.TypeKind != TypeKind.Class && containingType?.TypeKind != TypeKind.Struct)
            {
                return;
            }

            // No overrides in static classes.
            if (containingType.IsStatic)
            {
                return;
            }

            // Find all the possible instance fields/properties.  If there are any, then
            // show a dialog to the user to select the ones they want.
            var viableMembers = containingType.GetMembers().WhereAsArray(IsInstanceFieldOrProperty);
            if (viableMembers.Length == 0)
            {
                return;
            }

            GetExistingMemberInfo(
                containingType, out var hasEquals, out var hasGetHashCode);

            var actions = CreateActions(
                document, textSpan, containingType, viableMembers, 
                hasEquals, hasGetHashCode,
                withDialog: true);

            context.RegisterRefactorings(actions);
        }

        private void GetExistingMemberInfo(INamedTypeSymbol containingType, out bool hasEquals, out bool hasGetHashCode)
        {
            hasEquals = containingType.GetMembers(EqualsName)
                                      .OfType<IMethodSymbol>()
                                      .Any(m => m.Parameters.Length == 1 && !m.IsStatic);

            hasGetHashCode = containingType.GetMembers(GetHashCodeName)
                                           .OfType<IMethodSymbol>()
                                           .Any(m => m.Parameters.Length == 0 && !m.IsStatic);
        }

        public async Task<ImmutableArray<CodeAction>> GenerateEqualsAndGetHashCodeFromMembersAsync(
            Document document,
            TextSpan textSpan,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_GenerateFromMembers_GenerateEqualsAndGetHashCode, cancellationToken))
            {
                var info = await this.GetSelectedMemberInfoAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
                if (info != null &&
                    info.SelectedMembers.All(IsInstanceFieldOrProperty))
                {
                    if (info.ContainingType != null && info.ContainingType.TypeKind != TypeKind.Interface)
                    {
                        GetExistingMemberInfo(
                            info.ContainingType, out var hasEquals, out var hasGetHashCode);

                        return CreateActions(
                            document, textSpan, info.ContainingType, info.SelectedMembers, 
                            hasEquals, hasGetHashCode, withDialog: false);
                    }
                }

                return default(ImmutableArray<CodeAction>);
            }
        }

        private ImmutableArray<CodeAction> CreateActions(
            Document document,
            TextSpan textSpan,
            INamedTypeSymbol containingType,
            ImmutableArray<ISymbol> selectedMembers,
            bool hasEquals,
            bool hasGetHashCode,
            bool withDialog)
        {
            var result = ArrayBuilder<CodeAction>.GetInstance();

            if (!hasEquals && !hasGetHashCode)
            {
                // if we don't have either Equals or GetHashCode then offer:
                //  "Generate Equals" and
                //  "Generate Equals and GethashCode"
                //
                // Don't bother offering to just "Generate GetHashCode" as it's very unlikely 
                // the user would need to bother just generating that member without also 
                // generating 'Equals' as well.
                result.Add(CreateCodeAction(document, textSpan, containingType, selectedMembers,
                    generateEquals: true, generateGetHashCode: false, withDialog: withDialog));
                result.Add(CreateCodeAction(document, textSpan, containingType, selectedMembers,
                    generateEquals: true, generateGetHashCode: true, withDialog: withDialog));
            }
            else if (!hasEquals)
            {
                result.Add(CreateCodeAction(document, textSpan, containingType, selectedMembers,
                    generateEquals: true, generateGetHashCode: false, withDialog: withDialog));
            }
            else if (!hasGetHashCode)
            {
                result.Add(CreateCodeAction(document, textSpan, containingType, selectedMembers,
                    generateEquals: false, generateGetHashCode: true, withDialog: withDialog));
            }

            return result.ToImmutableAndFree();
        }

        private CodeAction CreateCodeAction(
            Document document, TextSpan textSpan,
            INamedTypeSymbol containingType, ImmutableArray<ISymbol> members,
            bool generateEquals, bool generateGetHashCode, bool withDialog)
        {
            if (withDialog)
            {
                return new GenerateEqualsAndGetHashCodeWithDialogCodeAction(
                    this, document, textSpan, containingType, members,
                    generateEquals, generateGetHashCode);
            }
            else
            {
                return new GenerateEqualsAndGetHashCodeAction(
                    this, document, textSpan, containingType, members,
                    generateEquals, generateGetHashCode);
            }
        }
    }
}