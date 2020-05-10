// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.SourceGeneration;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        private InvocationExpressionSyntax? TryGenerateNameOfExpression(INameOfOperation? operation, SyntaxType type)
        {
            if (operation == null || operation.IsImplicit)
                return null;

            if (type == SyntaxType.Statement)
                throw new ArgumentException($"{nameof(INameOfOperation)} cannot be converted to a {nameof(StatementSyntax)}");

            var argument = TryGenerateArgument(WrapWithArgument(operation.Argument));
            if (argument == null)
                return null;

            return InvocationExpression(
                SyntaxFactory.IdentifierName(ParseToken("nameof")),
                ArgumentList(SingletonSeparatedList(argument)));
        }
    }
}
