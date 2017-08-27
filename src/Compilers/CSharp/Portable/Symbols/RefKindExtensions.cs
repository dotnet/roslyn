// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static partial class RefKindExtensions
    {
        public static SyntaxToken GetToken(this RefKind refKind)
        {
            if (refKind == RefKind.Out)
            {
                return SyntaxFactory.Token(SyntaxKind.OutKeyword);
            }
            if (refKind == RefKind.Ref)
            {
                return SyntaxFactory.Token(SyntaxKind.RefKeyword);
            }
            return default(SyntaxToken);
        }

        public static RefKind GetRefKind(this SyntaxKind syntaxKind)
        {
            switch (syntaxKind)
            {
                case SyntaxKind.RefKeyword:
                    return RefKind.Ref;
                case SyntaxKind.OutKeyword:
                    return RefKind.Out;
                case SyntaxKind.None:
                    return RefKind.None;
                default:
                    throw ExceptionUtilities.UnexpectedValue(syntaxKind);
            }
        }
    }
}
