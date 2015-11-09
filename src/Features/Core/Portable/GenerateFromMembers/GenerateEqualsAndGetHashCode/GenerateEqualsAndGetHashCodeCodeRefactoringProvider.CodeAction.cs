// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateFromMembers.GenerateEqualsAndGetHashCode
{
    internal abstract partial class AbstractGenerateEqualsAndGetHashCodeService<TService, TMemberDeclarationSyntax>
    {
        private class GenerateEqualsAndHashCodeAction : CodeAction
        {
            private readonly bool _generateEquals;
            private readonly bool _generateGetHashCode;
            private readonly TService _service;
            private readonly Document _document;
            private readonly INamedTypeSymbol _containingType;
            private readonly IList<ISymbol> _selectedMembers;
            private readonly TextSpan _textSpan;

            public GenerateEqualsAndHashCodeAction(
                TService service,
                Document document,
                TextSpan textSpan,
                INamedTypeSymbol containingType,
                IList<ISymbol> selectedMembers,
                bool generateEquals = false,
                bool generateGetHashCode = false)
            {
                _service = service;
                _document = document;
                _containingType = containingType;
                _selectedMembers = selectedMembers;
                _textSpan = textSpan;
                _generateEquals = generateEquals;
                _generateGetHashCode = generateGetHashCode;
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var members = new List<ISymbol>();

                if (_generateEquals)
                {
                    var member = await CreateEqualsMethodAsync(cancellationToken).ConfigureAwait(false);
                    members.Add(member);
                }

                if (_generateGetHashCode)
                {
                    var member = await CreateGetHashCodeMethodAsync(cancellationToken).ConfigureAwait(false);
                    members.Add(member);
                }

                var syntaxTree = await _document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

                return await CodeGenerator.AddMemberDeclarationsAsync(
                    _document.Project.Solution,
                    _containingType,
                    members,
                    new CodeGenerationOptions(contextLocation: syntaxTree.GetLocation(_textSpan)),
                    cancellationToken)
                    .ConfigureAwait(false);
            }

            private async Task<IMethodSymbol> CreateGetHashCodeMethodAsync(CancellationToken cancellationToken)
            {
                var compilation = await _document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                return _document.GetLanguageService<SyntaxGenerator>().CreateGetHashCodeMethod(
                    compilation, _containingType, _selectedMembers, cancellationToken);
            }

            private async Task<IMethodSymbol> CreateEqualsMethodAsync(CancellationToken cancellationToken)
            {
                var compilation = await _document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                return _document.GetLanguageService<SyntaxGenerator>().CreateEqualsMethod(
                    compilation, _containingType, _selectedMembers, cancellationToken);
            }

            public override string Title
            {
                get
                {
                    return _generateEquals
                        ? _generateGetHashCode
                            ? FeaturesResources.GenerateBoth
                            : FeaturesResources.GenerateEqualsObject
                        : FeaturesResources.GenerateGetHashCode;
                }
            }
        }
    }
}
