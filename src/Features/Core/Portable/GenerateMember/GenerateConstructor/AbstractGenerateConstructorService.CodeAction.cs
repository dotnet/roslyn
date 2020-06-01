// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor
{
    internal abstract partial class AbstractGenerateConstructorService<TService, TArgumentSyntax, TAttributeArgumentSyntax>
    {
        private class GenerateConstructorCodeAction : CodeAction
        {
            private readonly Document _document;
            private readonly State _state;
            private readonly bool _withFields;
            private readonly bool _withProperties;

            public GenerateConstructorCodeAction(
                Document document,
                State state,
                bool withFields,
                bool withProperties)
            {
                _document = document;
                _state = state;
                _withFields = withFields;
                _withProperties = withProperties;
            }

            public override string Title
                => _withFields ? string.Format(FeaturesResources.Generate_constructor_in_0_with_fields, _state.TypeToGenerateIn.Name) :
                   _withProperties ? string.Format(FeaturesResources.Generate_constructor_in_0_with_properties, _state.TypeToGenerateIn.Name) :
                                     string.Format(FeaturesResources.Generate_constructor_in_0, _state.TypeToGenerateIn.Name);

            public override string EquivalenceKey => Title;

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
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
                return await GenerateThisOrBaseDelegatingConstructorAsync(cancellationToken).ConfigureAwait(false) ??
                       await GenerateMemberDelegatingConstructorAsync(cancellationToken).ConfigureAwait(false);
            }

            private async Task<Document> GenerateThisOrBaseDelegatingConstructorAsync(CancellationToken cancellationToken)
            {
                var delegatedConstructor = _state.DelegatedConstructor;
                if (delegatedConstructor == null)
                    return null;

                // There was a best match.  Call it directly.  
                var provider = _document.Project.Solution.Workspace.Services.GetLanguageServices(_state.TypeToGenerateIn.Language);
                var syntaxFactory = provider.GetService<SyntaxGenerator>();
                var codeGenerationService = provider.GetService<ICodeGenerationService>();

                var semanticModel = await _document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var (members, assignments) = GenerateMembersAndAssignments(semanticModel);

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

                return await codeGenerationService.AddMembersAsync(
                    _document.Project.Solution,
                    _state.TypeToGenerateIn,
                    members.Concat(constructor),
                    new CodeGenerationOptions(_state.Token.GetLocation()),
                    cancellationToken).ConfigureAwait(false);
            }

            private (ImmutableArray<ISymbol>, ImmutableArray<SyntaxNode>) GenerateMembersAndAssignments(SemanticModel semanticModel)
            {
                var provider = _document.Project.Solution.Workspace.Services.GetLanguageServices(_state.TypeToGenerateIn.Language);
                var syntaxFactory = provider.GetService<SyntaxGenerator>();

                var members = _withFields ? SyntaxGeneratorExtensions.CreateFieldsForParameters(_state.RemainingParameters, _state.ParameterToNewFieldMap) :
                              _withProperties ? SyntaxGeneratorExtensions.CreatePropertiesForParameters(_state.RemainingParameters, _state.ParameterToNewPropertyMap) :
                              ImmutableArray<ISymbol>.Empty;

                var assignments = !_withFields && !_withProperties
                    ? ImmutableArray<SyntaxNode>.Empty
                    : syntaxFactory.CreateAssignmentStatements(
                        semanticModel, _state.RemainingParameters,
                        _state.ParameterToExistingMemberMap, _withFields ? _state.ParameterToNewFieldMap : _state.ParameterToNewPropertyMap,
                        addNullChecks: false, preferThrowExpression: false);

                return (members, assignments);
            }

            private async Task<Document> GenerateMemberDelegatingConstructorAsync(CancellationToken cancellationToken)
            {
                var provider = _document.Project.Solution.Workspace.Services.GetLanguageServices(_state.TypeToGenerateIn.Language);
                var codeGenerationService = provider.GetService<ICodeGenerationService>();
                var syntaxFactory = provider.GetService<SyntaxGenerator>();

                var semanticModel = await _document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                var newMemberMap =
                    _withFields ? _state.ParameterToNewFieldMap :
                    _withProperties ? _state.ParameterToNewPropertyMap :
                    ImmutableDictionary<string, string>.Empty;

                var members = syntaxFactory.CreateMemberDelegatingConstructor(
                    semanticModel,
                    _state.TypeToGenerateIn.Name,
                    _state.TypeToGenerateIn,
                    _state.RemainingParameters,
                    _state.ParameterToExistingMemberMap,
                    newMemberMap,
                    addNullChecks: false,
                    preferThrowExpression: false,
                    generateProperties: _withProperties);

                return await codeGenerationService.AddMembersAsync(
                    _document.Project.Solution,
                    _state.TypeToGenerateIn,
                    members,
                    new CodeGenerationOptions(_state.Token.GetLocation()),
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
