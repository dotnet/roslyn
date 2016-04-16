// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Debugging
{
    internal class BreakpointResolutionResult
    {
        public Document Document { get; }
        public TextSpan TextSpan { get; }
        public string LocationNameOpt { get; }
        public bool IsLineBreakpoint { get; }

        private BreakpointResolutionResult(Document document, TextSpan textSpan, string locationNameOpt, bool isLineBreakpoint)
        {
            this.Document = document;
            this.TextSpan = textSpan;
            this.LocationNameOpt = locationNameOpt;
            this.IsLineBreakpoint = isLineBreakpoint;
        }

        internal static BreakpointResolutionResult CreateSpanResult(Document document, TextSpan textSpan, string locationNameOpt = null)
        {
            return new BreakpointResolutionResult(document, textSpan, locationNameOpt, isLineBreakpoint: false);
        }

        internal static BreakpointResolutionResult CreateLineResult(Document document, string locationNameOpt = null)
        {
            return new BreakpointResolutionResult(document, new TextSpan(), locationNameOpt, isLineBreakpoint: true);
        }
    }
}
