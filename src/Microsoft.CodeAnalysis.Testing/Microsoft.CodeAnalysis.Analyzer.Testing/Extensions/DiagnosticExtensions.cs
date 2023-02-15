// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Testing.Lightup;

namespace Microsoft.CodeAnalysis.Testing.Extensions
{
    internal static class DiagnosticExtensions
    {
        private static readonly Func<Diagnostic, bool> s_isSuppressed =
            LightupHelpers.CreatePropertyAccessor<Diagnostic, bool>(
                typeof(Diagnostic),
                nameof(IsSuppressed),
                defaultValue: false);

        public static bool IsSuppressed(this Diagnostic diagnostic)
            => s_isSuppressed(diagnostic);
    }
}
