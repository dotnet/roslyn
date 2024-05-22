// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.GenerateDefaultConstructors;

internal abstract class AbstractGenerateDefaultConstructorCodeFixProvider : CodeFixProvider
{
    public override FixAllProvider? GetFixAllProvider() => null;

    protected abstract SyntaxToken? TryGetTypeName(SyntaxNode typeDeclaration);

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var cancellationToken = context.CancellationToken;
        var document = context.Document;
        var diagnostic = context.Diagnostics.FirstOrDefault();
        if (diagnostic == null)
            return;

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var headerFacts = document.GetRequiredLanguageService<IHeaderFactsService>();
        if (!headerFacts.IsOnTypeHeader(root, diagnostic.Location.SourceSpan.Start, fullHeader: true, out var typeDecl))
            return;

        var typeName = TryGetTypeName(typeDecl);
        if (typeName == null)
            return;

        var service = document.GetRequiredLanguageService<IGenerateDefaultConstructorsService>();
        var actions = await service.GenerateDefaultConstructorsAsync(
            document, new TextSpan(typeName.Value.Span.Start, 0), context.Options, forRefactoring: false, cancellationToken).ConfigureAwait(false);
        context.RegisterFixes(actions, diagnostic);
    }
}
