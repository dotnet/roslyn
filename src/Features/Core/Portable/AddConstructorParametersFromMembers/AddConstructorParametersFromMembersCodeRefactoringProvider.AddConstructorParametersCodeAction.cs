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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.AddConstructorParametersFromMembers;

using static GenerateFromMembersHelpers;

internal sealed partial class AddConstructorParametersFromMembersCodeRefactoringProvider
{
    private sealed class AddConstructorParametersCodeAction(
        Document document,
        CodeGenerationContextInfo info,
        IMethodSymbol constructor,
        ISymbol containingType,
        ImmutableArray<(IParameterSymbol parameter, ISymbol fieldOrPropert)> missingParameters,
        bool useSubMenuName) : CodeAction
    {
        private readonly Document _document = document;
        private readonly CodeGenerationContextInfo _info = info;
        private readonly IMethodSymbol _constructor = constructor;
        private readonly ISymbol _containingType = containingType;
        private readonly ImmutableArray<(IParameterSymbol parameter, ISymbol fieldOrPropert)> _missingParameters = missingParameters;

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
                _constructor).Select(r => r.GetSyntax(cancellationToken)).First();

            return !_constructor.IsPrimaryConstructor()
                ? AddParametersToRegularConstructor(constructor, cancellationToken)
                : await AddParametersAndInitializersToPrimaryConstructorAsync(constructor, cancellationToken).ConfigureAwait(false);
        }

        private SyntaxNode GetNewConstructor(SyntaxNode oldConstructor, CancellationToken cancellationToken)
        {
            var codeGenerator = _document.GetRequiredLanguageService<ICodeGenerationService>();
            var newConstructor = codeGenerator.AddParameters(
                oldConstructor, _missingParameters.SelectAsArray(t => t.parameter), _info, cancellationToken);

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
            // 1. Update the primary constructor with new parameters
            // 2. Add initializers to the properties/fields

            var newConstructor = GetNewConstructor(constructor, cancellationToken);

            var solution = _document.Project.Solution;
            var solutionEditor = new SolutionEditor(solution);

            // First, update the primary constructor declaration with the new parameters
            var oldConstructor = _document.GetRequiredLanguageService<ISymbolDeclarationService>()
                .GetDeclarations(_constructor)
                .Select(r => r.GetSyntax(cancellationToken))
                .First();

            var oldConstructorSyntaxTree = oldConstructor.SyntaxTree;

            // First, update the members in different files.  This is trivial as we're not editing that file as well to
            // adjust the primary constructor, and thus do not have to worry about order of syntax editing operations.
            foreach (var (parameter, member) in _missingParameters)
            {
                if (member.DeclaringSyntaxReferences is [var syntaxRef, ..] &&
                    syntaxRef.SyntaxTree != oldConstructorSyntaxTree)
                {
                    await AddInitializerToMemberAsync(
                        solutionEditor, member, parameter, syntaxRef.GetSyntax(cancellationToken), cancellationToken).ConfigureAwait(false);
                }
            }

            var syntaxTree = oldConstructor.SyntaxTree;
            var documentToUpdate = solution.GetRequiredDocument(syntaxTree);
            var editor = await solutionEditor.GetDocumentEditorAsync(documentToUpdate.Id, cancellationToken).ConfigureAwait(false);

            // Now: 
            foreach (var (parameter, member) in _missingParameters)
            {
                if (member.DeclaringSyntaxReferences is [var syntaxRef, ..] &&
                    syntaxRef.SyntaxTree == oldConstructorSyntaxTree)
                {
                    nodesToTrack.Add(syntaxRef.GetSyntax(cancellationToken));
                }
            }

            var trackedRoot = root.TrackNodes(nodesToTrack);

            editor.ReplaceNode(oldConstructor, newConstructor.WithAdditionalAnnotations(Formatter.Annotation));

            // Now add initializers to each member
            foreach (var (parameter, member) in _missingParameters)
            {
                await AddInitializerToMemberAsync(
                    solutionEditor, member, parameter, cancellationToken).ConfigureAwait(false);
            }

            return solutionEditor.GetChangedSolution();
        }

        private async Task AddInitializerToMemberAsync(
            SolutionEditor solutionEditor,
            ISymbol member,
            IParameterSymbol parameter,
            SyntaxNode memberSyntax,
            CancellationToken cancellationToken)
        {
            var solution = solutionEditor.OriginalSolution;
            var generator = _document.GetRequiredLanguageService<SyntaxGenerator>();

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
            foreach (var (parameter, fieldOrProperty) in _missingParameters)
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
                var parameters = _constructor.Parameters.Select(p => p.ToDisplayString(SimpleFormat));
                var parameterString = string.Join(", ", parameters);
                var signature = $"{_containingType.Name}({parameterString})";

                if (_useSubMenuName)
                    return string.Format(CodeFixesResources.Add_to_0, signature);

                return _missingParameters[0].parameter.IsOptional
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
        internal string ActionName => _missingParameters[0].parameter.IsOptional
            ? nameof(FeaturesResources.Add_optional_parameters_to_0)
            : nameof(FeaturesResources.Add_parameters_to_0);
    }
}
