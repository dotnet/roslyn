// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.AliasAmbiguousType;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.AliasAmbiguousType
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AliasAmbiguousType), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.FullyQualify)]
    internal class CSharpAliasAmbiguousTypeCodeFixProvider : AbstractAliasAmbiguousTypeCodeFixProvider
    {
        /// <summary>
        /// 'reference' is an ambiguous reference between 'identifier' and 'identifier'
        /// </summary>
        private const string CS0104 = nameof(CS0104);

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(CS0104);

        protected override string GetTextPreviewOfChange(SyntaxNode aliasNode)
        {
            Debug.Assert(aliasNode is UsingDirectiveSyntax);
            // A poor man's name simplifier. For the preview of the context menu text the likely change is predicted by 
            // removing the global:: namespace alias if present. For the majority of cases this should be the same result
            // as what the real Simplifier produces in the preview pane and when the fix is applied.
            aliasNode = RemoveGlobalNamespaceAliasIfPresent(aliasNode);
            return aliasNode.NormalizeWhitespace().ToFullString();
        }

        NameSyntax GetLeftmostQualifiedName(NameSyntax nameSyntax)
        {
            while (nameSyntax is QualifiedNameSyntax qualifiedNameSyntax)
            {
                nameSyntax = qualifiedNameSyntax.Left;
            }

            return nameSyntax;
        }

        private SyntaxNode RemoveGlobalNamespaceAliasIfPresent(SyntaxNode aliasNode)
        {
            var usingDirective = (UsingDirectiveSyntax)aliasNode;
            var nameSyntax = usingDirective.Name;
            var leftmostName = GetLeftmostQualifiedName(nameSyntax);
            if (leftmostName is AliasQualifiedNameSyntax aliasQualifiedName &&
                aliasQualifiedName.Alias.Identifier.IsKind(SyntaxKind.GlobalKeyword))
            {
                if (aliasQualifiedName.Parent is QualifiedNameSyntax parentOfGlobalAlias)
                {
                    var replacement = parentOfGlobalAlias.WithLeft(SyntaxFactory.IdentifierName(aliasQualifiedName.Name.Identifier));
                    usingDirective = usingDirective.ReplaceNode(parentOfGlobalAlias, replacement);
                }
            }

            return usingDirective;
        }
    }
}
