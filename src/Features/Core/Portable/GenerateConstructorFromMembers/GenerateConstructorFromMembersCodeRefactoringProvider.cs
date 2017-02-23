// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.GenerateFromMembers;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PickMembers;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.GenerateConstructorFromMembers
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
        Name = PredefinedCodeRefactoringProviderNames.GenerateConstructorFromMembers), Shared]
    [ExtensionOrder(Before = PredefinedCodeRefactoringProviderNames.AddConstructorParametersFromMembers)]
    internal partial class GenerateConstructorFromMembersCodeRefactoringProvider : AbstractGenerateFromMembersCodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;

            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var actions = await this.GenerateConstructorFromMembersAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
            context.RegisterRefactorings(actions);

            if (actions.IsDefaultOrEmpty && textSpan.IsEmpty)
            {
                var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
                var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                if (syntaxFacts.IsOnTypeHeader(root, textSpan.Start) ||
                    syntaxFacts.IsBetweenTypeMembers(sourceText, root, textSpan.Start))
                {
                    var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    var containingType = GetEnclosingNamedType(semanticModel, root, textSpan.Start, cancellationToken);
                    if (containingType?.TypeKind == TypeKind.Class || containingType?.TypeKind == TypeKind.Struct)
                    {
                        if (!containingType.IsStatic)
                        {
                            var viableMembers = containingType.GetMembers().WhereAsArray(IsWritableInstanceFieldOrProperty);
                            if (viableMembers.Length == 0 &&
                                containingType.InstanceConstructors.Any(c => c.Parameters.Length == 0 && !c.IsImplicitlyDeclared))
                            {
                                // If there are no fields, and there's already an explicit empty parameter constructor
                                // then we can't offer anything.
                                return;
                            }

                            var action = new GenerateConstructorCodeAction(
                                this, document, textSpan, containingType, viableMembers);
                            context.RegisterRefactoring(action);
                        }
                    }
                }
            }
        }

        private INamedTypeSymbol GetEnclosingNamedType(
            SemanticModel semanticModel, SyntaxNode root, int start, CancellationToken cancellationToken)
        {
            for (var node = root.FindToken(start).Parent; node != null; node = node.Parent)
            {
                if (semanticModel.GetDeclaredSymbol(node) is INamedTypeSymbol declaration)
                {
                    return declaration;
                }
            }

            return null;
        }

        public async Task<ImmutableArray<CodeAction>> GenerateConstructorFromMembersAsync(
            Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_GenerateFromMembers_GenerateConstructorFromMembers, cancellationToken))
            {
                var info = await GetSelectedMemberInfoAsync(document, textSpan, cancellationToken).ConfigureAwait(false);
                if (info != null)
                {
                    var state = State.Generate(this, document, textSpan, info.ContainingType, info.SelectedMembers, cancellationToken);
                    if (state != null && state.MatchingConstructor == null)
                    {
                        return GetCodeActions(document, state);
                    }
                }

                return default(ImmutableArray<CodeAction>);
            }
        }

        private ImmutableArray<CodeAction> GetCodeActions(Document document, State state)
        {
            var result = ArrayBuilder<CodeAction>.GetInstance();

            result.Add(new FieldDelegatingCodeAction(this, document, state));
            if (state.DelegatedConstructor != null)
            {
                result.Add(new ConstructorDelegatingCodeAction(this, document, state));
            }

            return result.ToImmutableAndFree();
        }

        private class GenerateConstructorCodeAction : CodeActionWithOptions
        {
            private readonly Document _document;
            private readonly INamedTypeSymbol _containingType;
            private readonly GenerateConstructorFromMembersCodeRefactoringProvider _service;
            private readonly TextSpan _textSpan;
            private readonly ImmutableArray<ISymbol> _viableMembers;

            public override string Title => FeaturesResources.Generate_constructor;

            public GenerateConstructorCodeAction(
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

                var state = State.Generate(
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