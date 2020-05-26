﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.GenerateFromMembers;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateConstructorFromMembers
{
    internal abstract partial class AbstractGenerateConstructorFromMembersCodeRefactoringProvider : AbstractGenerateFromMembersCodeRefactoringProvider
    {
        private class GenerateConstructorWithDialogCodeAction : CodeActionWithOptions
        {
            private readonly Document _document;
            private readonly INamedTypeSymbol _containingType;
            private readonly AbstractGenerateConstructorFromMembersCodeRefactoringProvider _service;
            private readonly TextSpan _textSpan;
            private readonly ImmutableArray<ISymbol> _viableMembers;
            private readonly ImmutableArray<PickMembersOption> _pickMembersOptions;

            private bool? _addNullCheckOptionValue;

            public override string Title => FeaturesResources.Generate_constructor;

            public GenerateConstructorWithDialogCodeAction(
                AbstractGenerateConstructorFromMembersCodeRefactoringProvider service,
                Document document, TextSpan textSpan,
                INamedTypeSymbol containingType,
                ImmutableArray<ISymbol> viableMembers,
                ImmutableArray<PickMembersOption> pickMembersOptions)
            {
                _service = service;
                _document = document;
                _textSpan = textSpan;
                _containingType = containingType;
                _viableMembers = viableMembers;
                _pickMembersOptions = pickMembersOptions;
            }

            public override object GetOptions(CancellationToken cancellationToken)
            {
                var workspace = _document.Project.Solution.Workspace;
                var service = _service._pickMembersService_forTesting ?? workspace.Services.GetRequiredService<IPickMembersService>();

                return service.PickMembers(
                    FeaturesResources.Pick_members_to_be_used_as_constructor_parameters,
                    _viableMembers, _pickMembersOptions);
            }

            protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(
                object options, CancellationToken cancellationToken)
            {
                var result = (PickMembersResult)options;
                if (result.IsCanceled)
                {
                    return SpecializedCollections.EmptyEnumerable<CodeActionOperation>();
                }

                var addNullChecksOption = result.Options.FirstOrDefault(o => o.Id == AddNullChecksId);
                if (addNullChecksOption != null)
                {
                    // If we presented the 'Add null check' option, then persist whatever value
                    // the user chose.  That way we'll keep that as the default for the next time
                    // the user opens the dialog.
                    _addNullCheckOptionValue = addNullChecksOption.Value;
                }

                var addNullChecks = (addNullChecksOption?.Value).GetValueOrDefault();
                var state = await State.TryGenerateAsync(
                    _document, _textSpan, _containingType,
                    result.Members, cancellationToken).ConfigureAwait(false);

                if (state == null)
                {
                    return SpecializedCollections.EmptyEnumerable<CodeActionOperation>();
                }

                // There was an existing constructor that matched what the user wants to create.
                // Generate it if it's the implicit, no-arg, constructor, otherwise just navigate
                // to the existing constructor
                if (state.MatchingConstructor != null)
                {
                    if (state.MatchingConstructor.IsImplicitlyDeclared)
                    {
                        var codeAction = new FieldDelegatingCodeAction(_service, _document, state, addNullChecks);
                        return await codeAction.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
                    }

                    var constructorReference = state.MatchingConstructor.DeclaringSyntaxReferences[0];
                    var constructorSyntax = await constructorReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                    var constructorTree = constructorSyntax.SyntaxTree;
                    var constructorDocument = _document.Project.Solution.GetRequiredDocument(constructorTree);
                    return ImmutableArray.Create<CodeActionOperation>(new DocumentNavigationOperation(
                        constructorDocument.Id, constructorSyntax.SpanStart));
                }
                else
                {
                    var codeAction = state.DelegatedConstructor != null
                        ? new ConstructorDelegatingCodeAction(_service, _document, state, addNullChecks)
                        : (CodeAction)new FieldDelegatingCodeAction(_service, _document, state, addNullChecks);

                    return await codeAction.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            protected override async Task<Solution?> GetChangedSolutionAsync(CancellationToken cancellationToken)
            {
                var solution = await base.GetChangedSolutionAsync(cancellationToken).ConfigureAwait(false);

                if (_addNullCheckOptionValue.HasValue)
                {
                    solution = solution?.WithOptions(solution.Options.WithChangedOption(
                        GenerateConstructorFromMembersOptions.AddNullChecks,
                        _document.Project.Language,
                        _addNullCheckOptionValue.Value));
                }

                return solution;
            }
        }
    }
}
