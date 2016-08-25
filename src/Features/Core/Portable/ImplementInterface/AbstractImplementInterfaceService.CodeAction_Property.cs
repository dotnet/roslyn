// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

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
                CancellationToken cancellationToken)
            {
                var factory = this.Document.GetLanguageService<SyntaxGenerator>();
                var attributesToRemove = AttributesToRemove(compilation);

                var getAccessor = GenerateGetAccessor(compilation, property, accessibility, generateAbstractly,
                    useExplicitInterfaceSymbol, attributesToRemove, cancellationToken);

                var setAccessor = GenerateSetAccessor(compilation, property, accessibility,
                    generateAbstractly, useExplicitInterfaceSymbol, attributesToRemove, cancellationToken);

                var syntaxFacts = Document.GetLanguageService<ISyntaxFactsService>();
                var parameterNames = NameGenerator.EnsureUniqueness(
                    property.Parameters.Select(p => p.Name).ToList(), isCaseSensitive: syntaxFacts.IsCaseSensitive);

                var updatedProperty = property.RenameParameters(parameterNames);

                updatedProperty = updatedProperty.RemoveAttributeFromParameters(attributesToRemove);

                // TODO(cyrusn): Delegate through throughMember if it's non-null.
                return CodeGenerationSymbolFactory.CreatePropertySymbol(
                    updatedProperty,
                    accessibility: accessibility,
                    modifiers: modifiers,
                    explicitInterfaceSymbol: useExplicitInterfaceSymbol ? property : null,
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
                    compilation.DynamicAttributeType() };
            }

            private IMethodSymbol GenerateSetAccessor(
                Compilation compilation,
                IPropertySymbol property,
                Accessibility accessibility,
                bool generateAbstractly,
                bool useExplicitInterfaceSymbol,
                INamedTypeSymbol[] attributesToRemove,
                CancellationToken cancellationToken)
            {
                if (property.SetMethod == null)
                {
                    return null;
                }

                var setMethod = property.SetMethod.RemoveInaccessibleAttributesAndAttributesOfTypes(
                     this.State.ClassOrStructType,
                     attributesToRemove);

                return CodeGenerationSymbolFactory.CreateAccessorSymbol(
                    setMethod,
                    attributes: null,
                    accessibility: accessibility,
                    explicitInterfaceSymbol: useExplicitInterfaceSymbol ? property.SetMethod : null,
                    statements: GetSetAccessorStatements(compilation, property, generateAbstractly, cancellationToken));
            }

            private IMethodSymbol GenerateGetAccessor(
                Compilation compilation,
                IPropertySymbol property,
                Accessibility accessibility,
                bool generateAbstractly,
                bool useExplicitInterfaceSymbol,
                INamedTypeSymbol[] attributesToRemove,
                CancellationToken cancellationToken)
            {
                if (property.GetMethod == null)
                {
                    return null;
                }

                var getMethod = property.GetMethod.RemoveInaccessibleAttributesAndAttributesOfTypes(
                     this.State.ClassOrStructType,
                     attributesToRemove);

                return CodeGenerationSymbolFactory.CreateAccessorSymbol(
                    getMethod,
                    attributes: null,
                    accessibility: accessibility,
                    explicitInterfaceSymbol: useExplicitInterfaceSymbol ? property.GetMethod : null,
                    statements: GetGetAccessorStatements(compilation, property, generateAbstractly, cancellationToken));
            }

            private IList<SyntaxNode> GetSetAccessorStatements(
                Compilation compilation,
                IPropertySymbol property,
                bool generateAbstractly,
                CancellationToken cancellationToken)
            {
                if (generateAbstractly)
                {
                    return null;
                }

                var factory = this.Document.GetLanguageService<SyntaxGenerator>();
                if (ThroughMember != null)
                {
                    var throughExpression = CreateThroughExpression(factory);
                    SyntaxNode expression;

                    if (property.IsIndexer)
                    {
                        expression = throughExpression;
                    }
                    else
                    {
                        expression = factory.MemberAccessExpression(
                                                throughExpression, factory.IdentifierName(property.Name));
                    }

                    if (property.Parameters.Length > 0)
                    {
                        var arguments = factory.CreateArguments(property.Parameters.As<IParameterSymbol>());
                        expression = factory.ElementAccessExpression(expression, arguments);
                    }

                    expression = factory.AssignmentStatement(expression, factory.IdentifierName("value"));

                    return new[] { factory.ExpressionStatement(expression) };
                }

                return factory.CreateThrowNotImplementedStatementBlock(compilation);
            }

            private IList<SyntaxNode> GetGetAccessorStatements(
                Compilation compilation,
                IPropertySymbol property,
                bool generateAbstractly,
                CancellationToken cancellationToken)
            {
                if (generateAbstractly)
                {
                    return null;
                }

                var factory = this.Document.GetLanguageService<SyntaxGenerator>();
                if (ThroughMember != null)
                {
                    var throughExpression = CreateThroughExpression(factory);
                    SyntaxNode expression;

                    if (property.IsIndexer)
                    {
                        expression = throughExpression;
                    }
                    else
                    {
                        expression = factory.MemberAccessExpression(
                                                throughExpression, factory.IdentifierName(property.Name));
                    }

                    if (property.Parameters.Length > 0)
                    {
                        var arguments = factory.CreateArguments(property.Parameters.As<IParameterSymbol>());
                        expression = factory.ElementAccessExpression(expression, arguments);
                    }

                    return new[] { factory.ReturnStatement(expression) };
                }

                return factory.CreateThrowNotImplementedStatementBlock(compilation);
            }
        }
    }
}
