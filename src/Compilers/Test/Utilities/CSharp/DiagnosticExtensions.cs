// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class DiagnosticExtensions
    {
        public static void Verify(this IEnumerable<DiagnosticInfo> actual, params DiagnosticDescription[] expected)
        {
            actual.Select(info => new CSDiagnostic(info, NoLocation.Singleton)).Verify(expected);
        }

        public static void Verify(this ImmutableArray<DiagnosticInfo> actual, params DiagnosticDescription[] expected)
        {
            actual.Select(info => new CSDiagnostic(info, NoLocation.Singleton)).Verify(expected);
        }

        public static string ToLocalizedString(this MessageID id)
        {
            return new LocalizableErrorArgument(id).ToString(null, null);
        }
    }
}
