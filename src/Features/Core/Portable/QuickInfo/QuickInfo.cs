// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal sealed class QuickInfoData
    {
        public TextSpan Span { get; set; }
        public QuickInfoElement Element { get; set; }

        public QuickInfoData(TextSpan span, QuickInfoElement element)
        {
            this.Span = span;
            this.Element = element;
        }

        public static readonly QuickInfoData Empty = new QuickInfoData(default(TextSpan), QuickInfoElement.Empty);
    }
}