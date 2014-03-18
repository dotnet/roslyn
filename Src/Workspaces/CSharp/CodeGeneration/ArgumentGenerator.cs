// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal class ArgumentGenerator : AbstractCSharpCodeGenerator
    {
        public static ArgumentSyntax GenerateArgument(SyntaxNode argument)
        {
            if (argument is ExpressionSyntax)
            {
                return SyntaxFactory.Argument((ExpressionSyntax)argument);
            }

            return (ArgumentSyntax)argument;
        }

        public static ArgumentListSyntax GenerateArgumentList(IList<SyntaxNode> arguments)
        {
            return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments.Select(GenerateArgument)));
        }

        public static BracketedArgumentListSyntax GenerateBracketedArgumentList(IList<SyntaxNode> arguments)
        {
            return SyntaxFactory.BracketedArgumentList(SyntaxFactory.SeparatedList(arguments.Select(GenerateArgument)));
        }
    }
}