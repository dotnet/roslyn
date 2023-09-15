// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal static class RangeExtensions
    {
        public static int CompareTo(this Range range1, Range range2)
        {
            var result = range1.Start.CompareTo(range2.Start);

            if (result == 0)
            {
                result = range1.End.CompareTo(range2.End);
            }

            return result;
        }
    }
}
