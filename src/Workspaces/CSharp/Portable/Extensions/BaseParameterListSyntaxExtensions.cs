// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class BaseParameterListSyntaxExtensions
    {
        public static BaseParameterListSyntax WithParameters(
            this BaseParameterListSyntax parameterList,
            SeparatedSyntaxList<ParameterSyntax> parameters)
        {
            switch (parameterList.Kind())
            {
                case SyntaxKind.BracketedParameterList:
                    return ((BracketedParameterListSyntax)parameterList).WithParameters(parameters);
                case SyntaxKind.ParameterList:
                    return ((ParameterListSyntax)parameterList).WithParameters(parameters);
            }

            throw ExceptionUtilities.Unreachable;
        }
    }
}
