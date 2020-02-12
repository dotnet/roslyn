﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal readonly struct VSTypeScriptBreakpointResolutionResultWrapper
    {
        internal readonly BreakpointResolutionResult UnderlyingObject;

        private VSTypeScriptBreakpointResolutionResultWrapper(BreakpointResolutionResult result)
            => UnderlyingObject = result;

        public Document Document => UnderlyingObject.Document;
        public TextSpan TextSpan => UnderlyingObject.TextSpan;
        public string? LocationNameOpt => UnderlyingObject.LocationNameOpt;
        public bool IsLineBreakpoint => UnderlyingObject.IsLineBreakpoint;

        public static VSTypeScriptBreakpointResolutionResultWrapper CreateSpanResult(Document document, TextSpan textSpan, string? locationNameOpt = null)
            => new VSTypeScriptBreakpointResolutionResultWrapper(BreakpointResolutionResult.CreateSpanResult(document, textSpan, locationNameOpt));

        public static VSTypeScriptBreakpointResolutionResultWrapper CreateLineResult(Document document, string? locationNameOpt = null)
            => new VSTypeScriptBreakpointResolutionResultWrapper(BreakpointResolutionResult.CreateLineResult(document, locationNameOpt));
    }
}
