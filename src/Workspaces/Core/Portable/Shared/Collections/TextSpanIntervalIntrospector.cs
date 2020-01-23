// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Collections
{
    internal class TextSpanIntervalIntrospector : IIntervalIntrospector<TextSpan>
    {
        public static readonly IIntervalIntrospector<TextSpan> Instance = new TextSpanIntervalIntrospector();

        private TextSpanIntervalIntrospector()
        {
        }

        public int GetStart(TextSpan value)
        {
            return value.Start;
        }

        public int GetLength(TextSpan value)
        {
            return value.Length;
        }
    }
}
