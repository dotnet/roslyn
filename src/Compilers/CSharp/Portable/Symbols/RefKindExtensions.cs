// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static partial class RefKindExtensions
    {
        public static bool IsManagedReference(this RefKind refKind)
        {
            Debug.Assert(refKind <= RefKind.RefReadOnly);

            return refKind != RefKind.None;
        }

        public static RefKind GetRefKind(this SyntaxKind syntaxKind)
        {
            switch (syntaxKind)
            {
                case SyntaxKind.RefKeyword:
                    return RefKind.Ref;
                case SyntaxKind.OutKeyword:
                    return RefKind.Out;
                case SyntaxKind.InKeyword:
                    return RefKind.In;
                case SyntaxKind.None:
                    return RefKind.None;
                default:
                    throw ExceptionUtilities.UnexpectedValue(syntaxKind);
            }
        }

        public static bool IsWritableReference(this RefKind refKind)
        {
            switch (refKind)
            {
                case RefKind.Ref:
                case RefKind.Out:
                    return true;
                case RefKind.None:
                case RefKind.In:
                    return false;
                default:
                    throw ExceptionUtilities.UnexpectedValue(refKind);
            }
        }
    }
}
