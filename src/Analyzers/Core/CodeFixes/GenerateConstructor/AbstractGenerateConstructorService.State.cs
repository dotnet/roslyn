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
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Utilities;

#if CODE_STYLE
using DeclarationModifiers = Microsoft.CodeAnalysis.Internal.Editing.DeclarationModifiers;
#else
using DeclarationModifiers = Microsoft.CodeAnalysis.Editing.DeclarationModifiers;
#endif

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor;

using static GenerateConstructorHelpers;

internal abstract partial class AbstractGenerateConstructorService<TService, TExpressionSyntax>
{
    protected internal sealed class State
    {
        private readonly TService _service;
        private readonly SemanticDocument _document;

        private readonly NamingRule _parameterNamingRule;

        private ImmutableArray<Argument<TExpressionSyntax>> _arguments;

        // The type we're creating a constructor for.  Will be a class or struct type.
        public INamedTypeSymbol? TypeToGenerateIn { get; private set; }

        private ImmutableArray<RefKind> _parameterRefKinds;
        public ImmutableArray<ITypeSymbol> ParameterTypes;

        public SyntaxToken Token { get; private set; }
        public bool IsConstructorInitializerGeneration { get; private set; }

        private IMethodSymbol? _delegatedConstructor;

        private ImmutableArray<IParameterSymbol> _parameters;
        private ImmutableDictionary<string, ISymbol>? _parameterToExistingMemberMap;

        public ImmutableDictionary<string, string> ParameterToNewFieldMap { get; private set; }
        public ImmutableDictionary<string, string> ParameterToNewPropertyMap { get; private set; }
        public bool IsContainedInUnsafeType { get; private set; }

        private State(TService service, SemanticDocument document, NamingRule parameterNamingRule)
        {
            _service = service;
            _document = document;
            _parameterNamingRule = parameterNamingRule;

            ParameterToNewFieldMap = ImmutableDictionary<string, string>.Empty;
            ParameterToNewPropertyMap = ImmutableDictionary<string, string>.Empty;
        }

        public static async Task<State?> GenerateAsync(
            TService service,
            SemanticDocument document,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            var parameterNamingRule = await document.Document.GetApplicableNamingRuleAsync(SymbolKind.Parameter, Accessibility.NotApplicable, cancellationToken).ConfigureAwait(false);

            var state = new State(service, document, parameterNamingRule);
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
            else if (_service.IsImplicitObjectCreation(_document, node, cancellationToken))
            {
                if (!await TryInitializeImplicitObjectCreationAsync(node, cancellationToken).ConfigureAwait(false))
                    return false;
            }
            else
            {
                return false;
            }

            Contract.ThrowIfNull(TypeToGenerateIn);
            if (!CodeGenerator.CanAdd(_document.Project.Solution, TypeToGenerateIn, cancellationToken))
                return false;

            ParameterTypes = ParameterTypes.IsDefault ? GetParameterTypes(cancellationToken) : ParameterTypes;
            _parameterRefKinds = _arguments.SelectAsArray(a => a.RefKind);

            if (ClashesWithExistingConstructor())
                return false;

            if (!await TryInitializeDelegatedConstructorAsync(cancellationToken).ConfigureAwait(false))
                await InitializeNonDelegatedConstructorAsync(cancellationToken).ConfigureAwait(false);

            IsContainedInUnsafeType = _service.ContainingTypesOrSelfHasUnsafeKeyword(TypeToGenerateIn);

            return true;
        }

        private async Task InitializeNonDelegatedConstructorAsync(CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(TypeToGenerateIn);
            var typeParametersNames = TypeToGenerateIn.GetAllTypeParameters().Select(t => t.Name).ToImmutableArray();
            var parameterNames = GetParameterNames(_arguments, typeParametersNames, cancellationToken);

            (_parameters, _parameterToExistingMemberMap, ParameterToNewFieldMap, ParameterToNewPropertyMap) =
                await GetParametersAsync(
                    _document, this.TypeToGenerateIn, _arguments, ParameterTypes, parameterNames, cancellationToken).ConfigureAwait(false);
        }

        private ImmutableArray<ParameterName> GetParameterNames(
            ImmutableArray<Argument<TExpressionSyntax>> arguments, ImmutableArray<string> typeParametersNames, CancellationToken cancellationToken)
        {
            return _service.GenerateParameterNames(_document, arguments, typeParametersNames, _parameterNamingRule, cancellationToken);
        }

