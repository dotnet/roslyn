// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;

namespace Analyzer.CSharp.Utilities.Lightup
{
    internal static class SyntaxKindEx
    {
        public const SyntaxKind InitKeyword = (SyntaxKind)8443;
        public const SyntaxKind InitAccessorDeclaration = (SyntaxKind)9060;
        public const SyntaxKind ImplicitObjectCreationExpression = (SyntaxKind)8659;
    }
}
