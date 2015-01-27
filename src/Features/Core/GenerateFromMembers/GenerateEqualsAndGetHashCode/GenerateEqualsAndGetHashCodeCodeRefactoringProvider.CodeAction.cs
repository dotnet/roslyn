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
            private readonly bool generateEquals;
            private readonly bool generateGetHashCode;
            private readonly TService service;
            private readonly Document document;
            private readonly INamedTypeSymbol containingType;
            private readonly IList<ISymbol> selectedMembers;
            private readonly TextSpan textSpan;

            public GenerateEqualsAndHashCodeAction(
                TService service,
                Document document,
                TextSpan textSpan,
                INamedTypeSymbol containingType,
                IList<ISymbol> selectedMembers,
                bool generateEquals = false,
                bool generateGetHashCode = false)
            {
                this.service = service;
                this.document = document;
                this.containingType = containingType;
                this.selectedMembers = selectedMembers;
                this.textSpan = textSpan;
                this.generateEquals = generateEquals;
                this.generateGetHashCode = generateGetHashCode;
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var members = new List<ISymbol>();

                if (this.generateEquals)
                {
                    var member = await CreateEqualsMethodAsync(cancellationToken).ConfigureAwait(false);
                    members.Add(member);
                }

                if (this.generateGetHashCode)
                {
                    var member = await CreateGetHashCodeMethodAsync(cancellationToken).ConfigureAwait(false);
                    members.Add(member);
                }

                var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

                return await CodeGenerator.AddMemberDeclarationsAsync(
                    document.Project.Solution,
                    containingType,
                    members,
                    new CodeGenerationOptions(contextLocation: syntaxTree.GetLocation(textSpan)),
                    cancellationToken)
                    .ConfigureAwait(false);
            }

            private async Task<IMethodSymbol> CreateGetHashCodeMethodAsync(CancellationToken cancellationToken)
            {
                var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                return document.GetLanguageService<SyntaxGenerator>().CreateGetHashCodeMethod(
                    compilation, containingType, selectedMembers, cancellationToken);
            }

            private async Task<IMethodSymbol> CreateEqualsMethodAsync(CancellationToken cancellationToken)
            {
                var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                return document.GetLanguageService<SyntaxGenerator>().CreateEqualsMethod(
                    compilation, containingType, selectedMembers, cancellationToken);
            }

            public override string Title
            {
                get
                {
                    return generateEquals
                        ? generateGetHashCode
                            ? FeaturesResources.GenerateBoth
                            : FeaturesResources.GenerateEqualsObject
                        : FeaturesResources.GenerateGetHashCode;
                }
            }
        }
    }
}
