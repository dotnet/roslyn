// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Implementation.Debugging;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal sealed class VSTypeScriptBreakpointResolutionResult
    {
        public Document Document { get; }
        public TextSpan TextSpan { get; }
        public string LocationNameOpt { get; }
        public bool IsLineBreakpoint { get; }

        private VSTypeScriptBreakpointResolutionResult(Document document, TextSpan textSpan, string locationNameOpt, bool isLineBreakpoint)
        {
            Document = document;
            TextSpan = textSpan;
            LocationNameOpt = locationNameOpt;
            IsLineBreakpoint = isLineBreakpoint;
        }

        public static VSTypeScriptBreakpointResolutionResult CreateSpanResult(Document document, TextSpan textSpan, string locationNameOpt = null)
            => new VSTypeScriptBreakpointResolutionResult(document, textSpan, locationNameOpt, isLineBreakpoint: false);

        public static VSTypeScriptBreakpointResolutionResult CreateLineResult(Document document, string locationNameOpt = null)
            => new VSTypeScriptBreakpointResolutionResult(document, new TextSpan(), locationNameOpt, isLineBreakpoint: true);
    }
}
