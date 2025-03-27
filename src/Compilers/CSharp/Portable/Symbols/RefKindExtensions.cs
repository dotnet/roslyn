// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
                case RefKind.RefReadOnlyParameter:
                    return false;
                default:
                    throw ExceptionUtilities.UnexpectedValue(refKind);
            }
        }
    }
}