        private async Task<bool> TryInitializeDelegatedConstructorAsync(CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(TypeToGenerateIn);

            var parameters = ParameterTypes.Zip(_parameterRefKinds,
                (t, r) => CodeGenerationSymbolFactory.CreateParameterSymbol(r, t, name: "")).ToImmutableArray();
            var expressions = _arguments.SelectAsArray(a => a.Expression);
            var delegatedConstructor = FindConstructorToDelegateTo(parameters, expressions, cancellationToken);
            if (delegatedConstructor == null)
                return false;

            // Map the first N parameters to the other constructor in this type.  Then
            // try to map any further parameters to existing fields.  Finally, generate
            // new fields if no such parameters exist.

            // Find the names of the parameters that will follow the parameters we're
            // delegating.
            var argumentCount = delegatedConstructor.Parameters.Length;
            var remainingArguments = _arguments.Skip(argumentCount).ToImmutableArray();
            var remainingParameterNames = _service.GenerateParameterNames(
                _document, remainingArguments,
                delegatedConstructor.Parameters.Select(p => p.Name).ToList(),
                _parameterNamingRule,
                cancellationToken);

            // Can't generate the constructor if the parameter names we're copying over forcibly
            // conflict with any names we generated.
            if (delegatedConstructor.Parameters.Select(p => p.Name).Intersect(remainingParameterNames.Select(n => n.BestNameForParameter)).Any())
                return false;

            var remainingParameterTypes = ParameterTypes.Skip(argumentCount).ToImmutableArray();

            _delegatedConstructor = delegatedConstructor;
            (_parameters, _parameterToExistingMemberMap, ParameterToNewFieldMap, ParameterToNewPropertyMap) =
                await GetParametersAsync(
                    _document, this.TypeToGenerateIn, remainingArguments, remainingParameterTypes, remainingParameterNames, cancellationToken).ConfigureAwait(false);

            return true;
        }

        private IMethodSymbol? FindConstructorToDelegateTo(
            ImmutableArray<IParameterSymbol> allParameters,
            ImmutableArray<TExpressionSyntax?> allExpressions,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(TypeToGenerateIn);
            Contract.ThrowIfNull(TypeToGenerateIn.BaseType);

            for (var i = allParameters.Length; i > 0; i--)
            {
                var parameters = allParameters.TakeAsArray(i);
                var expressions = allExpressions.TakeAsArray(i);
                var result = FindConstructorToDelegateTo(parameters, expressions, TypeToGenerateIn.InstanceConstructors, cancellationToken) ??
                             FindConstructorToDelegateTo(parameters, expressions, TypeToGenerateIn.BaseType.InstanceConstructors, cancellationToken);
                if (result != null)
                    return result;
            }

            return null;
        }

        private IMethodSymbol? FindConstructorToDelegateTo(
            ImmutableArray<IParameterSymbol> parameters,
            ImmutableArray<TExpressionSyntax?> expressions,
            ImmutableArray<IMethodSymbol> constructors,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(TypeToGenerateIn);

            foreach (var constructor in constructors)
            {
                // Don't bother delegating to an implicit constructor. We don't want to add `: base()` as that's just
                // redundant for subclasses and `: this()` won't even work as we won't have an implicit constructor once
                // we add this new constructor.
                if (constructor.IsImplicitlyDeclared)
                    continue;

                // Don't delegate to another constructor in this type if it's got the same parameter types as the
                // one we're generating. This can happen if we're generating the new constructor because parameter
                // names don't match (when a user explicitly provides named parameters).
                if (TypeToGenerateIn.Equals(constructor.ContainingType) &&
                    constructor.Parameters.Select(p => p.Type).SequenceEqual(ParameterTypes))
                {
                    continue;
                }

                if (GenerateConstructorHelpers.CanDelegateTo(_document, parameters, expressions, constructor) &&
                    !_service.WillCauseConstructorCycle(this, _document, constructor, cancellationToken))
                {
                    return constructor;
                }
            }

            return null;
        }

        private TLanguageService GetRequiredLanguageService<TLanguageService>(string language) where TLanguageService : ILanguageService
            => _document.Project.Solution.Workspace.Services.GetExtendedLanguageServices(language).GetRequiredService<TLanguageService>();

