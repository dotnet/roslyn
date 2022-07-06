﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.SourceGeneration;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class CSharpSyntaxHelper : AbstractSyntaxHelper
    {
        public static readonly ISyntaxHelper Instance = new CSharpSyntaxHelper();

        private CSharpSyntaxHelper()
        {
        }

        public override bool IsCaseSensitive
            => true;

        public override int AttributeListKind
            => (int)SyntaxKind.AttributeList;

        public override bool IsValidIdentifier(string name)
            => SyntaxFacts.IsValidIdentifier(name);

        public override bool IsAnyNamespaceBlock(SyntaxNode node)
            => node is BaseNamespaceDeclarationSyntax;

        public override bool IsAttribute(SyntaxNode node)
            => node is AttributeSyntax;

        public override SyntaxNode GetNameOfAttribute(SyntaxNode node)
            => ((AttributeSyntax)node).Name;

        public override bool IsAttributeList(SyntaxNode node)
            => node is AttributeListSyntax;

        public override void AddAttributeTargets(SyntaxNode node, ArrayBuilder<SyntaxNode> targets)
        {
            var attributeList = (AttributeListSyntax)node;
            var container = attributeList.Parent;
            RoslynDebug.AssertNotNull(container);

            // For fields/events, the attribute applies to all the variables declared.
            if (container is FieldDeclarationSyntax field)
                targets.AddRange(field.Declaration.Variables);
            else if (container is EventFieldDeclarationSyntax ev)
                targets.AddRange(ev.Declaration.Variables);
            else
                targets.Add(container);
        }

        public override SeparatedSyntaxList<SyntaxNode> GetAttributesOfAttributeList(SyntaxNode node)
            => ((AttributeListSyntax)node).Attributes;

        public override bool IsLambdaExpression(SyntaxNode node)
            => node is LambdaExpressionSyntax;

        public override SyntaxToken GetUnqualifiedIdentifierOfName(SyntaxNode node)
            => ((NameSyntax)node).GetUnqualifiedName().Identifier;

        public override void AddAliases(GreenNode node, ArrayBuilder<(string aliasName, string symbolName)> aliases, bool global)
        {
            if (node is Syntax.InternalSyntax.CompilationUnitSyntax compilationUnit)
            {
                AddAliases(compilationUnit.Usings, aliases, global);
            }
            else if (node is Syntax.InternalSyntax.BaseNamespaceDeclarationSyntax namespaceDeclaration)
            {
                AddAliases(namespaceDeclaration.Usings, aliases, global);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(node.KindText);
            }
        }

        private static void AddAliases(
            CodeAnalysis.Syntax.InternalSyntax.SyntaxList<Syntax.InternalSyntax.UsingDirectiveSyntax> usings,
            ArrayBuilder<(string aliasName, string symbolName)> aliases,
            bool global)
        {
            foreach (var usingDirective in usings)
            {
                if (usingDirective.Alias is null)
                    continue;

                if (global != (usingDirective.GlobalKeyword != null))
                    continue;

                var aliasName = usingDirective.Alias.Name.Identifier.ValueText;
                var symbolName = GetUnqualifiedName(usingDirective.Name).Identifier.ValueText;
                aliases.Add((aliasName, symbolName));
            }
        }

        private static Syntax.InternalSyntax.SimpleNameSyntax GetUnqualifiedName(Syntax.InternalSyntax.NameSyntax name)
            => name switch
            {
                Syntax.InternalSyntax.AliasQualifiedNameSyntax alias => alias.Name,
                Syntax.InternalSyntax.QualifiedNameSyntax qualified => qualified.Right,
                Syntax.InternalSyntax.SimpleNameSyntax simple => simple,
                _ => throw ExceptionUtilities.UnexpectedValue(name.KindText),
            };

        public override void AddAliases(CompilationOptions compilation, ArrayBuilder<(string aliasName, string symbolName)> aliases)
        {
            // C# doesn't have global aliases at the compilation level.
            return;
        }
    }
}
