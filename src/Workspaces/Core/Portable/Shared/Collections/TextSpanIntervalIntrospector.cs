// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