        private bool ClashesWithExistingConstructor()
        {
            Contract.ThrowIfNull(TypeToGenerateIn);

            var syntaxFacts = GetRequiredLanguageService<ISyntaxFactsService>(TypeToGenerateIn.Language);
            return TypeToGenerateIn.InstanceConstructors.Any(static (c, arg) => arg.self.Matches(c, arg.syntaxFacts), (self: this, syntaxFacts));
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

                var parameterName = GetParameterName(i);
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

        private string GetParameterName(int index)
            => _arguments.IsDefault || index >= _arguments.Length ? string.Empty : _arguments[index].Name;

        internal ImmutableArray<ITypeSymbol> GetParameterTypes(CancellationToken cancellationToken)
        {
            var allTypeParameters = TypeToGenerateIn.GetAllTypeParameters();
            var semanticModel = _document.SemanticModel;
            var allTypes = _arguments.Select(a => _service.GetArgumentType(_document.SemanticModel, a, cancellationToken));

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

        private async Task<bool> TryInitializeImplicitObjectCreationAsync(SyntaxNode implicitObjectCreation, CancellationToken cancellationToken)
        {
            if (_service.TryInitializeImplicitObjectCreation(
                    _document, implicitObjectCreation, cancellationToken,
                    out var token, out var arguments, out var typeToGenerateIn))
            {
                Token = token;
                _arguments = arguments;

                var semanticInfo = _document.SemanticModel.GetSymbolInfo(implicitObjectCreation, cancellationToken);
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
                _document, simpleName, cancellationToken, out token, out arguments, out typeToGenerateIn))
            {
                Token = token;
                _arguments = arguments;
                //// Attribute parameters are restricted to be constant values (simple types or string, etc).
                if (GetParameterTypes(cancellationToken).Any(static t => !IsValidAttributeParameterType(t)))
                    return false;
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

            return TypeToGenerateIn?.TypeKind is (TypeKind?)TypeKind.Class or (TypeKind?)TypeKind.Struct;
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

        private async Task<Document?> GenerateThisOrBaseDelegatingConstructorAsync(
            Document document, bool withFields, bool withProperties, CancellationToken cancellationToken)
        {
            if (_delegatedConstructor == null)
                return null;

            Contract.ThrowIfNull(TypeToGenerateIn);

            var (members, assignments) = await GenerateMembersAndAssignmentsAsync(document, withFields, withProperties, cancellationToken).ConfigureAwait(false);
            var isThis = _delegatedConstructor.ContainingType.OriginalDefinition.Equals(TypeToGenerateIn.OriginalDefinition);
            var delegatingArguments = this.GetRequiredLanguageService<SyntaxGenerator>(TypeToGenerateIn.Language).CreateArguments(_delegatedConstructor.Parameters);

            var newParameters = _delegatedConstructor.Parameters.Concat(_parameters);
            var generateUnsafe = !IsContainedInUnsafeType && newParameters.Any(static p => p.RequiresUnsafeModifier());

            var constructor = CodeGenerationSymbolFactory.CreateConstructorSymbol(
                attributes: default,
                accessibility: Accessibility.Public,
                modifiers: new DeclarationModifiers(isUnsafe: generateUnsafe),
                typeName: TypeToGenerateIn.Name,
                parameters: newParameters,
                statements: assignments,
                baseConstructorArguments: isThis ? default : delegatingArguments,
                thisConstructorArguments: isThis ? delegatingArguments : default);

            var context = new CodeGenerationSolutionContext(
                document.Project.Solution,
                new CodeGenerationContext(Token.GetLocation()));

            return await CodeGenerator.AddMemberDeclarationsAsync(
                context,
                TypeToGenerateIn,
                members.Concat(constructor),
                cancellationToken).ConfigureAwait(false);
        }

        private async Task<(ImmutableArray<ISymbol>, ImmutableArray<SyntaxNode>)> GenerateMembersAndAssignmentsAsync(
            Document document, bool withFields, bool withProperties, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(TypeToGenerateIn);

            var members = withFields ? SyntaxGeneratorExtensions.CreateFieldsForParameters(_parameters, ParameterToNewFieldMap, IsContainedInUnsafeType) :
                          withProperties ? SyntaxGeneratorExtensions.CreatePropertiesForParameters(_parameters, ParameterToNewPropertyMap, IsContainedInUnsafeType) :
                          [];

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var assignments = !withFields && !withProperties
                ? []
                : GetRequiredLanguageService<SyntaxGenerator>(TypeToGenerateIn.Language).CreateAssignmentStatements(
                    GetRequiredLanguageService<SyntaxGeneratorInternal>(TypeToGenerateIn.Language),
                    semanticModel, _parameters,
                    _parameterToExistingMemberMap,
                    withFields ? ParameterToNewFieldMap : ParameterToNewPropertyMap,
                    addNullChecks: false, preferThrowExpression: false);

            return (members, assignments);
        }

        private async Task<Document> GenerateMemberDelegatingConstructorAsync(
            Document document, bool withFields, bool withProperties, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(TypeToGenerateIn);

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var newMemberMap =
                withFields ? ParameterToNewFieldMap :
                withProperties ? ParameterToNewPropertyMap :
                ImmutableDictionary<string, string>.Empty;

            return await CodeGenerator.AddMemberDeclarationsAsync(
                new CodeGenerationSolutionContext(
                    document.Project.Solution,
                    new CodeGenerationContext(Token.GetLocation())),
                TypeToGenerateIn,
                GetRequiredLanguageService<SyntaxGenerator>(TypeToGenerateIn.Language).CreateMemberDelegatingConstructor(
                    GetRequiredLanguageService<SyntaxGeneratorInternal>(TypeToGenerateIn.Language),
                    semanticModel,
                    TypeToGenerateIn.Name,
                    TypeToGenerateIn,
                    _parameters,
                    TypeToGenerateIn.IsAbstractClass() ? Accessibility.Protected : Accessibility.Public,
                    _parameterToExistingMemberMap,
                    newMemberMap,
                    addNullChecks: false,
                    preferThrowExpression: false,
                    generateProperties: withProperties,
                    IsContainedInUnsafeType),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
