// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.Collections;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal class SuppressIntervalIntrospector :
        IIntervalIntrospector<SuppressSpacingData>,
        IIntervalIntrospector<SuppressWrappingData>
    {
        public static readonly SuppressIntervalIntrospector Instance = new SuppressIntervalIntrospector();

        private SuppressIntervalIntrospector()
        {
        }

        int IIntervalIntrospector<SuppressSpacingData>.GetStart(SuppressSpacingData value)
        {
            return value.TextSpan.Start;
        }

        int IIntervalIntrospector<SuppressSpacingData>.GetLength(SuppressSpacingData value)
        {
            return value.TextSpan.Length;
        }

        int IIntervalIntrospector<SuppressWrappingData>.GetStart(SuppressWrappingData value)
        {
            return value.TextSpan.Start;
        }

        int IIntervalIntrospector<SuppressWrappingData>.GetLength(SuppressWrappingData value)
        {
            return value.TextSpan.Length;
        }
    }
}
