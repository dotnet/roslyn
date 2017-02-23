// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.GenerateFromMembers;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.GenerateConstructorFromMembers
{
    internal partial class GenerateConstructorFromMembersCodeRefactoringProvider : AbstractGenerateFromMembersCodeRefactoringProvider
    {
        private class GenerateConstructorWithDialogCodeAction : CodeActionWithOptions
        {
            private readonly Document _document;
            private readonly INamedTypeSymbol _containingType;
            private readonly GenerateConstructorFromMembersCodeRefactoringProvider _service;
            private readonly TextSpan _textSpan;
            private readonly ImmutableArray<ISymbol> _viableMembers;

            public override string Title => FeaturesResources.Generate_constructor;

            public GenerateConstructorWithDialogCodeAction(
                GenerateConstructorFromMembersCodeRefactoringProvider service,
                Document document, TextSpan textSpan,
                INamedTypeSymbol containingType,
                ImmutableArray<ISymbol> viableMembers)
            {
                _service = service;
                _document = document;
                _textSpan = textSpan;
                _containingType = containingType;
                _viableMembers = viableMembers;
            }

            public override object GetOptions(CancellationToken cancellationToken)
            {
                var workspace = _document.Project.Solution.Workspace;
                var service = workspace.Services.GetService<IPickMembersService>();
                return service.PickMembers(
                    FeaturesResources.Pick_members_to_be_used_as_constructor_parameters, _viableMembers);
            }

            protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(
                object options, CancellationToken cancellationToken)
            {
                var result = (PickMembersResult)options;
                if (result.IsCanceled)
                {
                    return ImmutableArray<CodeActionOperation>.Empty;
                }

                var state = State.TryGenerate(
                    _service, _document, _textSpan, _containingType, 
                    result.Members, cancellationToken);

                // There was an existing constructor that matched what the user wants to create.
                // Generate it if it's the implicit, no-arg, constructor, otherwise just navigate
                // to the existing constructor
                if (state.MatchingConstructor != null)
                {
                    if (state.MatchingConstructor.IsImplicitlyDeclared)
                    {
                        var codeAction = new FieldDelegatingCodeAction(_service, _document, state);
                        return await codeAction.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
                    }

                    var constructorReference = state.MatchingConstructor.DeclaringSyntaxReferences[0];
                    var constructorSyntax = await constructorReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                    var constructorTree = constructorSyntax.SyntaxTree;
                    var constructorDocument = _document.Project.Solution.GetDocument(constructorTree);
                    return ImmutableArray.Create<CodeActionOperation>(new DocumentNavigationOperation(
                        constructorDocument.Id, constructorSyntax.SpanStart));
                }
                else
                {
                    var codeAction = state.DelegatedConstructor != null
                        ? new ConstructorDelegatingCodeAction(_service, _document, state)
                        : (CodeAction)new FieldDelegatingCodeAction(_service, _document, state);

                    return await codeAction.GetOperationsAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}