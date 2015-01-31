// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateDefaultConstructors
{
    internal abstract partial class AbstractGenerateDefaultConstructorsService<TService>
    {
        private abstract class AbstractCodeAction : CodeAction
        {
            private readonly IList<IMethodSymbol> constructors;
            private readonly Document document;
            private readonly State state;
            private readonly TService service;
            private readonly string title;

            protected AbstractCodeAction(
                TService service,
                Document document,
                State state,
                IList<IMethodSymbol> constructors,
                string title)
            {
                this.service = service;
                this.document = document;
                this.state = state;
                this.constructors = constructors;
                this.title = title;
            }

            public override string Title
            {
                get { return this.title; }
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var result = await CodeGenerator.AddMemberDeclarationsAsync(
                    document.Project.Solution,
                    state.ClassType,
                    constructors.Select(CreateConstructorDefinition),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                return result;
            }

            private IMethodSymbol CreateConstructorDefinition(
                IMethodSymbol constructor)
            {
                var syntaxFactory = document.GetLanguageService<SyntaxGenerator>();
                var baseConstructorArguments = constructor.Parameters.Length != 0
                    ? syntaxFactory.CreateArguments(constructor.Parameters)
                    : null;

                return CodeGenerationSymbolFactory.CreateConstructorSymbol(
                    attributes: null,
                    accessibility: constructor.DeclaredAccessibility,
                    modifiers: new DeclarationModifiers(),
                    typeName: state.ClassType.Name,
                    parameters: constructor.Parameters,
                    statements: null,
                    baseConstructorArguments: baseConstructorArguments);
            }
        }
    }
}
