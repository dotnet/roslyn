// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Debugging
{
    internal class BreakpointResolutionResult
    {
        public Document Document { get; private set; }
        public TextSpan TextSpan { get; private set; }
        public string LocationNameOpt { get; private set; }
        public bool IsLineBreakpoint { get; private set; }

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
