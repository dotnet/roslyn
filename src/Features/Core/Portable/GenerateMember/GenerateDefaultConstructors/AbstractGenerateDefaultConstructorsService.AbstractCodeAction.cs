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
            private readonly IList<IMethodSymbol> _constructors;
            private readonly Document _document;
            private readonly State _state;
            private readonly TService _service;
            private readonly string _title;

            protected AbstractCodeAction(
                TService service,
                Document document,
                State state,
                IList<IMethodSymbol> constructors,
                string title)
            {
                _service = service;
                _document = document;
                _state = state;
                _constructors = constructors;
                _title = title;
            }

            public override string Title
            {
                get { return _title; }
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var result = await CodeGenerator.AddMemberDeclarationsAsync(
                    _document.Project.Solution,
                    _state.ClassType,
                    _constructors.Select(CreateConstructorDefinition),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                return result;
            }

            private IMethodSymbol CreateConstructorDefinition(
                IMethodSymbol constructor)
            {
                var syntaxFactory = _document.GetLanguageService<SyntaxGenerator>();
                var baseConstructorArguments = constructor.Parameters.Length != 0
                    ? syntaxFactory.CreateArguments(constructor.Parameters)
                    : null;

                return CodeGenerationSymbolFactory.CreateConstructorSymbol(
                    attributes: null,
                    accessibility: constructor.DeclaredAccessibility,
                    modifiers: new DeclarationModifiers(),
                    typeName: _state.ClassType.Name,
                    parameters: constructor.Parameters,
                    statements: null,
                    baseConstructorArguments: baseConstructorArguments);
            }
        }
    }
}
