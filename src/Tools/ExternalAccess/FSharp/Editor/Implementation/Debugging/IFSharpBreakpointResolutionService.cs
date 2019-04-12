// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.Implementation.Debugging
{
    public class BreakpointResolutionResult
    {
        public Document Document { get; }
        public TextSpan TextSpan { get; }
        public string LocationNameOpt { get; }
        public bool IsLineBreakpoint { get; }

        private BreakpointResolutionResult(Document document, TextSpan textSpan, string locationNameOpt, bool isLineBreakpoint)
        {
            Document = document;
            TextSpan = textSpan;
            LocationNameOpt = locationNameOpt;
            IsLineBreakpoint = isLineBreakpoint;
        }

        public static BreakpointResolutionResult CreateSpanResult(Document document, TextSpan textSpan, string locationNameOpt = null)
        {
            return new BreakpointResolutionResult(document, textSpan, locationNameOpt, isLineBreakpoint: false);
        }

        public static BreakpointResolutionResult CreateLineResult(Document document, string locationNameOpt = null)
        {
            return new BreakpointResolutionResult(document, new TextSpan(), locationNameOpt, isLineBreakpoint: true);
        }
    }

    public interface IFSharpBreakpointResolutionService
    {
        Task<BreakpointResolutionResult> ResolveBreakpointAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken = default);

        Task<IEnumerable<BreakpointResolutionResult>> ResolveBreakpointsAsync(Solution solution, string name, CancellationToken cancellationToken = default);
    }
}
