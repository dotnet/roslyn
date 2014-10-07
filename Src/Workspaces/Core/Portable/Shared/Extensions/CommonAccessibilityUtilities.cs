// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class CommonAccessibilityUtilities
    {
        public static Accessibility Minimum(Accessibility accessibility1, Accessibility accessibility2)
        {
            if (accessibility1 == Accessibility.Private || accessibility2 == Accessibility.Private)
            {
                return Accessibility.Private;
            }

            if (accessibility1 == Accessibility.ProtectedAndInternal || accessibility2 == Accessibility.ProtectedAndInternal)
            {
                return Accessibility.ProtectedAndInternal;
            }

            if (accessibility1 == Accessibility.Protected || accessibility2 == Accessibility.Protected)
            {
                return Accessibility.Protected;
            }

            if (accessibility1 == Accessibility.Internal || accessibility2 == Accessibility.Internal)
            {
                return Accessibility.Internal;
            }

            if (accessibility1 == Accessibility.ProtectedOrInternal || accessibility2 == Accessibility.ProtectedOrInternal)
            {
                return Accessibility.ProtectedOrInternal;
            }

            return Accessibility.Public;
        }
    }
}