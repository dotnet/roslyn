// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GenerateDefaultConstructors;

internal abstract partial class AbstractGenerateDefaultConstructorsService<TService>
{
    private sealed class GenerateDefaultConstructorsCodeAction(
        Document document,
        State state,
        string title,
        ImmutableArray<IMethodSymbol> constructors) : CodeAction
    {
        private readonly ImmutableArray<IMethodSymbol> _constructors = constructors;
        private readonly Document _document = document;
        private readonly State _state = state;

        public override string Title => title;

        protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_state.ClassType);
            var result = await CodeGenerator.AddMemberDeclarationsAsync(
                new CodeGenerationSolutionContext(
                    _document.Project.Solution,
                    CodeGenerationContext.Default),
                _state.ClassType,
                _constructors.Select(CreateConstructorDefinition),
                cancellationToken).ConfigureAwait(false);

            return result;
        }

        private IMethodSymbol CreateConstructorDefinition(
            IMethodSymbol baseConstructor)
        {
            var syntaxFactory = _document.GetRequiredLanguageService<SyntaxGenerator>();
            var baseConstructorArguments = baseConstructor.Parameters.Length != 0
                ? syntaxFactory.CreateArguments(baseConstructor.Parameters)
                : default;

            var classType = _state.ClassType;
            Contract.ThrowIfNull(classType);

            var accessibility = DetermineAccessibility(baseConstructor, classType);
            return CodeGenerationSymbolFactory.CreateConstructorSymbol(
                attributes: default,
                accessibility: accessibility,
                modifiers: DeclarationModifiers.None,
                typeName: classType.Name,
                parameters: baseConstructor.Parameters.SelectAsArray(p => WithoutInaccessibleAttributes(p, classType)),
                statements: default,
                baseConstructorArguments: baseConstructorArguments);
        }

        private static IParameterSymbol WithoutInaccessibleAttributes(
            IParameterSymbol parameter, INamedTypeSymbol classType)
        {
            return CodeGenerationSymbolFactory.CreateParameterSymbol(
                parameter, parameter.GetAttributes().WhereAsArray(a => a.AttributeClass is null || a.AttributeClass.IsAccessibleWithin(classType)));
        }

        private static Accessibility DetermineAccessibility(IMethodSymbol baseConstructor, INamedTypeSymbol classType)
        {
            // If our base is abstract, and we are not, then (since we likely want to be
            // instantiated) we make our constructor public by default.
            if (baseConstructor.ContainingType.IsAbstractClass() && !classType.IsAbstractClass())
                return Accessibility.Public;

            // If our base constructor is public, and we're abstract, we switch to being
            // protected as that's a more natural default for constructors in abstract classes.
            if (classType.IsAbstractClass() && baseConstructor.DeclaredAccessibility == Accessibility.Public)
                return Accessibility.Protected;

            if (classType.IsSealed)
            {
                // remove protected as it makes no sense in a sealed type.
                switch (baseConstructor.DeclaredAccessibility)
                {
                    case Accessibility.Protected:
                        return Accessibility.Public;
                    case Accessibility.ProtectedAndInternal:
                    case Accessibility.ProtectedOrInternal:
                        return Accessibility.Internal;
                }
            }

            // Defer to whatever the base constructor was declared as.
            return baseConstructor.DeclaredAccessibility;
        }
    }
}
