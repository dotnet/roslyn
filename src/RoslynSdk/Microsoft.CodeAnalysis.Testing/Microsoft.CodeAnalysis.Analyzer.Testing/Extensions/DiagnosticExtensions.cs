// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Testing.Lightup;

namespace Microsoft.CodeAnalysis.Testing.Extensions
{
    internal static class DiagnosticExtensions
    {
        private static readonly Func<Diagnostic, IReadOnlyList<object?>> s_arguments =
            LightupHelpers.CreatePropertyAccessor<Diagnostic, IReadOnlyList<object?>>(
                typeof(Diagnostic),
                nameof(Arguments),
                defaultValue: new object[0]);

        private static readonly Func<Diagnostic, bool> s_isSuppressed =
            LightupHelpers.CreatePropertyAccessor<Diagnostic, bool>(
                typeof(Diagnostic),
                nameof(IsSuppressed),
                defaultValue: false);

        private static readonly Func<Diagnostic, object?> s_grogrammaticSuppressionInfo =
            LightupHelpers.CreatePropertyAccessor<Diagnostic, object?>(
                typeof(Diagnostic),
                nameof(ProgrammaticSuppressionInfo),
                defaultValue: null);

        public static IReadOnlyList<object?> Arguments(this Diagnostic diagnostic)
            => s_arguments(diagnostic);

        public static bool IsSuppressed(this Diagnostic diagnostic)
            => s_isSuppressed(diagnostic);

        public static ProgrammaticSuppressionInfoWrapper? ProgrammaticSuppressionInfo(this Diagnostic diagnostic)
            => s_grogrammaticSuppressionInfo(diagnostic) is { } info ? ProgrammaticSuppressionInfoWrapper.FromInstance(info) : null;
    }
}
