// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;

namespace Analyzer.Utilities.Lightup
{
    internal static class SyntaxKindEx
    {
        // https://github.com/dotnet/roslyn/blob/main/src/Compilers/CSharp/Portable/Syntax/SyntaxKind.cs
        public const SyntaxKind Utf8StringLiteralToken = (SyntaxKind)8520;
        public const SyntaxKind Utf8StringLiteralExpression = (SyntaxKind)8756;
        public const SyntaxKind CollectionExpression = (SyntaxKind)9076;
    }
}
