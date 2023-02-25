// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            AsyncLazy<INamedTypeSymbol?> lazyDesignerCategoryType,
            Document document,
            CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            // Legacy behavior.  We only register the designer info for the first non-nested class
            // in the file.
            var firstClass = FindFirstNonNestedClass(
                syntaxFacts, syntaxFacts.GetMembersOfCompilationUnit(root), cancellationToken);
            if (firstClass == null)
                return null;

            // simple case.  If there's no DesignerCategory type in this compilation, then there's
            // definitely no designable types.
            var designerCategoryType = await lazyDesignerCategoryType.GetValueAsync(cancellationToken).ConfigureAwait(false);
            if (designerCategoryType == null)
                return null;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var firstClassType = (INamedTypeSymbol)semanticModel.GetRequiredDeclaredSymbol(firstClass, cancellationToken);
            return TryGetDesignerCategory(firstClassType, designerCategoryType, cancellationToken);
        }

        private static string? TryGetDesignerCategory(
            INamedTypeSymbol classType,
            INamedTypeSymbol designerCategoryType,
            CancellationToken cancellationToken)
        {
            foreach (var type in classType.GetBaseTypesAndThis())
            {
                cancellationToken.ThrowIfCancellationRequested();

                // if it has designer attribute, set it
                var attribute = type.GetAttributes().FirstOrDefault(d => designerCategoryType.Equals(d.AttributeClass));
                if (attribute is
                    {
                        ConstructorArguments:
                        [
                            {
                                Type.SpecialType: SpecialType.System_String,
                                Value: string stringValue,
                            }
                        ]
                    })
                {
                    return stringValue.Trim();
                }
            }

            return null;
        }

        private static SyntaxNode? FindFirstNonNestedClass(
            ISyntaxFactsService syntaxFacts, SyntaxList<SyntaxNode> members, CancellationToken cancellationToken)
        {
            foreach (var member in members)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (syntaxFacts.IsBaseNamespaceDeclaration(member))
                {
                    var firstClass = FindFirstNonNestedClass(
                        syntaxFacts, syntaxFacts.GetMembersOfBaseNamespaceDeclaration(member), cancellationToken);
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
