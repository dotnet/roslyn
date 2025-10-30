// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ImplementInterface;

internal abstract partial class AbstractImplementInterfaceService<TTypeDeclarationSyntax>
{
    private sealed partial class ImplementInterfaceGenerator
    {
        private ISymbol GenerateMethod(
            Compilation compilation,
            IMethodSymbol method,
            IMethodSymbol? conflictingMethod,
            Accessibility accessibility,
            DeclarationModifiers modifiers,
            bool generateAbstractly,
            bool useExplicitInterfaceSymbol,
            string memberName)
        {
            var syntaxFacts = Document.GetRequiredLanguageService<ISyntaxFactsService>();

            var updatedMethod = method.EnsureNonConflictingNames(State.ClassOrStructType, syntaxFacts);

            updatedMethod = updatedMethod.RemoveInaccessibleAttributesAndAttributesOfTypes(
                State.ClassOrStructType,
                AttributesToRemove(compilation));

            return CodeGenerationSymbolFactory.CreateMethodSymbol(
                updatedMethod,
                accessibility: accessibility,
                modifiers: modifiers,
                explicitInterfaceImplementations: useExplicitInterfaceSymbol ? [updatedMethod] : default,
                name: memberName,
                statements: generateAbstractly ? default : [GenerateStatement(compilation, updatedMethod, conflictingMethod)]);
        }

        private SyntaxNode GenerateStatement(
            Compilation compilation, IMethodSymbol updatedMethod, IMethodSymbol? conflictingMethod)
        {
            var factory = Document.GetRequiredLanguageService<SyntaxGenerator>();
            if (ThroughMember != null)
                return factory.GenerateDelegateThroughMemberStatement(updatedMethod, ThroughMember);

            // Forward from the explicit method we're creating to the existing method it conflicts with if possible.
            if (CanForwardToConflictingMethod(compilation, updatedMethod, conflictingMethod))
            {
                var invocation = factory.InvocationExpression(
                    factory.MemberAccessExpression(
                        factory.ThisExpression(),
                        conflictingMethod.Name),
                    factory.CreateArguments(updatedMethod.Parameters));

                return updatedMethod.ReturnsVoid
                    ? factory.ExpressionStatement(invocation)
                    : factory.ReturnStatement(invocation);
            }

            return factory.CreateThrowNotImplementedStatement(compilation);
        }

        private static bool CanForwardToConflictingMethod(
            Compilation compilation, IMethodSymbol method, [NotNullWhen(true)] IMethodSymbol? conflictingMethod)
        {
            if (conflictingMethod is null)
                return false;

            if (method.Parameters.Length != conflictingMethod.Parameters.Length)
                return false;

            if (method.ReturnsVoid != conflictingMethod.ReturnsVoid)
                return false;

            if (method.Parameters.Zip(conflictingMethod.Parameters, (p1, p2) => (p1, p2)).Any(
                    t => compilation.ClassifyCommonConversion(t.p1.Type, t.p2.Type) is not { IsImplicit: true, Exists: true }))
            {
                return false;
            }

            if (method.ReturnsVoid)
                return true;

            return compilation.ClassifyCommonConversion(conflictingMethod.ReturnType, method.ReturnType) is { IsImplicit: true, Exists: true };
        }
    }
}
