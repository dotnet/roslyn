// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Implementation.Debugging;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal readonly struct VSTypeScriptBreakpointResolutionResult
    {
        internal readonly BreakpointResolutionResult UnderlyingObject;

        private VSTypeScriptBreakpointResolutionResult(BreakpointResolutionResult result)
            => UnderlyingObject = result;

        public Document Document => UnderlyingObject.Document;
        public TextSpan TextSpan => UnderlyingObject.TextSpan;
        public string LocationNameOpt => UnderlyingObject.LocationNameOpt;
        public bool IsLineBreakpoint => UnderlyingObject.IsLineBreakpoint;

        public static VSTypeScriptBreakpointResolutionResult CreateSpanResult(Document document, TextSpan textSpan, string locationNameOpt = null)
            => new VSTypeScriptBreakpointResolutionResult(BreakpointResolutionResult.CreateSpanResult(document, textSpan, locationNameOpt));

        public static VSTypeScriptBreakpointResolutionResult CreateLineResult(Document document, string locationNameOpt = null)
            => new VSTypeScriptBreakpointResolutionResult(BreakpointResolutionResult.CreateLineResult(document, locationNameOpt));
    }
}
