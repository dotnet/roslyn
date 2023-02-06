// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Testing.Extensions
{
    internal static class DiagnosticExtensions
    {
        private static readonly Func<Diagnostic, bool> s_isSuppressed;

        static DiagnosticExtensions()
        {
            var isSuppressedProperty = typeof(Diagnostic).GetProperty(nameof(IsSuppressed), typeof(bool));
            if (isSuppressedProperty is { GetMethod: { } getMethod })
            {
                s_isSuppressed = (Func<Diagnostic, bool>)getMethod.CreateDelegate(typeof(Func<Diagnostic, bool>), target: null);
            }
            else
            {
                s_isSuppressed = diagnostic => false;
            }
        }

        public static bool IsSuppressed(this Diagnostic diagnostic)
            => s_isSuppressed(diagnostic);
    }
}
