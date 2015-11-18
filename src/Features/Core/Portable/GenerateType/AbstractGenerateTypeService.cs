// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.LanguageServices.ProjectInfoService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateType
{
    internal abstract partial class AbstractGenerateTypeService<TService, TSimpleNameSyntax, TObjectCreationExpressionSyntax, TExpressionSyntax, TTypeDeclarationSyntax, TArgumentSyntax> :
        IGenerateTypeService
        where TService : AbstractGenerateTypeService<TService, TSimpleNameSyntax, TObjectCreationExpressionSyntax, TExpressionSyntax, TTypeDeclarationSyntax, TArgumentSyntax>
        where TSimpleNameSyntax : TExpressionSyntax
        where TObjectCreationExpressionSyntax : TExpressionSyntax
        where TExpressionSyntax : SyntaxNode
        where TTypeDeclarationSyntax : SyntaxNode
        where TArgumentSyntax : SyntaxNode
    {
        protected AbstractGenerateTypeService()
        {
        }

        protected abstract bool TryInitializeState(SemanticDocument document, TSimpleNameSyntax simpleName, CancellationToken cancellationToken, out GenerateTypeServiceStateOptions generateTypeServiceStateOptions);
        protected abstract TExpressionSyntax GetLeftSideOfDot(TSimpleNameSyntax simpleName);
        protected abstract bool TryGetArgumentList(TObjectCreationExpressionSyntax objectCreationExpression, out IList<TArgumentSyntax> argumentList);

        protected abstract string DefaultFileExtension { get; }
        protected abstract IList<ITypeParameterSymbol> GetTypeParameters(State state, SemanticModel semanticModel, CancellationToken cancellationToken);
        protected abstract Accessibility GetAccessibility(State state, SemanticModel semanticModel, bool intoNamespace, CancellationToken cancellationToken);
        protected abstract IList<string> GenerateParameterNames(SemanticModel semanticModel, IList<TArgumentSyntax> arguments);

        protected abstract INamedTypeSymbol DetermineTypeToGenerateIn(SemanticModel semanticModel, TSimpleNameSyntax simpleName, CancellationToken cancellationToken);
        protected abstract ITypeSymbol DetermineArgumentType(SemanticModel semanticModel, TArgumentSyntax argument, CancellationToken cancellationToken);

        protected abstract bool IsInCatchDeclaration(TExpressionSyntax expression);
        protected abstract bool IsArrayElementType(TExpressionSyntax expression);
        protected abstract bool IsInVariableTypeContext(TExpressionSyntax expression);
        protected abstract bool IsInValueTypeConstraintContext(SemanticModel semanticModel, TExpressionSyntax expression, CancellationToken cancellationToken);
        protected abstract bool IsInInterfaceList(TExpressionSyntax expression);
        internal abstract bool TryGetBaseList(TExpressionSyntax expression, out TypeKindOptions returnValue);
        internal abstract bool IsPublicOnlyAccessibility(TExpressionSyntax expression, Project project);
        internal abstract bool IsGenericName(TSimpleNameSyntax simpleName);
        internal abstract bool IsSimpleName(TExpressionSyntax expression);
        internal abstract Task<Solution> TryAddUsingsOrImportToDocumentAsync(Solution updatedSolution, SyntaxNode modifiedRoot, Document document, TSimpleNameSyntax simpleName, string includeUsingsOrImports, CancellationToken cancellationToken);

        protected abstract bool TryGetNameParts(TExpressionSyntax expression, out IList<string> nameParts);

        public abstract string GetRootNamespace(CompilationOptions options);

        public abstract Task<Tuple<INamespaceSymbol, INamespaceOrTypeSymbol, Location>> GetOrGenerateEnclosingNamespaceSymbolAsync(INamedTypeSymbol namedTypeSymbol, string[] containers, Document selectedDocument, SyntaxNode selectedDocumentRoot, CancellationToken cancellationToken);

        public async Task<IEnumerable<CodeAction>> GenerateTypeAsync(
            Document document,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Refactoring_GenerateType, cancellationToken))
            {
                var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

                var state = await State.GenerateAsync((TService)this, semanticDocument, node, cancellationToken).ConfigureAwait(false);
                if (state != null)
                {
                    var actions = GetActions(semanticDocument, node, state, cancellationToken).ToList();
                    if (actions.Count > 1)
                    {
                        // Wrap the generate type actions into a single top level suggestion
                        // so as to not clutter the list.
                        return SpecializedCollections.SingletonEnumerable(
                            new MyCodeAction(FeaturesResources.Generate_type, actions.AsImmutable()));
                    }
                    else
                    {
                        return actions;
                    }
                }

                return SpecializedCollections.EmptyEnumerable<CodeAction>();
            }
        }

        private IEnumerable<CodeAction> GetActions(
            SemanticDocument document,
            SyntaxNode node,
            State state,
            CancellationToken cancellationToken)
        {
            var generateNewTypeInDialog = false;
            if (state.NamespaceToGenerateInOpt != null)
            {
                var workspace = document.Project.Solution.Workspace;
                if (workspace == null || workspace.CanApplyChange(ApplyChangesKind.AddDocument))
                {
                    generateNewTypeInDialog = true;
                    yield return new GenerateTypeCodeAction((TService)this, document.Document, state, intoNamespace: true, inNewFile: true);
                }

                // If they just are generating "Foo" then we want to offer to generate it into the
                // namespace in the same file.  However, if they are generating "SomeNS.Foo", then we
                // only want to allow them to generate if "SomeNS" is the namespace they are
                // currently in.
                var isSimpleName = state.SimpleName == state.NameOrMemberAccessExpression;
                var generateIntoContaining = IsGeneratingIntoContainingNamespace(document, node, state, cancellationToken);

                if ((isSimpleName || generateIntoContaining) &&
                    CanGenerateIntoContainingNamespace(document, node, state, cancellationToken))
                {
                    yield return new GenerateTypeCodeAction((TService)this, document.Document, state, intoNamespace: true, inNewFile: false);
                }
            }

            if (state.TypeToGenerateInOpt != null)
            {
                yield return new GenerateTypeCodeAction((TService)this, document.Document, state, intoNamespace: false, inNewFile: false);
            }

            if (generateNewTypeInDialog)
            {
                yield return new GenerateTypeCodeActionWithOption((TService)this, document.Document, state);
            }
        }

        private bool CanGenerateIntoContainingNamespace(SemanticDocument document, SyntaxNode node, State state, CancellationToken cancellationToken)
        {
            var containingNamespace = document.SemanticModel.GetEnclosingNamespace(node.SpanStart, cancellationToken);

            // Only allow if the containing namespace is one that can be generated
            // into.  
            var declarationService = document.Project.LanguageServices.GetService<ISymbolDeclarationService>();
            var decl = declarationService.GetDeclarations(containingNamespace)
                                         .Where(r => r.SyntaxTree == node.SyntaxTree)
                                         .Select(r => r.GetSyntax(cancellationToken))
                                         .FirstOrDefault(node.GetAncestorsOrThis<SyntaxNode>().Contains);

            return
                decl != null &&
                document.Project.LanguageServices.GetService<ICodeGenerationService>().CanAddTo(decl, document.Project.Solution, cancellationToken);
        }

        private bool IsGeneratingIntoContainingNamespace(
            SemanticDocument document,
            SyntaxNode node,
            State state,
            CancellationToken cancellationToken)
        {
            var containingNamespace = document.SemanticModel.GetEnclosingNamespace(node.SpanStart, cancellationToken);
            if (containingNamespace != null)
            {
                var containingNamespaceName = containingNamespace.ToDisplayString();
                return containingNamespaceName.Equals(state.NamespaceToGenerateInOpt);
            }

            return false;
        }

        protected static string GetTypeName(State state)
        {
            const string AttributeSuffix = "Attribute";

            return state.IsAttribute && !state.NameIsVerbatim && !state.Name.EndsWith(AttributeSuffix, StringComparison.Ordinal)
                ? state.Name + AttributeSuffix
                : state.Name;
        }

        protected IList<ITypeParameterSymbol> GetTypeParameters(
            State state,
            SemanticModel semanticModel,
            IEnumerable<SyntaxNode> typeArguments,
            CancellationToken cancellationToken)
        {
            var arguments = typeArguments.ToList();
            var arity = arguments.Count;
            var typeParameters = new List<ITypeParameterSymbol>();

            // For anything that was a type parameter, just use the name (if we haven't already
            // used it).  Otherwise, synthesize new names for the parameters.
            var names = new string[arity];
            var isFixed = new bool[arity];
            for (var i = 0; i < arity; i++)
            {
                var argument = i < arguments.Count ? arguments[i] : null;
                var type = argument == null ? null : semanticModel.GetTypeInfo(argument, cancellationToken).Type;
                if (type is ITypeParameterSymbol)
                {
                    var name = type.Name;

                    // If we haven't seen this type parameter already, then we can use this name
                    // and 'fix' it so that it doesn't change. Otherwise, use it, but allow it
                    // to be changed if it collides with anything else.
                    isFixed[i] = !names.Contains(name);
                    names[i] = name;
                    typeParameters.Add((ITypeParameterSymbol)type);
                }
                else
                {
                    names[i] = "T";
                    typeParameters.Add(null);
                }
            }

            // We can use a type parameter as long as it hasn't been used in an outer type.
            var canUse = state.TypeToGenerateInOpt == null
                ? default(Func<string, bool>)
                : s => state.TypeToGenerateInOpt.GetAllTypeParameters().All(t => t.Name != s);

            var uniqueNames = NameGenerator.EnsureUniqueness(names, isFixed, canUse: canUse);
            for (int i = 0; i < uniqueNames.Count; i++)
            {
                if (typeParameters[i] == null || typeParameters[i].Name != uniqueNames[i])
                {
                    typeParameters[i] = CodeGenerationSymbolFactory.CreateTypeParameterSymbol(uniqueNames[i]);
                }
            }

            return typeParameters;
        }

        protected Accessibility DetermineDefaultAccessibility(
            State state,
            SemanticModel semanticModel,
            bool intoNamespace,
            CancellationToken cancellationToken)
        {
            if (state.IsPublicAccessibilityForTypeGeneration)
            {
                return Accessibility.Public;
            }

            // If we're a nested type of the type being generated into, then the new type can be
            // private.  otherwise, it needs to be internal.
            if (!intoNamespace && state.TypeToGenerateInOpt != null)
            {
                var outerTypeSymbol = semanticModel.GetEnclosingNamedType(state.SimpleName.SpanStart, cancellationToken);

                if (outerTypeSymbol != null && outerTypeSymbol.IsContainedWithin(state.TypeToGenerateInOpt))
                {
                    return Accessibility.Private;
                }
            }

            return Accessibility.Internal;
        }

        protected IList<ITypeParameterSymbol> GetAvailableTypeParameters(
            State state,
            SemanticModel semanticModel,
            bool intoNamespace,
            CancellationToken cancellationToken)
        {
            var availableInnerTypeParameters = GetTypeParameters(state, semanticModel, cancellationToken);
            var availableOuterTypeParameters = !intoNamespace && state.TypeToGenerateInOpt != null
                ? state.TypeToGenerateInOpt.GetAllTypeParameters()
                : SpecializedCollections.EmptyEnumerable<ITypeParameterSymbol>();

            return availableOuterTypeParameters.Concat(availableInnerTypeParameters).ToList();
        }

        protected bool IsWithinTheImportingNamespace(Document document, int triggeringPosition, string includeUsingsOrImports, CancellationToken cancellationToken)
        {
            var semanticModel = document.GetSemanticModelAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            if (semanticModel != null)
            {
                var namespaceSymbol = semanticModel.GetEnclosingNamespace(triggeringPosition, cancellationToken);
                if (namespaceSymbol != null && namespaceSymbol.ToDisplayString().StartsWith(includeUsingsOrImports, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        protected bool GeneratedTypesMustBePublic(Project project)
        {
            var projectInfoService = project.Solution.Workspace.Services.GetService<IProjectInfoService>();
            if (projectInfoService != null)
            {
                return projectInfoService.GeneratedTypesMustBePublic(project);
            }

            return false;
        }

        private class MyCodeAction : CodeAction.SimpleCodeAction
        {
            public MyCodeAction(string title, ImmutableArray<CodeAction> nestedActions)
                : base(title, nestedActions)
            {
            }
        }
    }
}
