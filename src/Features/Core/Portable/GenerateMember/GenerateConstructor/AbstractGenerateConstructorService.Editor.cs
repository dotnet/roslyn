// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Naming;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;

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
            private readonly CancellationToken _cancellationToken;

            public Editor(
                TService service,
                SemanticDocument document,
                State state,
                bool withFields,
                CancellationToken cancellationToken)
            {
                _service = service;
                _document = document;
                _state = state;
                _withFields = withFields;
                _cancellationToken = cancellationToken;
            }

            internal async Task<(Document, bool addedFields)> GetEditAsync()
            {
                // Get naming rule for generating fields and parameters
                var rules = await _document.Document.GetNamingRulesAsync(
                    FallbackNamingRules.RefactoringMatchLookupRules, _cancellationToken).ConfigureAwait(false);
                var fieldNamingRule = rules.Where(c => c.SymbolSpecification.AppliesTo(new SymbolKindOrTypeKind(SymbolKind.Field),
                    DeclarationModifiers.None, Accessibility.Private)).First();
                var parameterNamingRule = rules.Where(c => c.SymbolSpecification.AppliesTo(new SymbolKindOrTypeKind(SymbolKind.Parameter),
                    DeclarationModifiers.None, Accessibility.NotApplicable)).First();

                // See if there's an accessible base constructor that would accept these
                // types, then just call into that instead of generating fields.
                //
                // then, see if there are any constructors that would take the first 'n' arguments
                // we've provided.  If so, delegate to those, and then create a field for any
                // remaining arguments.  Try to match from largest to smallest.
                var edit = await GenerateThisOrBaseDelegatingConstructorAsync(fieldNamingRule, parameterNamingRule).ConfigureAwait(false);
                if (edit.document != null)
                {
                    return edit;
                }

                // Otherwise, just generate a normal constructor that assigns any provided
                // parameters into fields.
                return await GenerateFieldDelegatingConstructorAsync(fieldNamingRule, parameterNamingRule).ConfigureAwait(false);
            }

            private async Task<(Document document, bool addedFields)> GenerateThisOrBaseDelegatingConstructorAsync(NamingRule fieldNamingRule, NamingRule parameterNamingRule)
            {
                // We don't have to deal with the zero length case, since there's nothing to
                // delegate.  It will fall out of the GenerateFieldDelegatingConstructor above.
                for (var i = _state.Arguments.Length; i >= 1; i--)
                {
                    var edit = await GenerateThisOrBaseDelegatingConstructorAsync(i, fieldNamingRule, parameterNamingRule).ConfigureAwait(false);
                    if (edit.document != null)
                    {
                        return edit;
                    }
                }

                return default;
            }

            private async Task<(Document document, bool addedFields)> GenerateThisOrBaseDelegatingConstructorAsync(
                int argumentCount, NamingRule fieldNamingRule, NamingRule parameterNamingRule)
            {
                (Document document, bool addedField) edit;
                if ((edit = await GenerateDelegatingConstructorAsync(argumentCount, _state.TypeToGenerateIn, fieldNamingRule, parameterNamingRule).ConfigureAwait(false)).document != null ||
                    (edit = await GenerateDelegatingConstructorAsync(argumentCount, _state.TypeToGenerateIn.BaseType, fieldNamingRule, parameterNamingRule).ConfigureAwait(false)).document != null)
                {
                    return edit;
                }

                return default;
            }

            private async Task<(Document, bool addedFields)> GenerateDelegatingConstructorAsync(
                int argumentCount, INamedTypeSymbol namedType, NamingRule fieldNamingRule, NamingRule parameterNamingRule)
            {
                if (namedType == null)
                {
                    return default;
                }

                // We can't resolve overloads across language.
                if (_document.Project.Language != namedType.Language)
                {
                    return default;
                }

                var arguments = _state.Arguments.Take(argumentCount).ToList();
                var remainingArguments = _state.Arguments.Skip(argumentCount).ToImmutableArray();
                var remainingAttributeArguments = _state.AttributeArguments != null
                    ? _state.AttributeArguments.Skip(argumentCount).ToImmutableArray()
                    : (ImmutableArray<TAttributeArgumentSyntax>?)null;
                var remainingParameterTypes = _state.ParameterTypes.Skip(argumentCount).ToImmutableArray();

                var instanceConstructors = namedType.InstanceConstructors.Where(IsSymbolAccessibleToDocument).ToSet();
                if (instanceConstructors.IsEmpty())
                {
                    return default;
                }

                var delegatedConstructor = _service.GetDelegatingConstructor(_state, _document, argumentCount, namedType, instanceConstructors, _cancellationToken);
                if (delegatedConstructor == null)
                {
                    return default;
                }

                // There was a best match.  Call it directly.  
                var provider = _document.Project.Solution.Workspace.Services.GetLanguageServices(_state.TypeToGenerateIn.Language);
                var syntaxFactory = provider.GetService<SyntaxGenerator>();
                var codeGenerationService = provider.GetService<ICodeGenerationService>();

                // Map the first N parameters to the other constructor in this type.  Then
                // try to map any further parameters to existing fields.  Finally, generate
                // new fields if no such parameters exist.

                // Find the names of the parameters that will follow the parameters we're
                // delegating.
                var remainingParameterNames = _service.GenerateParameterNames(
                    _document.SemanticModel, remainingArguments,
                    delegatedConstructor.Parameters.Select(p => p.Name).ToList(),
                    parameterNamingRule,
                    _cancellationToken);

                // Can't generate the constructor if the parameter names we're copying over forcibly
                // conflict with any names we generated.
                if (delegatedConstructor.Parameters.Select(p => p.Name)
                        .Intersect(remainingParameterNames.Select(n => n.BestNameForParameter)).Any())
                {
                    return default;
                }

                // Try to map those parameters to fields.
                GetParameters(remainingArguments, remainingAttributeArguments,
                    remainingParameterTypes, remainingParameterNames, fieldNamingRule, parameterNamingRule,
                    out var parameterToExistingFieldMap, out var parameterToNewFieldMap, out var remainingParameters);

                var fields = _withFields
                    ? syntaxFactory.CreateFieldsForParameters(remainingParameters, parameterToNewFieldMap)
                    : ImmutableArray<IFieldSymbol>.Empty;
                var assignStatements = syntaxFactory.CreateAssignmentStatements(
                    _document.SemanticModel, remainingParameters,
                    parameterToExistingFieldMap, parameterToNewFieldMap,
                    addNullChecks: false, preferThrowExpression: false);

                var allParameters = delegatedConstructor.Parameters.Concat(remainingParameters);

                var isThis = namedType.Equals(_state.TypeToGenerateIn);
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

                var members = fields.OfType<ISymbol>().Concat(constructor);
                var result = await codeGenerationService.AddMembersAsync(
                    _document.Project.Solution,
                    _state.TypeToGenerateIn,
                    members,
                    new CodeGenerationOptions(_state.Token.GetLocation()),
                    _cancellationToken)
                    .ConfigureAwait(false);

                return (result, fields.Length > 0);
            }

            private async Task<(Document, bool addedFields)> GenerateFieldDelegatingConstructorAsync(NamingRule fieldNamingRule, NamingRule parameterNamingRule)
            {
                var arguments = _state.Arguments;
                var parameterTypes = _state.ParameterTypes;

                var typeParametersNames = _state.TypeToGenerateIn.GetAllTypeParameters().Select(t => t.Name).ToImmutableArray();
                var parameterNames = GetParameterNames(arguments, typeParametersNames, parameterNamingRule);

                GetParameters(arguments, _state.AttributeArguments, parameterTypes, parameterNames, fieldNamingRule, parameterNamingRule,
                    out var parameterToExistingFieldMap, out var parameterToNewFieldMap, out var parameters);

                var provider = _document.Project.Solution.Workspace.Services.GetLanguageServices(_state.TypeToGenerateIn.Language);
                var syntaxFactory = provider.GetService<SyntaxGenerator>();
                var codeGenerationService = provider.GetService<ICodeGenerationService>();

                var syntaxTree = _document.SyntaxTree;
                var (fields, constructor) = syntaxFactory.CreateFieldDelegatingConstructor(
                    _document.SemanticModel,
                    _state.TypeToGenerateIn.Name,
                    _state.TypeToGenerateIn, parameters,
                    parameterToExistingFieldMap, parameterToNewFieldMap,
                    addNullChecks: false, preferThrowExpression: false);

                var result = await codeGenerationService.AddMembersAsync(
                    _document.Project.Solution,
                    _state.TypeToGenerateIn,
                    fields.Concat(constructor),
                    new CodeGenerationOptions(_state.Token.GetLocation()),
                    _cancellationToken)
                    .ConfigureAwait(false);

                return (result, fields.Length > 0);
            }

            private ImmutableArray<ParameterName> GetParameterNames(
                ImmutableArray<TArgumentSyntax> arguments, ImmutableArray<string> typeParametersNames, NamingRule parameterNamingRule)
            {
                return _state.AttributeArguments != null
                    ? _service.GenerateParameterNames(_document.SemanticModel, _state.AttributeArguments, typeParametersNames, parameterNamingRule, _cancellationToken)
                    : _service.GenerateParameterNames(_document.SemanticModel, arguments, typeParametersNames, parameterNamingRule, _cancellationToken);
            }

            private void GetParameters(
                ImmutableArray<TArgumentSyntax> arguments,
                ImmutableArray<TAttributeArgumentSyntax>? attributeArguments,
                ImmutableArray<ITypeSymbol> parameterTypes,
                ImmutableArray<ParameterName> parameterNames,
                NamingRule fieldNamingRule,
                NamingRule parameterNamingRule,
                out Dictionary<string, ISymbol> parameterToExistingFieldMap,
                out Dictionary<string, string> parameterToNewFieldMap,
                out ImmutableArray<IParameterSymbol> parameters)
            {
                parameterToExistingFieldMap = new Dictionary<string, ISymbol>();
                parameterToNewFieldMap = new Dictionary<string, string>();
                var result = ArrayBuilder<IParameterSymbol>.GetInstance();

                for (var i = 0; i < parameterNames.Length; i++)
                {
                    // See if there's a matching field we can use.  First test in a case sensitive
                    // manner, then case insensitively.
                    if (!TryFindMatchingField(
                            arguments, attributeArguments, parameterNames, parameterTypes, i, parameterToExistingFieldMap,
                            parameterToNewFieldMap, caseSensitive: true, fieldNamingRule, parameterNamingRule, newParameterNames: out parameterNames))
                    {
                        if (!TryFindMatchingField(
                                arguments, attributeArguments, parameterNames, parameterTypes, i, parameterToExistingFieldMap,
                                parameterToNewFieldMap, caseSensitive: false, fieldNamingRule, parameterNamingRule, newParameterNames: out parameterNames))
                        {
                            // If no matching field was found, use the fieldNamingRule to create suitable name
                            parameterToNewFieldMap[parameterNames[i].BestNameForParameter] = fieldNamingRule.NamingStyle.MakeCompliant(
                                parameterNames[i].NameBasedOnArgument).First();
                        }
                    }

                    result.Add(CodeGenerationSymbolFactory.CreateParameterSymbol(
                        attributes: default,
                        refKind: _service.GetRefKind(arguments[i]),
                        isParams: false,
                        type: parameterTypes[i],
                        name: parameterNames[i].BestNameForParameter));
                }

                if (!_withFields)
                {
                    parameterToNewFieldMap.Clear();
                }

                parameters = result.ToImmutableAndFree();
            }

            private bool TryFindMatchingField(
                ImmutableArray<TArgumentSyntax> arguments,
                ImmutableArray<TAttributeArgumentSyntax>? attributeArguments,
                ImmutableArray<ParameterName> parameterNames,
                ImmutableArray<ITypeSymbol> parameterTypes,
                int index,
                Dictionary<string, ISymbol> parameterToExistingFieldMap,
                Dictionary<string, string> parameterToNewFieldMap,
                bool caseSensitive,
                NamingRule fieldNamingRule,
                NamingRule parameterNamingRule,
                out ImmutableArray<ParameterName> newParameterNames)
            {
                var parameterName = parameterNames[index];
                var parameterType = parameterTypes[index];
                var expectedFieldName = fieldNamingRule.NamingStyle.MakeCompliant(parameterName.NameBasedOnArgument).First();
                var isFixed = _service.IsNamedArgument(arguments[index]);
                var newParameterNamesList = parameterNames.ToList();

                // For non-out parameters, see if there's already a field there with the same name.
                // If so, and it has a compatible type, then we can just assign to that field.
                // Otherwise, we'll need to choose a different name for this member so that it
                // doesn't conflict with something already in the type. First check the current type
                // for a matching field.  If so, defer to it.
                var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                foreach (var type in _state.TypeToGenerateIn.GetBaseTypesAndThis())
                {
                    var ignoreAccessibility = type.Equals(_state.TypeToGenerateIn);
                    var symbol = type.GetMembers()
                                     .FirstOrDefault(s => s.Name.Equals(expectedFieldName, comparison));

                    if (symbol != null)
                    {
                        if (ignoreAccessibility || IsSymbolAccessibleToDocument(symbol))
                        {
                            if (IsViableFieldOrProperty(parameterType, symbol))
                            {
                                // Ok!  We can just the existing field.  
                                parameterToExistingFieldMap[parameterName.BestNameForParameter] = symbol;
                            }
                            else
                            {
                                // Uh-oh.  Now we have a problem.  We can't assign this parameter to
                                // this field.  So we need to create a new field.  Find a name not in
                                // use so we can assign to that.  
                                var baseName = attributeArguments != null
                                    ? _service.GenerateNameForArgument(_document.SemanticModel, attributeArguments.Value[index], _cancellationToken)
                                    : _service.GenerateNameForArgument(_document.SemanticModel, arguments[index], _cancellationToken);
                                var baseWithNamingStyle = fieldNamingRule.NamingStyle.MakeCompliant(baseName).First();
                                var newFieldName = NameGenerator.EnsureUniqueness(baseWithNamingStyle, GetUnavailableMemberNames().Concat(parameterToNewFieldMap.Values));

                                if (isFixed)
                                {
                                    // Can't change the parameter name, so map the existing parameter
                                    // name to the new field name.
                                    parameterToNewFieldMap[parameterName.NameBasedOnArgument] = newFieldName;
                                }
                                else
                                {
                                    // Can change the parameter name, so do so.  
                                    // But first remove any prefix added due to field naming styles
                                    var fieldNameMinusPrefix = newFieldName.Substring(fieldNamingRule.NamingStyle.Prefix.Length);
                                    var newParameterName = new ParameterName(fieldNameMinusPrefix, isFixed: false, parameterNamingRule);
                                    newParameterNamesList[index] = newParameterName;
                                    parameterToNewFieldMap[newParameterName.BestNameForParameter] = newFieldName;
                                }
                            }

                            newParameterNames = newParameterNamesList.ToImmutableArray();
                            return true;
                        }
                    }
                }

                newParameterNames = newParameterNamesList.ToImmutableArray();
                return false;
            }

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
