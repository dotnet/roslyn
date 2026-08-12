// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Testing.Lightup
{
    internal readonly struct DiagnosticSuppressorWrapper
    {
        internal const string WrappedTypeName = "Microsoft.CodeAnalysis.Diagnostics.DiagnosticSuppressor";
        internal static readonly Type? WrappedType = typeof(Diagnostic).GetTypeInfo().Assembly.GetType(WrappedTypeName);
        private static readonly Func<DiagnosticAnalyzer, IEnumerable> s_supportedSuppressions;

        private readonly DiagnosticAnalyzer _instance;

        static DiagnosticSuppressorWrapper()
        {
            s_supportedSuppressions = LightupHelpers.CreatePropertyAccessor<DiagnosticAnalyzer, IEnumerable>(WrappedType, nameof(SupportedSuppressions), Enumerable.Empty<object>());
        }

        private DiagnosticSuppressorWrapper(DiagnosticAnalyzer instance)
        {
            _instance = instance;
        }

        public ImmutableArray<SuppressionDescriptorWrapper> SupportedSuppressions
        {
            get
            {
                var suppressions = s_supportedSuppressions(_instance).Cast<object>();
                return ImmutableArray.CreateRange(suppressions.Select(SuppressionDescriptorWrapper.FromInstance));
            }
        }

        public static DiagnosticSuppressorWrapper FromInstance(DiagnosticAnalyzer instance)
        {
            if (instance == null)
            {
                return default;
            }

            if (!IsInstance(instance))
            {
                throw new InvalidCastException($"Cannot cast '{instance.GetType().FullName}' to '{WrappedTypeName}'");
            }

            return new DiagnosticSuppressorWrapper(instance);
        }

        public static bool IsInstance(object value)
        {
            if (value is null)
            {
                return false;
            }

            if (WrappedType is null)
            {
                return false;
            }

            return WrappedType.IsAssignableFrom(value.GetType());
        }
    }
}
