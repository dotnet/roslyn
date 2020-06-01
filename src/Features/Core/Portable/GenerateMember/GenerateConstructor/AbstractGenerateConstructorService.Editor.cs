// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor
{
    internal abstract partial class AbstractGenerateConstructorService<TService, TArgumentSyntax, TAttributeArgumentSyntax>
    {
        protected abstract bool IsConversionImplicit(Compilation compilation, ITypeSymbol sourceType, ITypeSymbol targetType);

        internal abstract IMethodSymbol GetDelegatingConstructor(State state, SemanticDocument document, int argumentCount, INamedTypeSymbol namedType, ISet<IMethodSymbol> candidates, CancellationToken cancellationToken);

        protected abstract IMethodSymbol GetCurrentConstructor(SemanticModel semanticModel, SyntaxToken token, CancellationToken cancellationToken);

        protected abstract IMethodSymbol GetDelegatedConstructor(SemanticModel semanticModel, IMethodSymbol constructor, CancellationToken cancellationToken);

        protected bool CanDelegeteThisConstructor(State state, SemanticDocument document, IMethodSymbol delegatedConstructor, CancellationToken cancellationToken = default)
        {
            var currentConstructor = GetCurrentConstructor(document.SemanticModel, state.Token, cancellationToken);
            if (currentConstructor.Equals(delegatedConstructor))
            {
                return false;
            }

            // We need ensure that delegating constructor won't cause circular dependency.
            // The chain of dependency can not exceed the number for constructors
            var constructorsCount = delegatedConstructor.ContainingType.InstanceConstructors.Length;
            for (var i = 0; i < constructorsCount; i++)
            {
                delegatedConstructor = GetDelegatedConstructor(document.SemanticModel, delegatedConstructor, cancellationToken);
                if (delegatedConstructor == null)
                {
                    return true;
                }

                if (delegatedConstructor.Equals(currentConstructor))
                {
                    return false;
                }
            }

            return false;
        }

        private partial class Editor
        {
            private readonly SemanticDocument _document;
            private readonly State _state;
            private readonly bool _withFields;
            private readonly bool _withProperties;
            private readonly CancellationToken _cancellationToken;

            public Editor(
                SemanticDocument document,
                State state,
                bool withFields,
                bool withProperties,
                CancellationToken cancellationToken)
            {
                Debug.Assert(!withFields || !withProperties);

                _document = document;
                _state = state;
                _withFields = withFields;
                _withProperties = withProperties;
                _cancellationToken = cancellationToken;
            }

            public async Task<Document> GetEditAsync()
            {
                // See if there's an accessible base constructor that would accept these
                // types, then just call into that instead of generating fields.
                //
                // then, see if there are any constructors that would take the first 'n' arguments
                // we've provided.  If so, delegate to those, and then create a field for any
                // remaining arguments.  Try to match from largest to smallest.
                //
                // Otherwise, just generate a normal constructor that assigns any provided
                // parameters into fields.
                return await GenerateThisOrBaseDelegatingConstructorAsync().ConfigureAwait(false) ??
                       await GenerateFieldDelegatingConstructorAsync().ConfigureAwait(false);
            }

            private async Task<Document> GenerateThisOrBaseDelegatingConstructorAsync()
            {
                var delegatedConstructor = _state.DelegatedConstructor;
                if (delegatedConstructor == null)
                    return null;

                // There was a best match.  Call it directly.  
                var provider = _document.Project.Solution.Workspace.Services.GetLanguageServices(_state.TypeToGenerateIn.Language);
                var syntaxFactory = provider.GetService<SyntaxGenerator>();
                var codeGenerationService = provider.GetService<ICodeGenerationService>();

                var (members, assignments) = GenerateMembersAndAssignments();

                var allParameters = delegatedConstructor.Parameters.Concat(_state.RemainingParameters);

                var isThis = delegatedConstructor.ContainingType.OriginalDefinition.Equals(_state.TypeToGenerateIn.OriginalDefinition);
                var delegatingArguments = syntaxFactory.CreateArguments(delegatedConstructor.Parameters);
                var baseConstructorArguments = isThis ? default : delegatingArguments;
                var thisConstructorArguments = isThis ? delegatingArguments : default;

                var constructor = CodeGenerationSymbolFactory.CreateConstructorSymbol(
                    attributes: default,
                    accessibility: Accessibility.Public,
                    modifiers: default,
                    typeName: _state.TypeToGenerateIn.Name,
                    parameters: allParameters,
                    statements: assignments,
                    baseConstructorArguments: baseConstructorArguments,
                    thisConstructorArguments: thisConstructorArguments);

                members = members.Concat(constructor);
                var result = await codeGenerationService.AddMembersAsync(
                    _document.Project.Solution,
                    _state.TypeToGenerateIn,
                    members,
                    new CodeGenerationOptions(_state.Token.GetLocation()),
                    _cancellationToken)
                    .ConfigureAwait(false);

                return result;
            }

            private (ImmutableArray<ISymbol>, ImmutableArray<SyntaxNode>) GenerateMembersAndAssignments()
            {
                var provider = _document.Project.Solution.Workspace.Services.GetLanguageServices(_state.TypeToGenerateIn.Language);
                var syntaxFactory = provider.GetService<SyntaxGenerator>();

                if (_withFields)
                {
                    var members = SyntaxGeneratorExtensions.CreateFieldsForParameters(_state.RemainingParameters, _state.ParameterToNewFieldMap);
                    var assignments = syntaxFactory.CreateAssignmentStatements(
                        _document.SemanticModel, _state.RemainingParameters,
                        _state.ParameterToExistingMemberMap, _state.ParameterToNewFieldMap,
                        addNullChecks: false, preferThrowExpression: false);

                    return (members, assignments);
                }
                else if (_withProperties)
                {
                    var members = SyntaxGeneratorExtensions.CreatePropertiesForParameters(_state.RemainingParameters, _state.ParameterToNewPropertyMap);
                    var assignments = syntaxFactory.CreateAssignmentStatements(
                        _document.SemanticModel, _state.RemainingParameters,
                        _state.ParameterToExistingMemberMap, _state.ParameterToNewPropertyMap,
                        addNullChecks: false, preferThrowExpression: false);

                    return (members, assignments);
                }
                else
                {
                    return (ImmutableArray<ISymbol>.Empty, ImmutableArray<SyntaxNode>.Empty);
                }
            }

            private async Task<Document> GenerateFieldDelegatingConstructorAsync()
            {
                var provider = _document.Project.Solution.Workspace.Services.GetLanguageServices(_state.TypeToGenerateIn.Language);
                var codeGenerationService = provider.GetService<ICodeGenerationService>();
                var syntaxFactory = provider.GetService<SyntaxGenerator>();

                // var syntaxTree = _document.SyntaxTree;
                var members = syntaxFactory.CreateMemberDelegatingConstructor(
                    _document.SemanticModel,
                    _state.TypeToGenerateIn.Name,
                    _state.TypeToGenerateIn,
                    _state.RemainingParameters,
                    _state.ParameterToExistingMemberMap,
                    _state.ParameterToNewFieldMap,
                    addNullChecks: false,
                    preferThrowExpression: false,
                    generateProperties: _withProperties);

                var result = await codeGenerationService.AddMembersAsync(
                    _document.Project.Solution,
                    _state.TypeToGenerateIn,
                    members,
                    new CodeGenerationOptions(_state.Token.GetLocation()),
                    _cancellationToken)
                    .ConfigureAwait(false);

                return result;
            }
        }
    }
}
