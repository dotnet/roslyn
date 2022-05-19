// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.SourceGeneration;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class CSharpSyntaxHelper : AbstractSyntaxHelper
    {
        public static readonly ISyntaxHelper Instance = new CSharpSyntaxHelper();

        private CSharpSyntaxHelper()
        {
        }

        public override bool IsCaseSensitive => true;

        public override bool IsValidIdentifier(string name)
            => SyntaxFacts.IsValidIdentifier(name);

        public override bool IsCompilationUnit(SyntaxNode node)
            => node is CompilationUnitSyntax;

        public override bool IsAnyNamespaceBlock(SyntaxNode node)
            => node is BaseNamespaceDeclarationSyntax;

        public override bool IsAttribute(SyntaxNode node)
            => node is AttributeSyntax;

        public override SyntaxNode GetNameOfAttribute(SyntaxNode attribute)
            => ((AttributeSyntax)attribute).Name;

        public override bool IsAttributeList(SyntaxNode node)
            => node is AttributeListSyntax;

        public override SeparatedSyntaxList<SyntaxNode> GetAttributesOfAttributeList(SyntaxNode attributeList)
            => ((AttributeListSyntax)attributeList).Attributes;

        public override SyntaxToken GetUnqualifiedIdentifierOfName(SyntaxNode name)
            => ((NameSyntax)name).GetUnqualifiedName().Identifier;

        public override void AddAliases(SyntaxNode node, ArrayBuilder<(string aliasName, string symbolName)> aliases, bool global)
        {
            if (node is CompilationUnitSyntax compilationUnit)
            {
                AddAliases(compilationUnit.Usings, aliases, global);
            }
            else if (node is BaseNamespaceDeclarationSyntax namespaceDeclaration)
            {
                AddAliases(namespaceDeclaration.Usings, aliases, global);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }

        private static void AddAliases(SyntaxList<UsingDirectiveSyntax> usings, ArrayBuilder<(string aliasName, string symbolName)> aliases, bool global)
        {
            foreach (var usingDirective in usings)
            {
                if (usingDirective.Alias is null)
                    continue;

                if (global != usingDirective.GlobalKeyword.Kind() is SyntaxKind.GlobalKeyword)
                    continue;

                var aliasName = usingDirective.Alias.Name.Identifier.ValueText;
                var symbolName = usingDirective.Name.GetUnqualifiedName().Identifier.ValueText;
                aliases.Add((aliasName, symbolName));
            }
        }
    }
}
