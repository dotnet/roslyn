// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ImplementInterface
{
    internal abstract partial class AbstractImplementInterfaceService
    {
        internal partial class ImplementInterfaceCodeAction
        {
            private ISymbol GenerateMethod(
                Compilation compilation,
                IMethodSymbol method,
                Accessibility accessibility,
                DeclarationModifiers modifiers,
                bool generateAbstractly,
                bool useExplicitInterfaceSymbol,
                string memberName,
                CancellationToken cancellationToken)
            {
                var syntaxFacts = Document.GetLanguageService<ISyntaxFactsService>();

                var updatedMethod = method.EnsureNonConflictingNames(
                    State.ClassOrStructType, syntaxFacts, cancellationToken);

                updatedMethod = updatedMethod.RemoveInaccessibleAttributesAndAttributesOfTypes(
                    State.ClassOrStructType,
                    AttributesToRemove(compilation));

                return CodeGenerationSymbolFactory.CreateMethodSymbol(
                    updatedMethod,
                    accessibility: accessibility,
                    modifiers: modifiers,
                    explicitInterfaceImplementations: useExplicitInterfaceSymbol ? ImmutableArray.Create(updatedMethod) : default,
                    name: memberName,
                    statements: generateAbstractly
                        ? default
                        : ImmutableArray.Create(CreateStatement(compilation, updatedMethod, cancellationToken)));
            }

            private SyntaxNode CreateStatement(
                Compilation compilation,
                IMethodSymbol method,
                CancellationToken cancellationToken)
            {
                if (ThroughMember == null)
                {
                    var factory = Document.GetLanguageService<SyntaxGenerator>();
                    return factory.CreateThrowNotImplementedStatement(compilation);
                }
                else
                {
                    return CreateDelegationStatement(method);
                }
            }

            private SyntaxNode CreateDelegationStatement(
                IMethodSymbol method)
            {
                var factory = Document.GetLanguageService<SyntaxGenerator>();
                var through = CreateThroughExpression(factory);

                var memberName = method.IsGenericMethod
                    ? factory.GenericName(method.Name, method.TypeArguments.OfType<ITypeSymbol>().ToList())
                    : factory.IdentifierName(method.Name);

                through = factory.MemberAccessExpression(
                    through, memberName);

                var arguments = factory.CreateArguments(method.Parameters.As<IParameterSymbol>());
                var invocationExpression = factory.InvocationExpression(through, arguments);

                return method.ReturnsVoid
                    ? factory.ExpressionStatement(invocationExpression)
                    : factory.ReturnStatement(invocationExpression);
            }
        }
    }
}
