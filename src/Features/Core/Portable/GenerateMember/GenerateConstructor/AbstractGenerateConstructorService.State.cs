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
            public readonly TService Service;
            public readonly SemanticDocument Document;

            public NamingRule FieldNamingRule { get; private set; }
            public NamingRule PropertyNamingRule { get; private set; }
            public NamingRule ParameterNamingRule { get; private set; }

            public ImmutableArray<TArgumentSyntax> Arguments { get; private set; }

            public ImmutableArray<TAttributeArgumentSyntax> AttributeArguments { get; private set; }

            // The type we're creating a constructor for.  Will be a class or struct type.
            public INamedTypeSymbol TypeToGenerateIn { get; private set; }

            public IList<RefKind> ParameterRefKinds { get; private set; }
            public ImmutableArray<ITypeSymbol> ParameterTypes { get; private set; }

            public SyntaxToken Token { get; private set; }

            public bool IsConstructorInitializerGeneration { get; private set; }

            public IMethodSymbol DelegatedConstructor { get; private set; }

            public Dictionary<string, ISymbol> ParameterToExistingMemberMap { get; private set; }
            public Dictionary<string, string> ParameterToNewFieldMap { get; private set; }
            public Dictionary<string, string> ParameterToNewPropertyMap { get; private set; }
            public ImmutableArray<IParameterSymbol> RemainingParameters { get; private set; }

            public bool AddingMembers => ParameterToNewFieldMap.Count > 0 || ParameterToNewPropertyMap.Count > 0;

            private State(TService service, SemanticDocument document)
            {
                this.Service = service;
                this.Document = document;
            }

            public static async Task<State> GenerateAsync(
                TService service,
                SemanticDocument document,
                SyntaxNode node,
                CancellationToken cancellationToken)
            {
                var state = new State(service, document);
                if (!await state.TryInitializeAsync(node, cancellationToken).ConfigureAwait(false))
                {
                    return null;
                }

                return state;
            }

            private async Task<bool> TryInitializeAsync(
                SyntaxNode node,
                CancellationToken cancellationToken)
            {
                this.FieldNamingRule = await Document.Document.GetApplicableNamingRuleAsync(SymbolKind.Field, Accessibility.Private, cancellationToken).ConfigureAwait(false);
                this.PropertyNamingRule = await Document.Document.GetApplicableNamingRuleAsync(SymbolKind.Property, Accessibility.Public, cancellationToken).ConfigureAwait(false);
                this.ParameterNamingRule = await Document.Document.GetApplicableNamingRuleAsync(SymbolKind.Parameter, Accessibility.NotApplicable, cancellationToken).ConfigureAwait(false);

                if (Service.IsConstructorInitializerGeneration(Document, node, cancellationToken))
                {
                    if (!await TryInitializeConstructorInitializerGenerationAsync(node, cancellationToken).ConfigureAwait(false))
                        return false;
                }
                else if (Service.IsSimpleNameGeneration(Document, node, cancellationToken))
                {
                    if (!await TryInitializeSimpleNameGenerationAsync(node, cancellationToken).ConfigureAwait(false))
                        return false;
                }
                else
                {
                    return false;
                }

                if (!CodeGenerator.CanAdd(Document.Project.Solution, TypeToGenerateIn, cancellationToken))
                    return false;

                ParameterTypes = ParameterTypes.IsDefault
                    ? GetParameterTypes(cancellationToken)
                    : ParameterTypes;
                ParameterRefKinds ??= Arguments.Select(Service.GetRefKind).ToList();

                if (ClashesWithExistingConstructor())
                    return false;

                if (!this.TryInitializeDelegatedConstructor(cancellationToken))
                    this.InitializeNonDelegatedConstructor(cancellationToken);

                return true;
            }

            private void InitializeNonDelegatedConstructor(CancellationToken cancellationToken)
            {
                var arguments = this.Arguments;
                var parameterTypes = this.ParameterTypes;

                var typeParametersNames = this.TypeToGenerateIn.GetAllTypeParameters().Select(t => t.Name).ToImmutableArray();
                var parameterNames = GetParameterNames(arguments, typeParametersNames, cancellationToken);

                GetParameters(
                    arguments, this.AttributeArguments,
                    parameterTypes, parameterNames,
                    cancellationToken);
            }

            private ImmutableArray<ParameterName> GetParameterNames(
                ImmutableArray<TArgumentSyntax> arguments, ImmutableArray<string> typeParametersNames, CancellationToken cancellationToken)
            {
                return this.AttributeArguments != null
                    ? Service.GenerateParameterNames(Document.SemanticModel, this.AttributeArguments, typeParametersNames, ParameterNamingRule, cancellationToken)
                    : Service.GenerateParameterNames(Document.SemanticModel, arguments, typeParametersNames, ParameterNamingRule, cancellationToken);
            }

            private bool TryInitializeDelegatedConstructor(CancellationToken cancellationToken)
            {
                // We don't have to deal with the zero length case, since there's nothing to
                // delegate.  It will fall out of the GenerateFieldDelegatingConstructor above.
                for (var i = this.Arguments.Length; i >= 1; i--)
                {
                    if (InitializeDelegatedConstructor(i, cancellationToken))
                        return true;
                }

                return false;
            }

            private bool InitializeDelegatedConstructor(int argumentCount, CancellationToken cancellationToken)
                => InitializeDelegatedConstructor(argumentCount, this.TypeToGenerateIn, cancellationToken) ||
                   InitializeDelegatedConstructor(argumentCount, this.TypeToGenerateIn.BaseType, cancellationToken);

            private bool InitializeDelegatedConstructor(int argumentCount, INamedTypeSymbol namedType, CancellationToken cancellationToken)
            {
                // We can't resolve overloads across language.
                if (Document.Project.Language != namedType.Language)
                    return false;

                var arguments = this.Arguments.Take(argumentCount).ToList();
                var remainingArguments = this.Arguments.Skip(argumentCount).ToImmutableArray();
                var remainingAttributeArguments = this.AttributeArguments != null
                    ? this.AttributeArguments.Skip(argumentCount).ToImmutableArray()
                    : (ImmutableArray<TAttributeArgumentSyntax>?)null;
                var remainingParameterTypes = this.ParameterTypes.Skip(argumentCount).ToImmutableArray();

                var instanceConstructors = namedType.InstanceConstructors.Where(c => IsSymbolAccessible(c, Document)).ToSet();
                if (instanceConstructors.IsEmpty())
                    return false;

                var delegatedConstructor = Service.GetDelegatingConstructor(this, Document, argumentCount, namedType, instanceConstructors, cancellationToken);

                // There was a best match.  Call it directly.  
                var provider = Document.Project.Solution.Workspace.Services.GetLanguageServices(this.TypeToGenerateIn.Language);

                // Map the first N parameters to the other constructor in this type.  Then
                // try to map any further parameters to existing fields.  Finally, generate
                // new fields if no such parameters exist.

                // Find the names of the parameters that will follow the parameters we're
                // delegating.
                var remainingParameterNames = Service.GenerateParameterNames(
                    Document.SemanticModel, remainingArguments,
                    delegatedConstructor.Parameters.Select(p => p.Name).ToList(),
                    this.ParameterNamingRule,
                    cancellationToken);

                // Can't generate the constructor if the parameter names we're copying over forcibly
                // conflict with any names we generated.
                if (delegatedConstructor.Parameters.Select(p => p.Name)
                        .Intersect(remainingParameterNames.Select(n => n.BestNameForParameter)).Any())
                {
                    return false;
                }

                this.DelegatedConstructor = delegatedConstructor;
                // Try to map those parameters to fields.
                GetParameters(
                    remainingArguments, remainingAttributeArguments,
                    remainingParameterTypes, remainingParameterNames,
                    cancellationToken);
                return true;
            }

            private bool ClashesWithExistingConstructor()
            {
                var destinationProvider = Document.Project.Solution.Workspace.Services.GetLanguageServices(TypeToGenerateIn.Language);
                var syntaxFacts = destinationProvider.GetService<ISyntaxFactsService>();
                return TypeToGenerateIn.InstanceConstructors.Any(c => Matches(c, syntaxFacts));
            }

            private bool Matches(IMethodSymbol ctor, ISyntaxFactsService service)
            {
                if (ctor.Parameters.Length != ParameterTypes.Length)
                {
                    return false;
                }

                for (var i = 0; i < ParameterTypes.Length; i++)
                {
                    var ctorParameter = ctor.Parameters[i];
                    var result = SymbolEquivalenceComparer.Instance.Equals(ctorParameter.Type, ParameterTypes[i]) &&
                        ctorParameter.RefKind == ParameterRefKinds[i];

                    var parameterName = GetParameterName(service, i);
                    if (!string.IsNullOrEmpty(parameterName))
                    {
                        result &= service.IsCaseSensitive
                            ? ctorParameter.Name == parameterName
                            : string.Equals(ctorParameter.Name, parameterName, StringComparison.OrdinalIgnoreCase);
                    }

                    if (result == false)
                    {
                        return false;
                    }
                }

                return true;
            }

            private string GetParameterName(ISyntaxFactsService service, int index)
            {
                if (Arguments.IsDefault || index >= Arguments.Length)
                {
                    return string.Empty;
                }

                return service.GetNameForArgument(Arguments[index]);
            }

            internal ImmutableArray<ITypeSymbol> GetParameterTypes(CancellationToken cancellationToken)
            {
                var allTypeParameters = TypeToGenerateIn.GetAllTypeParameters();
                var semanticModel = Document.SemanticModel;
                var allTypes = AttributeArguments != null
                    ? AttributeArguments.Select(a => Service.GetAttributeArgumentType(semanticModel, a, cancellationToken))
                    : Arguments.Select(a => Service.GetArgumentType(semanticModel, a, cancellationToken));

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
                SyntaxNode constructorInitializer,
                CancellationToken cancellationToken)
            {
                if (!Service.TryInitializeConstructorInitializerGeneration(Document, constructorInitializer, cancellationToken,
                    out var token, out var arguments, out var typeToGenerateIn))
                {
                    return false;
                }

                Token = token;
                Arguments = arguments;
                IsConstructorInitializerGeneration = true;

                var semanticModel = Document.SemanticModel;
                var semanticInfo = semanticModel.GetSymbolInfo(constructorInitializer, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                if (semanticInfo.Symbol != null)
                {
                    return false;
                }

                return await TryDetermineTypeToGenerateInAsync(typeToGenerateIn, cancellationToken).ConfigureAwait(false);
            }

            private async Task<bool> TryInitializeSimpleNameGenerationAsync(
                SyntaxNode simpleName,
                CancellationToken cancellationToken)
            {
                if (Service.TryInitializeSimpleNameGenerationState(
                        Document, simpleName, cancellationToken,
                        out var token, out var arguments, out var typeToGenerateIn))
                {
                    Token = token;
                    Arguments = arguments;
                }
                else if (Service.TryInitializeSimpleAttributeNameGenerationState(
                    Document, simpleName, cancellationToken,
                    out token, out arguments, out var attributeArguments, out typeToGenerateIn))
                {
                    Token = token;
                    AttributeArguments = attributeArguments;
                    Arguments = arguments;

                    //// Attribute parameters are restricted to be constant values (simple types or string, etc).
                    if (AttributeArguments != null && GetParameterTypes(cancellationToken).Any(t => !IsValidAttributeParameterType(t)))
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
                var definition = await SymbolFinder.FindSourceDefinitionAsync(original, Document.Project.Solution, cancellationToken).ConfigureAwait(false);
                TypeToGenerateIn = definition as INamedTypeSymbol;

                return TypeToGenerateIn != null &&
                    (TypeToGenerateIn.TypeKind == TypeKind.Class ||
                     TypeToGenerateIn.TypeKind == TypeKind.Struct);
            }

            private void GetParameters(
                ImmutableArray<TArgumentSyntax> arguments,
                ImmutableArray<TAttributeArgumentSyntax>? attributeArguments,
                ImmutableArray<ITypeSymbol> parameterTypes,
                ImmutableArray<ParameterName> parameterNames,
                CancellationToken cancellationToken)
            {
                var parameterToExistingMemberMap = new Dictionary<string, ISymbol>();
                var parameterToNewFieldMap = new Dictionary<string, string>();
                var parameterToNewPropertyMap = new Dictionary<string, string>();

                using var _ = ArrayBuilder<IParameterSymbol>.GetInstance(out var result);

                for (var i = 0; i < parameterNames.Length; i++)
                {
                    // See if there's a matching field or property we can use.  First test in a case sensitive
                    // manner, then case insensitively.
                    if (!TryFindMatchingFieldOrProperty(
                            arguments, attributeArguments, parameterNames, parameterTypes, i,
                            parameterToExistingMemberMap, parameterToNewFieldMap, parameterToNewPropertyMap,
                            caseSensitive: true, newParameterNames: out parameterNames, cancellationToken) &&
                       !TryFindMatchingFieldOrProperty(
                           arguments, attributeArguments, parameterNames, parameterTypes, i,
                            parameterToExistingMemberMap, parameterToNewFieldMap, parameterToNewPropertyMap,
                            caseSensitive: false, newParameterNames: out parameterNames, cancellationToken))
                    {
                        // If no matching field was found, use the fieldNamingRule to create suitable name
                        var bestNameForParameter = parameterNames[i].BestNameForParameter;
                        var nameBasedOnArgument = parameterNames[i].NameBasedOnArgument;
                        parameterToNewFieldMap[bestNameForParameter] = FieldNamingRule.NamingStyle.MakeCompliant(nameBasedOnArgument).First();
                        parameterToNewPropertyMap[bestNameForParameter] = PropertyNamingRule.NamingStyle.MakeCompliant(nameBasedOnArgument).First();
                    }

                    result.Add(CodeGenerationSymbolFactory.CreateParameterSymbol(
                        attributes: default,
                        refKind: Service.GetRefKind(arguments[i]),
                        isParams: false,
                        type: parameterTypes[i],
                        name: parameterNames[i].BestNameForParameter));
                }

                this.ParameterToExistingMemberMap = parameterToExistingMemberMap;
                this.ParameterToNewFieldMap = parameterToNewFieldMap;
                this.ParameterToNewPropertyMap = parameterToNewPropertyMap;
                this.RemainingParameters = result.ToImmutable();
            }

            private bool TryFindMatchingFieldOrProperty(
                ImmutableArray<TArgumentSyntax> arguments,
                ImmutableArray<TAttributeArgumentSyntax>? attributeArguments,
                ImmutableArray<ParameterName> parameterNames,
                ImmutableArray<ITypeSymbol> parameterTypes,
                int index,
                Dictionary<string, ISymbol> parameterToExistingMemberMap,
                Dictionary<string, string> parameterToNewFieldMap,
                Dictionary<string, string> parameterToNewPropertyMap,
                bool caseSensitive,
                out ImmutableArray<ParameterName> newParameterNames,
                CancellationToken cancellationToken)
            {
                var parameterName = parameterNames[index];
                var parameterType = parameterTypes[index];
                var expectedFieldName = FieldNamingRule.NamingStyle.MakeCompliant(parameterName.NameBasedOnArgument).First();
                var expectedPropertyName = PropertyNamingRule.NamingStyle.MakeCompliant(parameterName.NameBasedOnArgument).First();
                var isFixed = Service.IsNamedArgument(arguments[index]);
                var newParameterNamesList = parameterNames.ToList();

                // For non-out parameters, see if there's already a field there with the same name.
                // If so, and it has a compatible type, then we can just assign to that field.
                // Otherwise, we'll need to choose a different name for this member so that it
                // doesn't conflict with something already in the type. First check the current type
                // for a matching field.  If so, defer to it.
                var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                var unavailableMemberNames = GetUnavailableMemberNames().ToImmutableArray();

                foreach (var type in this.TypeToGenerateIn.GetBaseTypesAndThis())
                {
                    var ignoreAccessibility = type.Equals(this.TypeToGenerateIn);
                    var symbol = type.GetMembers().FirstOrDefault(s => s.Name.Equals(expectedFieldName, comparison));

                    if (symbol != null)
                    {
                        if (ignoreAccessibility || IsSymbolAccessible(symbol, Document))
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
                                var baseName = attributeArguments != null
                                    ? Service.GenerateNameForArgument(Document.SemanticModel, attributeArguments.Value[index], cancellationToken)
                                    : Service.GenerateNameForArgument(Document.SemanticModel, arguments[index], cancellationToken);

                                var baseFieldWithNamingStyle = FieldNamingRule.NamingStyle.MakeCompliant(baseName).First();
                                var basePropertyWithNamingStyle = PropertyNamingRule.NamingStyle.MakeCompliant(baseName).First();

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
                                    var fieldNameMinusPrefix = newFieldName.Substring(FieldNamingRule.NamingStyle.Prefix.Length);
                                    var newParameterName = new ParameterName(fieldNameMinusPrefix, isFixed: false, ParameterNamingRule);
                                    newParameterNamesList[index] = newParameterName;

                                    parameterToNewFieldMap[newParameterName.BestNameForParameter] = newFieldName;
                                    parameterToNewPropertyMap[newParameterName.BestNameForParameter] = newPropertyName;
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
                return this.TypeToGenerateIn.MemberNames.Concat(
                    from type in this.TypeToGenerateIn.GetBaseTypes()
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
                            Service.IsConversionImplicit(Document.SemanticModel.Compilation, parameterType, field.Type);
                    }
                    else if (symbol is IPropertySymbol property)
                    {
                        return
                            property.Parameters.Length == 0 &&
                            property.IsWritableInConstructor() &&
                            Service.IsConversionImplicit(Document.SemanticModel.Compilation, parameterType, property.Type);
                    }
                }

                return false;
            }
        }
    }
}
