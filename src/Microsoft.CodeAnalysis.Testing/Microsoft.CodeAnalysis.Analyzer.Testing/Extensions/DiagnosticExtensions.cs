// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Microsoft.CodeAnalysis.Testing.Extensions
{
    internal static class DiagnosticExtensions
    {
        private static readonly MethodInfo? IsSuppressedGetMethod = typeof(Diagnostic).GetProperty("IsSuppressed")?.GetMethod;

        public static bool TryGetIsSuppressed(this Diagnostic diagnostic, out bool value)
        {
            value = false;

            var rawValue = IsSuppressedGetMethod?.Invoke(diagnostic, null);

            if (rawValue?.GetType() != typeof(bool))
            {
                return false;
            }

            value = (bool)rawValue;

            return true;
        }
    }
}
