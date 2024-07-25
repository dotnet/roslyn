// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

#if CODE_STYLE
using DeclarationModifiers = Microsoft.CodeAnalysis.Internal.Editing.DeclarationModifiers;
#else
using DeclarationModifiers = Microsoft.CodeAnalysis.Editing.DeclarationModifiers;
#endif

namespace Microsoft.CodeAnalysis.ImplementInterface;

internal abstract partial class AbstractImplementInterfaceService
{
    private partial class ImplementInterfaceGenerator
    {
        private IEnumerable<ISymbol?> GeneratePropertyMembers(
            Compilation compilation,
            IPropertySymbol property,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            bool generateAbstractly,
            bool useExplicitInterfaceSymbol,
            string memberName,
            ImplementTypePropertyGenerationBehavior propertyGenerationBehavior)
        {
            var factory = Document.GetLanguageService<SyntaxGenerator>();
            var attributesToRemove = AttributesToRemove(compilation);

            var getAccessor = GenerateGetAccessor(
                compilation, property, accessibility, generateAbstractly, useExplicitInterfaceSymbol,
                propertyGenerationBehavior, attributesToRemove);

            var setAccessor = GenerateSetAccessor(
                compilation, property, accessibility, generateAbstractly, useExplicitInterfaceSymbol,
                propertyGenerationBehavior, attributesToRemove);

            var syntaxFacts = Document.GetRequiredLanguageService<ISyntaxFactsService>();
            var semanticFacts = Document.GetRequiredLanguageService<ISemanticFactsService>();

            if (property is { IsIndexer: false, Parameters.Length: > 0 } &&
                !semanticFacts.SupportsParameterizedProperties)
            {
                yield return getAccessor;
                yield return setAccessor;
                yield break;
            }

            var parameterNames = NameGenerator.EnsureUniqueness(
                property.Parameters.SelectAsArray(p => p.Name),
                isCaseSensitive: syntaxFacts.IsCaseSensitive);

            var updatedProperty = property.RenameParameters(parameterNames);

            updatedProperty = updatedProperty.RemoveInaccessibleAttributesAndAttributesOfTypes(compilation.Assembly, attributesToRemove);

            yield return CodeGenerationSymbolFactory.CreatePropertySymbol(
                updatedProperty,
                accessibility: accessibility,
                modifiers: modifiers,
                explicitInterfaceImplementations: useExplicitInterfaceSymbol ? [property] : default,
                name: memberName,
                getMethod: getAccessor,
                setMethod: setAccessor);
        }

        /// <summary>
        /// Lists compiler attributes that we want to remove.
        /// The TupleElementNames attribute is compiler generated (it is used for naming tuple element names).
        /// We never want to place it in source code.
        /// Same thing for the Dynamic attribute.
        /// </summary>
        private static INamedTypeSymbol[] AttributesToRemove(Compilation compilation)
        {
            return new[] { compilation.ComAliasNameAttributeType(), compilation.TupleElementNamesAttributeType(),
                compilation.DynamicAttributeType(), compilation.NativeIntegerAttributeType() }.WhereNotNull().ToArray()!;
        }

        private IMethodSymbol? GenerateSetAccessor(
            Compilation compilation,
            IPropertySymbol property,
            Accessibility accessibility,
            bool generateAbstractly,
            bool useExplicitInterfaceSymbol,
            ImplementTypePropertyGenerationBehavior propertyGenerationBehavior,
            INamedTypeSymbol[] attributesToRemove)
        {
            if (property.SetMethod == null)
            {
                return null;
            }

            if (property.GetMethod == null)
            {
                // Can't have an auto-prop with just a setter.
                propertyGenerationBehavior = ImplementTypePropertyGenerationBehavior.PreferThrowingProperties;
            }

            var setMethod = property.SetMethod.RemoveInaccessibleAttributesAndAttributesOfTypes(
                 State.ClassOrStructType,
                 attributesToRemove);

            return CodeGenerationSymbolFactory.CreateAccessorSymbol(
                setMethod,
                attributes: default,
                accessibility: accessibility,
                explicitInterfaceImplementations: useExplicitInterfaceSymbol ? [property.SetMethod] : default,
                statements: GetSetAccessorStatements(
                    compilation, property, generateAbstractly, propertyGenerationBehavior));
        }

        private IMethodSymbol? GenerateGetAccessor(
            Compilation compilation,
            IPropertySymbol property,
            Accessibility accessibility,
            bool generateAbstractly,
            bool useExplicitInterfaceSymbol,
            ImplementTypePropertyGenerationBehavior propertyGenerationBehavior,
            INamedTypeSymbol[] attributesToRemove)
        {
            if (property.GetMethod == null)
            {
                return null;
            }

            var getMethod = property.GetMethod.RemoveInaccessibleAttributesAndAttributesOfTypes(
                 State.ClassOrStructType,
                 attributesToRemove);

            return CodeGenerationSymbolFactory.CreateAccessorSymbol(
                getMethod,
                attributes: default,
                accessibility: accessibility,
                explicitInterfaceImplementations: useExplicitInterfaceSymbol ? [property.GetMethod] : default,
                statements: GetGetAccessorStatements(
                    compilation, property, generateAbstractly, propertyGenerationBehavior));
        }

        private ImmutableArray<SyntaxNode> GetSetAccessorStatements(
            Compilation compilation,
            IPropertySymbol property,
            bool generateAbstractly,
            ImplementTypePropertyGenerationBehavior propertyGenerationBehavior)
        {
            if (generateAbstractly)
                return default;

            var generator = Document.GetRequiredLanguageService<SyntaxGenerator>();
            return generator.GetSetAccessorStatements(compilation, property, ThroughMember,
                propertyGenerationBehavior == ImplementTypePropertyGenerationBehavior.PreferAutoProperties);
        }

        private ImmutableArray<SyntaxNode> GetGetAccessorStatements(
            Compilation compilation,
            IPropertySymbol property,
            bool generateAbstractly,
            ImplementTypePropertyGenerationBehavior propertyGenerationBehavior)
        {
            if (generateAbstractly)
                return default;

            var generator = Document.Project.Services.GetRequiredService<SyntaxGenerator>();
            return generator.GetGetAccessorStatements(compilation, property, ThroughMember,
                propertyGenerationBehavior == ImplementTypePropertyGenerationBehavior.PreferAutoProperties);
        }
    }
}
