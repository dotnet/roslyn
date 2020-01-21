// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            return new Guid(type.GetTelemetryPrefix(), scope, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        public static int GetTelemetryPrefix(this Type type)
        {
            type = GetTypeForTelemetry(type);

            // AssemblyQualifiedName will change across version numbers, FullName won't
            return type.FullName.GetHashCode();
        }

        public static Type GetTypeForTelemetry(this Type type)
        {
            return type.IsConstructedGenericType ? type.GetGenericTypeDefinition() : type;
        }

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
