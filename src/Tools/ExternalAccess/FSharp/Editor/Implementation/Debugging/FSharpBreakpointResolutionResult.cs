// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.Implementation.Debugging
{
    internal class FSharpBreakpointResolutionResult
    {
        public Document Document { get; }
        public TextSpan TextSpan { get; }
        public string LocationNameOpt { get; }
        public bool IsLineBreakpoint { get; }

        private FSharpBreakpointResolutionResult(Document document, TextSpan textSpan, string locationNameOpt, bool isLineBreakpoint)
        {
            Document = document;
            TextSpan = textSpan;
            LocationNameOpt = locationNameOpt;
            IsLineBreakpoint = isLineBreakpoint;
        }

        public static FSharpBreakpointResolutionResult CreateSpanResult(Document document, TextSpan textSpan, string locationNameOpt = null)
        {
            return new FSharpBreakpointResolutionResult(document, textSpan, locationNameOpt, isLineBreakpoint: false);
        }

        public static FSharpBreakpointResolutionResult CreateLineResult(Document document, string locationNameOpt = null)
        {
            return new FSharpBreakpointResolutionResult(document, new TextSpan(), locationNameOpt, isLineBreakpoint: true);
        }
    }
}
