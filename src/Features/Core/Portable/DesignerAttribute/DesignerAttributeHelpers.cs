// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DesignerAttribute
{
    internal static class DesignerAttributeHelpers
    {
        public static async Task<string?> ComputeDesignerAttributeCategoryAsync(
            AsyncLazy<bool> lazyHasDesignerCategoryType,
            Document document,
            CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            // Legacy behavior.  We only register the designer info for the first non-nested class
            // in the file.
            var firstClass = FindFirstNonNestedClass(syntaxFacts.GetMembersOfCompilationUnit(root));
            if (firstClass == null)
                return null;

            // simple case.  If there's no DesignerCategory type in this compilation, then there's
            // definitely no designable types.
            var hasDesignerCategoryType = await lazyHasDesignerCategoryType.GetValueAsync(cancellationToken).ConfigureAwait(false);
            if (!hasDesignerCategoryType)
                return null;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var firstClassType = (INamedTypeSymbol)semanticModel.GetRequiredDeclaredSymbol(firstClass, cancellationToken);

            foreach (var type in firstClassType.GetBaseTypesAndThis())
            {
                cancellationToken.ThrowIfCancellationRequested();

                // See if it has the designer attribute on it. Use symbol-equivalence instead of direct equality
                // as the symbol we have 
                var attribute = type.GetAttributes().FirstOrDefault(d => IsDesignerAttribute(d.AttributeClass));
                if (attribute is { ConstructorArguments: [{ Type.SpecialType: SpecialType.System_String, Value: string stringValue }] })
                    return stringValue.Trim();
            }

            return null;

            static bool IsDesignerAttribute(INamedTypeSymbol? attributeClass)
                => attributeClass is
                {
                    Name: nameof(DesignerCategoryAttribute),
                    ContainingNamespace.Name: nameof(System.ComponentModel),
                    ContainingNamespace.ContainingNamespace.Name: nameof(System),
                    ContainingNamespace.ContainingNamespace.ContainingNamespace.IsGlobalNamespace: true,
                };

            SyntaxNode? FindFirstNonNestedClass(SyntaxList<SyntaxNode> members)
            {
                foreach (var member in members)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (syntaxFacts.IsBaseNamespaceDeclaration(member))
                    {
                        var firstClass = FindFirstNonNestedClass(syntaxFacts.GetMembersOfBaseNamespaceDeclaration(member));
                        if (firstClass != null)
                            return firstClass;
                    }
                    else if (syntaxFacts.IsClassDeclaration(member))
                    {
                        return member;
                    }
                }

                return null;
            }
        }
    }
}
