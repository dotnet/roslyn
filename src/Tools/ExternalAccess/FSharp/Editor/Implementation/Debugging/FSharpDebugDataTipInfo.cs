﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.Editor.Implementation.Debugging;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.Implementation.Debugging
{
    internal readonly struct FSharpDebugDataTipInfo
    {
        internal readonly DebugDataTipInfo UnderlyingObject;

        public FSharpDebugDataTipInfo(TextSpan span, string text)
            => UnderlyingObject = new DebugDataTipInfo(span, text);

        public readonly TextSpan Span => UnderlyingObject.Span;
        public readonly string Text => UnderlyingObject.Text;
        public bool IsDefault => UnderlyingObject.IsDefault;
    }
}
