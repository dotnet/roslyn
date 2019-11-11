// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.ImplementInterface
{
    internal static class Helpers
    {
        public static readonly SymbolDisplayFormat NameAndTypeParametersFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

        public static async Task<(SyntaxNode, ExplicitInterfaceSpecifierSyntax?, SyntaxToken)> GetContainerAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(span.Start);

            // Move back if the user is at: X.Goo$$(
            if (span.IsEmpty && token.Kind() == SyntaxKind.OpenParenToken)
                token = token.GetPreviousToken();

            var (container, explicitName, identifier) = GetContainer(token);
            var applicableSpan = explicitName == null
                ? identifier.FullSpan
                : TextSpan.FromBounds(explicitName.FullSpan.Start, identifier.FullSpan.End);

            if (!applicableSpan.Contains(span))
                return default;

            return (container, explicitName, identifier);
        }

        private static (SyntaxNode, ExplicitInterfaceSpecifierSyntax?, SyntaxToken) GetContainer(SyntaxToken token)
        {
            for (var node = token.Parent; node != null; node = node.Parent)
            {
                switch (node)
                {
                    case MethodDeclarationSyntax method:
                        return (method, method.ExplicitInterfaceSpecifier, method.Identifier);
                    case PropertyDeclarationSyntax property:
                        return (property, property.ExplicitInterfaceSpecifier, property.Identifier);
                    case EventDeclarationSyntax ev:
                        return (ev, ev.ExplicitInterfaceSpecifier, ev.Identifier);
                }
            }

            return default;
        }

    }
}
