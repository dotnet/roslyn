// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.CodeLens
{
    [ExportLanguageService(typeof(ICodeLensDisplayInfoService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpCodeLensDisplayInfoService : ICodeLensDisplayInfoService
    {
        private static readonly SymbolDisplayFormat Format =
            SymbolDisplayFormat.CSharpErrorMessageFormat.RemoveMemberOptions(
                SymbolDisplayMemberOptions.IncludeExplicitInterface);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpCodeLensDisplayInfoService()
        {
        }

        /// <summary>
        /// Returns the node that should be displayed
        /// </summary>
        public SyntaxNode GetDisplayNode(SyntaxNode node)
        {
            while (true)
            {
                switch (node.Kind())
                {
                    // LocalDeclarations do not have symbols themselves, you need a variable declarator
                    case SyntaxKind.LocalDeclarationStatement:
                        var localDeclarationNode = (LocalDeclarationStatementSyntax)node;
                        node = localDeclarationNode.Declaration.Variables.FirstOrDefault();
                        continue;

                    // Field and event declarations do not have symbols themselves, you need a variable declarator
                    case SyntaxKind.FieldDeclaration:
                    case SyntaxKind.EventFieldDeclaration:
                        var fieldNode = (BaseFieldDeclarationSyntax)node;
                        node = fieldNode.Declaration.Variables.FirstOrDefault();
                        continue;

                    // Variable is a field without access modifier. Parent is FieldDeclaration
                    case SyntaxKind.VariableDeclaration:
                        node = node.Parent;
                        continue;

                    // Built in types   
                    case SyntaxKind.PredefinedType:
                        node = node.Parent;
                        continue;

                    case SyntaxKind.MultiLineDocumentationCommentTrivia:
                    case SyntaxKind.SingleLineDocumentationCommentTrivia:
                        // For DocumentationCommentTrivia node, node.Parent is null. Obtain parent through ParentTrivia.Token
                        if (node.IsStructuredTrivia)
                        {
                            var structuredTriviaSyntax = (StructuredTriviaSyntax)node;
                            node = structuredTriviaSyntax.ParentTrivia.Token.Parent;
                            continue;
                        }

                        return null;

                    default:
                        return node;
                }
            }
        }

        /// <summary>
        /// Gets the DisplayName for the given node.
        /// </summary>
        public string GetDisplayName(SemanticModel semanticModel, SyntaxNode node)
        {
            if (node == null)
            {
                return FeaturesResources.paren_Unknown_paren;
            }

            if (CSharpSyntaxFacts.Instance.IsGlobalAssemblyAttribute(node))
            {
                return "assembly: " + node.ConvertToSingleLine();
            }

            // Don't discriminate between getters and setters for indexers
            if (node.Parent.IsKind(SyntaxKind.AccessorList) &&
                node.Parent.Parent.IsKind(SyntaxKind.IndexerDeclaration))
            {
                return GetDisplayName(semanticModel, node.Parent.Parent);
            }

            switch (node.Kind())
            {
                case SyntaxKind.ConstructorDeclaration:
                    {
                        // The constructor's name will be the name of the class, not ctor like we want
                        var symbol = semanticModel.GetDeclaredSymbol(node);
                        var displayName = symbol.ToDisplayString(Format);
                        var openParenIndex = displayName.IndexOf('(');
                        var lastDotBeforeOpenParenIndex = displayName.LastIndexOf('.', openParenIndex, openParenIndex);

                        var constructorName = symbol.IsStatic ? "cctor" : "ctor";

                        return displayName[..(lastDotBeforeOpenParenIndex + 1)] +
                               constructorName +
                               displayName[openParenIndex..];
                    }

                case SyntaxKind.IndexerDeclaration:
                    {
                        // The name will be "namespace.class.this[type] - we want "namespace.class[type] Indexer"
                        var symbol = semanticModel.GetDeclaredSymbol(node);
                        var displayName = symbol.ToDisplayString(Format);
                        var openBracketIndex = displayName.IndexOf('[');
                        var lastDotBeforeOpenBracketIndex = displayName.LastIndexOf('.', openBracketIndex, openBracketIndex);

                        return displayName[..lastDotBeforeOpenBracketIndex] +
                               displayName[openBracketIndex..] +
                               " Indexer";
                    }

                case SyntaxKind.OperatorDeclaration:
                    {
                        // The name will be "namespace.class.operator +(type)" - we want namespace.class.+(type) Operator
                        var symbol = semanticModel.GetDeclaredSymbol(node);
                        var displayName = symbol.ToDisplayString(Format);
                        var spaceIndex = displayName.IndexOf(' ');
                        var lastDotBeforeSpaceIndex = displayName.LastIndexOf('.', spaceIndex, spaceIndex);

                        return displayName[..(lastDotBeforeSpaceIndex + 1)] +
                               displayName[(spaceIndex + 1)..] +
                               " Operator";
                    }

                case SyntaxKind.ConversionOperatorDeclaration:
                    {
                        // The name will be "namespace.class.operator +(type)" - we want namespace.class.+(type) Operator
                        var symbol = semanticModel.GetDeclaredSymbol(node);
                        var displayName = symbol.ToDisplayString(Format);
                        var firstSpaceIndex = displayName.IndexOf(' ');
                        var secondSpaceIndex = displayName.IndexOf(' ', firstSpaceIndex + 1);
                        var lastDotBeforeSpaceIndex = displayName.LastIndexOf('.', firstSpaceIndex, firstSpaceIndex);

                        return displayName[..(lastDotBeforeSpaceIndex + 1)] +
                               displayName[(secondSpaceIndex + 1)..] +
                               " Operator";
                    }

                case SyntaxKind.UsingDirective:
                    {
                        // We want to see usings formatted as simply "Using", prefaced by the namespace they are in
                        var enclosingScopeString = GetEnclosingScopeString(node, semanticModel, Format);
                        return string.IsNullOrEmpty(enclosingScopeString) ? "Using" : enclosingScopeString + " Using";
                    }

                case SyntaxKind.ExternAliasDirective:
                    {
                        // We want to see aliases formatted as "Alias", prefaced by their enclosing scope, if any
                        var enclosingScopeString = GetEnclosingScopeString(node, semanticModel, Format);
                        return string.IsNullOrEmpty(enclosingScopeString) ? "Alias" : enclosingScopeString + " Alias";
                    }

                default:
                    {
                        var symbol = semanticModel.GetDeclaredSymbol(node);
                        return symbol != null ? symbol.ToDisplayString(Format) : FeaturesResources.paren_Unknown_paren;
                    }
            }
        }

        private static string GetEnclosingScopeString(SyntaxNode node, SemanticModel semanticModel, SymbolDisplayFormat symbolDisplayFormat)
        {
            var scopeNode = node;
            while (scopeNode != null && !SyntaxFacts.IsNamespaceMemberDeclaration(scopeNode.Kind()))
            {
                scopeNode = scopeNode.Parent;
            }

            if (scopeNode == null)
            {
                return null;
            }

            var scopeSymbol = semanticModel.GetDeclaredSymbol(scopeNode);
            return scopeSymbol.ToDisplayString(symbolDisplayFormat);
        }
    }
}
