// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
using Microsoft.CodeAnalysis.PooledObjects;
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
        public const string GenerateOperatorsId = nameof(GenerateOperatorsId);
        public const string ImplementIEquatableId = nameof(ImplementIEquatableId);

        private const string EqualsName = nameof(object.Equals);
        private const string GetHashCodeName = nameof(object.GetHashCode);

        private readonly IPickMembersService _pickMembersService_forTestingPurposes;

        [ImportingConstructor]
        public GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider()
            : this(pickMembersService: null)
        {
        }

        public GenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider(IPickMembersService pickMembersService)
        {
            _pickMembersService_forTestingPurposes = pickMembersService;
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var actions = await GenerateEqualsAndGetHashCodeFromMembersAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
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

            // No overrides in static classes.
            if (containingType.IsStatic)
            {
                return;
            }

            // Find all the possible instance fields/properties.  If there are any, then
            // show a dialog to the user to select the ones they want.
            var viableMembers = containingType.GetMembers().WhereAsArray(IsReadableInstanceFieldOrProperty);
            if (viableMembers.Length == 0)
            {
                return;
            }

            GetExistingMemberInfo(
                containingType, out var hasEquals, out var hasGetHashCode);

            var pickMembersOptions = ArrayBuilder<PickMembersOption>.GetInstance();

            var equatableTypeOpt = semanticModel.Compilation.GetTypeByMetadataName(typeof(IEquatable<>).FullName);
            if (equatableTypeOpt != null)
            {
                var constructedType = equatableTypeOpt.Construct(containingType);
                if (!containingType.AllInterfaces.Contains(constructedType))
                {
                    var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                    var value = options.GetOption(GenerateEqualsAndGetHashCodeFromMembersOptions.ImplementIEquatable);

                    var displayName = constructedType.ToDisplayString(new SymbolDisplayFormat(
                        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters));

                    pickMembersOptions.Add(new PickMembersOption(
                        ImplementIEquatableId,
                        string.Format(FeaturesResources.Implement_0, displayName),
                        value));
                }
            }

            if (!HasOperators(containingType))
            {
                var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                var value = options.GetOption(GenerateEqualsAndGetHashCodeFromMembersOptions.GenerateOperators);
                pickMembersOptions.Add(new PickMembersOption(
                    GenerateOperatorsId,
                    FeaturesResources.Generate_operators,
                    value));
            }

            var actions = CreateActions(
                document, textSpan, containingType,
                viableMembers, pickMembersOptions.ToImmutableAndFree(),
                hasEquals, hasGetHashCode,
                withDialog: true);

            context.RegisterRefactorings(actions);
        }

        private bool HasOperators(INamedTypeSymbol containingType)
            => HasOperator(containingType, WellKnownMemberNames.EqualityOperatorName) ||
               HasOperator(containingType, WellKnownMemberNames.InequalityOperatorName);

        private bool HasOperator(INamedTypeSymbol containingType, string operatorName)
            => containingType.GetMembers(operatorName)
                             .OfType<IMethodSymbol>()
                             .Any(m => m is
                             {
                                 MethodKind: MethodKind.UserDefinedOperator,
                                 Parameters: { Length: 2 }
                             } && containingType.Equals(m.Parameters[0].Type) && containingType.Equals(m.Parameters[1].Type));

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
                var info = await GetSelectedMemberInfoAsync(document, textSpan, allowPartialSelection: false, cancellationToken).ConfigureAwait(false);
                if (info != null &&
                    info.SelectedMembers.All(IsReadableInstanceFieldOrProperty))
                {
                    if (info.ContainingType != null && info.ContainingType.TypeKind != TypeKind.Interface)
                    {
                        GetExistingMemberInfo(
                            info.ContainingType, out var hasEquals, out var hasGetHashCode);

                        return CreateActions(
                            document, textSpan, info.ContainingType,
                            info.SelectedMembers, ImmutableArray<PickMembersOption>.Empty,
                            hasEquals, hasGetHashCode, withDialog: false);
                    }
                }

                return default;
            }
        }

        private ImmutableArray<CodeAction> CreateActions(
            Document document, TextSpan textSpan, INamedTypeSymbol containingType,
            ImmutableArray<ISymbol> selectedMembers, ImmutableArray<PickMembersOption> pickMembersOptions,
            bool hasEquals, bool hasGetHashCode, bool withDialog)
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
                result.Add(CreateCodeAction(document, textSpan, containingType,
                    selectedMembers, pickMembersOptions,
                    generateEquals: true, generateGetHashCode: false, withDialog: withDialog));
                result.Add(CreateCodeAction(document, textSpan, containingType,
                    selectedMembers, pickMembersOptions,
                    generateEquals: true, generateGetHashCode: true, withDialog: withDialog));
            }
            else if (!hasEquals)
            {
                result.Add(CreateCodeAction(document, textSpan, containingType,
                    selectedMembers, pickMembersOptions,
                    generateEquals: true, generateGetHashCode: false, withDialog: withDialog));
            }
            else if (!hasGetHashCode)
            {
                result.Add(CreateCodeAction(document, textSpan, containingType,
                    selectedMembers, pickMembersOptions,
                    generateEquals: false, generateGetHashCode: true, withDialog: withDialog));
            }

            return result.ToImmutableAndFree();
        }

        private CodeAction CreateCodeAction(
            Document document, TextSpan textSpan, INamedTypeSymbol containingType,
            ImmutableArray<ISymbol> members, ImmutableArray<PickMembersOption> pickMembersOptions,
            bool generateEquals, bool generateGetHashCode, bool withDialog)
        {
            if (withDialog)
            {
                return new GenerateEqualsAndGetHashCodeWithDialogCodeAction(
                    this, document, textSpan, containingType,
                    members, pickMembersOptions,
                    generateEquals, generateGetHashCode);
            }
            else
            {
                return new GenerateEqualsAndGetHashCodeAction(
                    document, textSpan, containingType, members,
                    generateEquals, generateGetHashCode,
                    implementIEquatable: false, generateOperators: false);
            }
        }
    }
}
