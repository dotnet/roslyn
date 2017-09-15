// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.NameArguments;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.NameArguments
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.NameArguments), Shared]
    internal sealed class CSharpNameArgumentsCodeFixProvider : AbstractNameArgumentsCodeFixProvider
    {
        internal override SyntaxNode MakeNamedArgument(string parameterName, SyntaxNode node)
        {
            SyntaxNode newArgument;
            switch (node)
            {
                case ArgumentSyntax argument:
                    newArgument = argument.WithoutTrivia()
                        .WithNameColon(SyntaxFactory.NameColon(parameterName)).WithTriviaFrom(argument);
                    break;
                case AttributeArgumentSyntax argument:
                    newArgument = argument.WithoutTrivia()
                        .WithNameColon(SyntaxFactory.NameColon(parameterName)).WithTriviaFrom(argument);
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }

            return newArgument;
        }
    }
}
