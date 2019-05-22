// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.Implementation.Debugging
{
    internal struct FSharpDebugLocationInfo
    {
        public readonly string Name;
        public readonly int LineOffset;

        public FSharpDebugLocationInfo(string name, int lineOffset)
        {
            Debug.Assert(name != null);
            Name = name;
            LineOffset = lineOffset;
        }

        public bool IsDefault
        {
            get { return Name == null; }
        }
    }

    internal struct FSharpDebugDataTipInfo
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

    internal interface IFSharpLanguageDebugInfoService
    {
        Task<FSharpDebugLocationInfo> GetLocationInfoAsync(Document document, int position, CancellationToken cancellationToken);

        /// <summary>
        /// Find an appropriate span to pass the debugger given a point in a snapshot.  Optionally
        /// pass back a string to pass to the debugger instead if no good span can be found.  For
        /// example, if the user hovers on "var" then we actually want to pass the fully qualified
        /// name of the type that 'var' binds to, to the debugger.
        /// </summary>
        Task<FSharpDebugDataTipInfo> GetDataTipInfoAsync(Document document, int position, CancellationToken cancellationToken);
    }
}
