﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal static class ArgumentGenerator
    {
        public static ArgumentSyntax GenerateArgument(SyntaxNode argument)
        {
            if (argument is ExpressionSyntax expression)
            {
                return SyntaxFactory.Argument(expression);
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
