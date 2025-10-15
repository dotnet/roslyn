// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.GenerateFromMembers;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.AddConstructorParametersFromMembers;

using static GenerateFromMembersHelpers;

internal sealed partial class AddConstructorParametersFromMembersCodeRefactoringProvider
{
    private sealed class AddConstructorParametersCodeAction(
        Document document,
        CodeGenerationContextInfo info,
        ConstructorCandidate constructorCandidate,
        bool useSubMenuName) : CodeAction
    {
        private readonly Document _document = document;
        private readonly CodeGenerationContextInfo _info = info;

        private IMethodSymbol Constructor => constructorCandidate.Constructor;
        private ImmutableArray<(IParameterSymbol parameter, ISymbol fieldOrPropert)> MissingParameters => constructorCandidate.MissingParametersAndMembers;

        /// <summary>
        /// If there is more than one constructor, the suggested actions will be split into two sub menus,
        /// one for regular parameters and one for optional. This boolean is used by the Title property
        /// to determine if the code action should be given the complete title or the sub menu title
        /// </summary>
        private readonly bool _useSubMenuName = useSubMenuName;

        protected override async Task<Solution?> GetChangedSolutionAsync(
            IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
        {
            var declarationService = _document.GetRequiredLanguageService<ISymbolDeclarationService>();
            var constructor = declarationService.GetDeclarations(
                Constructor).Select(r => r.GetSyntax(cancellationToken)).First();

            return !Constructor.IsPrimaryConstructor()
                ? AddParametersToRegularConstructor(constructor, cancellationToken)
                : await AddParametersAndInitializersToPrimaryConstructorAsync(constructor, cancellationToken).ConfigureAwait(false);
        }

        private SyntaxNode GetNewConstructor(SyntaxNode oldConstructor, CancellationToken cancellationToken)
        {
            var codeGenerator = _document.GetRequiredLanguageService<ICodeGenerationService>();
            var newConstructor = codeGenerator.AddParameters(
                oldConstructor, MissingParameters.SelectAsArray(t => t.parameter), _info, cancellationToken);

            return newConstructor;
        }

        private Solution AddParametersToRegularConstructor(SyntaxNode constructor, CancellationToken cancellationToken)
        {
            var codeGenerator = _document.GetRequiredLanguageService<ICodeGenerationService>();

            // For regular constructors, add assignment statements
            var newConstructor = GetNewConstructor(constructor, cancellationToken);
            newConstructor = codeGenerator
                .AddStatements(newConstructor, CreateAssignStatements(), _info, cancellationToken)
                .WithAdditionalAnnotations(Formatter.Annotation);

            var syntaxTree = constructor.SyntaxTree;
            var newRoot = syntaxTree.GetRoot(cancellationToken).ReplaceNode(constructor, newConstructor);

            // Make sure we get the document that contains the constructor we just updated
            var constructorDocument = _document.Project.GetDocument(syntaxTree);
            Contract.ThrowIfNull(constructorDocument);

            return constructorDocument.WithSyntaxRoot(newRoot).Project.Solution;
        }

        private async Task<Solution> AddParametersAndInitializersToPrimaryConstructorAsync(
            SyntaxNode constructor,
            CancellationToken cancellationToken)
        {
            // For primary constructors, we need to:
            //
            // 1. Add initializers to the properties/fields
            // 2. Update the primary constructor with new parameters
            //
            // We need to do it in this order so that we see the adjustment to the properties/fields when we're updating
            // the primary constructor that wraps them all.
            var newConstructor = GetNewConstructor(constructor, cancellationToken);

            var solution = _document.Project.Solution;
            var solutionEditor = new SolutionEditor(solution);

            // First, update the primary constructor declaration with the new parameters
            var oldConstructor = _document.GetRequiredLanguageService<ISymbolDeclarationService>()
                .GetDeclarations(Constructor)
                .Select(r => r.GetSyntax(cancellationToken))
                .First();

            var oldConstructorSyntaxTree = oldConstructor.SyntaxTree;

            foreach (var (parameter, member) in MissingParameters)
            {
                await AddInitializerToMemberAsync(
                    solutionEditor, member, parameter, cancellationToken).ConfigureAwait(false);
            }

            var syntaxTree = oldConstructor.SyntaxTree;
            var documentToUpdate = solution.GetRequiredDocument(syntaxTree);
            var editor = await solutionEditor.GetDocumentEditorAsync(documentToUpdate.Id, cancellationToken).ConfigureAwait(false);

            editor.ReplaceNode(
                oldConstructor,
                (currentOldConstructor, _) =>
                {
                    var newConstructor = GetNewConstructor(currentOldConstructor, cancellationToken);
                    return newConstructor.WithAdditionalAnnotations(Formatter.Annotation);
                });

            return solutionEditor.GetChangedSolution();
        }

        private async Task AddInitializerToMemberAsync(
            SolutionEditor solutionEditor,
            ISymbol member,
            IParameterSymbol parameter,
            CancellationToken cancellationToken)
        {
            var solution = solutionEditor.OriginalSolution;
            var generator = _document.GetRequiredLanguageService<SyntaxGenerator>();

            if (member.DeclaringSyntaxReferences is not [var syntaxRef, ..])
                return;

            var memberSyntax = syntaxRef.GetSyntax(cancellationToken);
            var memberDocument = solution.GetRequiredDocument(memberSyntax.SyntaxTree);
            var memberEditor = await solutionEditor.GetDocumentEditorAsync(memberDocument.Id, cancellationToken).ConfigureAwait(false);

            var equalsValueClause = generator.SyntaxGeneratorInternal.EqualsValueClause(
                generator.IdentifierName(parameter.Name));

            // Add the initializer to the member - use different methods for properties vs fields
            var newMemberSyntax = member is IPropertySymbol
                ? generator.SyntaxGeneratorInternal.WithPropertyInitializer(memberSyntax, equalsValueClause)
                : generator.WithInitializer(memberSyntax, equalsValueClause);

            memberEditor.ReplaceNode(memberSyntax, newMemberSyntax);
        }

        private IEnumerable<SyntaxNode> CreateAssignStatements()
        {
            var factory = _document.GetRequiredLanguageService<SyntaxGenerator>();
            foreach (var (parameter, fieldOrProperty) in MissingParameters)
            {
                yield return factory.ExpressionStatement(
                    factory.AssignmentStatement(
                        factory.MemberAccessExpression(
                            factory.ThisExpression(),
                            factory.IdentifierName(fieldOrProperty.Name)),
                        factory.IdentifierName(parameter.Name)));
            }
        }

        public override string Title
        {
            get
            {
                var parameters = Constructor.Parameters.Select(p => p.ToDisplayString(SimpleFormat));
                var parameterString = string.Join(", ", parameters);
                var signature = $"{this.Constructor.ContainingType.Name}({parameterString})";

                if (_useSubMenuName)
                    return string.Format(CodeFixesResources.Add_to_0, signature);

                return MissingParameters[0].parameter.IsOptional
                    ? string.Format(FeaturesResources.Add_optional_parameters_to_0, signature)
                    : string.Format(FeaturesResources.Add_parameters_to_0, signature);
            }
        }

        /// <summary>
        /// A metadata name used by telemetry to distinguish between the different kinds of this code action.
        /// This code action will perform 2 different actions depending on if missing parameters can be optional.
        /// 
        /// In this case we don't want to use the title as it depends on the class name for the ctor.
        /// </summary>
        internal string ActionName => MissingParameters[0].parameter.IsOptional
            ? nameof(FeaturesResources.Add_optional_parameters_to_0)
            : nameof(FeaturesResources.Add_parameters_to_0);
    }
}
