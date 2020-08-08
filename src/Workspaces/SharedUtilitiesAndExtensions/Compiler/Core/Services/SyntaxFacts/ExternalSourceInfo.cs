// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal struct ExternalSourceInfo
    {
        public readonly int? StartLine;
        public readonly bool Ends;

        public ExternalSourceInfo(int? startLine, bool ends)
        {
            this.StartLine = startLine;
            this.Ends = ends;
        }
    }
}
