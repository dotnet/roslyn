// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor
{
    internal abstract partial class AbstractGenerateConstructorService<TService, TArgumentSyntax, TAttributeArgumentSyntax>
    {
        protected internal class State
        {
            public IList<TArgumentSyntax> Arguments { get; private set; }

            public IList<TAttributeArgumentSyntax> AttributeArguments { get; private set; }

            // The type we're creating a constructor for.  Will be a class or struct type.
            public INamedTypeSymbol TypeToGenerateIn { get; private set; }

            public IList<RefKind> ParameterRefKinds { get; private set; }
            public IList<ITypeSymbol> ParameterTypes { get; private set; }

            public IMethodSymbol DelegatedConstructorOpt { get; private set; }

            public SyntaxToken Token { get; private set; }

            public bool IsConstructorInitializerGeneration { get; private set; }

            private State()
            {
            }

            public static async Task<State> GenerateAsync(
                TService service,
                SemanticDocument document,
                SyntaxNode node,
                CancellationToken cancellationToken)
            {
                var state = new State();
                if (!await state.TryInitializeAsync(service, document, node, cancellationToken).ConfigureAwait(false))
                {
                    return null;
                }

                return state;
            }

            private async Task<bool> TryInitializeAsync(
                TService service,
                SemanticDocument document,
                SyntaxNode node,
                CancellationToken cancellationToken)
            {
                if (service.IsConstructorInitializerGeneration(document, node, cancellationToken))
                {
                    if (!await TryInitializeConstructorInitializerGenerationAsync(service, document, node, cancellationToken).ConfigureAwait(false))
                    {
                        return false;
                    }
                }
                else if (service.IsSimpleNameGeneration(document, node, cancellationToken))
                {
                    if (!await TryInitializeSimpleNameGenerationAsync(service, document, node, cancellationToken).ConfigureAwait(false))
                    {
                        return false;
                    }
                }
                else if (service.IsClassDeclarationGeneration(document, node, cancellationToken))
                {
                    if (!await TryInitializeClassDeclarationGenerationAsync(service, document, node, cancellationToken).ConfigureAwait(false))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }

                if (!CodeGenerator.CanAdd(document.Project.Solution, this.TypeToGenerateIn, cancellationToken))
                {
                    return false;
                }

                this.ParameterTypes = this.ParameterTypes ?? GetParameterTypes(service, document, cancellationToken);
                this.ParameterRefKinds = this.ParameterRefKinds ?? this.Arguments.Select(service.GetRefKind).ToList();

                return !ClashesWithExistingConstructor(document, cancellationToken);
            }

            private bool ClashesWithExistingConstructor(SemanticDocument document, CancellationToken cancellationToken)
            {
                var destinationProvider = document.Project.Solution.Workspace.Services.GetLanguageServices(this.TypeToGenerateIn.Language);
                var syntaxFacts = destinationProvider.GetService<ISyntaxFactsService>();
                return this.TypeToGenerateIn.InstanceConstructors.Any(c => Matches(c, this.ParameterTypes, this.ParameterRefKinds, this.Arguments, syntaxFacts));
            }

            private static bool Matches(IMethodSymbol ctor, IList<ITypeSymbol> parameterTypes, IList<RefKind> parameterRefKinds, IList<TArgumentSyntax> arguments, ISyntaxFactsService service)
            {
                if (ctor.Parameters.Length != parameterTypes.Count)
                {
                    return false;
                }

                for (int i = 0; i < parameterTypes.Count; i++)
                {
                    var ctorParameter = ctor.Parameters[i];
                    var result = SymbolEquivalenceComparer.Instance.Equals(ctorParameter.Type, parameterTypes[i]) &&
                        ctorParameter.RefKind == parameterRefKinds[i];
                    
                    string parameterName = GetParameterName(arguments, i, service);
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

            private static string GetParameterName(IList<TArgumentSyntax> arguments, int index, ISyntaxFactsService service)
            {
                if (index >= arguments?.Count)
                {
                    return string.Empty;
                }

                return service.GetNameForArgument(arguments?[index]);
            }

            internal List<ITypeSymbol> GetParameterTypes(
                TService service,
                SemanticDocument document,
                CancellationToken cancellationToken)
            {
                var allTypeParameters = this.TypeToGenerateIn.GetAllTypeParameters();
                var semanticModel = document.SemanticModel;
                var allTypes = this.AttributeArguments != null
                    ? this.AttributeArguments.Select(a => service.GetAttributeArgumentType(semanticModel, a, cancellationToken))
                    : this.Arguments.Select(a => service.GetArgumentType(semanticModel, a, cancellationToken));

                return allTypes.Select(t => FixType(t, semanticModel, allTypeParameters)).ToList();
            }

            private ITypeSymbol FixType(ITypeSymbol typeSymbol, SemanticModel semanticModel, IEnumerable<ITypeParameterSymbol> allTypeParameters)
            {
                var compilation = semanticModel.Compilation;
                return typeSymbol.RemoveAnonymousTypes(compilation)
                    .RemoveUnavailableTypeParameters(compilation, allTypeParameters)
                    .RemoveUnnamedErrorTypes(compilation);
            }

            private async Task<bool> TryInitializeConstructorInitializerGenerationAsync(
                TService service,
                SemanticDocument document,
                SyntaxNode constructorInitializer,
                CancellationToken cancellationToken)
            {
                SyntaxToken token;
                IList<TArgumentSyntax> arguments;
                INamedTypeSymbol typeToGenerateIn;
                if (!service.TryInitializeConstructorInitializerGeneration(document, constructorInitializer, cancellationToken,
                    out token, out arguments, out typeToGenerateIn))
                {
                    return false;
                }

                this.Token = token;
                this.Arguments = arguments;
                this.IsConstructorInitializerGeneration = true;

                var semanticModel = document.SemanticModel;
                var semanticInfo = semanticModel.GetSymbolInfo(constructorInitializer, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                if (semanticInfo.Symbol != null)
                {
                    return false;
                }

                return await TryDetermineTypeToGenerateInAsync(document, typeToGenerateIn, cancellationToken).ConfigureAwait(false);
            }

            private async Task<bool> TryInitializeClassDeclarationGenerationAsync(
                TService service,
                SemanticDocument document,
                SyntaxNode simpleName,
                CancellationToken cancellationToken)
            {
                SyntaxToken token;
                INamedTypeSymbol typeToGenerateIn;
                IMethodSymbol constructor;
                if (service.TryInitializeClassDeclarationGenerationState(document, simpleName, cancellationToken,
                    out token, out constructor, out typeToGenerateIn))
                {
                    this.Token = token;
                    this.DelegatedConstructorOpt = constructor;
                    this.ParameterTypes = constructor.Parameters.Select(p => p.Type).ToList();
                    this.ParameterRefKinds = constructor.Parameters.Select(p => p.RefKind).ToList();
                }
                cancellationToken.ThrowIfCancellationRequested();

                return await TryDetermineTypeToGenerateInAsync(document, typeToGenerateIn, cancellationToken).ConfigureAwait(false);
            }

            private async Task<bool> TryInitializeSimpleNameGenerationAsync(
                TService service,
                SemanticDocument document,
                SyntaxNode simpleName,
                CancellationToken cancellationToken)
            {
                SyntaxToken token;
                IList<TArgumentSyntax> arguments;
                IList<TAttributeArgumentSyntax> attributeArguments;
                INamedTypeSymbol typeToGenerateIn;
                if (service.TryInitializeSimpleNameGenerationState(document, simpleName, cancellationToken,
                    out token, out arguments, out typeToGenerateIn))
                {
                    this.Token = token;
                    this.Arguments = arguments;
                }
                else if (service.TryInitializeSimpleAttributeNameGenerationState(document, simpleName, cancellationToken,
                    out token, out arguments, out attributeArguments, out typeToGenerateIn))
                {
                    this.Token = token;
                    this.AttributeArguments = attributeArguments;
                    this.Arguments = arguments;

                    //// Attribute parameters are restricted to be constant values (simple types or string, etc).
                    if (this.AttributeArguments != null && GetParameterTypes(service, document, cancellationToken).Any(t => !IsValidAttributeParameterType(t)))
                    {
                        return false;
                    }
                    else if (GetParameterTypes(service, document, cancellationToken).Any(t => !IsValidAttributeParameterType(t)))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }

                cancellationToken.ThrowIfCancellationRequested();

                return await TryDetermineTypeToGenerateInAsync(document, typeToGenerateIn, cancellationToken).ConfigureAwait(false);
            }

            private bool IsValidAttributeParameterType(ITypeSymbol type)
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
                SemanticDocument document,
                INamedTypeSymbol original,
                CancellationToken cancellationToken)
            {
                var definition = await SymbolFinder.FindSourceDefinitionAsync(original, document.Project.Solution, cancellationToken).ConfigureAwait(false);
                this.TypeToGenerateIn = definition as INamedTypeSymbol;

                return this.TypeToGenerateIn != null &&
                    (this.TypeToGenerateIn.TypeKind == TypeKind.Class ||
                     this.TypeToGenerateIn.TypeKind == TypeKind.Struct);
            }
        }
    }
}
