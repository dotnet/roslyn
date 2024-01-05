// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;

namespace Microsoft.CodeAnalysis.CSharp.Snippets
{
    internal abstract class AbstractCSharpMainMethodSnippetProvider : AbstractMainMethodSnippetProvider
    {
        protected override async Task<bool> IsValidSnippetLocationAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            var syntaxContext = (CSharpSyntaxContext)document.GetRequiredLanguageService<ISyntaxContextService>().CreateContext(document, semanticModel, position, cancellationToken);

            if (!syntaxContext.IsMemberDeclarationContext(
                validModifiers: SyntaxKindSet.AccessibilityModifiers,
                validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations,
                canBePartial: true,
                cancellationToken: cancellationToken))
            {
                return false;
            }

            // Syntactically correct position, now semantic checks

            var enclosingTypeSymbol = semanticModel.GetDeclaredSymbol(syntaxContext.ContainingTypeDeclaration!, cancellationToken);

            // If there are any members with name `Main` in enclosing type, inserting `Main` method will create an error
            if (enclosingTypeSymbol is not null &&
                !semanticModel.LookupSymbols(position, container: enclosingTypeSymbol, name: WellKnownMemberNames.EntryPointMethodName).IsEmpty)
            {
                return false;
            }

            // If compilation already has top-level statements, suppress showing `Main` method snippets
            return semanticModel.Compilation.GetTopLevelStatementsMethod() is null;
        }
    }
}
