// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Contracts;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CallstackExplorer
{
    internal class FileLineResult : ParsedLine
    {
        public TextSpan FileSpan { get; set; }

        public FileLineResult(string originalLine, TextSpan symbolSpan, TextSpan fileSpan)
            : base(originalLine, symbolSpan)
        {
            Contract.Requires(fileSpan.Length > 0);
            FileSpan = fileSpan;
        }
    }
}
