// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Contracts;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.StackTraceExplorer
{
    internal class ParsedFrameWithFile : ParsedStackFrame
    {
        public TextSpan FileSpan { get; set; }

        public ParsedFrameWithFile(
            string originalLine,
            TextSpan classSpan,
            TextSpan methodSpan,
            TextSpan argsSpan,
            TextSpan fileSpan)
            : base(originalLine, classSpan, methodSpan, argsSpan)
        {
            Contract.Requires(fileSpan.Length > 0);
            FileSpan = fileSpan;
        }
    }
}
