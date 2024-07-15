// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateDefaultConstructors;

internal abstract partial class AbstractGenerateDefaultConstructorsService<TService>
{
    private abstract class AbstractCodeAction : CodeAction
    {
        private readonly IList<IMethodSymbol> _constructors;
        private readonly Document _document;
        private readonly State _state;
        private readonly string _title;
        private readonly CodeAndImportGenerationOptionsProvider _fallbackOptions;

        protected AbstractCodeAction(
            Document document,
            State state,
            IList<IMethodSymbol> constructors,
            string title,
            CodeAndImportGenerationOptionsProvider fallbackOptions)
        {
            _document = document;
            _state = state;
            _constructors = constructors;
            _title = title;
            _fallbackOptions = fallbackOptions;
        }

        public override string Title => _title;

        protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_state.ClassType);
            var result = await CodeGenerator.AddMemberDeclarationsAsync(
                new CodeGenerationSolutionContext(
                    _document.Project.Solution,
                    CodeGenerationContext.Default,
                    _fallbackOptions),
                _state.ClassType,
                _constructors.Select(CreateConstructorDefinition),
                cancellationToken).ConfigureAwait(false);

            return result;
        }

        private IMethodSymbol CreateConstructorDefinition(
            IMethodSymbol baseConstructor)
        {
            var syntaxFactory = _document.GetLanguageService<SyntaxGenerator>();
            var baseConstructorArguments = baseConstructor.Parameters.Length != 0
                ? syntaxFactory.CreateArguments(baseConstructor.Parameters)
                : default;

            var classType = _state.ClassType;
            Contract.ThrowIfNull(classType);

            var accessibility = DetermineAccessibility(baseConstructor, classType);
            return CodeGenerationSymbolFactory.CreateConstructorSymbol(
                attributes: default,
                accessibility: accessibility,
                modifiers: new DeclarationModifiers(),
                typeName: classType.Name,
                parameters: baseConstructor.Parameters,
                statements: default,
                baseConstructorArguments: baseConstructorArguments);
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
