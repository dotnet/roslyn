// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

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
            private readonly TService _service;
            private readonly SemanticDocument _document;
            private readonly State _state;
            private readonly bool _withFields;
            private readonly bool _withProperties;
            private readonly CancellationToken _cancellationToken;

            public Editor(
                TService service,
                SemanticDocument document,
                State state,
                bool withFields,
                bool withProperties,
                CancellationToken cancellationToken)
            {
                Debug.Assert(!withFields || !withProperties);

                _service = service;
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
                //if (namedType == null)
                //{
                //    return default;
                //}

                //// We can't resolve overloads across language.
                //if (_document.Project.Language != namedType.Language)
                //{
                //    return default;
                //}

                //var arguments = _state.Arguments.Take(argumentCount).ToList();
                //var remainingArguments = _state.Arguments.Skip(argumentCount).ToImmutableArray();
                //var remainingAttributeArguments = _state.AttributeArguments != null
                //    ? _state.AttributeArguments.Skip(argumentCount).ToImmutableArray()
                //    : (ImmutableArray<TAttributeArgumentSyntax>?)null;
                //var remainingParameterTypes = _state.ParameterTypes.Skip(argumentCount).ToImmutableArray();

                //var instanceConstructors = namedType.InstanceConstructors.Where(IsSymbolAccessibleToDocument).ToSet();
                //if (instanceConstructors.IsEmpty())
                //{
                //    return default;
                //}

                var delegatedConstructor = _state.DelegatedConstructor;
                if (delegatedConstructor == null)
                    return null;

                // There was a best match.  Call it directly.  
                var provider = _document.Project.Solution.Workspace.Services.GetLanguageServices(_state.TypeToGenerateIn.Language);
                var syntaxFactory = provider.GetService<SyntaxGenerator>();
                var codeGenerationService = provider.GetService<ICodeGenerationService>();

                //// Map the first N parameters to the other constructor in this type.  Then
                //// try to map any further parameters to existing fields.  Finally, generate
                //// new fields if no such parameters exist.

                //// Find the names of the parameters that will follow the parameters we're
                //// delegating.
                //var remainingParameterNames = _service.GenerateParameterNames(
                //    _document.SemanticModel, _state.RemainingArguments,
                //    delegatedConstructor.Parameters.Select(p => p.Name).ToList(),
                //    _state.ParameterNamingRule,
                //    _cancellationToken);

                //// Can't generate the constructor if the parameter names we're copying over forcibly
                //// conflict with any names we generated.
                //if (delegatedConstructor.Parameters.Select(p => p.Name)
                //        .Intersect(remainingParameterNames.Select(n => n.BestNameForParameter)).Any())
                //{
                //    return default;
                //}

                //// Try to map those parameters to fields.
                //GetParameters(remainingArguments, remainingAttributeArguments,
                //    remainingParameterTypes, remainingParameterNames, fieldNamingRule, parameterNamingRule,
                //    out var parameterToExistingFieldMap, out var parameterToNewFieldMap, out var remainingParameters);

                var members = 
                    _withFields ? SyntaxGeneratorExtensions.CreateFieldsForParameters(_state.RemainingParameters, _state.ParameterToNewFieldMap) :
                    _withProperties ? SyntaxGeneratorExtensions.CreatePropertiesForParameters(_state.RemainingParameters, _state.ParameterToNewPropertyMap) :
                    ImmutableArray<ISymbol>.Empty;

                var assignStatements = syntaxFactory.CreateAssignmentStatements(
                    _document.SemanticModel, _state.RemainingParameters,
                    _state.ParameterToExistingMemberMap, _state.ParameterToNewFieldMap,
                    addNullChecks: false, preferThrowExpression: false);

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
                    statements: assignStatements,
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

            private async Task<Document> GenerateFieldDelegatingConstructorAsync()
            {
                //var arguments = _state.Arguments;
                //var parameterTypes = _state.ParameterTypes;

                //var typeParametersNames = _state.TypeToGenerateIn.GetAllTypeParameters().Select(t => t.Name).ToImmutableArray();
                //var parameterNames = GetParameterNames(arguments, typeParametersNames, parameterNamingRule);

                //GetParameters(arguments, _state.AttributeArguments, parameterTypes, parameterNames, fieldNamingRule, parameterNamingRule,
                //    out var parameterToExistingFieldMap, out var parameterToNewFieldMap, out var parameters);

                var provider = _document.Project.Solution.Workspace.Services.GetLanguageServices(_state.TypeToGenerateIn.Language);
                var syntaxFactory = provider.GetService<SyntaxGenerator>();
                var codeGenerationService = provider.GetService<ICodeGenerationService>();

                // var syntaxTree = _document.SyntaxTree;
                var (fields, constructor) = syntaxFactory.CreateFieldDelegatingConstructor(
                    _document.SemanticModel,
                    _state.TypeToGenerateIn.Name,
                    _state.TypeToGenerateIn,
                    _state.RemainingParameters,
                    parameterToExistingFieldMap, parameterToNewFieldMap,
                    addNullChecks: false, preferThrowExpression: false);

                var result = await codeGenerationService.AddMembersAsync(
                    _document.Project.Solution,
                    _state.TypeToGenerateIn,
                    fields.Concat(constructor),
                    new CodeGenerationOptions(_state.Token.GetLocation()),
                    _cancellationToken)
                    .ConfigureAwait(false);

                return result;
            }

            private ImmutableArray<ParameterName> GetParameterNames(
                ImmutableArray<TArgumentSyntax> arguments, ImmutableArray<string> typeParametersNames, NamingRule parameterNamingRule)
            {
                return _state.AttributeArguments != null
                    ? _service.GenerateParameterNames(_document.SemanticModel, _state.AttributeArguments, typeParametersNames, parameterNamingRule, _cancellationToken)
                    : _service.GenerateParameterNames(_document.SemanticModel, arguments, typeParametersNames, parameterNamingRule, _cancellationToken);
            }

            //private void GetParameters(
            //    ImmutableArray<TArgumentSyntax> arguments,
            //    ImmutableArray<TAttributeArgumentSyntax>? attributeArguments,
            //    ImmutableArray<ITypeSymbol> parameterTypes,
            //    ImmutableArray<ParameterName> parameterNames,
            //    NamingRule fieldNamingRule,
            //    NamingRule parameterNamingRule,
            //    out Dictionary<string, ISymbol> parameterToExistingFieldMap,
            //    out Dictionary<string, string> parameterToNewFieldMap,
            //    out ImmutableArray<IParameterSymbol> parameters)
            //{
            //    parameterToExistingFieldMap = new Dictionary<string, ISymbol>();
            //    parameterToNewFieldMap = new Dictionary<string, string>();
            //    var result = ArrayBuilder<IParameterSymbol>.GetInstance();

            //    for (var i = 0; i < parameterNames.Length; i++)
            //    {
            //        // See if there's a matching field we can use.  First test in a case sensitive
            //        // manner, then case insensitively.
            //        if (!TryFindMatchingField(
            //                arguments, attributeArguments, parameterNames, parameterTypes, i, parameterToExistingFieldMap,
            //                parameterToNewFieldMap, caseSensitive: true, fieldNamingRule, parameterNamingRule, newParameterNames: out parameterNames))
            //        {
            //            if (!TryFindMatchingField(
            //                    arguments, attributeArguments, parameterNames, parameterTypes, i, parameterToExistingFieldMap,
            //                    parameterToNewFieldMap, caseSensitive: false, fieldNamingRule, parameterNamingRule, newParameterNames: out parameterNames))
            //            {
            //                // If no matching field was found, use the fieldNamingRule to create suitable name
            //                parameterToNewFieldMap[parameterNames[i].BestNameForParameter] = fieldNamingRule.NamingStyle.MakeCompliant(
            //                    parameterNames[i].NameBasedOnArgument).First();
            //            }
            //        }

            //        result.Add(CodeGenerationSymbolFactory.CreateParameterSymbol(
            //            attributes: default,
            //            refKind: _service.GetRefKind(arguments[i]),
            //            isParams: false,
            //            type: parameterTypes[i],
            //            name: parameterNames[i].BestNameForParameter));
            //    }

            //    if (!_withFields)
            //    {
            //        parameterToNewFieldMap.Clear();
            //    }

            //    parameters = result.ToImmutableAndFree();
            //}

            private IEnumerable<string> GetUnavailableMemberNames()
            {
                return _state.TypeToGenerateIn.MemberNames.Concat(
                    from type in _state.TypeToGenerateIn.GetBaseTypes()
                    from member in type.GetMembers()
                    select member.Name);
            }

            private bool IsViableFieldOrProperty(
                ITypeSymbol parameterType,
                ISymbol symbol)
            {
                if (parameterType.Language != symbol.Language)
                {
                    return false;
                }

                if (symbol != null && !symbol.IsStatic)
                {
                    if (symbol is IFieldSymbol field)
                    {
                        return
                            !field.IsConst &&
                            _service.IsConversionImplicit(_document.SemanticModel.Compilation, parameterType, field.Type);
                    }
                    else if (symbol is IPropertySymbol property)
                    {
                        return
                            property.Parameters.Length == 0 &&
                            property.IsWritableInConstructor() &&
                            _service.IsConversionImplicit(_document.SemanticModel.Compilation, parameterType, property.Type);
                    }
                }

                return false;
            }

            private bool IsSymbolAccessibleToDocument(ISymbol symbol) => IsSymbolAccessible(symbol, _document);
        }
    }
}
