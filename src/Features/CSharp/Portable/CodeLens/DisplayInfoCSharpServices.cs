// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics
{
    [ExportLanguageService(typeof(IDisplayInfoLanguageServices), LanguageNames.CSharp), Shared]
    internal sealed class DisplayInfoCSharpServices : IDisplayInfoLanguageServices
    {
        private const SymbolDisplayMiscellaneousOptions MiscOptions =
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
            SymbolDisplayMiscellaneousOptions.UseAsterisksInMultiDimensionalArrays |
            SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName;

        private const SymbolDisplayMemberOptions MemberOptions =
            SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeContainingType;

        // Matches default ToDisplayString
        private static readonly SymbolDisplayFormat DefaultFormat = new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions: MemberOptions,
                parameterOptions: SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeType,
                miscellaneousOptions: MiscOptions);

        // Matches default ToDisplayString except removing global namspace and namespaces
        private static readonly SymbolDisplayFormat ShortFormat = new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions: MemberOptions,
                parameterOptions: SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeType,
                miscellaneousOptions: MiscOptions);

        /// <summary>
        /// Indicates if the given node is a declaration of some kind of symbol. 
        /// For example a class for a method declaration.
        /// </summary>
        public bool IsDeclaration(SyntaxNode node)
        {
            return IsTypeOrNamespaceDeclaration(node) || IsMemberDeclaration(node);
        }

        /// <summary>
        /// Indicates if the given node is a namespace import.
        /// </summary>
        public bool IsDirectiveOrImport(SyntaxNode node)
        {
            if (node.IsKind(SyntaxKind.UsingDirective) ||
                node.IsKind(SyntaxKind.ExternAliasDirective))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Indicates if the given node is an assembly level attribute "[assembly: MyAttribute]"
        /// </summary>
        public bool IsGlobalAttribute(SyntaxNode node)
        {
            if (node.IsKind(SyntaxKind.Attribute) &&
                node.Parent.IsKind(SyntaxKind.AttributeList))
            {
                var attributeListNode = (AttributeListSyntax)node.Parent;
                if (attributeListNode.Target != null)
                {
                    return attributeListNode.Target.Identifier.IsKind(SyntaxKind.AssemblyKeyword);
                }
            }

            return false;
        }

        /// <summary>
        /// Indicates if given node is DocumentationCommentTriviaSyntax
        /// </summary>
        public bool IsDocumentationComment(SyntaxNode node)
        {
            return node.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                   node.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia);
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
        public string GetDisplayName(
            SemanticModel semanticModel,
            SyntaxNode node,
            DisplayFormat displayFormat)
        {
            if (node == null)
            {
                return CSharpFeaturesResources.unknown_value;
            }

            SymbolDisplayFormat symbolDisplayFormat = DefaultFormat;
            if (displayFormat == DisplayFormat.Short)
            {
                symbolDisplayFormat = ShortFormat;
            }

            string displayName;
            string enclosingScopeString;

            int lastDotBeforeSpaceIndex;

            ISymbol symbol;

            if (IsGlobalAttribute(node))
            {
                return "assembly: " + node;
            }

            // Don't discriminate between getters and setters for indexers
            if (node.Parent != null && node.Parent.IsKind(SyntaxKind.AccessorList) &&
                node.Parent.Parent.IsKind(SyntaxKind.IndexerDeclaration))
            {
                return GetDisplayName(semanticModel, node.Parent.Parent, displayFormat);
            }

            switch (node.Kind())
            {
                case SyntaxKind.ConstructorDeclaration:
                    // The constructor's name will be the name of the class, not ctor like we want
                    symbol = semanticModel.GetDeclaredSymbol(node);
                    displayName = symbol.ToDisplayString(symbolDisplayFormat);
                    var openParenIndex = displayName.IndexOf('(');
                    int lastDotBeforeOpenParenIndex = displayName.LastIndexOf('.', openParenIndex, openParenIndex);

                    var constructorName = symbol.IsStatic ? "cctor" : "ctor";

                    displayName = displayName.Substring(0, lastDotBeforeOpenParenIndex + 1) +
                                         constructorName +
                                         displayName.Substring(openParenIndex);
                    break;

                case SyntaxKind.IndexerDeclaration:
                    // The name will be "namespace.class.this[type] - we want "namespace.class[type] Indexer"
                    symbol = semanticModel.GetDeclaredSymbol(node);
                    displayName = symbol.ToDisplayString(symbolDisplayFormat);
                    int openBracketIndex = displayName.IndexOf('[');
                    var lastDotBeforeOpenBracketIndex = displayName.LastIndexOf('.', openBracketIndex, openBracketIndex);

                    displayName = displayName.Substring(0, lastDotBeforeOpenBracketIndex) +
                                         displayName.Substring(openBracketIndex) +
                                         " Indexer";
                    break;

                case SyntaxKind.OperatorDeclaration:
                    // The name will be "namespace.class.operator +(type)" - we want namespace.class.+(type) Operator
                    symbol = semanticModel.GetDeclaredSymbol(node);
                    displayName = symbol.ToDisplayString(symbolDisplayFormat);
                    int spaceIndex = displayName.IndexOf(' ');
                    lastDotBeforeSpaceIndex = displayName.LastIndexOf('.', spaceIndex, spaceIndex);

                    displayName = displayName.Substring(0, lastDotBeforeSpaceIndex + 1) +
                                         displayName.Substring(spaceIndex + 1) +
                                         " Operator";
                    break;

                case SyntaxKind.ConversionOperatorDeclaration:
                    // The name will be "namespace.class.operator +(type)" - we want namespace.class.+(type) Operator
                    symbol = semanticModel.GetDeclaredSymbol(node);
                    displayName = symbol.ToDisplayString(symbolDisplayFormat);
                    int firstSpaceIndex = displayName.IndexOf(' ');
                    int secondSpaceIndex = displayName.IndexOf(' ', firstSpaceIndex + 1);
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

        private static bool IsTypeOrNamespaceDeclaration(SyntaxNode node)
        {
            // From the C# language spec:
            // type-declaration:
            //     class-declaration
            //     struct-declaration
            //     interface-declaration
            //     enum-declaration
            //     delegate-declaration
            switch (node.Kind())
            {
                case SyntaxKind.NamespaceDeclaration:
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.EnumDeclaration:
                case SyntaxKind.DelegateDeclaration:
                    return true;

                default:
                    return false;
            }
        }

        private static bool IsMemberDeclaration(SyntaxNode node)
        {
            // From the C# language spec:
            // class-member-declaration:
            //    constant-declaration
            //    field-declaration
            //    method-declaration
            //    property-declaration
            //    event-declaration
            //    indexer-declaration
            //    operator-declaration
            //    constructor-declaration
            //    destructor-declaration
            //    static-constructor-declaration
            //    type-declaration
            switch (node.Kind())
            {
                // Because fields declarations can define multiple symbols "public int a, b;" 
                // We want to get the VariableDeclarator node inside the field declaration to print out the symbol for the name.
                case SyntaxKind.VariableDeclarator:
                    return node.Parent.Parent.IsKind(SyntaxKind.FieldDeclaration) ||
                           node.Parent.Parent.IsKind(SyntaxKind.EventFieldDeclaration);

                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.EventDeclaration:
                case SyntaxKind.EventFieldDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                case SyntaxKind.IndexerDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                    return true;

                default:
                    return false;
            }
        }

        private static string GetEnclosingScopeString(SyntaxNode node, SemanticModel semanticModel, SymbolDisplayFormat symbolDisplayFormat)
        {
            SyntaxNode scopeNode = node;
            while (scopeNode != null && !IsTypeOrNamespaceDeclaration(scopeNode))
            {
                scopeNode = scopeNode.Parent;
            }

            if (scopeNode == null)
            {
                return null;
            }

            ISymbol scopeSymbol = semanticModel.GetDeclaredSymbol(scopeNode);
            return scopeSymbol.ToDisplayString(symbolDisplayFormat);
        }

        private static string GetSymbolDisplayString(SyntaxNode node, SemanticModel semanticModel, SymbolDisplayFormat symbolDisplayFormat)
        {
            if (node == null)
            {
                return CSharpFeaturesResources.unknown_value;
            }

            ISymbol symbol = semanticModel.GetDeclaredSymbol(node);
            return symbol != null ? symbol.ToDisplayString(symbolDisplayFormat) : CSharpFeaturesResources.unknown_value;
        }
    }
}
