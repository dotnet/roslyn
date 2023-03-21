// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SourceGeneration;
using Roslyn.Utilities;

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

        protected override int AttributeListKind
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

        public override string GetUnqualifiedIdentifierOfName(SyntaxNode node)
            => ((NameSyntax)node).GetUnqualifiedName().Identifier.ValueText;

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

                // We only care about aliases from one name to another name.  e.g. `using X = A.B.C;`  That's because
                // the caller is only interested in finding a fully-qualified-metadata-name to an attribute.
                if (usingDirective.NamespaceOrType is not Syntax.InternalSyntax.NameSyntax name)
                    continue;

                var aliasName = usingDirective.Alias.Name.Identifier.ValueText;
                var symbolName = GetUnqualifiedName(name).Identifier.ValueText;
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
            // C# doesn't have global aliases at the compilation-options, only the compilation-unit level.
            return;
        }

        public override bool ContainsGlobalAliases(SyntaxNode root)
        {
            // Walk down the green tree to avoid unnecessary allocations of red nodes.
            //
            // Global usings can only exist at the compilation-unit level, so no need to dive any deeper than that.
            var compilationUnit = (Syntax.InternalSyntax.CompilationUnitSyntax)root.Green;

            foreach (var directive in compilationUnit.Usings)
            {
                if (directive.GlobalKeyword != null &&
                    directive.Alias != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
