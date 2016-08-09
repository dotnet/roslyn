// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.CodeLens
{
    [ExportLanguageService(typeof(ICodeLensDisplayInfoService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpCodeLensDisplayInfoService : ICodeLensDisplayInfoService
    {
        private static readonly SymbolDisplayFormat DefaultFormat =
            SymbolDisplayFormat.CSharpErrorMessageFormat.RemoveMemberOptions(
                SymbolDisplayMemberOptions.IncludeExplicitInterface);

        // Matches default ToDisplayString except removing global namspace and namespaces
        private static readonly SymbolDisplayFormat ShortFormat =
            DefaultFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)
                .WithTypeQualificationStyle(SymbolDisplayTypeQualificationStyle.NameAndContainingTypes);

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
                        var localDeclarationNode = (LocalDeclarationStatementSyntax) node;
                        node = localDeclarationNode.Declaration.Variables.First();
                        continue;

                        // Field and event declarations do not have symbols themselves, you need a variable declarator
                    case SyntaxKind.FieldDeclaration:
                    case SyntaxKind.EventFieldDeclaration:
                        var fieldNode = (BaseFieldDeclarationSyntax) node;
                        node = fieldNode.Declaration.Variables.First();
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
                            var structuredTriviaSyntax = (StructuredTriviaSyntax) node;
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
        public string GetDisplayName(SemanticModel semanticModel, SyntaxNode node, bool useShortName)
        {
            if (node == null)
            {
                return CSharpFeaturesResources.paren_unknown_paren;
            }

            var symbolDisplayFormat = useShortName ? ShortFormat : DefaultFormat;
            string displayName;
            string enclosingScopeString;

            int lastDotBeforeSpaceIndex;

            ISymbol symbol;

            if (SyntaxFacts.IsGlobalAttribute(node))
            {
                return "assembly: " + node;
            }

            // Don't discriminate between getters and setters for indexers
            if (node.Parent.IsKind(SyntaxKind.AccessorList) &&
                node.Parent.Parent.IsKind(SyntaxKind.IndexerDeclaration))
            {
                return GetDisplayName(semanticModel, node.Parent.Parent, useShortName);
            }

            switch (node.Kind())
            {
                case SyntaxKind.ConstructorDeclaration:
                    // The constructor's name will be the name of the class, not ctor like we want
                    symbol = semanticModel.GetDeclaredSymbol(node);
                    displayName = symbol.ToDisplayString(symbolDisplayFormat);
                    var openParenIndex = displayName.IndexOf('(');
                    var lastDotBeforeOpenParenIndex = displayName.LastIndexOf('.', openParenIndex, openParenIndex);

                    var constructorName = symbol.IsStatic ? "cctor" : "ctor";

                    displayName = displayName.Substring(0, lastDotBeforeOpenParenIndex + 1) +
                                         constructorName +
                                         displayName.Substring(openParenIndex);
                    break;

                case SyntaxKind.IndexerDeclaration:
                    // The name will be "namespace.class.this[type] - we want "namespace.class[type] Indexer"
                    symbol = semanticModel.GetDeclaredSymbol(node);
                    displayName = symbol.ToDisplayString(symbolDisplayFormat);
                    var openBracketIndex = displayName.IndexOf('[');
                    var lastDotBeforeOpenBracketIndex = displayName.LastIndexOf('.', openBracketIndex, openBracketIndex);

                    displayName = displayName.Substring(0, lastDotBeforeOpenBracketIndex) +
                                         displayName.Substring(openBracketIndex) +
                                         " Indexer";
                    break;

                case SyntaxKind.OperatorDeclaration:
                    // The name will be "namespace.class.operator +(type)" - we want namespace.class.+(type) Operator
                    symbol = semanticModel.GetDeclaredSymbol(node);
                    displayName = symbol.ToDisplayString(symbolDisplayFormat);
                    var spaceIndex = displayName.IndexOf(' ');
                    lastDotBeforeSpaceIndex = displayName.LastIndexOf('.', spaceIndex, spaceIndex);

                    displayName = displayName.Substring(0, lastDotBeforeSpaceIndex + 1) +
                                         displayName.Substring(spaceIndex + 1) +
                                         " Operator";
                    break;

                case SyntaxKind.ConversionOperatorDeclaration:
                    // The name will be "namespace.class.operator +(type)" - we want namespace.class.+(type) Operator
                    symbol = semanticModel.GetDeclaredSymbol(node);
                    displayName = symbol.ToDisplayString(symbolDisplayFormat);
                    var firstSpaceIndex = displayName.IndexOf(' ');
                    var secondSpaceIndex = displayName.IndexOf(' ', firstSpaceIndex + 1);
                    lastDotBeforeSpaceIndex = displayName.LastIndexOf('.', firstSpaceIndex, firstSpaceIndex);

                    displayName = displayName.Substring(0, lastDotBeforeSpaceIndex + 1) +
                                         displayName.Substring(secondSpaceIndex + 1) +
                                         " Operator";
                    break;

                case SyntaxKind.UsingDirective:
                    // We want to see usings formatted as simply "Using", prefaced by the namespace they are in
                    enclosingScopeString = GetEnclosingScopeString(node, semanticModel, symbolDisplayFormat);
                    displayName = string.IsNullOrEmpty(enclosingScopeString) ? "Using" : enclosingScopeString + " Using";
                    break;

                case SyntaxKind.ExternAliasDirective:
                    // We want to see aliases formatted as "Alias", prefaced by their enclosing scope, if any
                    enclosingScopeString = GetEnclosingScopeString(node, semanticModel, symbolDisplayFormat);
                    displayName = string.IsNullOrEmpty(enclosingScopeString) ? "Alias" : enclosingScopeString + " Alias";
                    break;

                default:
                    displayName = GetSymbolDisplayString(node, semanticModel, symbolDisplayFormat);
                    break;
            }

            return displayName;
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

        private static string GetSymbolDisplayString(SyntaxNode node, SemanticModel semanticModel, SymbolDisplayFormat symbolDisplayFormat)
        {
            if (node == null)
            {
                return CSharpFeaturesResources.paren_unknown_paren;
            }

            var symbol = semanticModel.GetDeclaredSymbol(node);
            return symbol != null ? symbol.ToDisplayString(symbolDisplayFormat) : CSharpFeaturesResources.paren_unknown_paren;
        }
    }
}
