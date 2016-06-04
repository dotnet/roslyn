// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ArgumentSyntax
    {
        /// <summary>
        /// Get expression representing the value of the argument, or null if arguments declares 
        /// an out variable.
        /// </summary>
        public ExpressionSyntax Expression
        {
            get
            {
                return ExpressionOrDeclaration as ExpressionSyntax;
            }
        }

        public ArgumentSyntax WithExpression(ExpressionSyntax expression)
        {
            return this.WithExpressionOrDeclaration(expression);
        }

        public ArgumentSyntax WithDeclaration(VariableDeclarationSyntax declaration)
        {
            if (this.RefOrOutKeyword.Kind() != SyntaxKind.OutKeyword)
            {
                throw new InvalidOperationException();
            }

            return this.WithExpressionOrDeclaration(declaration);
        }

        /// <summary>
        /// Get declaration of an Out Variable for the argument, or null
        /// if it doesn't declare any.
        /// </summary>
        public VariableDeclarationSyntax Declaration
        {
            get
            {
                return ExpressionOrDeclaration as VariableDeclarationSyntax;
            }
        }

        /// <summary>
        /// Get identifier for out variable declaration, or default(SyntaxToken).
        /// </summary>
        internal SyntaxToken Identifier
        {
            get
            {
                VariableDeclarationSyntax declaration = Declaration;
                if (declaration != null)
                {
                    return declaration.Variables.First().Identifier;
                }

                return default(SyntaxToken);
            }
        }

        /// <summary>
        /// Get type syntax for out variable declaration, or null.
        /// </summary>
        internal TypeSyntax Type
        {
            get
            {
                VariableDeclarationSyntax declaration = Declaration;
                if (declaration != null)
                {
                    return declaration.Type;
                }

                return null;
            }
        }

        public ArgumentSyntax Update(NameColonSyntax nameColon, SyntaxToken refOrOutKeyword, ExpressionSyntax expression)
        {
            return this.Update(nameColon, refOrOutKeyword, (CSharpSyntaxNode)expression) ;
        }

        public ArgumentSyntax Update(NameColonSyntax nameColon, SyntaxToken outKeyword, VariableDeclarationSyntax declaration)
        {
            return this.Update(nameColon, outKeyword, (CSharpSyntaxNode)declaration);
        }

        internal static bool IsValidOutVariableDeclaration(VariableDeclarationSyntax declaration)
        {
            return (declaration.Variables.Count == 1 &&
                    declaration.Variables.First().ArgumentList == null &&
                    declaration.Variables.First().Initializer == null);
        }

        internal static bool IsIdentifierOfOutVariableDeclaration(SyntaxToken identifier, out ArgumentSyntax declaringArgument)
        {
            Debug.Assert(identifier.Kind() == SyntaxKind.IdentifierToken || identifier.Kind() == SyntaxKind.None);
            SyntaxNode parent;

            if ((parent = identifier.Parent)?.Kind() == SyntaxKind.VariableDeclarator &&
                (parent = parent.Parent)?.Kind() == SyntaxKind.VariableDeclaration &&
                (parent = parent.Parent)?.Kind() == SyntaxKind.Argument)
            {
                var argument = (ArgumentSyntax)parent;

                if (argument.Identifier == identifier)
                {
                    declaringArgument = argument;
                    return true;
                }
            }

            declaringArgument = null;
            return false;
        }

        partial void ValidateWithRefOrOutKeywordInput(SyntaxToken refOrOutKeyword)
        {
            if (ExpressionOrDeclaration.Kind() == SyntaxKind.VariableDeclaration && refOrOutKeyword.Kind() != SyntaxKind.OutKeyword)
            {
                throw new ArgumentException(nameof(refOrOutKeyword));
            }
        }
    }
}
