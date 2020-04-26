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
            => new Guid(type.GetTelemetryPrefix(), scope, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        public static int GetTelemetryPrefix(this Type type)
        {
            type = GetTypeForTelemetry(type);

            // AssemblyQualifiedName will change across version numbers, FullName won't
            return type.FullName.GetHashCode();
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
