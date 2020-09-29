// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor
{
    internal abstract partial class AbstractGenerateConstructorService<TService, TArgumentSyntax, TAttributeArgumentSyntax>
    {
        protected internal class State
        {
            private readonly TService _service;
            private readonly SemanticDocument _document;

            private readonly NamingRule _fieldNamingRule;
            private readonly NamingRule _propertyNamingRule;
            private readonly NamingRule _parameterNamingRule;

            private ImmutableArray<TArgumentSyntax> _arguments;
            private ImmutableArray<TAttributeArgumentSyntax> _attributeArguments;

            // The type we're creating a constructor for.  Will be a class or struct type.
            public INamedTypeSymbol TypeToGenerateIn { get; private set; }

            private ImmutableArray<RefKind> _parameterRefKinds;
            public ImmutableArray<ITypeSymbol> ParameterTypes;

            public SyntaxToken Token { get; private set; }
            public bool IsConstructorInitializerGeneration { get; private set; }

            private IMethodSymbol _delegatedConstructor;

            private ImmutableArray<IParameterSymbol> _parameters;
            private ImmutableDictionary<string, ISymbol> _parameterToExistingMemberMap;

            public ImmutableDictionary<string, string> ParameterToNewFieldMap { get; private set; }
            public ImmutableDictionary<string, string> ParameterToNewPropertyMap { get; private set; }
            public bool IsContainedInUnsafeType { get; private set; }

            private State(TService service, SemanticDocument document, NamingRule fieldNamingRule, NamingRule propertyNamingRule, NamingRule parameterNamingRule)
            {
                _service = service;
                _document = document;
                _fieldNamingRule = fieldNamingRule;
                _propertyNamingRule = propertyNamingRule;
                _parameterNamingRule = parameterNamingRule;
            }

            public static async Task<State> GenerateAsync(
                TService service,
                SemanticDocument document,
                SyntaxNode node,
                CancellationToken cancellationToken)
            {
                var fieldNamingRule = await document.Document.GetApplicableNamingRuleAsync(SymbolKind.Field, Accessibility.Private, cancellationToken).ConfigureAwait(false);
                var propertyNamingRule = await document.Document.GetApplicableNamingRuleAsync(SymbolKind.Property, Accessibility.Public, cancellationToken).ConfigureAwait(false);
                var parameterNamingRule = await document.Document.GetApplicableNamingRuleAsync(SymbolKind.Parameter, Accessibility.NotApplicable, cancellationToken).ConfigureAwait(false);

                var state = new State(service, document, fieldNamingRule, propertyNamingRule, parameterNamingRule);
                if (!await state.TryInitializeAsync(node, cancellationToken).ConfigureAwait(false))
                {
                    return null;
                }

                return state;
            }

            private async Task<bool> TryInitializeAsync(
                SyntaxNode node, CancellationToken cancellationToken)
            {
                if (_service.IsConstructorInitializerGeneration(_document, node, cancellationToken))
                {
                    if (!await TryInitializeConstructorInitializerGenerationAsync(node, cancellationToken).ConfigureAwait(false))
                        return false;
                }
                else if (_service.IsSimpleNameGeneration(_document, node, cancellationToken))
                {
                    if (!await TryInitializeSimpleNameGenerationAsync(node, cancellationToken).ConfigureAwait(false))
                        return false;
                }
                else
                {
                    return false;
                }

                if (!CodeGenerator.CanAdd(_document.Project.Solution, TypeToGenerateIn, cancellationToken))
                    return false;

                ParameterTypes = ParameterTypes.IsDefault ? GetParameterTypes(cancellationToken) : ParameterTypes;
                _parameterRefKinds = _arguments.Select(_service.GetRefKind).ToImmutableArray();

                if (ClashesWithExistingConstructor())
                    return false;

                if (!TryInitializeDelegatedConstructor(cancellationToken))
                    InitializeNonDelegatedConstructor(cancellationToken);

                IsContainedInUnsafeType = _service.ContainingTypesOrSelfHasUnsafeKeyword(TypeToGenerateIn);

                return true;
            }

            private void InitializeNonDelegatedConstructor(CancellationToken cancellationToken)
            {
                var typeParametersNames = TypeToGenerateIn.GetAllTypeParameters().Select(t => t.Name).ToImmutableArray();
                var parameterNames = GetParameterNames(_arguments, typeParametersNames, cancellationToken);

                GetParameters(_arguments, _attributeArguments, ParameterTypes, parameterNames, cancellationToken);
            }

            private ImmutableArray<ParameterName> GetParameterNames(
                ImmutableArray<TArgumentSyntax> arguments, ImmutableArray<string> typeParametersNames, CancellationToken cancellationToken)
            {
                return _attributeArguments != null
                    ? _service.GenerateParameterNames(_document.SemanticModel, _attributeArguments, typeParametersNames, _parameterNamingRule, cancellationToken)
                    : _service.GenerateParameterNames(_document.SemanticModel, arguments, typeParametersNames, _parameterNamingRule, cancellationToken);
            }

            private bool TryInitializeDelegatedConstructor(CancellationToken cancellationToken)
            {
                // We don't have to deal with the zero length case, since there's nothing to
                // delegate.  It will fall out of the GenerateFieldDelegatingConstructor above.
                for (var i = _arguments.Length; i >= 1; i--)
                {
                    if (InitializeDelegatedConstructor(i, cancellationToken))
                        return true;
                }

                return false;
            }

            private bool InitializeDelegatedConstructor(int argumentCount, CancellationToken cancellationToken)
                => InitializeDelegatedConstructor(argumentCount, TypeToGenerateIn, cancellationToken) ||
                   InitializeDelegatedConstructor(argumentCount, TypeToGenerateIn.BaseType, cancellationToken);

            private bool InitializeDelegatedConstructor(int argumentCount, INamedTypeSymbol namedType, CancellationToken cancellationToken)
            {
                // We can't resolve overloads across language.
                if (_document.Project.Language != namedType.Language)
                    return false;

                var arguments = _arguments.Take(argumentCount).ToList();
                var remainingArguments = _arguments.Skip(argumentCount).ToImmutableArray();
                var remainingAttributeArguments = _attributeArguments != null
                    ? _attributeArguments.Skip(argumentCount).ToImmutableArray()
                    : (ImmutableArray<TAttributeArgumentSyntax>?)null;
                var remainingParameterTypes = ParameterTypes.Skip(argumentCount).ToImmutableArray();

                var instanceConstructors = namedType.InstanceConstructors.Where(c => IsSymbolAccessible(c, _document)).ToSet();
                if (instanceConstructors.IsEmpty())
                    return false;

                var delegatedConstructor = _service.GetDelegatingConstructor(this, _document, argumentCount, namedType, instanceConstructors, cancellationToken);
                if (delegatedConstructor == null)
                    return false;

                // Map the first N parameters to the other constructor in this type.  Then
                // try to map any further parameters to existing fields.  Finally, generate
                // new fields if no such parameters exist.

                // Find the names of the parameters that will follow the parameters we're
                // delegating.
                var remainingParameterNames = _service.GenerateParameterNames(
                    _document.SemanticModel, remainingArguments,
                    delegatedConstructor.Parameters.Select(p => p.Name).ToList(),
                    _parameterNamingRule,
                    cancellationToken);

                // Can't generate the constructor if the parameter names we're copying over forcibly
                // conflict with any names we generated.
                if (delegatedConstructor.Parameters.Select(p => p.Name).Intersect(remainingParameterNames.Select(n => n.BestNameForParameter)).Any())
                {
                    return false;
                }

                _delegatedConstructor = delegatedConstructor;
                GetParameters(remainingArguments, remainingAttributeArguments, remainingParameterTypes, remainingParameterNames, cancellationToken);
                return true;
            }

            private bool ClashesWithExistingConstructor()
            {
                var destinationProvider = _document.Project.Solution.Workspace.Services.GetLanguageServices(TypeToGenerateIn.Language);
                var syntaxFacts = destinationProvider.GetService<ISyntaxFactsService>();
                return TypeToGenerateIn.InstanceConstructors.Any(c => Matches(c, syntaxFacts));
            }

            private bool Matches(IMethodSymbol ctor, ISyntaxFactsService service)
            {
                if (ctor.Parameters.Length != ParameterTypes.Length)
                    return false;

                for (var i = 0; i < ParameterTypes.Length; i++)
                {
                    var ctorParameter = ctor.Parameters[i];
                    var result = SymbolEquivalenceComparer.Instance.Equals(ctorParameter.Type, ParameterTypes[i]) &&
                        ctorParameter.RefKind == _parameterRefKinds[i];

                    var parameterName = GetParameterName(service, i);
                    if (!string.IsNullOrEmpty(parameterName))
                    {
                        result &= service.IsCaseSensitive
                            ? ctorParameter.Name == parameterName
                            : string.Equals(ctorParameter.Name, parameterName, StringComparison.OrdinalIgnoreCase);
                    }

                    if (result == false)
                        return false;
                }

                return true;
            }

            private string GetParameterName(ISyntaxFactsService service, int index)
            {
                if (_arguments.IsDefault || index >= _arguments.Length)
                {
                    return string.Empty;
                }

                return service.GetNameForArgument(_arguments[index]);
            }

            internal ImmutableArray<ITypeSymbol> GetParameterTypes(CancellationToken cancellationToken)
            {
                var allTypeParameters = TypeToGenerateIn.GetAllTypeParameters();
                var semanticModel = _document.SemanticModel;
                var allTypes = _attributeArguments != null
                    ? _attributeArguments.Select(a => _service.GetAttributeArgumentType(semanticModel, a, cancellationToken))
                    : _arguments.Select(a => _service.GetArgumentType(semanticModel, a, cancellationToken));

                return allTypes.Select(t => FixType(t, semanticModel, allTypeParameters)).ToImmutableArray();
            }

            private static ITypeSymbol FixType(ITypeSymbol typeSymbol, SemanticModel semanticModel, IEnumerable<ITypeParameterSymbol> allTypeParameters)
            {
                var compilation = semanticModel.Compilation;
                return typeSymbol.RemoveAnonymousTypes(compilation)
                    .RemoveUnavailableTypeParameters(compilation, allTypeParameters)
                    .RemoveUnnamedErrorTypes(compilation);
            }

            private async Task<bool> TryInitializeConstructorInitializerGenerationAsync(
                SyntaxNode constructorInitializer, CancellationToken cancellationToken)
            {
                if (_service.TryInitializeConstructorInitializerGeneration(
                        _document, constructorInitializer, cancellationToken,
                        out var token, out var arguments, out var typeToGenerateIn))
                {
                    Token = token;
                    _arguments = arguments;
                    IsConstructorInitializerGeneration = true;

                    var semanticInfo = _document.SemanticModel.GetSymbolInfo(constructorInitializer, cancellationToken);
                    if (semanticInfo.Symbol == null)
                        return await TryDetermineTypeToGenerateInAsync(typeToGenerateIn, cancellationToken).ConfigureAwait(false);
                }

                return false;
            }

            private async Task<bool> TryInitializeSimpleNameGenerationAsync(
                SyntaxNode simpleName,
                CancellationToken cancellationToken)
            {
                if (_service.TryInitializeSimpleNameGenerationState(
                        _document, simpleName, cancellationToken,
                        out var token, out var arguments, out var typeToGenerateIn))
                {
                    Token = token;
                    _arguments = arguments;
                }
                else if (_service.TryInitializeSimpleAttributeNameGenerationState(
                    _document, simpleName, cancellationToken,
                    out token, out arguments, out var attributeArguments, out typeToGenerateIn))
                {
                    Token = token;
                    _attributeArguments = attributeArguments;
                    _arguments = arguments;

                    //// Attribute parameters are restricted to be constant values (simple types or string, etc).
                    if (_attributeArguments != null && GetParameterTypes(cancellationToken).Any(t => !IsValidAttributeParameterType(t)))
                    {
                        return false;
                    }
                    else if (GetParameterTypes(cancellationToken).Any(t => !IsValidAttributeParameterType(t)))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }

                cancellationToken.ThrowIfCancellationRequested();

                return await TryDetermineTypeToGenerateInAsync(typeToGenerateIn, cancellationToken).ConfigureAwait(false);
            }

            private static bool IsValidAttributeParameterType(ITypeSymbol type)
            {
                if (type.Kind == SymbolKind.ArrayType)
                {
                    var arrayType = (IArrayTypeSymbol)type;
                    if (arrayType.Rank != 1)
                    {
                        return false;
                    }

                    type = arrayType.ElementType;
                }

                if (type.IsEnumType())
                {
                    return true;
                }

                switch (type.SpecialType)
                {
                    case SpecialType.System_Boolean:
                    case SpecialType.System_Byte:
                    case SpecialType.System_Char:
                    case SpecialType.System_Int16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_Double:
                    case SpecialType.System_Single:
                    case SpecialType.System_String:
                        return true;

                    default:
                        return false;
                }
            }

            private async Task<bool> TryDetermineTypeToGenerateInAsync(
                INamedTypeSymbol original, CancellationToken cancellationToken)
            {
                var definition = await SymbolFinder.FindSourceDefinitionAsync(original, _document.Project.Solution, cancellationToken).ConfigureAwait(false);
                TypeToGenerateIn = definition as INamedTypeSymbol;

                return TypeToGenerateIn?.TypeKind == TypeKind.Class || TypeToGenerateIn?.TypeKind == TypeKind.Struct;
            }

            private void GetParameters(
                ImmutableArray<TArgumentSyntax> arguments,
                ImmutableArray<TAttributeArgumentSyntax>? attributeArguments,
                ImmutableArray<ITypeSymbol> parameterTypes,
                ImmutableArray<ParameterName> parameterNames,
                CancellationToken cancellationToken)
            {
                var parameterToExistingMemberMap = ImmutableDictionary.CreateBuilder<string, ISymbol>();
                var parameterToNewFieldMap = ImmutableDictionary.CreateBuilder<string, string>();
                var parameterToNewPropertyMap = ImmutableDictionary.CreateBuilder<string, string>();

                using var _ = ArrayBuilder<IParameterSymbol>.GetInstance(out var parameters);

                for (var i = 0; i < parameterNames.Length; i++)
                {
                    var parameterName = parameterNames[i];
                    var parameterType = parameterTypes[i];
                    var argument = arguments[i];
                    var attributeArgument = attributeArguments != null ? attributeArguments.Value[i] : null;

                    // See if there's a matching field or property we can use, or create a new member otherwise.
                    FindExistingOrCreateNewMember(
                        ref parameterName, parameterType, argument, attributeArgument,
                        parameterToExistingMemberMap, parameterToNewFieldMap, parameterToNewPropertyMap,
                        cancellationToken);

                    parameters.Add(CodeGenerationSymbolFactory.CreateParameterSymbol(
                        attributes: default,
                        refKind: _service.GetRefKind(argument),
                        isParams: false,
                        type: parameterType,
                        name: parameterName.BestNameForParameter));
                }

                _parameters = parameters.ToImmutable();
                _parameterToExistingMemberMap = parameterToExistingMemberMap.ToImmutable();
                ParameterToNewFieldMap = parameterToNewFieldMap.ToImmutable();
                ParameterToNewPropertyMap = parameterToNewPropertyMap.ToImmutable();
            }

            private void FindExistingOrCreateNewMember(
                ref ParameterName parameterName,
                ITypeSymbol parameterType,
                TArgumentSyntax argument,
                TAttributeArgumentSyntax attributeArgument,
                ImmutableDictionary<string, ISymbol>.Builder parameterToExistingMemberMap,
                ImmutableDictionary<string, string>.Builder parameterToNewFieldMap,
                ImmutableDictionary<string, string>.Builder parameterToNewPropertyMap,
                CancellationToken cancellationToken)
            {
                var expectedFieldName = _fieldNamingRule.NamingStyle.MakeCompliant(parameterName.NameBasedOnArgument).First();
                var expectedPropertyName = _propertyNamingRule.NamingStyle.MakeCompliant(parameterName.NameBasedOnArgument).First();
                var isFixed = _service.IsNamedArgument(argument);

                // For non-out parameters, see if there's already a field there with the same name.
                // If so, and it has a compatible type, then we can just assign to that field.
                // Otherwise, we'll need to choose a different name for this member so that it
                // doesn't conflict with something already in the type. First check the current type
                // for a matching field.  If so, defer to it.

                var unavailableMemberNames = GetUnavailableMemberNames().ToImmutableArray();

                var members = from t in TypeToGenerateIn.GetBaseTypesAndThis()
                              let ignoreAccessibility = t.Equals(TypeToGenerateIn)
                              from m in t.GetMembers()
                              where m.Name.Equals(expectedFieldName, StringComparison.OrdinalIgnoreCase)
                              where ignoreAccessibility || IsSymbolAccessible(m, _document)
                              select m;

                var membersArray = members.ToImmutableArray();
                var symbol = membersArray.FirstOrDefault(m => m.Name.Equals(expectedFieldName, StringComparison.Ordinal)) ?? membersArray.FirstOrDefault();
                if (symbol != null)
                {
                    if (IsViableFieldOrProperty(parameterType, symbol))
                    {
                        // Ok!  We can just the existing field.  
                        parameterToExistingMemberMap[parameterName.BestNameForParameter] = symbol;
                    }
                    else
                    {
                        // Uh-oh.  Now we have a problem.  We can't assign this parameter to
                        // this field.  So we need to create a new field.  Find a name not in
                        // use so we can assign to that.  
                        var baseName = attributeArgument != null
                            ? _service.GenerateNameForArgument(_document.SemanticModel, attributeArgument, cancellationToken)
                            : _service.GenerateNameForArgument(_document.SemanticModel, argument, cancellationToken);

                        var baseFieldWithNamingStyle = _fieldNamingRule.NamingStyle.MakeCompliant(baseName).First();
                        var basePropertyWithNamingStyle = _propertyNamingRule.NamingStyle.MakeCompliant(baseName).First();

                        var newFieldName = NameGenerator.EnsureUniqueness(baseFieldWithNamingStyle, unavailableMemberNames.Concat(parameterToNewFieldMap.Values));
                        var newPropertyName = NameGenerator.EnsureUniqueness(basePropertyWithNamingStyle, unavailableMemberNames.Concat(parameterToNewPropertyMap.Values));

                        if (isFixed)
                        {
                            // Can't change the parameter name, so map the existing parameter
                            // name to the new field name.
                            parameterToNewFieldMap[parameterName.NameBasedOnArgument] = newFieldName;
                            parameterToNewPropertyMap[parameterName.NameBasedOnArgument] = newPropertyName;
                        }
                        else
                        {
                            // Can change the parameter name, so do so.  
                            // But first remove any prefix added due to field naming styles
                            var fieldNameMinusPrefix = newFieldName.Substring(_fieldNamingRule.NamingStyle.Prefix.Length);
                            var newParameterName = new ParameterName(fieldNameMinusPrefix, isFixed: false, _parameterNamingRule);
                            parameterName = newParameterName;

                            parameterToNewFieldMap[newParameterName.BestNameForParameter] = newFieldName;
                            parameterToNewPropertyMap[newParameterName.BestNameForParameter] = newPropertyName;
                        }
                    }

                    return;
                }

                // If no matching field was found, use the fieldNamingRule to create suitable name
                var bestNameForParameter = parameterName.BestNameForParameter;
                var nameBasedOnArgument = parameterName.NameBasedOnArgument;
                parameterToNewFieldMap[bestNameForParameter] = _fieldNamingRule.NamingStyle.MakeCompliant(nameBasedOnArgument).First();
                parameterToNewPropertyMap[bestNameForParameter] = _propertyNamingRule.NamingStyle.MakeCompliant(nameBasedOnArgument).First();
            }

            private IEnumerable<string> GetUnavailableMemberNames()
            {
                return TypeToGenerateIn.MemberNames.Concat(
                    from type in TypeToGenerateIn.GetBaseTypes()
                    from member in type.GetMembers()
                    select member.Name);
            }

            private bool IsViableFieldOrProperty(
                ITypeSymbol parameterType,
                ISymbol symbol)
            {
                if (parameterType.Language != symbol.Language)
                    return false;

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

            public async Task<Document> GetChangedDocumentAsync(
                Document document, bool withFields, bool withProperties, CancellationToken cancellationToken)
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
                return await GenerateThisOrBaseDelegatingConstructorAsync(document, withFields, withProperties, cancellationToken).ConfigureAwait(false) ??
                       await GenerateMemberDelegatingConstructorAsync(document, withFields, withProperties, cancellationToken).ConfigureAwait(false);
            }

            private async Task<Document> GenerateThisOrBaseDelegatingConstructorAsync(
                Document document, bool withFields, bool withProperties, CancellationToken cancellationToken)
            {
                if (_delegatedConstructor == null)
                    return null;

                var provider = document.Project.Solution.Workspace.Services.GetLanguageServices(TypeToGenerateIn.Language);
                var (members, assignments) = await GenerateMembersAndAssignmentsAsync(document, withFields, withProperties, cancellationToken).ConfigureAwait(false);
                var isThis = _delegatedConstructor.ContainingType.OriginalDefinition.Equals(TypeToGenerateIn.OriginalDefinition);
                var delegatingArguments = provider.GetService<SyntaxGenerator>().CreateArguments(_delegatedConstructor.Parameters);

                var newParameters = _delegatedConstructor.Parameters.Concat(_parameters);
                var generateUnsafe = !IsContainedInUnsafeType && newParameters.Any(p => p.RequiresUnsafeModifier());

                var constructor = CodeGenerationSymbolFactory.CreateConstructorSymbol(
                    attributes: default,
                    accessibility: Accessibility.Public,
                    modifiers: new DeclarationModifiers(isUnsafe: generateUnsafe),
                    typeName: TypeToGenerateIn.Name,
                    parameters: newParameters,
                    statements: assignments,
                    baseConstructorArguments: isThis ? default : delegatingArguments,
                    thisConstructorArguments: isThis ? delegatingArguments : default);

                return await provider.GetService<ICodeGenerationService>().AddMembersAsync(
                    document.Project.Solution,
                    TypeToGenerateIn,
                    members.Concat(constructor),
                    new CodeGenerationOptions(
                        Token.GetLocation(),
                        options: await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false)),
                    cancellationToken).ConfigureAwait(false);
            }

            private async Task<(ImmutableArray<ISymbol>, ImmutableArray<SyntaxNode>)> GenerateMembersAndAssignmentsAsync(
                Document document, bool withFields, bool withProperties, CancellationToken cancellationToken)
            {
                var provider = document.Project.Solution.Workspace.Services.GetLanguageServices(TypeToGenerateIn.Language);

                var members = withFields ? SyntaxGeneratorExtensions.CreateFieldsForParameters(_parameters, ParameterToNewFieldMap, IsContainedInUnsafeType) :
                              withProperties ? SyntaxGeneratorExtensions.CreatePropertiesForParameters(_parameters, ParameterToNewPropertyMap, IsContainedInUnsafeType) :
                              ImmutableArray<ISymbol>.Empty;

                var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var assignments = !withFields && !withProperties
                    ? ImmutableArray<SyntaxNode>.Empty
                    : provider.GetService<SyntaxGenerator>().CreateAssignmentStatements(
                        semanticModel, _parameters,
                        _parameterToExistingMemberMap,
                        withFields ? ParameterToNewFieldMap : ParameterToNewPropertyMap,
                        addNullChecks: false, preferThrowExpression: false);

                return (members, assignments);
            }

            private async Task<Document> GenerateMemberDelegatingConstructorAsync(
                Document document, bool withFields, bool withProperties, CancellationToken cancellationToken)
            {
                var provider = document.Project.Solution.Workspace.Services.GetLanguageServices(TypeToGenerateIn.Language);
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                var newMemberMap =
                    withFields ? ParameterToNewFieldMap :
                    withProperties ? ParameterToNewPropertyMap :
                    ImmutableDictionary<string, string>.Empty;

                return await provider.GetService<ICodeGenerationService>().AddMembersAsync(
                    document.Project.Solution,
                    TypeToGenerateIn,
                    provider.GetService<SyntaxGenerator>().CreateMemberDelegatingConstructor(
                        semanticModel,
                        TypeToGenerateIn.Name,
                        TypeToGenerateIn,
                        _parameters,
                        _parameterToExistingMemberMap,
                        newMemberMap,
                        addNullChecks: false,
                        preferThrowExpression: false,
                        generateProperties: withProperties,
                        IsContainedInUnsafeType),
                    new CodeGenerationOptions(
                        Token.GetLocation(),
                        options: await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false)),
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
