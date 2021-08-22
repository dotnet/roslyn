// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.GenerateDefaultConstructors
{
    internal abstract class AbstractGenerateDefaultConstructorCodeFixProvider
        : CodeFixProvider
    {
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var document = context.Document;
            var diagnostic = context.Diagnostics.FirstOrDefault();
            if (diagnostic == null)
                return;

            var node = diagnostic.Location.FindNode(cancellationToken);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            var typeDecl = node.AncestorsAndSelf().Where(n => syntaxFacts.IsTypeDeclaration(n)).FirstOrDefault();
            if (typeDecl == null)
                return;

            var typeName = syntaxFacts.GetIdentifierOfTypeDeclaration(typeDecl);
            var service = document.GetRequiredLanguageService<IGenerateDefaultConstructorsService>();
            var actions = await service.GenerateDefaultConstructorsAsync(document, typeName.Span, cancellationToken).ConfigureAwait(false);
            context.RegisterFixes(actions, diagnostic);
        }
    }
}
