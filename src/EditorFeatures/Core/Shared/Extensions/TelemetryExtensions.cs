﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class TelemetryExtensions
    {
        public static Guid GetTelemetryId(this Type type, short scope = 0)
        {
            type = GetTypeForTelemetry(type);
            Contract.ThrowIfNull(type.FullName);

            // AssemblyQualifiedName will change across version numbers, FullName won't

            // Use a stable hashing algorithm (FNV) that doesn't depend on platform
            // or .NET implementation.
            var suffix = Roslyn.Utilities.Hash.GetFNVHashCode(type.FullName);

            // Suffix is the remaining 8 bytes, and the hash code only makes up 4. Pad
            // the remainder with an empty byte array
            var suffixBytes = BitConverter.GetBytes(suffix).Concat(new byte[4]).ToArray();

            return new Guid(0, scope, 0, suffixBytes);
        }

        public static Type GetTypeForTelemetry(this Type type)
            => type.IsConstructedGenericType ? type.GetGenericTypeDefinition() : type;

        public static short GetScopeIdForTelemetry(this FixAllScope scope)
            => (short)(scope switch
            {
                FixAllScope.Document => 1,
                FixAllScope.Project => 2,
                FixAllScope.Solution => 3,
                _ => 4,
            });

        public static string GetTelemetryDiagnosticID(this Diagnostic diagnostic)
        {
            // we log diagnostic id as it is if it is from us
            if (diagnostic.Descriptor.CustomTags.Any(t => t == WellKnownDiagnosticTags.Telemetry))
            {
                return diagnostic.Id;
            }

            // if it is from third party, we use hashcode
            return diagnostic.GetHashCode().ToString(CultureInfo.InvariantCulture);
        }
    }
}
