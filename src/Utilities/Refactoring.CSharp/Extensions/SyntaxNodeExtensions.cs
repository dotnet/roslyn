// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Analyzer.Utilities.Extensions
{
    internal static class SyntaxNodeExtensions
    {
        public static SyntaxList<AttributeListSyntax> GetAttributeLists(this SyntaxNode declaration)
            => declaration switch
            {
                MemberDeclarationSyntax memberDecl => memberDecl.AttributeLists,
                AccessorDeclarationSyntax accessor => accessor.AttributeLists,
                ParameterSyntax parameter => parameter.AttributeLists,
                CompilationUnitSyntax compilationUnit => compilationUnit.AttributeLists,
                _ => default,
            };
    }
}
