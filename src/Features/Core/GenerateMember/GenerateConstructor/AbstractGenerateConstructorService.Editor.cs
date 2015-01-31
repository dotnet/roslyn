// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor
{
    internal abstract partial class AbstractGenerateConstructorService<TService, TArgumentSyntax, TAttributeArgumentSyntax>
    {
        protected abstract bool IsConversionImplicit(Compilation compilation, ITypeSymbol sourceType, ITypeSymbol targetType);

        internal abstract IMethodSymbol GetDelegatingConstructor(State state, SemanticDocument document, int argumentCount, INamedTypeSymbol namedType, ISet<IMethodSymbol> candidates, CancellationToken cancellationToken);

        private partial class Editor
        {
            private readonly TService service;
            private readonly SemanticDocument document;
            private readonly State state;
            private readonly CancellationToken cancellationToken;

            public Editor(
                TService service,
                SemanticDocument document,
                State state,
                CancellationToken cancellationToken)
            {
                this.service = service;
                this.document = document;
                this.state = state;
                this.cancellationToken = cancellationToken;
            }

            internal async Task<Document> GetEditAsync()
            {
                // First, see if there's an accessible base constructor that would accept these
                // types, then just call into that instead of generating fields.
                //
                // then, see if there are any constructors that would take the first 'n' arguments
                // we've provided.  If so, delegate to those, and then create a field for any
                // remaining arguments.  Try to match from largest to smallest.
                //
                // Otherwise, just generate a normal constructor that assigns any provided
                // parameters into fields.

                var edit = await GenerateThisOrBaseDelegatingConstructorAsync().ConfigureAwait(false);
                if (edit != null)
                {
                    return edit;
                }

                return await GenerateFieldDelegatingConstructorAsync().ConfigureAwait(false);
            }

            private async Task<Document> GenerateThisOrBaseDelegatingConstructorAsync()
            {
                // We don't have to deal with the zero length case, since there's nothing to
                // delegate.  It will fall out of the GenerateFieldDelegatingConstructor above.
                for (int i = state.Arguments.Count; i >= 1; i--)
                {
                    var edit = await GenerateThisOrBaseDelegatingConstructorAsync(i).ConfigureAwait(false);
                    if (edit != null)
                    {
                        return edit;
                    }
                }

                return null;
            }

            private async Task<Document> GenerateThisOrBaseDelegatingConstructorAsync(int argumentCount)
            {
                Document edit;
                if ((edit = await GenerateDelegatingConstructorAsync(argumentCount, state.TypeToGenerateIn).ConfigureAwait(false)) != null ||
                    (edit = await GenerateDelegatingConstructorAsync(argumentCount, state.TypeToGenerateIn.BaseType).ConfigureAwait(false)) != null)
                {
                    return edit;
                }

                return null;
            }

            private async Task<Document> GenerateDelegatingConstructorAsync(
                int argumentCount,
                INamedTypeSymbol namedType)
            {
                if (namedType == null)
                {
                    return null;
                }

                // We can't resolve overloads across language.
                if (this.document.Project.Language != namedType.Language)
                {
                    return null;
                }

                var arguments = state.Arguments.Take(argumentCount).ToList();
                var remainingArguments = state.Arguments.Skip(argumentCount).ToList();
                var remainingAttributeArguments = state.AttributeArguments != null ? state.AttributeArguments.Skip(argumentCount).ToList() : null;
                var remainingParameterTypes = state.ParameterTypes.Skip(argumentCount).ToList();

                var instanceConstructors = namedType.InstanceConstructors.Where(IsSymbolAccessible).ToSet();
                if (instanceConstructors.IsEmpty())
                {
                    return null;
                }

                var delegatedConstructor = service.GetDelegatingConstructor(state, document, argumentCount, namedType, instanceConstructors, cancellationToken);
                if (delegatedConstructor == null)
                {
                    return null;
                }

                // There was a best match.  Call it directly.  
                var provider = document.Project.Solution.Workspace.Services.GetLanguageServices(this.state.TypeToGenerateIn.Language);
                var syntaxFactory = provider.GetService<SyntaxGenerator>();
                var codeGenerationService = provider.GetService<ICodeGenerationService>();
                
                // Map the first N parameters to the other constructor in this type.  Then
                // try to map any further parameters to existing fields.  Finally, generate
                // new fields if no such parameters exist.

                // Find the names of the parameters that will follow the parameters we're
                // delegating.
                var remainingParameterNames = service.GenerateParameterNames(
                    document.SemanticModel, remainingArguments, delegatedConstructor.Parameters.Select(p => p.Name).ToList());

                // Can't generate the constructor if the parameter names we're copying over forcibly
                // conflict with any names we generated.
                if (delegatedConstructor.Parameters.Select(p => p.Name).Intersect(remainingParameterNames).Any())
                {
                    return null;
                }

                // Try to map those parameters to fields.
                Dictionary<string, ISymbol> parameterToExistingFieldMap;
                Dictionary<string, string> parameterToNewFieldMap;
                List<IParameterSymbol> remainingParameters;
                this.GetParameters(remainingArguments, remainingAttributeArguments, remainingParameterTypes, remainingParameterNames, out parameterToExistingFieldMap, out parameterToNewFieldMap, out remainingParameters);

                var fields = syntaxFactory.CreateFieldsForParameters(remainingParameters, parameterToNewFieldMap);
                var assignStatements = syntaxFactory.CreateAssignmentStatements(remainingParameters, parameterToExistingFieldMap, parameterToNewFieldMap);

                var allParameters = delegatedConstructor.Parameters.Concat(remainingParameters).ToList();

                var isThis = namedType.Equals(state.TypeToGenerateIn);
                var delegatingArguments = syntaxFactory.CreateArguments(delegatedConstructor.Parameters);
                var baseConstructorArguments = isThis ? null : delegatingArguments;
                var thisConstructorArguments = isThis ? delegatingArguments : null;

                var constructor = CodeGenerationSymbolFactory.CreateConstructorSymbol(
                    attributes: null,
                    accessibility: Accessibility.Public,
                    modifiers: default(DeclarationModifiers),
                    typeName: this.state.TypeToGenerateIn.Name,
                    parameters: allParameters,
                    statements: assignStatements.ToList(),
                    baseConstructorArguments: baseConstructorArguments,
                    thisConstructorArguments: thisConstructorArguments);

                var members = new List<ISymbol>(fields) { constructor };
                var result = await codeGenerationService.AddMembersAsync(
                    this.document.Project.Solution,
                    this.state.TypeToGenerateIn,
                    members,
                    new CodeGenerationOptions(this.state.Token.GetLocation()),
                    cancellationToken)
                    .ConfigureAwait(false);

                return result;
            }

            private async Task<Document> GenerateFieldDelegatingConstructorAsync()
            {
                var arguments = state.Arguments.ToList();
                var parameterTypes = state.ParameterTypes;

                var typeParametersNames = state.TypeToGenerateIn.GetAllTypeParameters().Select(t => t.Name).ToList();
                var parameterNames = state.AttributeArguments != null
                    ? service.GenerateParameterNames(document.SemanticModel, state.AttributeArguments, typeParametersNames)
                    : service.GenerateParameterNames(document.SemanticModel, arguments, typeParametersNames);

                Dictionary<string, ISymbol> parameterToExistingFieldMap;
                Dictionary<string, string> parameterToNewFieldMap;
                List<IParameterSymbol> parameters;
                GetParameters(arguments, state.AttributeArguments, parameterTypes, parameterNames, out parameterToExistingFieldMap, out parameterToNewFieldMap, out parameters);

                var provider = document.Project.Solution.Workspace.Services.GetLanguageServices(this.state.TypeToGenerateIn.Language);
                var syntaxFactory = provider.GetService<SyntaxGenerator>();
                var codeGenerationService = provider.GetService<ICodeGenerationService>();

                var syntaxTree = document.SyntaxTree;
                var members = syntaxFactory.CreateFieldDelegatingConstructor(
                    state.TypeToGenerateIn.Name, state.TypeToGenerateIn, parameters,
                    parameterToExistingFieldMap, parameterToNewFieldMap, cancellationToken);

                var result = await codeGenerationService.AddMembersAsync(
                    this.document.Project.Solution,
                    this.state.TypeToGenerateIn,
                    members,
                    new CodeGenerationOptions(this.state.Token.GetLocation()),
                    cancellationToken)
                    .ConfigureAwait(false);

                return result;
            }

            private void GetParameters(
                IList<TArgumentSyntax> arguments,
                IList<TAttributeArgumentSyntax> attributeArguments,
                IList<ITypeSymbol> parameterTypes,
                IList<string> parameterNames,
                out Dictionary<string, ISymbol> parameterToExistingFieldMap,
                out Dictionary<string, string> parameterToNewFieldMap,
                out List<IParameterSymbol> parameters)
            {
                parameterToExistingFieldMap = new Dictionary<string, ISymbol>();
                parameterToNewFieldMap = new Dictionary<string, string>();
                parameters = new List<IParameterSymbol>();

                for (var i = 0; i < parameterNames.Count; i++)
                {
                    // See if there's a matching field we can use.  First test in a case sensitive
                    // manner, then case insensitively.
                    if (!TryFindMatchingField(arguments, attributeArguments, parameterNames, parameterTypes, i, parameterToExistingFieldMap, parameterToNewFieldMap, caseSentitive: true))
                    {
                        if (!TryFindMatchingField(arguments, attributeArguments, parameterNames, parameterTypes, i, parameterToExistingFieldMap, parameterToNewFieldMap, caseSentitive: false))
                        {
                            parameterToNewFieldMap[parameterNames[i]] = parameterNames[i];
                        }
                    }

                    parameters.Add(CodeGenerationSymbolFactory.CreateParameterSymbol(
                        attributes: null,
                        refKind: service.GetRefKind(arguments[i]),
                        isParams: false,
                        type: parameterTypes[i],
                        name: parameterNames[i]));
                }
            }

            private bool TryFindMatchingField(
                IList<TArgumentSyntax> arguments,
                IList<TAttributeArgumentSyntax> attributeArguments,
                IList<string> parameterNames,
                IList<ITypeSymbol> parameterTypes,
                int index,
                Dictionary<string, ISymbol> parameterToExistingFieldMap,
                Dictionary<string, string> parameterToNewFieldMap,
                bool caseSentitive)
            {
                var parameterName = parameterNames[index];
                var parameterType = parameterTypes[index];
                var isFixed = service.IsNamedArgument(arguments[index]);

                // For non-out parameters, see if there's already a field there with the same name.
                // If so, and it has a compatible type, then we can just assign to that field.
                // Otherwise, we'll need to choose a different name for this member so that it
                // doesn't conflict with something already in the type. First check the current type
                // for a matching field.  If so, defer to it.
                var comparison = caseSentitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                foreach (var type in this.state.TypeToGenerateIn.GetBaseTypesAndThis())
                {
                    var ignoreAccessibility = type.Equals(this.state.TypeToGenerateIn);
                    var symbol = type.GetMembers()
                                     .FirstOrDefault(s => s.Name.Equals(parameterName, comparison));

                    if (symbol != null)
                    {
                        if (ignoreAccessibility || IsSymbolAccessible(symbol))
                        {
                            if (IsViableFieldOrProperty(parameterType, symbol))
                            {
                                // Ok!  We can just the existing field.  
                                parameterToExistingFieldMap[parameterName] = symbol;
                            }
                            else
                            {
                                // Uh-oh.  Now we have a problem.  We can't assign this parameter to
                                // this field.  So we need to create a new field.  Find a name not in
                                // use so we can assign to that.  
                                var newFieldName = NameGenerator.EnsureUniqueness(
                                    attributeArguments != null ?
                                    service.GenerateNameForArgument(document.SemanticModel, attributeArguments[index]) :
                                    service.GenerateNameForArgument(document.SemanticModel, arguments[index]),
                                    GetUnavailableMemberNames().Concat(parameterToNewFieldMap.Values));

                                if (isFixed)
                                {
                                    // Can't change the parameter name, so map the existing parameter
                                    // name to the new field name.
                                    parameterToNewFieldMap[parameterName] = newFieldName;
                                }
                                else
                                {
                                    // Can change the parameter name, so do so.
                                    parameterNames[index] = newFieldName;
                                    parameterToNewFieldMap[newFieldName] = newFieldName;
                                }
                            }

                            return true;
                        }
                    }
                }

                return false;
            }

            private IEnumerable<string> GetUnavailableMemberNames()
            {
                return this.state.TypeToGenerateIn.MemberNames.Concat(
                    from type in this.state.TypeToGenerateIn.GetBaseTypes()
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
                    if (symbol is IFieldSymbol)
                    {
                        var field = (IFieldSymbol)symbol;
                        return
                            !field.IsConst &&
                            service.IsConversionImplicit(document.SemanticModel.Compilation, parameterType, field.Type);
                    }
                    else if (symbol is IPropertySymbol)
                    {
                        var property = (IPropertySymbol)symbol;
                        return
                            property.Parameters.Length == 0 &&
                            property.SetMethod != null &&
                            service.IsConversionImplicit(document.SemanticModel.Compilation, parameterType, property.Type);
                    }
                }

                return false;
            }

            private bool IsSymbolAccessible(
                ISymbol symbol)
            {
                if (symbol == null)
                {
                    return false;
                }

                if (symbol.Kind == SymbolKind.Property)
                {
                    if (!IsSymbolAccessible(((IPropertySymbol)symbol).SetMethod))
                    {
                        return false;
                    }
                }

                // Public and protected constructors are accessible.  Internal constructors are
                // accessible if we have friend access.  We can't call the normal accessibility
                // checkers since they will think that a protected constructor isn't accessible
                // (since we don't have the destination type that would have access to them yet).
                switch (symbol.DeclaredAccessibility)
                {
                    case Accessibility.ProtectedOrInternal:
                    case Accessibility.Protected:
                    case Accessibility.Public:
                        return true;
                    case Accessibility.ProtectedAndInternal:
                    case Accessibility.Internal:
                        return document.SemanticModel.Compilation.Assembly.IsSameAssemblyOrHasFriendAccessTo(
                            symbol.ContainingAssembly);

                    default:
                        return false;
                }
            }
        }
    }
}
