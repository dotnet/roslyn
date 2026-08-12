// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpToVisualBasicConverter.Utilities
{
    internal static class CSharpExtensions
    {
        public static IEnumerable<T> GetAncestorsOrThis<T>(this SyntaxNode node, bool allowStructuredTrivia = false)
            where T : SyntaxNode
        {
            SyntaxNode current = node;
            while (current != null)
            {
                if (current is T)
                {
                    yield return (T)current;
                }

                if (allowStructuredTrivia &&
                    current.IsStructuredTrivia &&
                    current.Parent == null)
                {
                    StructuredTriviaSyntax structuredTrivia = (StructuredTriviaSyntax)current;
                    SyntaxTrivia parentTrivia = structuredTrivia.ParentTrivia;
                    current = parentTrivia.Token.Parent;
                }
                else
                {
                    current = current.Parent;
                }
            }
        }

        public static SyntaxNode GetParent(this SyntaxTree syntaxTree, SyntaxNode node) => node?.Parent;

        public static TypeSyntax GetVariableType(this VariableDeclaratorSyntax variable)
        {
            if (!(variable.Parent is VariableDeclarationSyntax parent))
            {
                return null;
            }

            return parent.Type;
        }

        public static bool IsBreakableConstruct(this SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.DoStatement:
                case SyntaxKind.WhileStatement:
                case SyntaxKind.SwitchStatement:
                case SyntaxKind.ForStatement:
                case SyntaxKind.ForEachStatement:
                    return true;
            }

            return false;
        }

        public static bool IsContinuableConstruct(this SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.DoStatement:
                case SyntaxKind.WhileStatement:
                case SyntaxKind.ForStatement:
                case SyntaxKind.ForEachStatement:
                    return true;
            }

            return false;
        }

        public static bool IsParentKind(this SyntaxNode node, SyntaxKind kind)
            => node?.Parent.IsKind(kind) == true;
    }
}
