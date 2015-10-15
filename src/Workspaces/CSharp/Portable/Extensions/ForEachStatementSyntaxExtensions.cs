// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class ForEachStatementSyntaxExtensions
    {
        public static bool IsTypeInferred(this ForEachStatementSyntax forEachStatement, SemanticModel semanticModel)
        {
            return forEachStatement.Type.IsTypeInferred(semanticModel);
        }
    }
}
