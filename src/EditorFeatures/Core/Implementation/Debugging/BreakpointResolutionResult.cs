// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Debugging
{
    internal sealed class BreakpointResolutionResult
    {
        public Document Document { get; }
        public TextSpan TextSpan { get; }
        public string? LocationNameOpt { get; }
        public bool IsLineBreakpoint { get; }

        private BreakpointResolutionResult(Document document, TextSpan textSpan, string? locationNameOpt, bool isLineBreakpoint)
        {
            Document = document;
            TextSpan = textSpan;
            LocationNameOpt = locationNameOpt;
            IsLineBreakpoint = isLineBreakpoint;
        }

        internal static BreakpointResolutionResult CreateSpanResult(Document document, TextSpan textSpan, string? locationNameOpt = null)
            => new BreakpointResolutionResult(document, textSpan, locationNameOpt, isLineBreakpoint: false);

        internal static BreakpointResolutionResult CreateLineResult(Document document, string? locationNameOpt = null)
            => new BreakpointResolutionResult(document, new TextSpan(), locationNameOpt, isLineBreakpoint: true);
    }
}
