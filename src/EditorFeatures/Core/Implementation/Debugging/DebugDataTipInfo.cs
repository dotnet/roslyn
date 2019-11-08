// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Debugging
{
    internal struct DebugDataTipInfo
    {
        public readonly TextSpan Span;
        public readonly string Text;

        public DebugDataTipInfo(TextSpan span, string text)
        {
            this.Span = span;
            this.Text = text;
        }

        public bool IsDefault
        {
            get { return Span is { Length: 0, Start: 0 } && Text is null; }
        }
    }
}
