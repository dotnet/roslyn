﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ImplementInterface
{
    internal abstract partial class AbstractImplementInterfaceService
    {
        internal partial class ImplementInterfaceCodeAction
        {
            private ISymbol GenerateProperty(
                Compilation compilation,
                IPropertySymbol property,
                Accessibility accessibility,
                DeclarationModifiers modifiers,
                bool generateAbstractly,
                bool useExplicitInterfaceSymbol,
                string memberName,
                ImplementTypePropertyGenerationBehavior propertyGenerationBehavior,
                CancellationToken cancellationToken)
            {
                var factory = Document.GetLanguageService<SyntaxGenerator>();
                var attributesToRemove = AttributesToRemove(compilation);

                var getAccessor = GenerateGetAccessor(
                    compilation, property, accessibility, generateAbstractly, useExplicitInterfaceSymbol,
                    propertyGenerationBehavior, attributesToRemove);

                var setAccessor = GenerateSetAccessor(
                    compilation, property, accessibility, generateAbstractly, useExplicitInterfaceSymbol,
                    propertyGenerationBehavior, attributesToRemove);

                var syntaxFacts = Document.Project.LanguageServices.GetRequiredService<ISyntaxFactsService>();

                var parameterNames = NameGenerator.EnsureUniqueness(
                    property.Parameters.SelectAsArray(p => p.Name),
                    isCaseSensitive: syntaxFacts.IsCaseSensitive);

                var updatedProperty = property.RenameParameters(parameterNames);

                updatedProperty = updatedProperty.RemoveInaccessibleAttributesAndAttributesOfTypes(compilation.Assembly, attributesToRemove);

                return CodeGenerationSymbolFactory.CreatePropertySymbol(
                    updatedProperty,
                    accessibility: accessibility,
                    modifiers: modifiers,
                    explicitInterfaceImplementations: useExplicitInterfaceSymbol ? ImmutableArray.Create(property) : default,
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
            private INamedTypeSymbol[] AttributesToRemove(Compilation compilation)
            {
                return new[] { compilation.ComAliasNameAttributeType(), compilation.TupleElementNamesAttributeType(),
                    compilation.DynamicAttributeType() }.WhereNotNull().ToArray()!;
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
                    explicitInterfaceImplementations: useExplicitInterfaceSymbol ? ImmutableArray.Create(property.SetMethod) : default,
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
                    explicitInterfaceImplementations: useExplicitInterfaceSymbol ? ImmutableArray.Create(property.GetMethod) : default,
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
                return generator.GetSetAccessorStatements(compilation, property, this.ThroughMember,
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

                var generator = Document.Project.LanguageServices.GetRequiredService<SyntaxGenerator>();
                return generator.GetGetAccessorStatements(compilation, property, ThroughMember,
                    propertyGenerationBehavior == ImplementTypePropertyGenerationBehavior.PreferAutoProperties);
            }
        }
    }
}
