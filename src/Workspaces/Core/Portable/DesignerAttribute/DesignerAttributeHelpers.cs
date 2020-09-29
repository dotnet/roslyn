// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.DesignerAttribute
{
    internal static class DesignerAttributeHelpers
    {
        public static async Task<string?> ComputeDesignerAttributeCategoryAsync(
            INamedTypeSymbol? designerCategoryType,
            Document document,
            CancellationToken cancellationToken)
        {
            // simple case.  If there's no DesignerCategory type in this compilation, then there's
            // definitely no designable types.  Just immediately bail out.
            if (designerCategoryType == null)
                return null;

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            // Legacy behavior.  We only register the designer info for the first non-nested class
            // in the file.
            var firstClass = FindFirstNonNestedClass(
                syntaxFacts, syntaxFacts.GetMembersOfCompilationUnit(root), cancellationToken);
            if (firstClass == null)
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
                if (attribute?.ConstructorArguments.Length == 1)
                    return GetArgumentString(attribute.ConstructorArguments[0]);
            }

            return null;
        }

        private static SyntaxNode? FindFirstNonNestedClass(
            ISyntaxFactsService syntaxFacts, SyntaxList<SyntaxNode> members, CancellationToken cancellationToken)
        {
            foreach (var member in members)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (syntaxFacts.IsNamespaceDeclaration(member))
                {
                    var firstClass = FindFirstNonNestedClass(
                        syntaxFacts, syntaxFacts.GetMembersOfNamespaceDeclaration(member), cancellationToken);
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

        private static string? GetArgumentString(TypedConstant argument)
        {
            if (argument.Type == null ||
                argument.Type.SpecialType != SpecialType.System_String ||
                argument.IsNull ||
                !(argument.Value is string stringValue))
            {
                return null;
            }

            return stringValue.Trim();
        }
    }
}
