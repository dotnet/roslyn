// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
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
                CancellationToken cancellationToken)
            {
                var factory = this.Document.GetLanguageService<SyntaxGenerator>();
                var comAliasNameAttribute = compilation.ComAliasNameAttributeType();

                var getAccessor = property.GetMethod == null
                    ? null
                    : CodeGenerationSymbolFactory.CreateAccessorSymbol(
                        property.GetMethod.RemoveInaccessibleAttributesAndAttributesOfType(
                            accessibleWithin: this.State.ClassOrStructType,
                            removeAttributeType: comAliasNameAttribute),
                        attributes: null,
                        accessibility: accessibility,
                        explicitInterfaceSymbol: useExplicitInterfaceSymbol ? property.GetMethod : null,
                        statements: GetGetAccessorStatements(compilation, property, generateAbstractly, cancellationToken));

                var setAccessor = property.SetMethod == null
                    ? null
                    : CodeGenerationSymbolFactory.CreateAccessorSymbol(
                        property.SetMethod.RemoveInaccessibleAttributesAndAttributesOfType(
                            accessibleWithin: this.State.ClassOrStructType,
                            removeAttributeType: comAliasNameAttribute),
                        attributes: null,
                        accessibility: accessibility,
                        explicitInterfaceSymbol: useExplicitInterfaceSymbol ? property.SetMethod : null,
                        statements: GetSetAccessorStatements(compilation, property, generateAbstractly, cancellationToken));

                var syntaxFacts = Document.GetLanguageService<ISyntaxFactsService>();
                var parameterNames = NameGenerator.EnsureUniqueness(
                    property.Parameters.Select(p => p.Name).ToList(), isCaseSensitive: syntaxFacts.IsCaseSensitive);

                var updatedProperty = property.RenameParameters(parameterNames);

                updatedProperty = updatedProperty.RemoveAttributeFromParameters(comAliasNameAttribute);

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
