// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.Implementation.Debugging
{
    internal readonly struct FSharpDebugDataTipInfo
    {
        public readonly TextSpan Span;
        public readonly string Text;

        public FSharpDebugDataTipInfo(TextSpan span, string text)
        {
            Span = span;
            Text = text;
        }

        public bool IsDefault
        {
            get { return Span.Length == 0 && Span.Start == 0 && Text == null; }
        }
    }
}
