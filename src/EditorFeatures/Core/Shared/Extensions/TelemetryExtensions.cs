// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static class TelemetryExtensions
    {
        public static Guid GetTelemetryId(this Type type, short scope = 0)
        {
            type = GetTypeForTelemetry(type);

            // AssemblyQualifiedName will change across version numbers, FullName won't

            // GetHashCode on string is not stable. From documentation: 
            // The hash code itself is not guaranteed to be stable. 
            // Hash codes for identical strings can differ across .NET implementations, across .NET versions, 
            // and across .NET platforms (such as 32-bit and 64-bit) for a single version of .NET. In some cases, 
            // they can even differ by application domain. 
            // This implies that two subsequent runs of the same program may return different hash codes.
            //
            // As such, we keep the original prefix that was being used for legacy purposes, but 
            // use a stable hashing algorithm (FNV) that doesn't depend on platform 
            // or .NET implementation. We can map the prefix across legacy versions, but 
            // as we support more platforms and variations of builds the suffix will be constant
            // and usable
            var prefix = type.FullName.GetHashCode();
            var suffix = Roslyn.Utilities.Hash.GetFNVHashCode(type.FullName);

            // Suffix is the remaining 8 bytes, and the hash code only makes up 4. Pad 
            // the remainder with an empty byte array
            var suffixBytes = BitConverter.GetBytes(suffix).Concat(new byte[4]).ToArray();

            return new Guid(prefix, scope, 0, suffixBytes);
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
